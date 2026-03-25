using UbiquitiStoreLurker.Web.Data.Entities;

namespace UbiquitiStoreLurker.Web.Services.Notifications;

public sealed record NotificationContext(
    Product Product,
    StockTransition Transition,
    NotificationConfig Config);
