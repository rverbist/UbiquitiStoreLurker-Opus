using UbiquitiStoreLurker.Web.Data.Entities;

namespace UbiquitiStoreLurker.Web.Services.Parsing;

public sealed record StockParseResult(
    StockState State,
    double Confidence,
    string Strategy,
    string? Evidence);

public static class StockParseResults
{
    public static StockParseResult Indeterminate(string strategy) =>
        new(StockState.Indeterminate, 0.0, strategy, null);
}
