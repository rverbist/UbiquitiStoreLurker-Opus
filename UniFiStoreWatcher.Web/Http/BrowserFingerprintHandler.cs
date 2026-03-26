using Microsoft.Extensions.Options;
using UniFiStoreWatcher.Web.Services.Polling;

namespace UniFiStoreWatcher.Web.Http;

/// <summary>
/// Sets browser-realistic request headers indistinguishable from Chrome 124 on Windows 10.
/// Replaces the old UserAgentHandler. Registered as transient via DI.
/// </summary>
public sealed class BrowserFingerprintHandler(IOptions<PollOptions> options) : DelegatingHandler
{
    internal const string DefaultUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var ua = options.Value.BrowserUserAgent;

        // Fall back to Chrome UA if blank or still set to the old bot-identifiable default.
        if (string.IsNullOrWhiteSpace(ua) ||
            ua.StartsWith("UniFiStoreWatcher/", StringComparison.OrdinalIgnoreCase))
        {
            ua = DefaultUserAgent;
        }

        request.Headers.TryAddWithoutValidation("User-Agent", ua);
        request.Headers.TryAddWithoutValidation("Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif," +
            "image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
        request.Headers.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.9");
        // Accept-Encoding: HttpClient's automatic decompression handles gzip/br/deflate;
        // advertising them here keeps the fingerprint realistic.
        request.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate, br, zstd");
        request.Headers.TryAddWithoutValidation("Cache-Control", "max-age=0");
        request.Headers.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");
        request.Headers.TryAddWithoutValidation("Sec-Fetch-Dest", "document");
        request.Headers.TryAddWithoutValidation("Sec-Fetch-Mode", "navigate");
        // Sec-Fetch-Site: none = direct URL navigation (typed or bookmark)
        request.Headers.TryAddWithoutValidation("Sec-Fetch-Site", "none");
        request.Headers.TryAddWithoutValidation("Sec-Fetch-User", "?1");
        request.Headers.TryAddWithoutValidation("DNT", "1");

        return base.SendAsync(request, cancellationToken);
    }
}
