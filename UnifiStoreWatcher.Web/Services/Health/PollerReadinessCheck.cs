using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace UnifiStoreWatcher.Web.Services.Health;

/// <summary>
/// Returns Healthy once <see cref="IReadinessIndicator.MarkReady"/> has been called by
/// <see cref="Polling.PollSchedulerService"/> after its first scheduler scan loop completes.
/// </summary>
public sealed class PollerReadinessCheck(IReadinessIndicator indicator) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var result = indicator.IsReady
            ? HealthCheckResult.Healthy("Poller initialised")
            : HealthCheckResult.Unhealthy("Poller has not completed its first scan loop");

        return Task.FromResult(result);
    }
}
