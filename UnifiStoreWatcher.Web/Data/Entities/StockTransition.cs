namespace UniFiStoreWatcher.Web.Data.Entities;

public class StockTransition
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public StockState FromState { get; set; }
    public StockState ToState { get; set; }

    public DateTimeOffset DetectedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public long StockCheckId { get; set; }
    public StockCheck StockCheck { get; set; } = null!;
}
