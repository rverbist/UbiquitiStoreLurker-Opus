using AngleSharp.Dom;
using System.Diagnostics;
using UniFiStoreWatcher.Web.Data.Entities;
using UniFiStoreWatcher.Web.Telemetry;

namespace UniFiStoreWatcher.Web.Services.Parsing;

public sealed class ButtonStateParser : IStockParser
{
    private static readonly string[] AddToCartPhrases =
    [
        "add to cart", "add to bag", "buy now", "order now", "purchase",
    ];

    private static readonly string[] OutOfStockPhrases =
    [
        "sold out", "out of stock", "unavailable", "currently unavailable",
        "notify me", "join waitlist", "coming soon",
    ];

    public string Name => "ButtonState";

    public Task<StockParseResult> ParseAsync(IDocument document, CancellationToken ct = default)
    {
        using var activity = UniFiStoreWatcherActivities.Source.StartActivity("parse.button", ActivityKind.Internal);

        var buttons = document.QuerySelectorAll("button, [role='button'], input[type='submit']");

        foreach (var button in buttons)
        {
            var text = button.TextContent.Trim().ToLowerInvariant();
            var isDisabled = button.HasAttribute("disabled") ||
                             button.ClassList.Contains("disabled") ||
                             button.GetAttribute("aria-disabled") == "true";

            if (AddToCartPhrases.Any(p => text.Contains(p)) && !isDisabled)
            {
                activity?.SetTag("parser.matched", true);
                activity?.SetTag("parser.confidence", 0.85);
                return Task.FromResult(new StockParseResult(StockState.InStock, 0.85, Name, $"Button: '{button.TextContent.Trim()}'"));
            }

            if (OutOfStockPhrases.Any(p => text.Contains(p)))
            {
                activity?.SetTag("parser.matched", true);
                activity?.SetTag("parser.confidence", 0.85);
                return Task.FromResult(new StockParseResult(StockState.OutOfStock, 0.85, Name, $"Button: '{button.TextContent.Trim()}'"));
            }

            if (AddToCartPhrases.Any(p => text.Contains(p)) && isDisabled)
            {
                activity?.SetTag("parser.matched", true);
                activity?.SetTag("parser.confidence", 0.80);
                return Task.FromResult(new StockParseResult(StockState.OutOfStock, 0.80, Name, $"Disabled add-to-cart: '{button.TextContent.Trim()}'"));
            }
        }

        activity?.SetTag("parser.matched", false);
        activity?.SetTag("parser.confidence", 0.0);
        return Task.FromResult(StockParseResults.Indeterminate(Name));
    }
}
