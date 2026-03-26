using AngleSharp;
using Microsoft.Extensions.Logging.Abstractions;
using UniFiStoreWatcher.Web.Data.Entities;
using UniFiStoreWatcher.Web.Services.Parsing;

namespace UniFiStoreWatcher.Tests.Parsing;

[TestFixture]
public class JsonLdStockParserTests
{
    private JsonLdStockParser _parser = null!;

    [SetUp]
    public void Setup() =>
        _parser = new JsonLdStockParser(NullLogger<JsonLdStockParser>.Instance);

    private static async Task<AngleSharp.Dom.IDocument> ParseHtmlAsync(string html)
    {
        var context = BrowsingContext.New(Configuration.Default);
        return await context.OpenAsync(req => req.Content(html));
    }

    [Test]
    public async Task DetectsInStock_WhenJsonLdAvailabilityIsInStock()
    {
        var html = await File.ReadAllTextAsync(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "fixture-instock-jsonld.html"));
        var doc = await ParseHtmlAsync(html);

        var result = await _parser.ParseAsync(doc);

        Assert.That(result.State, Is.EqualTo(StockState.InStock));
        Assert.That(result.Confidence, Is.GreaterThanOrEqualTo(0.90));
        Assert.That(result.Strategy, Is.EqualTo("JsonLd"));
    }

    [Test]
    public async Task DetectsOutOfStock_WhenJsonLdAvailabilityIsOutOfStock()
    {
        var html = await File.ReadAllTextAsync(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "fixture-outofstock-jsonld.html"));
        var doc = await ParseHtmlAsync(html);

        var result = await _parser.ParseAsync(doc);

        Assert.That(result.State, Is.EqualTo(StockState.OutOfStock));
        Assert.That(result.Confidence, Is.GreaterThanOrEqualTo(0.90));
    }

    [Test]
    public async Task ReturnsIndeterminate_WhenNoJsonLdPresent()
    {
        var html = await File.ReadAllTextAsync(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "fixture-instock-button.html"));
        var doc = await ParseHtmlAsync(html);

        var result = await _parser.ParseAsync(doc);

        Assert.That(result.State, Is.EqualTo(StockState.Indeterminate));
    }

    [Test]
    public async Task ReturnsIndeterminate_WhenJsonLdIsMalformed()
    {
        var html = await File.ReadAllTextAsync(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "fixture-malformed-jsonld.html"));
        var doc = await ParseHtmlAsync(html);

        var result = await _parser.ParseAsync(doc);

        Assert.That(result.State, Is.EqualTo(StockState.Indeterminate));
    }

    [Test]
    public async Task ParseAsync_RealUnasProOutOfStock_ReturnsOutOfStock()
    {
        var html = await File.ReadAllTextAsync(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "real-unas-pro-4-outofstock.html"));
        var doc = await ParseHtmlAsync(html);

        var result = await _parser.ParseAsync(doc);

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(StockState.OutOfStock),
                "UNAS-Pro-4 JSON-LD reports https://schema.org/OutOfStock");
            Assert.That(result.Confidence, Is.GreaterThanOrEqualTo(0.90));
            Assert.That(result.Strategy, Is.EqualTo("JsonLd"));
        });
    }

    [Test]
    public async Task ParseAsync_RealUnasProInStock_ReturnsInStock()
    {
        var html = await File.ReadAllTextAsync(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "real-unas-pro-8-instock.html"));
        var doc = await ParseHtmlAsync(html);

        var result = await _parser.ParseAsync(doc);

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(StockState.InStock),
                "UNAS-Pro-8 JSON-LD reports https://schema.org/InStock");
            Assert.That(result.Confidence, Is.GreaterThanOrEqualTo(0.90));
            Assert.That(result.Strategy, Is.EqualTo("JsonLd"));
        });
    }
}
