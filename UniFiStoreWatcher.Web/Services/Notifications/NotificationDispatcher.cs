using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using UniFiStoreWatcher.Web.Data;
using UniFiStoreWatcher.Web.Data.Entities;
using UniFiStoreWatcher.Web.Metrics;
using UniFiStoreWatcher.Web.Telemetry;

namespace UniFiStoreWatcher.Web.Services.Notifications;

public sealed partial class NotificationDispatcher(
    IEnumerable<INotificationProvider> providers,
    IServiceScopeFactory scopeFactory,
    ILogger<NotificationDispatcher> logger)
{
    public async Task DispatchAsync(Product product, StockTransition transition, CancellationToken ct)
    {
        using var activity = UniFiStoreWatcherActivities.Source.StartActivity("notification.dispatch", ActivityKind.Internal);

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UniFiStoreWatcherDbContext>();

        var enabledConfigs = await db.NotificationConfigs
            .Where(c => c.IsEnabled)
            .ToListAsync(ct);

        if (enabledConfigs.Count == 0)
        {
            LogNoProviders(logger);
            return;
        }

        activity?.SetTag("notification.providers", string.Join(",", enabledConfigs.Select(c => c.ProviderType)));

        var providerMap = providers.ToDictionary(p => p.ProviderType, StringComparer.OrdinalIgnoreCase);

        // Send to all providers concurrently, collect results
        var tasks = enabledConfigs.Select(config => SendAsync(config, product, transition, providerMap, ct));
        var results = await Task.WhenAll(tasks);

        var successCount = results.Count(r => r.Result.Success);
        activity?.SetTag("notification.success_count", successCount);
        activity?.SetTag("notification.failure_count", results.Length - successCount);

        // Add logs sequentially — DbContext is not thread-safe
        foreach (var (config, result) in results)
        {
            db.NotificationLogs.Add(new NotificationLog
            {
                NotificationConfigId = config.Id,
                StockTransitionId = transition.Id,
                ProviderType = config.ProviderType,
                Success = result.Success,
                ErrorMessage = result.ErrorMessage,
            });
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task<(NotificationConfig Config, NotificationResult Result)> SendAsync(
        NotificationConfig config,
        Product product,
        StockTransition transition,
        Dictionary<string, INotificationProvider> providerMap,
        CancellationToken ct)
    {
        if (!providerMap.TryGetValue(config.ProviderType, out var provider))
        {
            LogNoProviderImpl(logger, config.ProviderType);
            return (config, NotificationResult.Fail($"No provider found for {config.ProviderType}"));
        }

        var context = new NotificationContext(product, transition, config);

        var sw = Stopwatch.StartNew();
        try
        {
            var result = await provider.SendAsync(context, ct);
            sw.Stop();
            UniFiStoreWatcherMetrics.NotificationLatencySeconds.Observe(sw.Elapsed.TotalSeconds);
            UniFiStoreWatcherMetrics.NotificationsSentTotal
                .WithLabels(config.ProviderType, result.Success ? "true" : "false")
                .Inc();
            return (config, result);
        }
        catch (Exception ex)
        {
            sw.Stop();
            UniFiStoreWatcherMetrics.NotificationLatencySeconds.Observe(sw.Elapsed.TotalSeconds);
            UniFiStoreWatcherMetrics.NotificationsSentTotal
                .WithLabels(config.ProviderType, "false")
                .Inc();
            LogProviderException(logger, ex, config.ProviderType);
            return (config, NotificationResult.Fail(ex.Message));
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "No enabled notification providers \u2014 skipping dispatch")]
    private static partial void LogNoProviders(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No provider implementation found for type {ProviderType}")]
    private static partial void LogNoProviderImpl(ILogger logger, string providerType);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Unhandled exception in {ProviderType} provider")]
    private static partial void LogProviderException(ILogger logger, Exception ex, string providerType);
}

