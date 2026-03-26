using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using UniFiStoreWatcher.Web.Data.Entities;
using UniFiStoreWatcher.Web.Telemetry;

namespace UniFiStoreWatcher.Web.Services.Notifications;

public sealed record DiscordSettings(string WebhookUrl);

public sealed partial class DiscordWebhookProvider(
    IHttpClientFactory httpClientFactory,
    ILogger<DiscordWebhookProvider> logger) : INotificationProvider
{
    public string ProviderType => "Discord";

    public bool ValidateConfig(string? settingsJson, out string? configError)
    {
        configError = null;
        if (string.IsNullOrWhiteSpace(settingsJson))
        {
            configError = "Discord webhook URL is required";
            return false;
        }

        try
        {
            var settings = JsonSerializer.Deserialize<DiscordSettings>(settingsJson);
            if (string.IsNullOrWhiteSpace(settings?.WebhookUrl) || !Uri.IsWellFormedUriString(settings.WebhookUrl, UriKind.Absolute))
            {
                configError = "Invalid webhook URL";
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
        using var activity = UniFiStoreWatcherActivities.Source.StartActivity("notification.send.discord", ActivityKind.Internal);
        activity?.SetTag("provider.type", ProviderType);

        if (!ValidateConfig(context.Config.SettingsJson, out var configError))
        {
            activity?.SetTag("provider.success", false);
            activity?.SetTag("provider.error", $"Invalid config: {configError}");
            return NotificationResult.Fail($"Invalid config: {configError}");
        }

        var settings = JsonSerializer.Deserialize<DiscordSettings>(context.Config.SettingsJson!)!;
        var product = context.Product;
        var transition = context.Transition;

        var color = transition.ToState == StockState.InStock
            ? 0x2FB343  // green
            : 0xF44336; // red

        try
        {
            var client = httpClientFactory.CreateClient();

            var payload = new
            {
                embeds = new[]
                {
                    new
                    {
                        title = "Stock Alert!",
                        description = $"**{product.Name ?? product.Url}** is now **{transition.ToState}**",
                        color,
                        fields = new[]
                        {
                            new { name = "Previous State", value = transition.FromState.ToString(), inline = true },
                            new { name = "New State", value = transition.ToState.ToString(), inline = true },
                        },
                        url = product.Url,
                        timestamp = transition.DetectedAtUtc.ToString("o"),
                    },
                },
            };

            var response = await client.PostAsJsonAsync(settings.WebhookUrl, payload, ct);
            response.EnsureSuccessStatusCode();

            LogDiscordSent(logger, product.ProductCode);
            activity?.SetTag("provider.success", true);
            return NotificationResult.Ok();
        }
        catch (Exception ex)
        {
            LogDiscordFailed(logger, ex);
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Discord notification sent for product {ProductCode}")]
    private static partial void LogDiscordSent(ILogger logger, string? productCode);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to send Discord notification")]
    private static partial void LogDiscordFailed(ILogger logger, Exception ex);
}
