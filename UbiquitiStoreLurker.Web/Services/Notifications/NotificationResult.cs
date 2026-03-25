namespace UbiquitiStoreLurker.Web.Services.Notifications;

public sealed record NotificationResult(bool Success, string? ErrorMessage = null)
{
    public static NotificationResult Ok() => new(true);
    public static NotificationResult Fail(string error) => new(false, error);
}
