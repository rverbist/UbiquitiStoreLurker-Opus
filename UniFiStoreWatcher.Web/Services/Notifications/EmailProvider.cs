using System.Diagnostics;
using System.Text.Json;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using UniFiStoreWatcher.Web.Telemetry;

namespace UniFiStoreWatcher.Web.Services.Notifications;

public sealed record EmailSettings(
    string Host,
    int Port,
    bool UseTls,
    string Username,
    string Password,
    string FromAddress,
    string ToAddress);

public sealed partial class EmailProvider(ILogger<EmailProvider> logger) : INotificationProvider
{
    public string ProviderType => "Email";

    public bool ValidateConfig(string? settingsJson, out string? configError)
    {
        configError = null;
        if (string.IsNullOrWhiteSpace(settingsJson))
        {
            configError = "Email settings are required";
            return false;
        }

        try
        {
            var settings = JsonSerializer.Deserialize<EmailSettings>(settingsJson);
            if (settings is null || string.IsNullOrWhiteSpace(settings.Host))
            {
                configError = "Invalid email settings: Host is required";
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
        using var activity = UniFiStoreWatcherActivities.Source.StartActivity("notification.send.email", ActivityKind.Internal);
        activity?.SetTag("provider.type", ProviderType);

        if (!ValidateConfig(context.Config.SettingsJson, out var configError))
        {
            activity?.SetTag("provider.success", false);
            activity?.SetTag("provider.error", $"Invalid config: {configError}");
            return NotificationResult.Fail($"Invalid config: {configError}");
        }

        var settings = JsonSerializer.Deserialize<EmailSettings>(context.Config.SettingsJson!)!;
        var product = context.Product;
        var transition = context.Transition;

        try
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(settings.FromAddress));
            message.To.Add(MailboxAddress.Parse(settings.ToAddress));
            message.Subject = $"[UniFiStoreWatcher] {product.Name ?? product.Url} is now {transition.ToState}";
            message.Body = new TextPart("plain")
            {
                Text = $"""
                    Stock Alert!

                    Product: {product.Name ?? product.Url}
                    Previous state: {transition.FromState}
                    New state: {transition.ToState}
                    Detected at: {transition.DetectedAtUtc:u}

                    View product: {product.Url}
                    """,
            };

            using var client = new SmtpClient();
            var secureOptions = settings.UseTls
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;

            await client.ConnectAsync(settings.Host, settings.Port, secureOptions, ct);
            await client.AuthenticateAsync(settings.Username, settings.Password, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(quit: true, ct);

            LogEmailSent(logger, product.ProductCode, settings.ToAddress);
            activity?.SetTag("provider.success", true);
            return NotificationResult.Ok();
        }
        catch (Exception ex)
        {
            LogEmailFailed(logger, ex);
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Email sent for product {ProductCode} to {ToAddress}")]
    private static partial void LogEmailSent(ILogger logger, string? productCode, string toAddress);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to send email notification")]
    private static partial void LogEmailFailed(ILogger logger, Exception ex);
}
