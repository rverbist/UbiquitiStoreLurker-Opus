using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using UbiquitiStoreLurker.Web.Http;
using UbiquitiStoreLurker.Web.Services.Polling;

namespace UbiquitiStoreLurker.Tests.Http;

/// <summary>
/// Unit tests for BrowserFingerprintHandler, UbiquitiCookieJar/Handler, and the
/// rewritten ClientSideRateLimitHandler.
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
        var options = Options.Create(new PollOptions { BrowserUserAgent = "UbiquitiStoreLurker/1.0 (+github)" });
        var handler = new BrowserFingerprintHandler(options) { InnerHandler = inner };

        using var client = new HttpClient(handler);
        await client.GetAsync("http://localhost/");

        var ua = captured!.Headers.UserAgent.ToString();
        Assert.That(ua, Does.Contain("Chrome/124"), "Should fall back to Chrome UA");
        Assert.That(ua, Does.Not.Contain("UbiquitiStoreLurker"), "Should not expose bot identity");
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

            // Build a UbiquitiCookieJar pointing at that temp DB path.
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Default"] = $"Data Source={Path.Combine(tempPath, "test.db")}"
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
    // ClientSideRateLimitHandler tests
    // ──────────────────────────────────────────────────────────────────────────

    [Test]
    public async Task ClientSideRateLimitHandler_RespectsJitteredGap()
    {
        // Use a small gap with no jitter so timing is deterministic.
        const int gapMs = 300;
        var inner = new EchoHandler();
        var handler = new ClientSideRateLimitHandler(minGapMs: gapMs, jitterPercent: 0)
        {
            InnerHandler = inner,
        };

        using var client = new HttpClient(handler);

        // First request — consumes the "free" slot and sets the gap.
        await client.GetAsync("http://localhost/");

        // Measure time for the second request (must wait ≥ gapMs).
        var sw = Stopwatch.StartNew();
        await client.GetAsync("http://localhost/");
        sw.Stop();

        // Allow 30% under-measurement tolerance (timer resolution, CI jitter).
        Assert.That(sw.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(gapMs * 0.6),
            $"Expected second request to wait at least {gapMs * 0.6} ms, took {sw.ElapsedMilliseconds} ms");
    }

    [Test]
    public async Task ClientSideRateLimitHandler_BlocksAllRequestsOn429WithRetryAfter()
    {
        // Server returns 429 with Retry-After: 30 s on the first request.
        var responses = new Queue<HttpResponseMessage>();
        var tooManyRequests = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        tooManyRequests.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(
            TimeSpan.FromSeconds(30));
        responses.Enqueue(tooManyRequests);
        responses.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)); // should never be reached in time

        var inner = new QueuedResponseHandler(responses);
        // Use a very short base gap so only the Retry-After matters.
        var handler = new ClientSideRateLimitHandler(minGapMs: 50, jitterPercent: 0)
        {
            InnerHandler = inner,
        };

        using var client = new HttpClient(handler);

        // Issue first request → receives 429.
        await client.GetAsync("http://localhost/");

        // Second request must be blocked for 30 s; cancel after 500 ms to prove it's waiting.
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        Assert.That(
            async () => await client.GetAsync("http://localhost/", cts.Token),
            Throws.InstanceOf<OperationCanceledException>(),
            "Second request should have been blocked by the Retry-After delay");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static UbiquitiCookieJar CreateJar()
    {
        // Point at an in-memory path so no files are written during unit tests.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = "" })
            .Build();
        return new UbiquitiCookieJar(config, NullLogger<UbiquitiCookieJar>.Instance);
    }

    private sealed class CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> factory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(factory(request));
    }

    private sealed class EchoHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }

    private sealed class QueuedResponseHandler(Queue<HttpResponseMessage> responses) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responses.Dequeue());
    }
}
