using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using UbiquitiStoreLurker.Web.Data;

namespace UbiquitiStoreLurker.Web.Services.Health;

/// <summary>
/// Verifies the SQLite database is mounted, migrations applied, and accessible.
/// Runs a lightweight scalar query rather than a full schema probe.
/// </summary>
public sealed class DatabaseReadinessCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<UbiquitiStoreLurkerDbContext>();
            // AnyAsync(ct) translates to a cheap existence check that exercises the
            // full EF Core + SQLite stack without loading any rows.
            await db.AppSettings.AnyAsync(cancellationToken);
            return HealthCheckResult.Healthy("Database reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database unreachable", ex);
        }
    }
}
