using AngleSharp;
using AngleSharp.Dom;
using System.Diagnostics;
using UniFiStoreWatcher.Web.Data.Entities;
using UniFiStoreWatcher.Web.Telemetry;

namespace UniFiStoreWatcher.Web.Services.Parsing;

public sealed partial class CompositeStockParser(
    IEnumerable<IStockParser> parsers,
    ILogger<CompositeStockParser> logger)
{
    private const double MinConfidenceThreshold = 0.60;

    public async Task<StockParseResult> ParseAsync(string html, CancellationToken ct = default)
    {
        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        IDocument document = await context.OpenAsync(req => req.Content(html), ct);

        return await ParseDocumentAsync(document, ct);
    }

    public async Task<StockParseResult> ParseDocumentAsync(IDocument document, CancellationToken ct = default)
    {
        using var activity = UniFiStoreWatcherActivities.Source.StartActivity("parse.composite", ActivityKind.Internal);

        foreach (var parser in parsers)
        {
            var result = await parser.ParseAsync(document, ct);

            if (result.State != StockState.Indeterminate && result.Confidence >= MinConfidenceThreshold)
            {
                LogParserDetected(logger, result.Strategy, result.State, result.Confidence, result.Evidence);
                activity?.SetTag("parser.winner", result.Strategy);
                activity?.SetTag("stock.state", result.State.ToString());
                return result;
            }

            LogParserIndeterminate(logger, parser.Name);
        }

        LogAllIndeterminate(logger);
        activity?.SetTag("parser.winner", "none");
        activity?.SetTag("stock.state", StockState.Indeterminate.ToString());
        return new StockParseResult(StockState.Indeterminate, 0.0, "Composite", null);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Parser {Parser} detected {State} with confidence {Confidence:P0}: {Evidence}")]
    private static partial void LogParserDetected(ILogger logger, string parser, StockState state, double confidence, string? evidence);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Parser {Parser} returned Indeterminate")]
    private static partial void LogParserIndeterminate(ILogger logger, string parser);

    [LoggerMessage(Level = LogLevel.Warning, Message = "All parsers returned Indeterminate — no stock state detected")]
    private static partial void LogAllIndeterminate(ILogger logger);
}
