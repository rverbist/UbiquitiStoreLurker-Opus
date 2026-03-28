using Microsoft.AspNetCore.SignalR;

namespace UniFiStoreWatcher.Web.Hubs;

/// <summary>
/// Real-time push channel for stock monitor events.
/// Clients subscribe by connecting to /UniFiStoreWatcher-hub.
/// </summary>
public sealed class UniFiStoreWatcherHub : Hub
{
    // Methods callable by clients
    public async Task JoinGroup(string group) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, group);

    public async Task LeaveGroup(string group) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
}
