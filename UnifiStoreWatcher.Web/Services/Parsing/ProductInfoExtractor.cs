using System.Text.Json;
using AngleSharp;
using AngleSharp.Dom;

namespace UniFiStoreWatcher.Web.Services.Parsing;

public sealed record ProductInfo(
    string? Name,
    string? ProductCode,
    string? Description,
    string? ImageUrl,
    string[]? ImageUrls,
    string? Price,
    string? PriceCurrency);

public sealed partial class ProductInfoExtractor(ILogger<ProductInfoExtractor> logger)
{
    public async Task<ProductInfo> ExtractAsync(string html, CancellationToken ct = default)
    {
        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(html), ct);
        return ExtractFromDocument(document);
    }

    public ProductInfo ExtractFromDocument(IDocument document)
    {
        var jsonLdInfo = TryExtractFromJsonLd(document);
        if (jsonLdInfo is not null) return jsonLdInfo;

        var name = GetMeta(document, "og:title")
            ?? document.QuerySelector("h1")?.TextContent?.Trim();

        var description = GetMeta(document, "og:description");
        var imageUrl = GetMeta(document, "og:image");
        var productCode = GetMeta(document, "product:sku");

        return new ProductInfo(name, productCode, description, imageUrl, null, null, null);
    }

    private ProductInfo? TryExtractFromJsonLd(IDocument document)
    {
        var scripts = document.QuerySelectorAll("script[type='application/ld+json']");

        foreach (var script in scripts)
        {
            try
            {
                using var doc = JsonDocument.Parse(script.TextContent.Trim());
                var root = doc.RootElement;

                if (!root.TryGetProperty("@type", out var typeEl) ||
                    !string.Equals(typeEl.GetString(), "Product", StringComparison.OrdinalIgnoreCase))
                    continue;

                var name = TryGetString(root, "name");
                var sku = TryGetString(root, "sku");
                var description = TryGetString(root, "description");

                string? imageUrl = null;
                string[]? imageUrls = null;
                if (root.TryGetProperty("image", out var imageProp))
                {
                    if (imageProp.ValueKind == JsonValueKind.Array)
                    {
                        imageUrls = imageProp.EnumerateArray()
                            .Select(i => i.ValueKind == JsonValueKind.Object
                                ? TryGetString(i, "url")
                                : i.GetString())
                            .Where(s => s is not null)
                            .ToArray()!;
                        imageUrl = imageUrls.FirstOrDefault();
                    }
                    else if (imageProp.ValueKind == JsonValueKind.Object)
                    {
                        // ImageObject: { "@type": "ImageObject", "url": "..." }
                        imageUrl = TryGetString(imageProp, "url");
                    }
                    else if (imageProp.ValueKind == JsonValueKind.String)
                    {
                        imageUrl = imageProp.GetString();
                    }
                }

                string? price = null;
                string? currency = null;
                if (root.TryGetProperty("offers", out var offers))
                {
                    var offersEl = offers.ValueKind == JsonValueKind.Array ? offers[0] : offers;
                    price = TryGetString(offersEl, "price");
                    currency = TryGetString(offersEl, "priceCurrency");
                }

                return new ProductInfo(name, sku, description, imageUrl, imageUrls, price, currency);
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException)
            {
                LogJsonLdProductInfoFailed(logger, ex);
            }
        }

        return null;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Failed to parse JSON-LD for product info extraction")]
    private static partial void LogJsonLdProductInfoFailed(ILogger logger, Exception ex);

    private static string? GetMeta(IDocument document, string property)
    {
        var el = document.QuerySelector($"meta[property='{property}']")
            ?? document.QuerySelector($"meta[name='{property}']");
        return el?.GetAttribute("content");
    }

    private static string? TryGetString(JsonElement el, string propertyName)
    {
        return el.TryGetProperty(propertyName, out var prop) ? prop.GetString() : null;
    }
}
