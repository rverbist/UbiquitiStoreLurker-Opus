namespace UnifiStoreWatcher.Web.Services;

public sealed partial class ProductImageService(
    IHttpClientFactory httpClientFactory,
    IWebHostEnvironment env,
    ILogger<ProductImageService> logger)
{
    private const long MaxImageBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly Dictionary<string, string> ContentTypeToExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/gif"] = ".gif",
        ["image/webp"] = ".webp",
        ["image/svg+xml"] = ".svg",
        ["image/avif"] = ".avif",
    };

    public async Task<string?> DownloadAndCacheAsync(int productId, string imageUrl, CancellationToken ct = default)
    {
        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            LogInvalidUrl(logger, imageUrl);
            return null;
        }

        string? tempPath = null;
        try
        {
            var client = httpClientFactory.CreateClient("UniFiStoreWatchPoller");
            using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!response.IsSuccessStatusCode)
            {
                LogDownloadFailed(logger, imageUrl, (int)response.StatusCode);
                return null;
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
            if (!ContentTypeToExtension.TryGetValue(contentType, out var ext))
            {
                LogUnsupportedContentType(logger, contentType, imageUrl);
                return null;
            }

            var imagesDir = Path.Combine(env.WebRootPath, "images", "products");
            Directory.CreateDirectory(imagesDir);

            var fileName = $"{productId}{ext}";
            var filePath = Path.Combine(imagesDir, fileName);
            tempPath = filePath + ".tmp";

            await using var networkStream = await response.Content.ReadAsStreamAsync(ct);
            await using (var fileStream = File.Create(tempPath))
            {
                var buf = new byte[81920];
                long written = 0;
                int read;
                while ((read = await networkStream.ReadAsync(buf, ct)) > 0)
                {
                    written += read;
                    if (written > MaxImageBytes)
                    {
                        LogImageTooLarge(logger, imageUrl, written);
                        return null; // finally block cleans up tempPath
                    }
                    await fileStream.WriteAsync(buf.AsMemory(0, read), ct);
                }
            }

            File.Move(tempPath, filePath, overwrite: true);
            tempPath = null; // move succeeded, nothing left to clean up

            var webPath = $"/images/products/{fileName}";
            LogImageCached(logger, imageUrl, webPath);
            return webPath;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogDownloadError(logger, ex, imageUrl);
            return null;
        }
        finally
        {
            if (tempPath is not null && File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Image URL is not a valid HTTPS URL: {Url}")]
    private static partial void LogInvalidUrl(ILogger logger, string url);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Image download failed with status {StatusCode}: {Url}")]
    private static partial void LogDownloadFailed(ILogger logger, string url, int statusCode);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Unsupported image content type '{ContentType}' for {Url}")]
    private static partial void LogUnsupportedContentType(ILogger logger, string contentType, string url);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Image exceeds size limit ({Bytes} bytes) for {Url}")]
    private static partial void LogImageTooLarge(ILogger logger, string url, long bytes);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cached image from {Url} → {LocalPath}")]
    private static partial void LogImageCached(ILogger logger, string url, string localPath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to download image from {Url}")]
    private static partial void LogDownloadError(ILogger logger, Exception ex, string url);
}
