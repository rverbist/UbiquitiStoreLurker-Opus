using System.Net;

namespace UnifiStoreWatcher.Tests.Api;

[TestFixture]
public class MetricsEndpointTests
{
    private TestApiFactory _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void Setup()
    {
        _factory = new TestApiFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task GetMetrics_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/metrics");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task GetMetrics_ContainsUnifiStoreWatcherMetrics()
    {
        var response = await _client.GetAsync("/api/metrics");
        var body = await response.Content.ReadAsStringAsync();

        Assert.That(body, Does.Contain("UnifiStoreWatcher_checks_total"));
        Assert.That(body, Does.Contain("UnifiStoreWatcher_active_products"));
        Assert.That(body, Does.Contain("UnifiStoreWatcher_monitored_products_total"));
    }

    [Test]
    public async Task GetMetrics_ContainsHttpMetrics()
    {
        // Make a request first so http_requests_received_total is populated
        await _client.GetAsync("/health");
        var response = await _client.GetAsync("/api/metrics");
        var body = await response.Content.ReadAsStringAsync();

        Assert.That(body, Does.Contain("http_requests_received_total").Or.Contains("http_request_duration"));
    }
}
