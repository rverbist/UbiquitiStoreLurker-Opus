using UniFiStoreWatcher.Web.Data.Entities;

namespace UniFiStoreWatcher.Web.Services.Notifications;

public sealed record NotificationContext(
    Product Product,
    StockTransition Transition,
    NotificationConfig Config);
