using UbiquitiStoreLurker.Web.Data.Entities;

namespace UbiquitiStoreLurker.Web.Hubs;

// Fired any time a product's stock state changes
public sealed record StockStatusChanged(
    int ProductId,
    string Url,
    string? ProductName,
    StockState FromState,
    StockState ToState,
    DateTimeOffset DetectedAtUtc);

// Fired after every completed poll cycle (success or error)
public sealed record PollCycleCompleted(
    int ProductId,
    string Url,
    bool Success,
    int HttpStatusCode,
    int DurationMs,
    DateTimeOffset CompletedAtUtc);

// Fired when polling encounters a persistent error
public sealed record PollErrorOccurred(
    int ProductId,
    string Url,
    string ErrorMessage,
    int ConsecutiveErrors,
    DateTimeOffset OccurredAtUtc);

// Fired when poller is about to start a poll (good for UI "refreshing" indicator)
public sealed record PollStarted(
    int ProductId,
    string Url,
    DateTimeOffset StartedAtUtc);
