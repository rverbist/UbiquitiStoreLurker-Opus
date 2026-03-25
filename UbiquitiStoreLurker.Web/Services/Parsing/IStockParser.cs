using AngleSharp.Dom;

namespace UbiquitiStoreLurker.Web.Services.Parsing;

public interface IStockParser
{
    string Name { get; }
    Task<StockParseResult> ParseAsync(IDocument document, CancellationToken ct = default);
}
