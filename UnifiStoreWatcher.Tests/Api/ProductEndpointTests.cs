using System.Net;
using System.Net.Http.Json;
using UniFiStoreWatcher.Web.Endpoints;

namespace UniFiStoreWatcher.Tests.Api;

[TestFixture]
public class ProductEndpointTests
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
    public async Task GetProducts_ReturnsOk_WithEmptyList()
    {
        var response = await _client.GetAsync("/api/products");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        Assert.That(products, Is.Not.Null);
    }

    [Test]
    public async Task CreateProduct_ValidUrl_Returns201()
    {
        var url = $"https://eu.store.ui.com/eu/en/products/test-product-{Guid.NewGuid():N}";
        var request = new CreateProductRequest(url);
        var response = await _client.PostAsJsonAsync("/api/products", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(response.Headers.Location, Is.Not.Null);

        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.That(product, Is.Not.Null);
        Assert.That(product!.Url, Is.EqualTo(request.Url));
    }

    [Test]
    public async Task GetProduct_UnknownId_Returns404()
    {
        var response = await _client.GetAsync("/api/products/99999");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task CreateAndGetProduct_RoundTrip()
    {
        var url = $"https://eu.store.ui.com/eu/en/products/round-trip-test-{Guid.NewGuid():N}";
        var createResponse = await _client.PostAsJsonAsync("/api/products", new CreateProductRequest(url));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();

        var getResponse = await _client.GetAsync($"/api/products/{created!.Id}");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var retrieved = await getResponse.Content.ReadFromJsonAsync<ProductDto>();
        Assert.That(retrieved!.Url, Is.EqualTo(url));
    }

    [Test]
    public async Task UpdateProduct_Toggle_IsActive()
    {
        var url = $"https://eu.store.ui.com/eu/en/products/update-test-{Guid.NewGuid():N}";
        var createResp = await _client.PostAsJsonAsync("/api/products", new CreateProductRequest(url));
        var created = await createResp.Content.ReadFromJsonAsync<ProductDto>();

        var updateResp = await _client.PutAsJsonAsync(
            $"/api/products/{created!.Id}",
            new UpdateProductRequest(IsActive: false, null, null));

        Assert.That(updateResp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var updated = await updateResp.Content.ReadFromJsonAsync<ProductDto>();
        Assert.That(updated!.IsActive, Is.False);
    }

    [Test]
    public async Task DeleteProduct_ExistingId_Returns204()
    {
        var url = $"https://eu.store.ui.com/eu/en/products/delete-test-{Guid.NewGuid():N}";
        var createResp = await _client.PostAsJsonAsync("/api/products", new CreateProductRequest(url));
        var created = await createResp.Content.ReadFromJsonAsync<ProductDto>();

        var deleteResp = await _client.DeleteAsync($"/api/products/{created!.Id}");
        Assert.That(deleteResp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var getResp = await _client.GetAsync($"/api/products/{created.Id}");
        Assert.That(getResp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
