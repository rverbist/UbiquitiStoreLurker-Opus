using Microsoft.EntityFrameworkCore;
using UniFiStoreWatcher.Web.Data;

namespace UniFiStoreWatcher.Web.Endpoints;

public static class NotificationEndpoints
{
    public static RouteGroupBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications")
            .WithTags("Notifications");

        group.MapGet("/configs", GetConfigs)
            .WithName("GetNotificationConfigs")
            .Produces<IReadOnlyList<NotificationConfigDto>>();

        group.MapPut("/configs/{id:int}", UpdateConfig)
            .WithName("UpdateNotificationConfig")
            .Accepts<UpdateNotificationConfigRequest>("application/json")
            .Produces<NotificationConfigDto>()
            .ProducesProblem(404);

        group.MapGet("/logs", GetLogs)
            .WithName("GetNotificationLogs")
            .Produces<PagedResult<NotificationLogDto>>();

        return group;
    }

    private static async Task<IResult> GetConfigs(UniFiStoreWatcherDbContext db, CancellationToken ct)
    {
        var configs = await db.NotificationConfigs
            .OrderBy(c => c.Id)
            .Select(c => new NotificationConfigDto(c.Id, c.ProviderType, c.DisplayName, c.IsEnabled, c.SettingsJson))
            .ToListAsync(ct);

        return Results.Ok(configs);
    }

    private static async Task<IResult> UpdateConfig(
        int id,
        UpdateNotificationConfigRequest request,
        UniFiStoreWatcherDbContext db,
        CancellationToken ct)
    {
        var config = await db.NotificationConfigs.FindAsync([id], ct);
        if (config is null) return Results.Problem(title: "Config not found", statusCode: 404);

        if (request.IsEnabled.HasValue) config.IsEnabled = request.IsEnabled.Value;
        if (request.SettingsJson is not null) config.SettingsJson = request.SettingsJson;
        config.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Results.Ok(new NotificationConfigDto(config.Id, config.ProviderType, config.DisplayName, config.IsEnabled, config.SettingsJson));
    }

    private static async Task<IResult> GetLogs(
        UniFiStoreWatcherDbContext db,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var total = await db.NotificationLogs.CountAsync(ct);
        var items = await db.NotificationLogs
            .OrderByDescending(l => l.SentAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new NotificationLogDto(l.Id, l.ProviderType, l.Success, l.ErrorMessage, l.SentAtUtc))
            .ToListAsync(ct);

        return Results.Ok(new PagedResult<NotificationLogDto>(items, total, page, pageSize));
    }
}
