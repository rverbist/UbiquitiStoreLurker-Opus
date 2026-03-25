using System.Net;
using System.Net.Http.Json;
using UbiquitiStoreLurker.Web.Endpoints;

namespace UbiquitiStoreLurker.Tests.Api;

[TestFixture]
public class NotificationEndpointTests
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
    public async Task GetConfigs_ReturnsSeededProviders()
    {
        var response = await _client.GetAsync("/api/notifications/configs");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var configs = await response.Content.ReadFromJsonAsync<List<NotificationConfigDto>>();
        Assert.That(configs, Has.Count.EqualTo(5));
        Assert.That(configs!.Select(c => c.ProviderType),
            Does.Contain("BrowserPush").And.Contains("Email").And.Contains("Discord"));
    }

    [Test]
    public async Task UpdateConfig_Enables_Provider()
    {
        // Get email config (id=2)
        var response = await _client.PutAsJsonAsync("/api/notifications/configs/2",
            new UpdateNotificationConfigRequest(IsEnabled: true, null));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var config = await response.Content.ReadFromJsonAsync<NotificationConfigDto>();
        Assert.That(config!.IsEnabled, Is.True);
    }

    [Test]
    public async Task GetLogs_ReturnsOk_WithEmptyList()
    {
        var response = await _client.GetAsync("/api/notifications/logs");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
