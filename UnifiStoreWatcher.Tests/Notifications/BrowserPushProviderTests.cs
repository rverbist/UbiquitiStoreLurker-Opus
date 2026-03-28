using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using UnifiStoreWatcher.Web.Data;
using UnifiStoreWatcher.Web.Data.Entities;
using UnifiStoreWatcher.Web.Services.Notifications;

namespace UnifiStoreWatcher.Tests.Notifications;

[TestFixture]
public class BrowserPushProviderTests
{
    private UnifiStoreWatcherDbContext _db = null!;
    private IServiceScopeFactory _scopeFactory = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<UnifiStoreWatcherDbContext>()
            .UseInMemoryDatabase($"BrowserPushTests-{Guid.NewGuid():N}")
            .Options;
        _db = new UnifiStoreWatcherDbContext(options);
        _db.Database.EnsureCreated();

        var settings = _db.AppSettings.Find(1);
        if (settings is not null)
        {
            settings.VapidPublicKey = "BFakePublicKey123456789=";
            settings.VapidPrivateKey = "FakePrivateKey123456789=";
            _db.SaveChanges();
        }

        _scopeFactory = CreateScopeFactory(_db);
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
    }

    // CreateAsyncScope() is an extension method backed by CreateScope().
    // Mocking CreateScope() is sufficient for NSubstitute to intercept it.
    private static IServiceScopeFactory CreateScopeFactory(UnifiStoreWatcherDbContext db)
    {
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        var svcProvider = Substitute.For<IServiceProvider>();

        svcProvider.GetService(typeof(UnifiStoreWatcherDbContext)).Returns(db);
        scope.ServiceProvider.Returns(svcProvider);
        scopeFactory.CreateScope().Returns(scope);

        return scopeFactory;
    }

    private static NotificationContext BuildContext(StockState toState = StockState.InStock)
    {
        var product = new Product { Url = "https://ui.com/test", CurrentState = toState };
        var transition = new StockTransition { FromState = StockState.OutOfStock, ToState = toState };
        var config = new NotificationConfig
        {
            ProviderType = "BrowserPush",
            DisplayName = "Browser Push",
            IsEnabled = true,
        };
        return new NotificationContext(product, transition, config);
    }

    [Test]
    public async Task NoSubscriptions_ReturnsOk()
    {
        var provider = new BrowserPushProvider(_scopeFactory, NullLogger<BrowserPushProvider>.Instance);

        var result = await provider.SendAsync(BuildContext(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task WithSubscription_NoVapidKeys_ReturnsFail()
    {
        var settings = _db.AppSettings.Find(1);
        if (settings is not null)
        {
            settings.VapidPublicKey = null;
            settings.VapidPrivateKey = null;
            _db.SaveChanges();
        }

        _db.PushSubscriptions.Add(new PushSubscription
        {
            Endpoint = "https://fcm.example.com/push/1",
            P256dh = "fakep256dh",
            Auth = "fakeauth",
        });
        _db.SaveChanges();

        var provider = new BrowserPushProvider(_scopeFactory, NullLogger<BrowserPushProvider>.Instance);

        var result = await provider.SendAsync(BuildContext(), CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("VAPID"));
    }
}
