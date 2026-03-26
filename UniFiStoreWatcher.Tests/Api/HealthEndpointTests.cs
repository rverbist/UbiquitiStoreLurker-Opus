using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace UniFiStoreWatcher.Tests.Api;

[TestFixture]
public class HealthEndpointTests
{
    // -------------------------------------------------------------------------
    // /api/health/live  — liveness probe (always 200 regardless of checks)
    // -------------------------------------------------------------------------

    [Test]
    public async Task HealthLive_Returns200_WhenProcessRunning()
    {
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health/live");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    // -------------------------------------------------------------------------
    // /api/health/ready — readiness probe (200 when DB + poller ready)
    // -------------------------------------------------------------------------

    [Test]
    public async Task HealthReady_Returns200_WhenDatabaseAccessible()
    {
        // Default TestApiFactory uses in-memory SQLite with migrations applied.
        // PollSchedulerService starts and marks the readiness indicator after its
        // first scan loop (which finds no products and completes immediately).
        using var factory = new TestApiFactory();
        using var client = factory.CreateClient();

        // Allow background services a moment to start and mark ready.
        await Task.Delay(1000);

        var response = await client.GetAsync("/api/health/ready");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task HealthReady_Returns503_WhenDatabaseCheckFails()
    {
        // Use a standalone WebApplicationFactory (not TestApiFactory) so we can
        // override ConfigureWebHost to replace the "database" health check with a
        // stub that always returns Unhealthy — without breaking app startup by
        // providing a valid in-memory DB for MigrateAsync while reporting the check
        // as failed to the health endpoint.
        var dbName = $"health-test-{Guid.NewGuid():N}";
        var connStr = $"Data Source={dbName};Mode=Memory;Cache=Shared";
        using var keepAlive = new SqliteConnection(connStr);
        keepAlive.Open();

        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration(config =>
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:UniFiStoreWatcher-db"] = connStr,
                    }));

                builder.ConfigureServices(services =>
                {
                    // Replace the "database" named health check with one that always fails.
                    services.Configure<HealthCheckServiceOptions>(options =>
                    {
                        var existing = options.Registrations.FirstOrDefault(r => r.Name == "database");
                        if (existing is not null)
                            options.Registrations.Remove(existing);

                        options.Registrations.Add(new HealthCheckRegistration(
                            name: "database",
                            factory: _ => new AlwaysUnhealthyCheck(),
                            failureStatus: HealthStatus.Unhealthy,
                            tags: ["ready"]));
                    });
                });
            });

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health/ready");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
    }

    private sealed class AlwaysUnhealthyCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
            => Task.FromResult(HealthCheckResult.Unhealthy("Simulated database failure"));
    }
}
