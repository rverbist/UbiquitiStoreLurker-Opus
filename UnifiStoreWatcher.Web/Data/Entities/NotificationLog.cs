using System.ComponentModel.DataAnnotations;

namespace UnifiStoreWatcher.Web.Data.Entities;

public class NotificationLog
{
    public long Id { get; set; }

    public int NotificationConfigId { get; set; }
    public NotificationConfig NotificationConfig { get; set; } = null!;

    public int StockTransitionId { get; set; }
    public StockTransition StockTransition { get; set; } = null!;

    [Required, MaxLength(64)]
    public string ProviderType { get; set; } = string.Empty;

    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTimeOffset SentAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
