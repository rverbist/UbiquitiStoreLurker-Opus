using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
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

        // Apply DateTimeOffset → binary converter globally
        var dateTimeOffsetConverter = new DateTimeOffsetToBinaryConverter();
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTimeOffset) ||
                    property.ClrType == typeof(DateTimeOffset?))
                {
                    property.SetValueConverter(dateTimeOffsetConverter);
                }
            }
        }

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

        // StockTransition indexes
        modelBuilder.Entity<StockTransition>(e =>
        {
            e.HasIndex(t => t.ProductId);
            e.HasIndex(t => t.DetectedAtUtc);
        });

        // PushSubscription index
        modelBuilder.Entity<PushSubscription>(e =>
        {
            e.HasIndex(s => s.Endpoint).IsUnique();
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
