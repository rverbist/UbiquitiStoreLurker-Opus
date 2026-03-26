using System.Net;
using System.Net.Http.Headers;
using UniFiStoreWatcher.Web.Metrics;

namespace UniFiStoreWatcher.Web.Http;

/// <summary>
/// Rate-limiting delegating handler that enforces a minimum jittered gap between successive
/// outbound requests so the Ubiquiti EU store is never triggered to issue a 429.
///
/// Design:
/// - A SemaphoreSlim serialises all requests (one at a time).
/// - Before sending, waits until <c>_nextAllowedAtMs</c> has elapsed.
/// - After a successful response the gap grows by a random ±<c>jitterPercent</c>%.
/// - After a server 429 with a Retry-After header the gap is extended by the server's
///   requested delay so all subsequent requests are suspended for that duration.
/// - NEVER emits a synthetic 429 — the old token-bucket reject behaviour is gone.
/// </summary>
public sealed partial class ClientSideRateLimitHandler : DelegatingHandler
{
    private readonly int _minGapMs;
    private readonly int _jitterPercent;
    private readonly ILogger<ClientSideRateLimitHandler>? _logger;

    // SemaphoreSlim(1,1) = one request at a time, queue all others.
    private readonly SemaphoreSlim _gate = new(1, 1);

    // Unix epoch milliseconds; time after which the next request is allowed.
    private long _nextAllowedAtMs;

    public ClientSideRateLimitHandler(int minGapMs = 12_000, int jitterPercent = 30,
        ILogger<ClientSideRateLimitHandler>? logger = null)
    {
        _minGapMs = minGapMs;
        _jitterPercent = Math.Clamp(jitterPercent, 0, 100);
        _logger = logger;
        _nextAllowedAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Acquire the gate — callers queue here if another request is in flight.
        await _gate.WaitAsync(cancellationToken);
        try
        {
            // Wait until the earliest allowed time has passed.
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var delayMs = (int)Math.Max(0L, _nextAllowedAtMs - nowMs);
            if (delayMs > 0)
                await Task.Delay(delayMs, cancellationToken);

            var response = await base.SendAsync(request, cancellationToken);

            var afterMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                // Server issued a 429: respect its Retry-After and block ALL subsequent
                // requests for that duration. This is a last-resort guard; normal
                // operation should never reach this branch.
                var retryAfterMs = ParseRetryAfterMs(response);
                _nextAllowedAtMs = afterMs + retryAfterMs;

                UniFiStoreWatcherMetrics.RateLimitServer429Total.Inc();
                if (_logger is not null)
                    Log429Received(_logger, request.RequestUri, retryAfterMs);
            }
            else
            {
                // Successful response: schedule next request with jittered gap.
                var jitteredMs = ApplyJitter(_minGapMs, _jitterPercent);
                _nextAllowedAtMs = afterMs + jitteredMs;
                UniFiStoreWatcherMetrics.PollRequestGapMs.Set(jitteredMs);
            }

            return response;
        }
        finally
        {
            _gate.Release();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _gate.Dispose();
        base.Dispose(disposing);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static int ApplyJitter(int baseMs, int jitterPercent)
    {
        if (jitterPercent == 0) return baseMs;
        var range = (int)(baseMs * jitterPercent / 100.0);
        return baseMs + Random.Shared.Next(-range, range + 1);
    }

    private static long ParseRetryAfterMs(HttpResponseMessage response)
    {
        var ra = response.Headers.RetryAfter;
        if (ra?.Delta is { } delta)
            return (long)delta.TotalMilliseconds;
        if (ra?.Date is { } date)
        {
            var ms = (date - DateTimeOffset.UtcNow).TotalMilliseconds;
            return Math.Max(0L, (long)ms);
        }
        return 30_000; // Conservative 30 s fallback
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Server returned 429 on {Url}. Suspending all poll requests for {RetryAfterMs} ms.")]
    private static partial void Log429Received(ILogger logger, Uri? url, long retryAfterMs);
}
