using System.ComponentModel.DataAnnotations;

namespace UniFiStoreWatcher.Web.Data.Entities;

public class StockCheck
{
    public long Id { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    [MaxLength(16)]
    public string HttpMethod { get; set; } = "GET";

    [MaxLength(2048)]
    public string RequestUrl { get; set; } = string.Empty;

    public int? HttpStatusCode { get; set; }

    public StockState DetectedState { get; set; } = StockState.Unknown;

    [MaxLength(128)]
    public string? ParserStrategy { get; set; }

    public double? ParserConfidence { get; set; }

    [MaxLength(1024)]
    public string? ParserEvidence { get; set; }

    public int DurationMs { get; set; }

    public string? ErrorMessage { get; set; }

    public bool IsRetry { get; set; }
    public int RetryAttempt { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
