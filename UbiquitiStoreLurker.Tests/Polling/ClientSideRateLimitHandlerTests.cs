using System.Diagnostics;
using System.Net;
using UbiquitiStoreLurker.Web.Http;

namespace UbiquitiStoreLurker.Tests.Polling;

[TestFixture]
public class ClientSideRateLimitHandlerTests
{
    [Test]
    public async Task FirstRequest_Passes()
    {
        var inner = new EchoHandler();
        var handler = new ClientSideRateLimitHandler(minGapMs: 100, jitterPercent: 0)
        {
            InnerHandler = inner,
        };

        using var client = new HttpClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/");
        using var response = await client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task SecondRequest_IsQueued_AndCompletesAfterGap()
    {
        // The new handler NEVER rejects — it always queues and waits the gap.
        const int gapMs = 300;
        var inner = new EchoHandler();
        var handler = new ClientSideRateLimitHandler(minGapMs: gapMs, jitterPercent: 0)
        {
            InnerHandler = inner,
        };

        using var client = new HttpClient(handler);

        // Issue the first request to consume the free slot.
        await client.GetAsync("http://localhost/");

        // Second request must be queued (not rejected) and arrive after the gap.
        var sw = Stopwatch.StartNew();
        using var res2 = await client.GetAsync("http://localhost/2");
        sw.Stop();

        Assert.Multiple(() =>
        {
            // Must succeed, never return 429.
            Assert.That(res2.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            // Must have waited at least 60 % of gapMs (accounting for timer resolution).
            Assert.That(sw.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(gapMs * 0.6),
                $"Second request should have waited ≥{gapMs * 0.6} ms");
        });
    }

    private sealed class EchoHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
