using AngleSharp.Dom;
using System.Diagnostics;
using UnifiStoreWatcher.Web.Data.Entities;
using UnifiStoreWatcher.Web.Telemetry;

namespace UnifiStoreWatcher.Web.Services.Parsing;

public sealed class TextContentParser : IStockParser
{
    private static readonly string[] InStockPhrases =
    [
        "in stock", "available", "ships in", "ready to ship", "add to cart",
    ];

    private static readonly string[] OutOfStockPhrases =
    [
        "out of stock", "sold out", "unavailable", "currently unavailable",
        "not available", "back order", "backorder", "notify me when available",
        "temporarily out",
    ];

    public string Name => "TextContent";

    public Task<StockParseResult> ParseAsync(IDocument document, CancellationToken ct = default)
    {
        using var activity = UnifiStoreWatcherActivities.Source.StartActivity("parse.text", ActivityKind.Internal);

        var bodyText = document.Body?.TextContent?.ToLowerInvariant() ?? string.Empty;

        foreach (var phrase in OutOfStockPhrases)
        {
            if (bodyText.Contains(phrase))
            {
                activity?.SetTag("parser.matched", true);
                activity?.SetTag("parser.matched_phrase", phrase);
                activity?.SetTag("parser.confidence", 0.70);
                return Task.FromResult(new StockParseResult(StockState.OutOfStock, 0.70, Name, $"'{phrase}' found in body text"));
            }
        }

        foreach (var phrase in InStockPhrases)
        {
            if (bodyText.Contains(phrase))
            {
                activity?.SetTag("parser.matched", true);
                activity?.SetTag("parser.matched_phrase", phrase);
                activity?.SetTag("parser.confidence", 0.65);
                return Task.FromResult(new StockParseResult(StockState.InStock, 0.65, Name, $"'{phrase}' found in body text"));
            }
        }

        activity?.SetTag("parser.matched", false);
        activity?.SetTag("parser.confidence", 0.0);
        return Task.FromResult(StockParseResults.Indeterminate(Name));
    }
}
