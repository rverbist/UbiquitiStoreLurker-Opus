using Microsoft.AspNetCore.SignalR;

namespace UbiquitiStoreLurker.Web.Hubs;

/// <summary>
/// Real-time push channel for stock monitor events.
/// Clients subscribe by connecting to /ubiquitistorelurker-hub.
/// </summary>
public sealed class UbiquitiStoreLurkerHub : Hub
{
    // Methods callable by clients
    public async Task JoinGroup(string group) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, group);

    public async Task LeaveGroup(string group) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
}
