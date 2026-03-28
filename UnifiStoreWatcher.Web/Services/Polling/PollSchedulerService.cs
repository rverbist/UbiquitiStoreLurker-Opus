using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UnifiStoreWatcher.Web.Data;
using UnifiStoreWatcher.Web.Services.Health;
using UnifiStoreWatcher.Web.Telemetry;

namespace UnifiStoreWatcher.Web.Services.Polling;

public sealed partial class PollSchedulerService(
    IServiceScopeFactory scopeFactory,
    ChannelWriter<PollWorkItem> queue,
    IOptions<PollOptions> options,
    IReadinessIndicator readinessIndicator,
    ILogger<PollSchedulerService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarted(logger);

        // On first boot, stagger products that have never been polled so they don't all
        // fire simultaneously and trigger a rate-limit response from the store.
        await ApplyStartupStaggerAsync(stoppingToken);

        // Mark the app as ready after the first scan so /api/health/ready returns 200.
        bool firstScan = true;
        while (!stoppingToken.IsCancellationRequested)
        {
            await EnqueueDueProductsAsync(stoppingToken);

            if (firstScan)
            {
                readinessIndicator.MarkReady();
                firstScan = false;
            }

            await Task.Delay(
                TimeSpan.FromSeconds(options.Value.SchedulerScanIntervalSeconds),
                stoppingToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Any product whose NextPollDueAtUtc is far in the past (default DateTimeOffset.UtcNow
    /// set at entity creation) or equal to DateTimeOffset.MinValue is considered
    /// "never polled". Assigns staggered future times so that products are polled
    /// sequentially rather than in a burst. The rate-limiter in the HTTP handler chain
    /// provides the primary protection, but this prevents filling the queue all at once.
    /// </summary>
    private async Task ApplyStartupStaggerAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UnifiStoreWatcherDbContext>();

        // Products that have never been polled: PollCount == 0.
        var neverPolled = await db.Products
            .Where(p => p.IsActive && p.PollCount == 0)
            .OrderBy(p => p.Id)
            .ToListAsync(ct);

        if (neverPolled.Count <= 1)
            return; // 0 or 1 product — no burst possible

        var gapMs = options.Value.MinRequestGapMs;
        var now = DateTimeOffset.UtcNow;

        for (int i = 1; i < neverPolled.Count; i++)  // index 0 stays at "now"
        {
            neverPolled[i].NextPollDueAtUtc = now.AddMilliseconds((long)i * gapMs);
        }

        await db.SaveChangesAsync(ct);
        LogStartupStagger(logger, neverPolled.Count, gapMs);
    }

    private async Task EnqueueDueProductsAsync(CancellationToken ct)
    {
        using var activity = UnifiStoreWatcherActivities.Source.StartActivity("scheduler.scan", ActivityKind.Internal);

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UnifiStoreWatcherDbContext>();

        var now = DateTimeOffset.UtcNow;
        var dueProducts = await db.Products
            .Where(p => p.IsActive && p.NextPollDueAtUtc <= now)
            .Select(p => new PollWorkItem(p.Id, p.Url))
            .ToListAsync(ct);

        activity?.SetTag("scheduler.products_due", dueProducts.Count);

        int enqueued = 0;
        foreach (var item in dueProducts)
        {
            if (await queue.WaitToWriteAsync(ct))
            {
                await queue.WriteAsync(item, ct);
                LogEnqueued(logger, item.ProductId, item.Url);
                enqueued++;
            }
        }

        activity?.SetTag("scheduler.products_enqueued", enqueued);

        if (dueProducts.Count > 0)
            LogEnqueuedBatch(logger, dueProducts.Count);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "PollSchedulerService started")]
    private static partial void LogStarted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Startup stagger applied: {Count} never-polled products scheduled with {GapMs} ms gaps")]
    private static partial void LogStartupStagger(ILogger logger, int count, int gapMs);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Enqueued poll for ProductId={ProductId} Url={Url}")]
    private static partial void LogEnqueued(ILogger logger, int productId, string url);

    [LoggerMessage(Level = LogLevel.Information, Message = "Enqueued {Count} due poll(s)")]
    private static partial void LogEnqueuedBatch(ILogger logger, int count);
}

