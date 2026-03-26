namespace UniFiStoreWatcher.Web.Data.Entities;

public sealed class PushSubscription
{
    public int Id { get; set; }
    public required string Endpoint { get; set; }
    public required string P256dh { get; set; }
    public required string Auth { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
