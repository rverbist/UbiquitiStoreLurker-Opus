using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UnifiStoreWatcher.Web.Data;
using UnifiStoreWatcher.Web.Data.Entities;
using UnifiStoreWatcher.Web.Hubs;
using UnifiStoreWatcher.Web.Metrics;
using UnifiStoreWatcher.Web.Services;
using UnifiStoreWatcher.Web.Services.Parsing;
using UnifiStoreWatcher.Web.Services.Notifications;
using UnifiStoreWatcher.Web.Services.StateMachine;
using UnifiStoreWatcher.Web.Telemetry;

namespace UnifiStoreWatcher.Web.Services.Polling;

public sealed partial class PollWorkerService(
    IServiceScopeFactory scopeFactory,
    ChannelReader<PollWorkItem> queue,
    IHttpClientFactory httpClientFactory,
    IOptions<PollOptions> options,
    ILogger<PollWorkerService> logger,
    StockHubBroadcaster broadcaster)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarted(logger);

        await foreach (var item in queue.ReadAllAsync(stoppingToken))
        {
            await ProcessItemAsync(item, stoppingToken);
        }
    }

    private async Task ProcessItemAsync(PollWorkItem item, CancellationToken ct)
    {
        using var activity = UnifiStoreWatcherActivities.Source.StartActivity("poll.execute", ActivityKind.Internal);
        activity?.SetTag("product.id", item.ProductId);
        activity?.SetTag("product.url", item.Url);

        var sw = Stopwatch.StartNew();
        int? statusCode = null;

        try
        {
            await broadcaster.BroadcastPollStartedAsync(
                new PollStarted(item.ProductId, item.Url, DateTimeOffset.UtcNow), ct);

            var client = httpClientFactory.CreateClient("UniFiStoreWatchPoller");
            var response = await client.GetAsync(item.Url, ct);
            statusCode = (int)response.StatusCode;
            sw.Stop();

            LogPolled(logger, item.ProductId, item.Url, statusCode.Value, sw.ElapsedMilliseconds);

            var html = response.IsSuccessStatusCode
                ? await response.Content.ReadAsStringAsync(ct)
                : null;

            UnifiStoreWatcherMetrics.StockChecksTotal.WithLabels("success").Inc();
            UnifiStoreWatcherMetrics.PollDurationSeconds.Observe(sw.Elapsed.TotalSeconds);

            await PersistCheckAndEvaluateAsync(item, statusCode, (int)sw.ElapsedMilliseconds, null, html, ct);

            activity?.SetTag("poll.result", "success");
            await broadcaster.BroadcastPollCycleCompletedAsync(
                new PollCycleCompleted(
                    item.ProductId,
                    item.Url,
                    Success: true,
                    HttpStatusCode: statusCode.Value,
                    DurationMs: (int)sw.ElapsedMilliseconds,
                    CompletedAtUtc: DateTimeOffset.UtcNow), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            sw.Stop();

            LogPollFailed(logger, ex, item.ProductId, item.Url, sw.ElapsedMilliseconds);

            UnifiStoreWatcherMetrics.StockChecksTotal.WithLabels("error").Inc();

            await PersistCheckAsync(item, statusCode, (int)sw.ElapsedMilliseconds, ex.Message, ct);
            await IncrementErrorCountAsync(item.ProductId, ct);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
            {
                ["exception.type"] = ex.GetType().FullName,
                ["exception.message"] = ex.Message,
            }));
            activity?.SetTag("poll.result", ex.GetType().Name);
            await broadcaster.BroadcastPollErrorOccurredAsync(
                new PollErrorOccurred(
                    item.ProductId,
                    item.Url,
                    ErrorMessage: ex.Message,
                    ConsecutiveErrors: 0, // updated value available after IncrementErrorCountAsync
                    OccurredAtUtc: DateTimeOffset.UtcNow), ct);
        }
    }

    private async Task PersistCheckAndEvaluateAsync(
        PollWorkItem item,
        int? statusCode,
        int durationMs,
        string? errorMessage,
        string? html,
        CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UnifiStoreWatcherDbContext>();
        var parser = scope.ServiceProvider.GetRequiredService<CompositeStockParser>();
        var stateMachine = scope.ServiceProvider.GetRequiredService<StockStateMachine>();

        StockParseResult? parseResult = null;
        if (html is not null)
            parseResult = await parser.ParseAsync(html, ct);

        var check = new StockCheck
        {
            ProductId = item.ProductId,
            RequestUrl = item.Url,
            HttpStatusCode = statusCode,
            DurationMs = durationMs,
            ErrorMessage = errorMessage,
            DetectedState = parseResult?.State ?? StockState.Unknown,
            ParserStrategy = parseResult?.Strategy,
            ParserConfidence = parseResult?.Confidence,
            ParserEvidence = parseResult?.Evidence,
        };
        db.StockChecks.Add(check);
        await db.SaveChangesAsync(ct);

        var product = await db.Products.FindAsync([item.ProductId], ct);
        if (product is not null)
        {
            // Enrich metadata on every successful fetch – update fields that changed or are missing
            if (html is not null)
            {
                var extractor = scope.ServiceProvider.GetRequiredService<ProductInfoExtractor>();
                var info = await extractor.ExtractAsync(html, ct);

                if (info.Name is not null && info.Name != product.Name)
                    product.Name = info.Name;
                if (info.ProductCode is not null && info.ProductCode != product.ProductCode)
                    product.ProductCode = info.ProductCode;
                if (info.Description is not null && info.Description != product.Description)
                    product.Description = info.Description;
                if (info.ImageUrl is not null && info.ImageUrl != product.ImageUrl)
                {
                    product.ImageUrl = info.ImageUrl;
                    // Invalidate the cached copy so it gets re-downloaded with the new URL
                    product.LocalImagePath = null;
                }
                if (info.ImageUrls is { Length: > 0 })
                {
                    var serialized = JsonSerializer.Serialize(info.ImageUrls);
                    if (serialized != product.ImageUrls)
                        product.ImageUrls = serialized;
                }
            }

            // Download and cache the primary image if not yet stored locally
            if (product.ImageUrl is not null && product.LocalImagePath is null)
            {
                var imageService = scope.ServiceProvider.GetRequiredService<ProductImageService>();
                product.LocalImagePath = await imageService.DownloadAndCacheAsync(product.Id, product.ImageUrl, ct);
            }

            TransitionResult? transitionResult = null;
            if (parseResult is not null)
            {
                transitionResult = stateMachine.Evaluate(product, parseResult, check);
                if (transitionResult.StateChanged && transitionResult.Transition is not null)
                {
                    product.PreviousState = product.CurrentState;
                    product.CurrentState = transitionResult.Transition.ToState;
                    product.LastStateChangeAtUtc = DateTimeOffset.UtcNow;
                    db.StockTransitions.Add(transitionResult.Transition);
                    UnifiStoreWatcherMetrics.StockTransitionsTotal
                        .WithLabels(transitionResult.Transition.FromState.ToString(), transitionResult.Transition.ToState.ToString())
                        .Inc();
                }
            }

            var jitterSeconds = Random.Shared.Next(options.Value.MinIntervalSeconds, options.Value.MaxIntervalSeconds + 1);
            product.LastPollAtUtc = DateTimeOffset.UtcNow;
            product.NextPollDueAtUtc = DateTimeOffset.UtcNow.AddSeconds(jitterSeconds);
            product.PollCount++;
            product.ConsecutiveErrors = 0;
            product.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync(ct);

            // Dispatch notifications after the transition is persisted so StockTransitionId FK is valid
            if (transitionResult is { StateChanged: true, Transition: not null })
            {
                await broadcaster.BroadcastStockStatusChangedAsync(
                    new StockStatusChanged(
                        product.Id,
                        item.Url,
                        ProductName: product.Name,
                        FromState: transitionResult.Transition.FromState,
                        ToState: transitionResult.Transition.ToState,
                        DetectedAtUtc: product.LastStateChangeAtUtc ?? DateTimeOffset.UtcNow), ct);
            }

            if (transitionResult is { ShouldNotify: true, Transition: not null })
            {
                await using var notifyScope = scopeFactory.CreateAsyncScope();
                var dispatcher = notifyScope.ServiceProvider.GetRequiredService<NotificationDispatcher>();
                await dispatcher.DispatchAsync(product, transitionResult.Transition, ct);
            }
        }
    }

    private async Task PersistCheckAsync(
        PollWorkItem item,
        int? statusCode,
        int durationMs,
        string? errorMessage,
        CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UnifiStoreWatcherDbContext>();

        db.StockChecks.Add(new StockCheck
        {
            ProductId = item.ProductId,
            RequestUrl = item.Url,
            HttpStatusCode = statusCode,
            DurationMs = durationMs,
            ErrorMessage = errorMessage,
            DetectedState = StockState.Unknown,
        });

        await db.SaveChangesAsync(ct);
    }

    private async Task IncrementErrorCountAsync(int productId, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UnifiStoreWatcherDbContext>();

        var product = await db.Products.FindAsync([productId], ct);
        if (product is null) return;

        product.ErrorCount++;
        product.ConsecutiveErrors++;

        // Exponential backoff: cap at 10 minutes
        var backoffSeconds = Math.Min(
            (int)Math.Pow(2, product.ConsecutiveErrors) * 30,
            600);
        product.NextPollDueAtUtc = DateTimeOffset.UtcNow.AddSeconds(backoffSeconds);
        product.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "PollWorkerService started")]
    private static partial void LogStarted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Polled ProductId={ProductId} Url={Url} StatusCode={StatusCode} DurationMs={DurationMs}")]
    private static partial void LogPolled(ILogger logger, int productId, string url, int statusCode, long durationMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Poll failed ProductId={ProductId} Url={Url} DurationMs={DurationMs}")]
    private static partial void LogPollFailed(ILogger logger, Exception ex, int productId, string url, long durationMs);
}
