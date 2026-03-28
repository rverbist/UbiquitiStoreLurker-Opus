using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using UniFiStoreWatcher.Web.Http;
using UniFiStoreWatcher.Web.Services.Polling;

namespace UniFiStoreWatcher.Tests.Http;

/// <summary>
/// Unit tests for BrowserFingerprintHandler and UbiquitiCookieJar/Handler.
/// </summary>
[TestFixture]
public class HttpHandlerTests
{
    // ──────────────────────────────────────────────────────────────────────────
    // BrowserFingerprintHandler tests
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task BrowserFingerprintHandler_SetsUserAgentHeader()
    {
        HttpRequestMessage? captured = null;
        var inner = new CapturingHandler(req => { captured = req; return new HttpResponseMessage(HttpStatusCode.OK); });

        var handler = new BrowserFingerprintHandler(Options.Create(new PollOptions()))
        {
            InnerHandler = inner,
        };

        using var client = new HttpClient(handler);
        await client.GetAsync("http://localhost/");

        Assert.That(captured, Is.Not.Null);
        var ua = captured!.Headers.UserAgent.ToString();
        Assert.That(ua, Does.Contain("Chrome/124"));
        Assert.That(ua, Does.Contain("AppleWebKit/537.36"));
    }

    [Test]
    public async Task BrowserFingerprintHandler_SetsAllRequiredBrowserHeaders()
    {
        HttpRequestMessage? captured = null;
        var inner = new CapturingHandler(req => { captured = req; return new HttpResponseMessage(HttpStatusCode.OK); });

        var handler = new BrowserFingerprintHandler(Options.Create(new PollOptions()))
        {
            InnerHandler = inner,
        };

        using var client = new HttpClient(handler);
        await client.GetAsync("http://localhost/");

        Assert.That(captured, Is.Not.Null);

        // Verify each required browser fingerprint header is present.
        Assert.Multiple(() =>
        {
            Assert.That(captured!.Headers.Contains("Accept"), Is.True, "Accept header missing");
            Assert.That(captured.Headers.Contains("Accept-Language"), Is.True, "Accept-Language header missing");
            Assert.That(captured.Headers.Contains("Accept-Encoding"), Is.True, "Accept-Encoding header missing");
            Assert.That(captured.Headers.Contains("Sec-Fetch-Dest"), Is.True, "Sec-Fetch-Dest header missing");
            Assert.That(captured.Headers.Contains("Sec-Fetch-Mode"), Is.True, "Sec-Fetch-Mode header missing");
            Assert.That(captured.Headers.Contains("Sec-Fetch-Site"), Is.True, "Sec-Fetch-Site header missing");
            Assert.That(captured.Headers.Contains("Sec-Fetch-User"), Is.True, "Sec-Fetch-User header missing");
            Assert.That(captured.Headers.Contains("DNT"), Is.True, "DNT header missing");
            Assert.That(captured.Headers.Contains("Cache-Control"), Is.True, "Cache-Control header missing");
        });

        // Accept must advertise HTML content types.
        var accept = string.Join(",", captured!.Headers.Accept.Select(a => a.ToString()));
        Assert.That(accept, Does.Contain("text/html"));
    }

    [Test]
    public async Task BrowserFingerprintHandler_FallsBackToChrome_WhenUserAgentIsLegacyBotString()
    {
        HttpRequestMessage? captured = null;
        var inner = new CapturingHandler(req => { captured = req; return new HttpResponseMessage(HttpStatusCode.OK); });

        // Old-style bot user agent that should trigger the fallback.
        var options = Options.Create(new PollOptions { BrowserUserAgent = "UniFiStoreWatcher/1.0 (+github)" });
        var handler = new BrowserFingerprintHandler(options) { InnerHandler = inner };

        using var client = new HttpClient(handler);
        await client.GetAsync("http://localhost/");

        var ua = captured!.Headers.UserAgent.ToString();
        Assert.That(ua, Does.Contain("Chrome/124"), "Should fall back to Chrome UA");
        Assert.That(ua, Does.Not.Contain("UniFiStoreWatcher"), "Should not expose bot identity");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UbiquitiCookieJar / UbiquitiCookieHandler tests
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task UbiquitiCookieHandler_SeedsThreeRequiredCookies()
    {
        var jar = CreateJar();
        HttpRequestMessage? captured = null;
        var inner = new CapturingHandler(req =>
        {
            captured = req;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var handler = new UbiquitiCookieHandler(jar) { InnerHandler = inner };
        using var client = new HttpClient(handler);
        await client.GetAsync("https://eu.store.ui.com/eu/en/products/test");

        Assert.That(captured, Is.Not.Null);
        var cookieHeader = captured!.Headers.GetValues("Cookie").FirstOrDefault() ?? string.Empty;

        Assert.Multiple(() =>
        {
            Assert.That(cookieHeader, Does.Contain("curr_language=en"), "curr_language cookie missing");
            Assert.That(cookieHeader, Does.Contain("curr_store=eu"), "curr_store cookie missing");
            Assert.That(cookieHeader, Does.Contain("store_modal_shown=true"), "store_modal_shown cookie missing");
        });
    }

    [Test]
    public async Task UbiquitiCookieHandler_PersistsServerSetCookies()
    {
        var jar = CreateJar();

        // First request: server sets a new session cookie.
        var responseWithCookie = new HttpResponseMessage(HttpStatusCode.OK);
        responseWithCookie.Headers.Add("Set-Cookie",
            "eu_has_ui_care=true; Path=/; Secure; SameSite=Lax; Max-Age=300");

        int callCount = 0;
        HttpRequestMessage? secondRequest = null;
        var inner = new CapturingHandler(req =>
        {
            callCount++;
            if (callCount == 1) return responseWithCookie;
            secondRequest = req;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var handler = new UbiquitiCookieHandler(jar) { InnerHandler = inner };
        using var client = new HttpClient(handler);

        await client.GetAsync("https://eu.store.ui.com/eu/en/products/test");
        // Wait a tick to allow fire-and-forget persistence
        await Task.Delay(50);

        // Second request: the server-set cookie must be replayed.
        await client.GetAsync("https://eu.store.ui.com/eu/en/products/test2");

        var cookieHeader = secondRequest?.Headers.GetValues("Cookie").FirstOrDefault() ?? string.Empty;
        Assert.That(cookieHeader, Does.Contain("eu_has_ui_care=true"),
            "Server-set cookie was not captured and replayed on next request");
    }

    [Test]
    public void UbiquitiCookieJar_ReloadsPersistedCookiesFromDisk()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"test-cookies-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempPath);
        var cookieFile = Path.Combine(tempPath, "http-cookies.json");

        try
        {
            // Write a persisted jar with a custom cookie.
            var jar = new
            {
                SavedAtUtc = DateTimeOffset.UtcNow,
                Cookies = new[]
                {
                    new { Name = "test_session", Value = "abc123", Domain = "eu.store.ui.com",
                          Path = "/", Secure = true, HttpOnly = false, Expires = DateTimeOffset.MinValue }
                }
            };
            File.WriteAllText(cookieFile, System.Text.Json.JsonSerializer.Serialize(jar));

            // Build a UbiquitiCookieJar pointing at that temp cookie file.
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CookieJar:PersistPath"] = cookieFile
                })
                .Build();

            var testJar = new UbiquitiCookieJar(config, NullLogger<UbiquitiCookieJar>.Instance);

            var cookieHeader = testJar.GetCookieHeader(new Uri("https://eu.store.ui.com/"));
            Assert.That(cookieHeader, Does.Contain("test_session=abc123"),
                "Persisted cookie was not restored on startup");
        }
        finally
        {
            Directory.Delete(tempPath, recursive: true);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static UbiquitiCookieJar CreateJar()
    {
        // No CookieJar:PersistPath configured — cookie jar operates without persistence.
        var config = new ConfigurationBuilder().Build();
        return new UbiquitiCookieJar(config, NullLogger<UbiquitiCookieJar>.Instance);
    }

    private sealed class CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> factory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(factory(request));
    }
}
