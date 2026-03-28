using System.ComponentModel.DataAnnotations;

namespace UniFiStoreWatcher.Web.Data.Entities;

public class NotificationConfig
{
    public int Id { get; set; }

    [Required, MaxLength(64)]
    public string ProviderType { get; set; } = string.Empty;

    [Required, MaxLength(128)]
    public string DisplayName { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    // JSON blob of provider-specific settings (webhook URL, SMTP, etc.)
    public string? SettingsJson { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<NotificationLog> NotificationLogs { get; set; } = [];
}
