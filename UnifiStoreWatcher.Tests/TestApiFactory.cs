using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace UniFiStoreWatcher.Tests;

/// <summary>
/// WebApplicationFactory for .NET 10 / Minimal API (WebApplicationBuilder) + EF Core InMemory.
///
/// Problem: WebApplicationFactory uses DeferredHostBuilder which re-runs the entry point.
/// The entry point branches on the connection string prefix: when it starts with "InMemory:",
/// Program.cs calls UseInMemoryDatabase and EnsureCreatedAsync instead of MigrateAsync.
///
/// Fix:
///   DeferredHostBuilder.ConfigureHostConfiguration runs immediately and stores settings in
///   _hostConfiguration. DeferredHostBuilder.Build() transforms those into --key=value
///   command-line args passed to the entry point factory, making
///   builder.Configuration.GetConnectionString("UniFiStoreWatch-db") return our override BEFORE
///   EnsureCreatedAsync() executes.
///
///   Each factory instance uses a unique InMemory database name for full test isolation.
/// </summary>
public sealed class TestApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public TestApiFactory()
    {
        var dbName = $"test-{Guid.NewGuid():N}";
        _connectionString = $"InMemory:{dbName}";
    }

    // ConfigureWebHost intentionally left empty; all overrides are done in CreateHost
    // where we have direct access to the DeferredHostBuilder.
    protected override void ConfigureWebHost(IWebHostBuilder builder) { }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // builder is DeferredHostBuilder for Minimal API / WebApplicationBuilder apps.
        //
        // ConfigureHostConfiguration runs immediately on the DeferredHostBuilder's internal
        // _hostConfiguration ConfigurationManager; Build() then converts it to
        // --ConnectionStrings:UniFiStoreWatch-db=<value> passed as command-line args to the real
        // entry point, overriding the DB configuration before EnsureCreatedAsync() accesses it.
        builder.ConfigureHostConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:UniFiStoreWatch-db"] = _connectionString,
            });
        });

        return base.CreateHost(builder);
    }
}
