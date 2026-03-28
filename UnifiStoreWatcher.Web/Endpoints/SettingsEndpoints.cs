using Microsoft.EntityFrameworkCore;
using UnifiStoreWatcher.Web.Data;

namespace UnifiStoreWatcher.Web.Endpoints;

public static class SettingsEndpoints
{
    public static RouteGroupBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/settings")
            .WithTags("Settings");

        group.MapGet("/", GetSettings)
            .WithName("GetSettings")
            .Produces<AppSettingsDto>();

        group.MapPut("/", UpdateSettings)
            .WithName("UpdateSettings")
            .Accepts<UpdateSettingsRequest>("application/json")
            .Produces<AppSettingsDto>();

        return group;
    }

    private static async Task<IResult> GetSettings(UnifiStoreWatcherDbContext db, CancellationToken ct)
    {
        var settings = await db.AppSettings.FindAsync([1], ct)
            ?? new Data.Entities.AppSettings();

        return Results.Ok(new AppSettingsDto(
            settings.Nickname,
            settings.Email,
            settings.Phone,
            settings.PollIntervalMinSeconds,
            settings.PollIntervalMaxSeconds,
            settings.MaxRetryAttempts,
            settings.RetryBaseDelaySeconds,
            settings.MinDelayBetweenRequestsSeconds,
            settings.VapidPublicKey));
    }

    private static async Task<IResult> UpdateSettings(
        UpdateSettingsRequest request,
        UnifiStoreWatcherDbContext db,
        CancellationToken ct)
    {
        var settings = await db.AppSettings.FindAsync([1], ct);
        if (settings is null)
        {
            settings = new Data.Entities.AppSettings { Id = 1 };
            db.AppSettings.Add(settings);
        }

        if (request.Nickname is not null) settings.Nickname = request.Nickname;
        if (request.Email is not null) settings.Email = request.Email;
        if (request.Phone is not null) settings.Phone = request.Phone;
        if (request.PollIntervalMinSeconds.HasValue) settings.PollIntervalMinSeconds = request.PollIntervalMinSeconds.Value;
        if (request.PollIntervalMaxSeconds.HasValue) settings.PollIntervalMaxSeconds = request.PollIntervalMaxSeconds.Value;
        if (request.MaxRetryAttempts.HasValue) settings.MaxRetryAttempts = request.MaxRetryAttempts.Value;
        if (request.RetryBaseDelaySeconds.HasValue) settings.RetryBaseDelaySeconds = request.RetryBaseDelaySeconds.Value;
        if (request.MinDelayBetweenRequestsSeconds.HasValue) settings.MinDelayBetweenRequestsSeconds = request.MinDelayBetweenRequestsSeconds.Value;

        await db.SaveChangesAsync(ct);
        return await GetSettings(db, ct);
    }
}
