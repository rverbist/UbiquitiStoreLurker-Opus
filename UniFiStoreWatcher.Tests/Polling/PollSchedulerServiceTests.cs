using System.Threading.Channels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using UniFiStoreWatcher.Web.Data;
using UniFiStoreWatcher.Web.Data.Entities;
using UniFiStoreWatcher.Web.Services.Health;
using UniFiStoreWatcher.Web.Services.Polling;

namespace UniFiStoreWatcher.Tests.Polling;

[TestFixture]
public class PollSchedulerServiceTests
{
    // Opens a SQLite in-memory connection and creates the schema.
    // The connection must remain open for the lifetime of the test;
    // passing it to DI ensures all contexts share the same database.
    private static SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<UniFiStoreWatcherDbContext>()
            .UseSqlite(connection)
            .Options;
        using var db = new UniFiStoreWatcherDbContext(options);
        db.Database.EnsureCreated();
        return connection;
    }

    private static ServiceProvider BuildProvider(SqliteConnection connection)
    {
        var services = new ServiceCollection();
        services.AddEntityFrameworkSqlite();
        services.AddDbContext<UniFiStoreWatcherDbContext>(o => o.UseSqlite(connection));
        return services.BuildServiceProvider();
    }

    [Test]
    public async Task EnqueuesDueProducts_WhenNextPollDueInPast()
    {
        await using var connection = CreateConnection();

        var dbOptions = new DbContextOptionsBuilder<UniFiStoreWatcherDbContext>()
            .UseSqlite(connection).Options;
        await using var db = new UniFiStoreWatcherDbContext(dbOptions);
        db.Products.Add(new Product
        {
            Url = "https://store.ui.com/eu/en/products/test-a",
            IsActive = true,
            NextPollDueAtUtc = DateTimeOffset.UtcNow.AddSeconds(-10),
        });
        await db.SaveChangesAsync();

        var channel = Channel.CreateBounded<PollWorkItem>(10);
        await using var provider = BuildProvider(connection);

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

        Assert.That(items, Has.Count.EqualTo(1));
        Assert.That(items[0].Url, Does.Contain("test-a"));
    }

    [Test]
    public async Task DoesNotEnqueue_InactiveProducts()
    {
        await using var connection = CreateConnection();

        var dbOptions = new DbContextOptionsBuilder<UniFiStoreWatcherDbContext>()
            .UseSqlite(connection).Options;
        await using var db = new UniFiStoreWatcherDbContext(dbOptions);
        db.Products.Add(new Product
        {
            Url = "https://store.ui.com/eu/en/products/inactive",
            IsActive = false,
            NextPollDueAtUtc = DateTimeOffset.UtcNow.AddSeconds(-10),
        });
        await db.SaveChangesAsync();

        var channel = Channel.CreateBounded<PollWorkItem>(10);
        await using var provider = BuildProvider(connection);

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
        await using var connection = CreateConnection();

        var dbOptions = new DbContextOptionsBuilder<UniFiStoreWatcherDbContext>()
            .UseSqlite(connection).Options;
        await using var db = new UniFiStoreWatcherDbContext(dbOptions);
        db.Products.Add(new Product
        {
            Url = "https://store.ui.com/eu/en/products/future",
            IsActive = true,
            NextPollDueAtUtc = DateTimeOffset.UtcNow.AddHours(1),
        });
        await db.SaveChangesAsync();

        var channel = Channel.CreateBounded<PollWorkItem>(10);
        await using var provider = BuildProvider(connection);

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
