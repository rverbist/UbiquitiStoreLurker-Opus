using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using UnifiStoreWatcher.Web.Data;
using UnifiStoreWatcher.Web.Data.Entities;
using UnifiStoreWatcher.Web.Services.Health;
using UnifiStoreWatcher.Web.Services.Polling;

namespace UnifiStoreWatcher.Tests.Polling;

[TestFixture]
public class PollSchedulerServiceTests
{
    // Each test seeds the database via a direct DbContext, then verifies behaviour
    // through the DI-provided DbContext inside PollSchedulerService. Both use the
    // same dbName so EF InMemory routes all operations to the same in-memory store.
    private static ServiceProvider BuildProvider(string dbName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<UnifiStoreWatcherDbContext>(o =>
            o.UseInMemoryDatabase(dbName));
        return services.BuildServiceProvider();
    }

    [Test]
    public async Task EnqueuesDueProducts_WhenNextPollDueInPast()
    {
        var dbName = $"PollScheduler-{Guid.NewGuid():N}";

        // Skip EnsureCreated() so HasData() seed products don't interfere.
        var dbOptions = new DbContextOptionsBuilder<UnifiStoreWatcherDbContext>()
            .UseInMemoryDatabase(dbName).Options;
        await using var db = new UnifiStoreWatcherDbContext(dbOptions);
        db.Products.Add(new Product
        {
            Url = "https://store.ui.com/eu/en/products/test-a",
            IsActive = true,
            NextPollDueAtUtc = DateTimeOffset.UtcNow.AddSeconds(-10),
        });
        await db.SaveChangesAsync();

        var channel = Channel.CreateBounded<PollWorkItem>(10);
        await using var provider = BuildProvider(dbName);

        var scheduler = new PollSchedulerService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            channel.Writer,
            Options.Create(new PollOptions()),
            Substitute.For<IReadinessIndicator>(),
            NullLogger<PollSchedulerService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await scheduler.StartAsync(cts.Token);
        await Task.Delay(200, CancellationToken.None);
        await scheduler.StopAsync(CancellationToken.None);

        var items = new List<PollWorkItem>();
        while (channel.Reader.TryRead(out var item))
            items.Add(item);

        Assert.That(items.Any(i => i.Url.Contains("test-a")), Is.True);
    }

    [Test]
    public async Task DoesNotEnqueue_InactiveProducts()
    {
        var dbName = $"PollScheduler-{Guid.NewGuid():N}";

        // Skip EnsureCreated() so HasData() seed products don't interfere.
        var dbOptions = new DbContextOptionsBuilder<UnifiStoreWatcherDbContext>()
            .UseInMemoryDatabase(dbName).Options;
        await using var db = new UnifiStoreWatcherDbContext(dbOptions);
        db.Products.Add(new Product
        {
            Url = "https://store.ui.com/eu/en/products/inactive",
            IsActive = false,
            NextPollDueAtUtc = DateTimeOffset.UtcNow.AddSeconds(-10),
        });
        await db.SaveChangesAsync();

        var channel = Channel.CreateBounded<PollWorkItem>(10);
        await using var provider = BuildProvider(dbName);

        var scheduler = new PollSchedulerService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            channel.Writer,
            Options.Create(new PollOptions()),
            Substitute.For<IReadinessIndicator>(),
            NullLogger<PollSchedulerService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await scheduler.StartAsync(cts.Token);
        await Task.Delay(200, CancellationToken.None);
        await scheduler.StopAsync(CancellationToken.None);

        Assert.That(channel.Reader.TryRead(out _), Is.False);
    }

    [Test]
    public async Task DoesNotEnqueue_ProductsNotYetDue()
    {
        var dbName = $"PollScheduler-{Guid.NewGuid():N}";

        // Skip EnsureCreated() so HasData() seed products don't interfere.
        var dbOptions = new DbContextOptionsBuilder<UnifiStoreWatcherDbContext>()
            .UseInMemoryDatabase(dbName).Options;
        await using var db = new UnifiStoreWatcherDbContext(dbOptions);
        db.Products.Add(new Product
        {
            Url = "https://store.ui.com/eu/en/products/future",
            IsActive = true,
            NextPollDueAtUtc = DateTimeOffset.UtcNow.AddHours(1),
        });
        await db.SaveChangesAsync();

        var channel = Channel.CreateBounded<PollWorkItem>(10);
        await using var provider = BuildProvider(dbName);

        var scheduler = new PollSchedulerService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            channel.Writer,
            Options.Create(new PollOptions()),
            Substitute.For<IReadinessIndicator>(),
            NullLogger<PollSchedulerService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await scheduler.StartAsync(cts.Token);
        await Task.Delay(200, CancellationToken.None);
        await scheduler.StopAsync(CancellationToken.None);

        Assert.That(channel.Reader.TryRead(out _), Is.False);
    }
}
