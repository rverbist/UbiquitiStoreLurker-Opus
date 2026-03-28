using Microsoft.EntityFrameworkCore;
using UniFiStoreWatcher.Web.Data.Entities;

namespace UniFiStoreWatcher.Web.Data;

public class UniFiStoreWatcherDbContext(DbContextOptions<UniFiStoreWatcherDbContext> options)
    : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockCheck> StockChecks => Set<StockCheck>();
    public DbSet<StockTransition> StockTransitions => Set<StockTransition>();
    public DbSet<NotificationConfig> NotificationConfigs => Set<NotificationConfig>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<AppSettings> AppSettings => Set<AppSettings>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Product indexes
        modelBuilder.Entity<Product>(e =>
        {
            e.HasIndex(p => p.Url).IsUnique();
            e.HasIndex(p => p.ProductCode).IsUnique().HasFilter("[ProductCode] IS NOT NULL");
            e.HasIndex(p => new { p.IsActive, p.NextPollDueAtUtc });
        });

        // StockCheck indexes
        modelBuilder.Entity<StockCheck>(e =>
        {
            e.HasIndex(c => c.ProductId);
            e.HasIndex(c => c.CreatedAtUtc);
        });

        // StockTransition indexes + FK delete behavior.
        // SQL Server rejects multiple CASCADE paths to the same table.
        // The chain Products→StockChecks→StockTransitions already has two paths
        // from Products (direct via ProductId CASCADE, and indirect via StockCheckId CASCADE).
        // Setting Restrict on StockCheckId breaks the second path while still guaranteeing
        // that deleting a Product cascades directly through StockTransitions.
        modelBuilder.Entity<StockTransition>(e =>
        {
            e.HasIndex(t => t.ProductId);
            e.HasIndex(t => t.DetectedAtUtc);
            e.HasOne(t => t.StockCheck)
             .WithMany()
             .HasForeignKey(t => t.StockCheckId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // PushSubscription index
        modelBuilder.Entity<PushSubscription>(e =>
        {
            e.HasIndex(s => s.Endpoint).IsUnique();
        });

        // Product seed — pre-populated watchlist
        modelBuilder.Entity<Product>(e =>
        {
            e.HasData(
                new Product
                {
                    Id = 1, Url = "https://eu.store.ui.com/eu/en/category/network-storage/products/unas-pro-4",
                    ProductCode = "UNAS-Pro-4", Name = "UNAS Pro 4",
                    IsActive = true, SubscribedEvents = SubscriptionType.InStock,
                    CurrentState = StockState.Unknown, PreviousState = StockState.Unknown,
                    NextPollDueAtUtc = DateTimeOffset.UnixEpoch,
                    CreatedAtUtc = DateTimeOffset.UnixEpoch, UpdatedAtUtc = DateTimeOffset.UnixEpoch,
                },
                new Product
                {
                    Id = 2, Url = "https://eu.store.ui.com/eu/en/category/network-storage/products/unas-pro-8",
                    ProductCode = "UNAS-Pro-8", Name = "UNAS Pro 8",
                    IsActive = true, SubscribedEvents = SubscriptionType.InStock,
                    CurrentState = StockState.Unknown, PreviousState = StockState.Unknown,
                    NextPollDueAtUtc = DateTimeOffset.UnixEpoch,
                    CreatedAtUtc = DateTimeOffset.UnixEpoch, UpdatedAtUtc = DateTimeOffset.UnixEpoch,
                },
                new Product
                {
                    Id = 3, Url = "https://eu.store.ui.com/eu/en/category/wifi-bridging/products/udb-iot",
                    ProductCode = "UDB-IoT", Name = "UDB IoT",
                    IsActive = true, SubscribedEvents = SubscriptionType.InStock,
                    CurrentState = StockState.Unknown, PreviousState = StockState.Unknown,
                    NextPollDueAtUtc = DateTimeOffset.UnixEpoch,
                    CreatedAtUtc = DateTimeOffset.UnixEpoch, UpdatedAtUtc = DateTimeOffset.UnixEpoch,
                },
                new Product
                {
                    Id = 4, Url = "https://eu.store.ui.com/eu/en/category/wifi-special-devices/products/utr",
                    ProductCode = "UTR", Name = "UTR",
                    IsActive = true, SubscribedEvents = SubscriptionType.InStock,
                    CurrentState = StockState.Unknown, PreviousState = StockState.Unknown,
                    NextPollDueAtUtc = DateTimeOffset.UnixEpoch,
                    CreatedAtUtc = DateTimeOffset.UnixEpoch, UpdatedAtUtc = DateTimeOffset.UnixEpoch,
                },
                new Product
                {
                    Id = 5, Url = "https://eu.store.ui.com/eu/en/category/cameras-dome-turret/products/uvc-g6-edge-turret",
                    ProductCode = "UVC-G6-Edge-Turret", Name = "UVC G6 Edge Turret",
                    IsActive = true, SubscribedEvents = SubscriptionType.InStock,
                    CurrentState = StockState.Unknown, PreviousState = StockState.Unknown,
                    NextPollDueAtUtc = DateTimeOffset.UnixEpoch,
                    CreatedAtUtc = DateTimeOffset.UnixEpoch, UpdatedAtUtc = DateTimeOffset.UnixEpoch,
                },
                new Product
                {
                    Id = 6, Url = "https://eu.store.ui.com/eu/en/category/cameras-dome-turret/products/uvc-g6-edge-dome",
                    ProductCode = "UVC-G6-Edge-Dome", Name = "UVC G6 Edge Dome",
                    IsActive = true, SubscribedEvents = SubscriptionType.InStock,
                    CurrentState = StockState.Unknown, PreviousState = StockState.Unknown,
                    NextPollDueAtUtc = DateTimeOffset.UnixEpoch,
                    CreatedAtUtc = DateTimeOffset.UnixEpoch, UpdatedAtUtc = DateTimeOffset.UnixEpoch,
                },
                new Product
                {
                    Id = 7, Url = "https://eu.store.ui.com/eu/en/category/accessories-storage/collections/unifi-accessory-tech-hdd/products/uacc-hdd-e-24tb",
                    ProductCode = "UACC-HDD-E-24TB", Name = "UACC HDD E-24TB",
                    IsActive = true, SubscribedEvents = SubscriptionType.InStock,
                    CurrentState = StockState.Unknown, PreviousState = StockState.Unknown,
                    NextPollDueAtUtc = DateTimeOffset.UnixEpoch,
                    CreatedAtUtc = DateTimeOffset.UnixEpoch, UpdatedAtUtc = DateTimeOffset.UnixEpoch,
                }
            );
        });

        // AppSettings — singleton, always Id = 1
        modelBuilder.Entity<AppSettings>(e =>
        {
            e.HasData(new AppSettings { Id = 1 });
        });

        // NotificationConfig seed — one per provider type
        modelBuilder.Entity<NotificationConfig>(e =>
        {
            e.HasData(
                new NotificationConfig
                {
                    Id = 1, ProviderType = "BrowserPush", DisplayName = "Browser Push",
                    IsEnabled = true,
                    CreatedAtUtc = DateTimeOffset.UnixEpoch, UpdatedAtUtc = DateTimeOffset.UnixEpoch,
                },
                new NotificationConfig
                {
                    Id = 2, ProviderType = "Email", DisplayName = "Email",
                    IsEnabled = false,
                    CreatedAtUtc = DateTimeOffset.UnixEpoch, UpdatedAtUtc = DateTimeOffset.UnixEpoch,
                },
                new NotificationConfig
                {
                    Id = 3, ProviderType = "Sms", DisplayName = "SMS (Twilio)",
                    IsEnabled = false,
                    CreatedAtUtc = DateTimeOffset.UnixEpoch, UpdatedAtUtc = DateTimeOffset.UnixEpoch,
                },
                new NotificationConfig
                {
                    Id = 4, ProviderType = "Teams", DisplayName = "Microsoft Teams",
                    IsEnabled = false,
                    CreatedAtUtc = DateTimeOffset.UnixEpoch, UpdatedAtUtc = DateTimeOffset.UnixEpoch,
                },
                new NotificationConfig
                {
                    Id = 5, ProviderType = "Discord", DisplayName = "Discord",
                    IsEnabled = false,
                    CreatedAtUtc = DateTimeOffset.UnixEpoch, UpdatedAtUtc = DateTimeOffset.UnixEpoch,
                }
            );
        });
    }
}
