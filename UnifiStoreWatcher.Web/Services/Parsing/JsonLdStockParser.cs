using System.Diagnostics;
using System.Text.Json;
using AngleSharp.Dom;
using UnifiStoreWatcher.Web.Data.Entities;
using UnifiStoreWatcher.Web.Telemetry;

namespace UnifiStoreWatcher.Web.Services.Parsing;

public sealed partial class JsonLdStockParser(ILogger<JsonLdStockParser> logger) : IStockParser
{
    private static readonly string[] InStockValues =
    [
        "https://schema.org/InStock",
        "http://schema.org/InStock",
        "InStock",
    ];

    private static readonly string[] OutOfStockValues =
    [
        "https://schema.org/OutOfStock",
        "http://schema.org/OutOfStock",
        "OutOfStock",
        "https://schema.org/SoldOut",
        "http://schema.org/SoldOut",
        "SoldOut",
    ];

    public string Name => "JsonLd";

    public Task<StockParseResult> ParseAsync(IDocument document, CancellationToken ct = default)
    {
        using var activity = UnifiStoreWatcherActivities.Source.StartActivity("parse.jsonld", ActivityKind.Internal);

        var scripts = document.QuerySelectorAll("script[type='application/ld+json']");

        foreach (var script in scripts)
        {
            var json = script.TextContent.Trim();
            if (string.IsNullOrWhiteSpace(json)) continue;

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!IsProductType(root)) continue;

                var availability = ExtractAvailability(root);
                if (availability is null) continue;

                if (InStockValues.Any(v => string.Equals(v, availability, StringComparison.OrdinalIgnoreCase)))
                {
                    activity?.SetTag("parser.matched", true);
                    activity?.SetTag("parser.confidence", 0.95);
                    return Task.FromResult(new StockParseResult(StockState.InStock, 0.95, Name, availability));
                }

                if (OutOfStockValues.Any(v => string.Equals(v, availability, StringComparison.OrdinalIgnoreCase)))
                {
                    activity?.SetTag("parser.matched", true);
                    activity?.SetTag("parser.confidence", 0.95);
                    return Task.FromResult(new StockParseResult(StockState.OutOfStock, 0.95, Name, availability));
                }
            }
            catch (JsonException ex)
            {
                LogMalformedJsonLd(logger, ex);
            }
        }

        activity?.SetTag("parser.matched", false);
        activity?.SetTag("parser.confidence", 0.0);
        return Task.FromResult(StockParseResults.Indeterminate(Name));
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Skipping malformed JSON-LD block")]
    private static partial void LogMalformedJsonLd(ILogger logger, Exception ex);

    private static bool IsProductType(JsonElement root)
    {
        if (root.TryGetProperty("@type", out var typeEl))
        {
            var typeValue = typeEl.GetString();
            if (string.Equals(typeValue, "Product", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static string? ExtractAvailability(JsonElement root)
    {
        if (!root.TryGetProperty("offers", out var offers)) return null;

        var offersEl = offers.ValueKind == JsonValueKind.Array
            ? offers[0]
            : offers;

        return offersEl.TryGetProperty("availability", out var avail)
            ? avail.GetString()
            : null;
    }
}
