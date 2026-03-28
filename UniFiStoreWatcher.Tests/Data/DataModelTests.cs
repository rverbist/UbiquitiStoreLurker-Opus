using Microsoft.EntityFrameworkCore;
using UniFiStoreWatcher.Web.Data;
using UniFiStoreWatcher.Web.Data.Entities;

namespace UniFiStoreWatcher.Tests.Data;

[TestFixture]
public class DataModelTests
{
    private static readonly string[] ExpectedProviderTypes =
        ["BrowserPush", "Email", "Sms", "Teams", "Discord"];

    private UniFiStoreWatcherDbContext _db = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<UniFiStoreWatcherDbContext>()
            .UseInMemoryDatabase($"DataModelTests-{Guid.NewGuid():N}")
            .Options;

        _db = new UniFiStoreWatcherDbContext(options);
        _db.Database.EnsureCreated();
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
    }

    [Test]
    public async Task AppSettings_Seeded_WithDefaults()
    {
        var settings = await _db.AppSettings.FindAsync(1);

        Assert.That(settings, Is.Not.Null);
        Assert.That(settings!.PollIntervalMinSeconds, Is.EqualTo(30));
        Assert.That(settings.PollIntervalMaxSeconds, Is.EqualTo(90));
        Assert.That(settings.MaxRetryAttempts, Is.EqualTo(3));
    }

    [Test]
    public async Task NotificationConfigs_Seeded_FiveProviders()
    {
        var configs = await _db.NotificationConfigs.ToListAsync();

        Assert.That(configs, Has.Count.EqualTo(5));
        Assert.That(configs.Select(c => c.ProviderType),
            Is.EquivalentTo(ExpectedProviderTypes));
    }

    [Test]
    public async Task NotificationConfigs_OnlyBrowserPushEnabled_ByDefault()
    {
        var configs = await _db.NotificationConfigs.ToListAsync();

        Assert.That(configs.Single(c => c.ProviderType == "BrowserPush").IsEnabled, Is.True);
        Assert.That(configs.Where(c => c.ProviderType != "BrowserPush").All(c => !c.IsEnabled), Is.True);
    }

    [Test]
    public async Task Product_CanBeSavedAndRetrieved()
    {
        var product = new Product
        {
            Url = "https://store.ui.com/eu/en/products/unifi-dream-machine-pro",
            Name = "UniFi Dream Machine Pro",
            IsActive = true,
            SubscribedEvents = SubscriptionType.InStock,
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        var retrieved = await _db.Products.FindAsync(product.Id);

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Url, Is.EqualTo(product.Url));
        Assert.That(retrieved.CurrentState, Is.EqualTo(StockState.Unknown));
    }

    [Test]
    public async Task StockCheck_ForeignKey_LinksToProduct()
    {
        var product = new Product { Url = "https://store.ui.com/eu/en/products/test" };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        var check = new StockCheck
        {
            ProductId = product.Id,
            RequestUrl = product.Url,
            DetectedState = StockState.InStock,
            DurationMs = 1200,
        };

        _db.StockChecks.Add(check);
        await _db.SaveChangesAsync();

        var retrieved = await _db.StockChecks
            .Include(c => c.Product)
            .FirstAsync(c => c.Id == check.Id);

        Assert.That(retrieved.Product.Id, Is.EqualTo(product.Id));
        Assert.That(retrieved.DetectedState, Is.EqualTo(StockState.InStock));
    }

    [Test]
    public async Task DateTimeOffset_RoundTrips_Correctly()
    {
        // SQL Server stores datetimeoffset with 100-nanosecond precision.
        // Truncate to milliseconds to keep the assertion tolerance tight.
        var now = DateTimeOffset.UtcNow;
        var product = new Product { Url = "https://store.ui.com/eu/en/products/dto-test" };
        product.NextPollDueAtUtc = now;
        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        _db.ChangeTracker.Clear();
        var retrieved = await _db.Products.FindAsync(product.Id);

        Assert.That(retrieved!.NextPollDueAtUtc,
            Is.EqualTo(product.NextPollDueAtUtc).Within(TimeSpan.FromMilliseconds(1)));
    }
}
