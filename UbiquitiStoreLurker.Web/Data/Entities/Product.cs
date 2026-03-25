using System.ComponentModel.DataAnnotations;

namespace UbiquitiStoreLurker.Web.Data.Entities;

public class Product
{
    public int Id { get; set; }

    [Required, MaxLength(2048)]
    public string Url { get; set; } = string.Empty;

    [MaxLength(128)]
    public string? ProductCode { get; set; }

    [MaxLength(512)]
    public string? Name { get; set; }

    [MaxLength(2048)]
    public string? Description { get; set; }

    [MaxLength(2048)]
    public string? ImageUrl { get; set; }

    // JSON array of image URLs
    public string? ImageUrls { get; set; }

    // Relative web path to the locally cached primary image (e.g. /images/products/1.jpg)
    [MaxLength(512)]
    public string? LocalImagePath { get; set; }

    public StockState CurrentState { get; set; } = StockState.Unknown;
    public StockState PreviousState { get; set; } = StockState.Unknown;

    public bool IsActive { get; set; } = true;

    public SubscriptionType SubscribedEvents { get; set; } = SubscriptionType.InStock;

    public DateTimeOffset NextPollDueAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastPollAtUtc { get; set; }
    public DateTimeOffset? LastStateChangeAtUtc { get; set; }

    public int PollCount { get; set; }
    public int ErrorCount { get; set; }
    public int ConsecutiveErrors { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<StockCheck> StockChecks { get; set; } = [];
    public ICollection<StockTransition> StockTransitions { get; set; } = [];
}
