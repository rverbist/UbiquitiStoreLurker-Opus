namespace UniFiStoreWatcher.Web.Data.Entities;

public class AppSettings
{
    // Singleton: always Id = 1
    public int Id { get; set; } = 1;

    public string Nickname { get; set; } = "Stock Monitor";

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public int PollIntervalMinSeconds { get; set; } = 30;
    public int PollIntervalMaxSeconds { get; set; } = 90;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryBaseDelaySeconds { get; set; } = 2;
    public int MinDelayBetweenRequestsSeconds { get; set; } = 5;

    // VAPID keys (auto-generated on first setup)
    public string? VapidPublicKey { get; set; }
    public string? VapidPrivateKey { get; set; }
}
