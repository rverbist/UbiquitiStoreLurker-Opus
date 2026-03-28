using System.Net;
using System.Net.Http.Json;
using UniFiStoreWatcher.Web.Endpoints;

namespace UniFiStoreWatcher.Tests.Api;

[TestFixture]
public class SettingsEndpointTests
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
    public async Task GetSettings_ReturnsOk_WithDefaults()
    {
        var response = await _client.GetAsync("/api/settings");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var settings = await response.Content.ReadFromJsonAsync<AppSettingsDto>();
        Assert.That(settings, Is.Not.Null);
        Assert.That(settings!.PollIntervalMinSeconds, Is.EqualTo(30));
    }

    [Test]
    public async Task UpdateSettings_ChangesNickname()
    {
        var response = await _client.PutAsJsonAsync("/api/settings",
            new UpdateSettingsRequest(Nickname: "My Monitor", null, null, null, null, null, null, null));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var settings = await response.Content.ReadFromJsonAsync<AppSettingsDto>();
        Assert.That(settings!.Nickname, Is.EqualTo("My Monitor"));
    }
}
