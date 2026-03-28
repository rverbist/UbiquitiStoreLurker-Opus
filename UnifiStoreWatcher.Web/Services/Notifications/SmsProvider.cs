using System.Diagnostics;
using System.Text.Json;
using UniFiStoreWatcher.Web.Telemetry;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace UniFiStoreWatcher.Web.Services.Notifications;

public sealed record SmsSettings(string AccountSid, string AuthToken, string FromPhone, string ToPhone);

public sealed partial class SmsProvider(ILogger<SmsProvider> logger) : INotificationProvider
{
    public string ProviderType => "Sms";

    public bool ValidateConfig(string? settingsJson, out string? configError)
    {
        configError = null;
        if (string.IsNullOrWhiteSpace(settingsJson))
        {
            configError = "SMS settings are required";
            return false;
        }

        try
        {
            var settings = JsonSerializer.Deserialize<SmsSettings>(settingsJson);
            if (settings is null || string.IsNullOrWhiteSpace(settings.AccountSid))
            {
                configError = "AccountSid is required";
                return false;
            }
        }
        catch (JsonException ex)
        {
            configError = $"Invalid JSON: {ex.Message}";
            return false;
        }

        return true;
    }

    public async Task<NotificationResult> SendAsync(NotificationContext context, CancellationToken ct)
    {
        using var activity = UniFiStoreWatcherActivities.Source.StartActivity("notification.send.sms", ActivityKind.Internal);
        activity?.SetTag("provider.type", ProviderType);

        if (!ValidateConfig(context.Config.SettingsJson, out var configError))
        {
            activity?.SetTag("provider.success", false);
            activity?.SetTag("provider.error", $"Invalid config: {configError}");
            return NotificationResult.Fail($"Invalid config: {configError}");
        }

        var settings = JsonSerializer.Deserialize<SmsSettings>(context.Config.SettingsJson!)!;
        var product = context.Product;
        var transition = context.Transition;

        try
        {
            TwilioClient.Init(settings.AccountSid, settings.AuthToken);

            var message = await MessageResource.CreateAsync(
                body: $"[UniFiStoreWatcher] {product.Name ?? product.ProductCode} is now {transition.ToState}! {product.Url}",
                from: new Twilio.Types.PhoneNumber(settings.FromPhone),
                to: new Twilio.Types.PhoneNumber(settings.ToPhone));

            LogSmsSent(logger, product.ProductCode, message.Sid);
            activity?.SetTag("provider.success", true);
            return NotificationResult.Ok();
        }
        catch (Exception ex)
        {
            LogSmsFailed(logger, ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
            {
                ["exception.type"] = ex.GetType().FullName,
                ["exception.message"] = ex.Message,
            }));
            activity?.SetTag("provider.success", false);
            activity?.SetTag("provider.error", ex.Message);
            return NotificationResult.Fail(ex.Message);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "SMS sent for product {ProductCode}, Twilio SID={Sid}")]
    private static partial void LogSmsSent(ILogger logger, string? productCode, string sid);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to send SMS notification")]
    private static partial void LogSmsFailed(ILogger logger, Exception ex);
}
