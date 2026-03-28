using UnifiStoreWatcher.Web.Data.Entities;

namespace UnifiStoreWatcher.Web.Services.Notifications;

public sealed record NotificationContext(
    Product Product,
    StockTransition Transition,
    NotificationConfig Config);
