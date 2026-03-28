using Microsoft.EntityFrameworkCore;
using UnifiStoreWatcher.Web.Data;
using UnifiStoreWatcher.Web.Services.Notifications;
using EntityPushSubscription = UnifiStoreWatcher.Web.Data.Entities.PushSubscription;

namespace UnifiStoreWatcher.Web.Endpoints;

public static class PushEndpoints
{
    public static RouteGroupBuilder MapPushEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/push")
            .WithTags("Push");

        group.MapPost("/subscribe", Subscribe)
            .WithName("PushSubscribe")
            .Accepts<PushSubscribeRequest>("application/json")
            .Produces(201)
            .ProducesProblem(409);

        group.MapDelete("/unsubscribe", Unsubscribe)
            .WithName("PushUnsubscribe")
            .Accepts<PushSubscribeRequest>("application/json")
            .Produces(204);

        group.MapPost("/test", SendTest)
            .WithName("PushTest")
            .Produces(204)
            .ProducesProblem(400);

        return group;
    }

    private static async Task<IResult> Subscribe(
        PushSubscribeRequest request,
        UnifiStoreWatcherDbContext db,
        CancellationToken ct)
    {
        if (await db.PushSubscriptions.AnyAsync(s => s.Endpoint == request.Endpoint, ct))
            return Results.Problem(title: "Already subscribed", statusCode: 409);

        db.PushSubscriptions.Add(new EntityPushSubscription
        {
            Endpoint = request.Endpoint,
            P256dh = request.P256dh,
            Auth = request.Auth,
        });
        await db.SaveChangesAsync(ct);
        return Results.StatusCode(201);
    }

    private static async Task<IResult> Unsubscribe(
        [Microsoft.AspNetCore.Mvc.FromBody] PushSubscribeRequest request,
        UnifiStoreWatcherDbContext db,
        CancellationToken ct)
    {
        var sub = await db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == request.Endpoint, ct);
        if (sub is not null)
        {
            db.PushSubscriptions.Remove(sub);
            await db.SaveChangesAsync(ct);
        }
        return Results.NoContent();
    }

    private static async Task<IResult> SendTest(
        BrowserPushProvider pushProvider,
        UnifiStoreWatcherDbContext db,
        CancellationToken ct)
    {
        var count = await db.PushSubscriptions.CountAsync(ct);
        if (count == 0)
            return Results.Problem(title: "No push subscriptions registered", statusCode: 400);

        var result = await pushProvider.SendTestAsync(ct);
        return result.Success ? Results.NoContent() : Results.Problem(title: result.ErrorMessage, statusCode: 500);
    }
}
