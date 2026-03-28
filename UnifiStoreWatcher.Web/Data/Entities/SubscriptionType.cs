namespace UnifiStoreWatcher.Web.Data.Entities;

[Flags]
public enum SubscriptionType
{
    None = 0,
    InStock = 1,
    OutOfStock = 2,
    Both = InStock | OutOfStock,
}
