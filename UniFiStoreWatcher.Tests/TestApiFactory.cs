using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace UniFiStoreWatcher.Tests;

/// <summary>
/// WebApplicationFactory for .NET 10 / Minimal API (WebApplicationBuilder) + SQLite.
///
/// Problem: WebApplicationFactory uses DeferredHostBuilder which re-runs the entry point.
/// The entry point calls db.Database.MigrateAsync() against /data/UniFiStoreWatcher.db which
/// does not exist in the test environment, so the host is never built ("entry point exited
/// without ever building an IHost").
///
/// Fix:
///   DeferredHostBuilder.ConfigureHostConfiguration runs immediately and stores settings in
///   _hostConfiguration. DeferredHostBuilder.Build() transforms those into --key=value
///   command-line args passed to the entry point factory, making
///   builder.Configuration.GetConnectionString("Default") return our override BEFORE
///   MigrateAsync() executes.
///
///   We use a named shared in-memory SQLite connection (Mode=Memory;Cache=Shared) so all
///   connections in the same process share one database as long as _keepAliveConnection is open.
/// </summary>
public sealed class TestApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly SqliteConnection _keepAliveConnection;

    public TestApiFactory()
    {
        // Unique name per factory so parallel test fixtures don't share state.
        var dbName = $"test-{Guid.NewGuid():N}";
        _connectionString = $"Data Source={dbName};Mode=Memory;Cache=Shared";

        // Keep at least one connection open so SQLite doesn't destroy the in-memory DB.
        _keepAliveConnection = new SqliteConnection(_connectionString);
        _keepAliveConnection.Open();
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
        // --ConnectionStrings:Default=<value> passed as command-line args to the real
        // entry point, overriding the DB path before MigrateAsync() accesses it.
        builder.ConfigureHostConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:UniFiStoreWatcher-db"] = _connectionString,
            });
        });

        return base.CreateHost(builder);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _keepAliveConnection.Close();
            _keepAliveConnection.Dispose();
        }
    }
}
