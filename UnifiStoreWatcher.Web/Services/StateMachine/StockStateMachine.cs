using UnifiStoreWatcher.Web.Data;
using UnifiStoreWatcher.Web.Data.Entities;
using UnifiStoreWatcher.Web.Services.Parsing;
using System.Diagnostics;
using UnifiStoreWatcher.Web.Telemetry;

namespace UnifiStoreWatcher.Web.Services.StateMachine;

public sealed record TransitionResult(
    bool StateChanged,
    bool ShouldNotify,
    StockTransition? Transition);

public sealed partial class StockStateMachine(
    ILogger<StockStateMachine> logger)
{
    public TransitionResult Evaluate(
        Product product,
        StockParseResult parseResult,
        StockCheck check)
    {
        using var activity = UnifiStoreWatcherActivities.Source.StartActivity("state.evaluate", ActivityKind.Internal);

        var newState = parseResult.State;

        if (newState == StockState.Indeterminate)
        {
            LogIndeterminate(logger, product.Id);
            activity?.SetTag("state.changed", false);
            activity?.SetTag("state.should_notify", false);
            return new TransitionResult(false, false, null);
        }

        var previousState = product.CurrentState;
        activity?.SetTag("state.from", previousState.ToString());
        activity?.SetTag("state.to", newState.ToString());

        if (previousState == newState)
        {
            activity?.SetTag("state.changed", false);
            activity?.SetTag("state.should_notify", false);
            return new TransitionResult(false, false, null);
        }

        var transition = new StockTransition
        {
            ProductId = product.Id,
            FromState = previousState,
            ToState = newState,
            DetectedAtUtc = DateTimeOffset.UtcNow,
            StockCheckId = check.Id,
        };

        var isInitialDiscovery = previousState == StockState.Unknown;
        var shouldNotify = !isInitialDiscovery && ShouldSendNotification(product, newState);

        LogStateTransition(logger, product.Id, previousState, newState, shouldNotify);

        activity?.SetTag("state.changed", true);
        activity?.SetTag("state.should_notify", shouldNotify);
        return new TransitionResult(true, shouldNotify, transition);
    }

    private static bool ShouldSendNotification(Product product, StockState newState)
    {
        return newState switch
        {
            StockState.InStock => product.SubscribedEvents.HasFlag(SubscriptionType.InStock),
            StockState.OutOfStock => product.SubscribedEvents.HasFlag(SubscriptionType.OutOfStock),
            _ => false,
        };
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "ProductId={ProductId} Indeterminate — skipping state evaluation")]
    private static partial void LogIndeterminate(ILogger logger, int productId);

    [LoggerMessage(Level = LogLevel.Information, Message = "ProductId={ProductId} state transition: {From} \u2192 {To} ShouldNotify={ShouldNotify}")]
    private static partial void LogStateTransition(ILogger logger, int productId, StockState from, StockState to, bool shouldNotify);
}
