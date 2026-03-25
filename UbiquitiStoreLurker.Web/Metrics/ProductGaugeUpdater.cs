using Microsoft.EntityFrameworkCore;
using UbiquitiStoreLurker.Web.Data;

namespace UbiquitiStoreLurker.Web.Metrics;

public sealed partial class ProductGaugeUpdater(IServiceScopeFactory scopeFactory, ILogger<ProductGaugeUpdater> logger)
    : BackgroundService
{
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await UpdateGaugesAsync(stoppingToken);
            await Task.Delay(UpdateInterval, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task UpdateGaugesAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<UbiquitiStoreLurkerDbContext>();
            var total = await db.Products.CountAsync(ct);
            var active = await db.Products.CountAsync(p => p.IsActive, ct);
            UbiquitiStoreLurkerMetrics.MonitoredProductsTotal.Set(total);
            UbiquitiStoreLurkerMetrics.ActiveProducts.Set(active);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogUpdateFailed(logger, ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to update product gauges")]
    private static partial void LogUpdateFailed(ILogger logger, Exception ex);
}
