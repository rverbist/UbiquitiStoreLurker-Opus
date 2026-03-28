using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using UnifiStoreWatcher.Web.Data;
using UnifiStoreWatcher.Web.Data.Entities;
using UnifiStoreWatcher.Web.Services.Notifications;

namespace UnifiStoreWatcher.Tests.Notifications;

[TestFixture]
public class NotificationDispatcherTests
{
    private static UnifiStoreWatcherDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<UnifiStoreWatcherDbContext>()
            .UseInMemoryDatabase($"NotificationDispatcher-{Guid.NewGuid():N}")
            .Options;
        var db = new UnifiStoreWatcherDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    // CreateAsyncScope() is an extension method that calls CreateScope() internally.
    // We mock CreateScope() so the extension method picks up our mock scope.
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

    // Seed a complete object graph so NotificationLog FK references are satisfied.
    // EF InMemory does not enforce FK constraints, but seeding correctly keeps
    // tests honest about the real data shape.
    private static async Task<(Product Product, StockTransition Transition)> SeedTransitionAsync(
        UnifiStoreWatcherDbContext db)
    {
        var product = new Product { Url = "https://example.com", ProductCode = "TEST-001" };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var check = new StockCheck
        {
            ProductId = product.Id,
            RequestUrl = "https://example.com",
            DetectedState = StockState.InStock,
            HttpStatusCode = 200,
            DurationMs = 100,
        };
        db.StockChecks.Add(check);
        await db.SaveChangesAsync();

        var transition = new StockTransition
        {
            ProductId = product.Id,
            StockCheckId = check.Id,
            FromState = StockState.OutOfStock,
            ToState = StockState.InStock,
        };
        db.StockTransitions.Add(transition);
        await db.SaveChangesAsync();

        return (product, transition);
    }

    [Test]
    public async Task DispatchAsync_CallsAllEnabledProviders()
    {
        await using var db = CreateDb();

        // BrowserPush (Id=1) is already enabled by seed; enable Email (Id=2) too
        var config2 = await db.NotificationConfigs.FindAsync(2);
        config2!.IsEnabled = true;
        await db.SaveChangesAsync();

        var (product, transition) = await SeedTransitionAsync(db);

        var pushProvider = Substitute.For<INotificationProvider>();
        pushProvider.ProviderType.Returns("BrowserPush");
        pushProvider.SendAsync(Arg.Any<NotificationContext>(), Arg.Any<CancellationToken>())
            .Returns(NotificationResult.Ok());

        var emailProvider = Substitute.For<INotificationProvider>();
        emailProvider.ProviderType.Returns("Email");
        emailProvider.SendAsync(Arg.Any<NotificationContext>(), Arg.Any<CancellationToken>())
            .Returns(NotificationResult.Ok());

        var dispatcher = new NotificationDispatcher(
            [pushProvider, emailProvider],
            CreateScopeFactory(db),
            NullLogger<NotificationDispatcher>.Instance);

        await dispatcher.DispatchAsync(product, transition, CancellationToken.None);

        await pushProvider.Received(1).SendAsync(Arg.Any<NotificationContext>(), Arg.Any<CancellationToken>());
        await emailProvider.Received(1).SendAsync(Arg.Any<NotificationContext>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchAsync_DoesNotCallDisabledProviders()
    {
        await using var db = CreateDb();

        // Disable BrowserPush (Id=1) so all configs are disabled
        var config1 = await db.NotificationConfigs.FindAsync(1);
        config1!.IsEnabled = false;
        await db.SaveChangesAsync();

        var provider = Substitute.For<INotificationProvider>();
        provider.ProviderType.Returns("BrowserPush");

        var dispatcher = new NotificationDispatcher(
            [provider],
            CreateScopeFactory(db),
            NullLogger<NotificationDispatcher>.Instance);

        // No StockTransition needed — dispatcher returns early when no configs are enabled
        var product = new Product { Id = 1, Url = "https://example.com" };
        var transition = new StockTransition { Id = 1, ProductId = 1, FromState = StockState.OutOfStock, ToState = StockState.InStock };

        await dispatcher.DispatchAsync(product, transition, CancellationToken.None);

        await provider.DidNotReceive().SendAsync(Arg.Any<NotificationContext>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchAsync_ContinuesDispatch_WhenOneProviderFails()
    {
        await using var db = CreateDb();

        // Enable BrowserPush (Id=1, already enabled) and Email (Id=2)
        var config2 = await db.NotificationConfigs.FindAsync(2);
        config2!.IsEnabled = true;
        await db.SaveChangesAsync();

        var (product, transition) = await SeedTransitionAsync(db);

        var failingProvider = Substitute.For<INotificationProvider>();
        failingProvider.ProviderType.Returns("BrowserPush");
        failingProvider.SendAsync(Arg.Any<NotificationContext>(), Arg.Any<CancellationToken>())
            .Returns(NotificationResult.Fail("Connection refused"));

        var successProvider = Substitute.For<INotificationProvider>();
        successProvider.ProviderType.Returns("Email");
        successProvider.SendAsync(Arg.Any<NotificationContext>(), Arg.Any<CancellationToken>())
            .Returns(NotificationResult.Ok());

        var dispatcher = new NotificationDispatcher(
            [failingProvider, successProvider],
            CreateScopeFactory(db),
            NullLogger<NotificationDispatcher>.Instance);

        // Must not throw even when one provider fails
        Assert.DoesNotThrowAsync(async () =>
            await dispatcher.DispatchAsync(product, transition, CancellationToken.None));

        await successProvider.Received(1).SendAsync(Arg.Any<NotificationContext>(), Arg.Any<CancellationToken>());
    }
}
