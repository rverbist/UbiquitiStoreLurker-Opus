using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using UniFiStoreWatcher.Web.Telemetry;

namespace UniFiStoreWatcher.Web.Hubs;

/// <summary>
/// Thin wrapper that PollWorkerService calls to push events to SignalR clients.
/// Injected as a singleton so BackgroundService can hold a stable reference.
/// </summary>
public sealed class StockHubBroadcaster(IHubContext<UniFiStoreWatcherHub> hub)
{
    private readonly IHubContext<UniFiStoreWatcherHub> _hub = hub;

    public async Task BroadcastStockStatusChangedAsync(StockStatusChanged evt, CancellationToken ct = default)
    {
        using var activity = UniFiStoreWatcherActivities.Source.StartActivity("signalr.broadcast", ActivityKind.Internal);
        activity?.SetTag("hub.event", "StockStatusChanged");
        activity?.SetTag("product.id", evt.ProductId);
        await _hub.Clients.All.SendAsync("StockStatusChanged", evt, ct);
    }

    public Task BroadcastPollCycleCompletedAsync(PollCycleCompleted evt, CancellationToken ct = default) =>
        _hub.Clients.All.SendAsync("PollCycleCompleted", evt, ct);

    public Task BroadcastPollErrorOccurredAsync(PollErrorOccurred evt, CancellationToken ct = default) =>
        _hub.Clients.All.SendAsync("PollErrorOccurred", evt, ct);

    public Task BroadcastPollStartedAsync(PollStarted evt, CancellationToken ct = default) =>
        _hub.Clients.All.SendAsync("PollStarted", evt, ct);
}
