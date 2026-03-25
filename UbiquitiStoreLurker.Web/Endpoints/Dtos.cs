using System.ComponentModel.DataAnnotations;
using UbiquitiStoreLurker.Web.Data.Entities;

namespace UbiquitiStoreLurker.Web.Endpoints;

// Products
public sealed record ProductDto(
    int Id,
    string Url,
    string? ProductCode,
    string? Name,
    string? Description,
    string? ImageUrl,
    StockState CurrentState,
    bool IsActive,
    SubscriptionType SubscribedEvents,
    DateTimeOffset NextPollDueAtUtc,
    DateTimeOffset? LastPollAtUtc,
    DateTimeOffset? LastStateChangeAtUtc,
    int PollCount,
    int ErrorCount);

public sealed record CreateProductRequest([Required, Url] string Url, SubscriptionType SubscribedEvents = SubscriptionType.InStock);

public sealed record UpdateProductRequest(bool? IsActive, SubscriptionType? SubscribedEvents, int? PollIntervalOverrideSeconds);

public sealed record PushSubscribeRequest(
    [Required] string Endpoint,
    [Required] string P256dh,
    [Required] string Auth);

// Stock history
public sealed record StockCheckDto(
    long Id,
    string RequestUrl,
    int? HttpStatusCode,
    StockState DetectedState,
    string? ParserStrategy,
    double? ParserConfidence,
    int DurationMs,
    string? ErrorMessage,
    DateTimeOffset CreatedAtUtc);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

// Settings
public sealed record AppSettingsDto(
    string Nickname,
    string? Email,
    string? Phone,
    int PollIntervalMinSeconds,
    int PollIntervalMaxSeconds,
    int MaxRetryAttempts,
    int RetryBaseDelaySeconds,
    int MinDelayBetweenRequestsSeconds,
    string? VapidPublicKey);

public sealed record UpdateSettingsRequest(
    string? Nickname,
    string? Email,
    string? Phone,
    int? PollIntervalMinSeconds,
    int? PollIntervalMaxSeconds,
    int? MaxRetryAttempts,
    int? RetryBaseDelaySeconds,
    int? MinDelayBetweenRequestsSeconds);

// Notifications
public sealed record NotificationConfigDto(
    int Id,
    string ProviderType,
    string DisplayName,
    bool IsEnabled,
    string? SettingsJson);

public sealed record UpdateNotificationConfigRequest(bool? IsEnabled, string? SettingsJson);

public sealed record NotificationLogDto(
    long Id,
    string ProviderType,
    bool Success,
    string? ErrorMessage,
    DateTimeOffset SentAtUtc);

// Push subscription
public sealed record PushSubscriptionRequest(
    [Required] string Endpoint,
    [Required] string P256dh,
    [Required] string Auth);
