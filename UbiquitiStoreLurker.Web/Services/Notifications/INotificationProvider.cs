namespace UbiquitiStoreLurker.Web.Services.Notifications;

public interface INotificationProvider
{
    string ProviderType { get; }
    bool ValidateConfig(string? settingsJson, out string? configError);
    Task<NotificationResult> SendAsync(NotificationContext context, CancellationToken ct);
}
