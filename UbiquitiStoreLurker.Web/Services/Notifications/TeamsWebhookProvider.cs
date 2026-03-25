using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using UbiquitiStoreLurker.Web.Telemetry;

namespace UbiquitiStoreLurker.Web.Services.Notifications;

public sealed record TeamsSettings(string WebhookUrl);

public sealed partial class TeamsWebhookProvider(
    IHttpClientFactory httpClientFactory,
    ILogger<TeamsWebhookProvider> logger) : INotificationProvider
{
    public string ProviderType => "Teams";

    public bool ValidateConfig(string? settingsJson, out string? configError)
    {
        configError = null;
        if (string.IsNullOrWhiteSpace(settingsJson))
        {
            configError = "Teams webhook URL is required";
            return false;
        }

        try
        {
            var settings = JsonSerializer.Deserialize<TeamsSettings>(settingsJson);
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
        using var activity = UbiquitiStoreLurkerActivities.Source.StartActivity("notification.send.teams", ActivityKind.Internal);
        activity?.SetTag("provider.type", ProviderType);

        if (!ValidateConfig(context.Config.SettingsJson, out var configError))
        {
            activity?.SetTag("provider.success", false);
            activity?.SetTag("provider.error", $"Invalid config: {configError}");
            return NotificationResult.Fail($"Invalid config: {configError}");
        }

        var settings = JsonSerializer.Deserialize<TeamsSettings>(context.Config.SettingsJson!)!;
        var product = context.Product;
        var transition = context.Transition;

        try
        {
            var client = httpClientFactory.CreateClient();

            // Teams Adaptive Card payload (Workflows-compatible)
            var payload = new
            {
                type = "message",
                attachments = new[]
                {
                    new
                    {
                        contentType = "application/vnd.microsoft.card.adaptive",
                        content = new
                        {
                            type = "AdaptiveCard",
                            version = "1.4",
                            body = new object[]
                            {
                                new { type = "TextBlock", size = "Large", weight = "Bolder", text = "Stock Alert!" },
                                new { type = "TextBlock", text = $"**{product.Name ?? product.Url}** is now **{transition.ToState}**" },
                                new { type = "TextBlock", text = $"{transition.FromState} → {transition.ToState}", isSubtle = true },
                            },
                            actions = new[]
                            {
                                new { type = "Action.OpenUrl", title = "View Product", url = product.Url },
                            },
                        },
                    },
                },
            };

            var response = await client.PostAsJsonAsync(settings.WebhookUrl, payload, ct);
            response.EnsureSuccessStatusCode();

            LogTeamsSent(logger, product.ProductCode);
            activity?.SetTag("provider.success", true);
            return NotificationResult.Ok();
        }
        catch (Exception ex)
        {
            LogTeamsFailed(logger, ex);
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Teams notification sent for product {ProductCode}")]
    private static partial void LogTeamsSent(ILogger logger, string? productCode);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to send Teams notification")]
    private static partial void LogTeamsFailed(ILogger logger, Exception ex);
}
