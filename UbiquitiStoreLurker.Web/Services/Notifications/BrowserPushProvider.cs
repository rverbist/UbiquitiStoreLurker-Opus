using System.Diagnostics;
using System.Text.Json;
using Lib.Net.Http.WebPush.Authentication;
using Microsoft.EntityFrameworkCore;
using UbiquitiStoreLurker.Web.Data;
using UbiquitiStoreLurker.Web.Data.Entities;
using UbiquitiStoreLurker.Web.Telemetry;
using EntityPushSubscription = UbiquitiStoreLurker.Web.Data.Entities.PushSubscription;
using LibPushSubscription = Lib.Net.Http.WebPush.PushSubscription;
using LibPushMessage = Lib.Net.Http.WebPush.PushMessage;
using LibPushServiceClient = Lib.Net.Http.WebPush.PushServiceClient;
using LibPushMessageUrgency = Lib.Net.Http.WebPush.PushMessageUrgency;
using LibPushEncryptionKeyName = Lib.Net.Http.WebPush.PushEncryptionKeyName;
using LibPushServiceClientException = Lib.Net.Http.WebPush.PushServiceClientException;

namespace UbiquitiStoreLurker.Web.Services.Notifications;

public sealed partial class BrowserPushProvider(
    IServiceScopeFactory scopeFactory,
    ILogger<BrowserPushProvider> logger)
    : INotificationProvider
{
    private static readonly int[] VibratePattern = [200, 100, 200];

    public string ProviderType => "BrowserPush";

    public bool ValidateConfig(string? settingsJson, out string? configError)
    {
        configError = null;
        return true;
    }

    public async Task<NotificationResult> SendAsync(NotificationContext context, CancellationToken ct)
    {
        using var activity = UbiquitiStoreLurkerActivities.Source.StartActivity("notification.send.push", ActivityKind.Internal);
        activity?.SetTag("provider.type", ProviderType);

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UbiquitiStoreLurkerDbContext>();

        var appSettings = await db.AppSettings.FindAsync([1], ct);
        if (appSettings is null || string.IsNullOrEmpty(appSettings.VapidPublicKey) || string.IsNullOrEmpty(appSettings.VapidPrivateKey))
        {
            activity?.SetTag("provider.success", false);
            activity?.SetTag("provider.error", "VAPID keys not configured");
            return NotificationResult.Fail("VAPID keys not configured");
        }

        var subscriptions = await db.PushSubscriptions.ToListAsync(ct);
        if (subscriptions.Count == 0)
        {
            activity?.SetTag("provider.success", true);
            return NotificationResult.Ok();
        }

        var isInStock = context.Product.CurrentState == StockState.InStock;
        var payload = JsonSerializer.Serialize(new
        {
            title = isInStock ? "🎉 In Stock!" : "📦 Out of Stock",
            body = context.Product.Name ?? context.Product.Url,
            url = context.Product.Url,
            icon = "/icon-192.png",
            // badge: 96×96 monochrome PNG for the Android status bar / Win11 corner.
            badge = "/badge-96.png",
            // image: wide banner shown below the notification body on Chrome/Edge (desktop + Android).
            // Null when the product has no scraped thumbnail — the service worker omits it.
            image = context.Product.ImageUrl,
            // tag: deduplicates per-product notifications in the tray/Action Center.
            tag = $"product-{context.Product.Id}",
            // renotify: re-alert (sound + vibration) even when the same tag is already showing.
            renotify = true,
            // timestamp: when the stock event occurred (epoch ms); browsers render relative time.
            timestamp = (context.Product.LastStateChangeAtUtc ?? DateTimeOffset.UtcNow).ToUnixTimeMilliseconds(),
            // requireInteraction: keeps the Win11 toast visible until the user acts. Ignored on Android.
            requireInteraction = isInStock,
            // vibrate: [on, off, on, …] pattern in ms. Chrome/Edge on Android only; ignored on desktop.
            vibrate = VibratePattern,
        });

        var vapid = new VapidAuthentication(appSettings.VapidPublicKey, appSettings.VapidPrivateKey)
        {
            Subject = "mailto:admin@rverbist.io",
        };

        var client = new LibPushServiceClient();
        client.DefaultAuthentication = vapid;

        var errors = new List<string>();
        var staleEndpoints = new List<EntityPushSubscription>();

        foreach (var sub in subscriptions)
        {
            var libSub = new LibPushSubscription();
            libSub.Endpoint = sub.Endpoint;
            libSub.SetKey(LibPushEncryptionKeyName.P256DH, sub.P256dh);
            libSub.SetKey(LibPushEncryptionKeyName.Auth, sub.Auth);

            var message = new LibPushMessage(payload)
            {
                Topic = "stock-status",
                TimeToLive = 300,
                Urgency = LibPushMessageUrgency.High,
            };

            try
            {
                await client.RequestPushMessageDeliveryAsync(libSub, message, ct);
            }
            catch (LibPushServiceClientException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone)
            {
                staleEndpoints.Add(sub);
                LogStaleSubscription(logger, sub.Endpoint);
            }
            catch (Exception ex)
            {
                errors.Add(ex.Message);
                LogPushFailed(logger, sub.Endpoint, ex);
            }
        }

        if (staleEndpoints.Count > 0)
        {
            await using var cleanupScope = scopeFactory.CreateAsyncScope();
            var cleanupDb = cleanupScope.ServiceProvider.GetRequiredService<UbiquitiStoreLurkerDbContext>();
            foreach (var stale in staleEndpoints)
            {
                var entity = await cleanupDb.PushSubscriptions
                    .FirstOrDefaultAsync(s => s.Endpoint == stale.Endpoint, ct);
                if (entity is not null) cleanupDb.PushSubscriptions.Remove(entity);
            }
            await cleanupDb.SaveChangesAsync(ct);
        }

        return errors.Count == 0
            ? NotificationResult.Ok()
            : NotificationResult.Fail(string.Join("; ", errors));
    }

    public async Task<NotificationResult> SendTestAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UbiquitiStoreLurkerDbContext>();

        var appSettings = await db.AppSettings.FindAsync([1], ct);
        if (appSettings is null || string.IsNullOrEmpty(appSettings.VapidPublicKey) || string.IsNullOrEmpty(appSettings.VapidPrivateKey))
            return NotificationResult.Fail("VAPID keys not configured");

        var subscriptions = await db.PushSubscriptions.ToListAsync(ct);
        if (subscriptions.Count == 0)
            return NotificationResult.Ok();

        var payload = JsonSerializer.Serialize(new
        {
            title = "🔔 Test Notification",
            body = "Stock Monitor push notifications are working!",
            url = "/",
            icon = "/icon-192.png",
            badge = "/badge-96.png",
            tag = "test",
            renotify = true,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            requireInteraction = false,
            vibrate = VibratePattern,
        });

        var vapid = new VapidAuthentication(appSettings.VapidPublicKey, appSettings.VapidPrivateKey)
        {
            Subject = "mailto:admin@rverbist.io",
        };

        var client = new LibPushServiceClient();
        client.DefaultAuthentication = vapid;

        var errors = new List<string>();
        var staleEndpoints = new List<EntityPushSubscription>();

        foreach (var sub in subscriptions)
        {
            var libSub = new LibPushSubscription();
            libSub.Endpoint = sub.Endpoint;
            libSub.SetKey(LibPushEncryptionKeyName.P256DH, sub.P256dh);
            libSub.SetKey(LibPushEncryptionKeyName.Auth, sub.Auth);

            var message = new LibPushMessage(payload)
            {
                Topic = "stock-test",
                TimeToLive = 300,
                Urgency = LibPushMessageUrgency.High,
            };

            try
            {
                await client.RequestPushMessageDeliveryAsync(libSub, message, ct);
            }
            catch (LibPushServiceClientException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone)
            {
                staleEndpoints.Add(sub);
                LogStaleSubscription(logger, sub.Endpoint);
            }
            catch (Exception ex)
            {
                errors.Add(ex.Message);
                LogPushFailed(logger, sub.Endpoint, ex);
            }
        }

        if (staleEndpoints.Count > 0)
        {
            await using var cleanupScope = scopeFactory.CreateAsyncScope();
            var cleanupDb = cleanupScope.ServiceProvider.GetRequiredService<UbiquitiStoreLurkerDbContext>();
            foreach (var stale in staleEndpoints)
            {
                var entity = await cleanupDb.PushSubscriptions
                    .FirstOrDefaultAsync(s => s.Endpoint == stale.Endpoint, ct);
                if (entity is not null) cleanupDb.PushSubscriptions.Remove(entity);
            }
            await cleanupDb.SaveChangesAsync(ct);
        }

        return errors.Count == 0
            ? NotificationResult.Ok()
            : NotificationResult.Fail(string.Join("; ", errors));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Push subscription {Endpoint} expired (410 Gone) — removing")]
    private static partial void LogStaleSubscription(ILogger logger, string endpoint);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Push to {Endpoint} failed")]
    private static partial void LogPushFailed(ILogger logger, string endpoint, Exception ex);
}
