using Microsoft.Extensions.Logging.Abstractions;
using UniFiStoreWatcher.Web.Data.Entities;
using UniFiStoreWatcher.Web.Services.Parsing;

namespace UniFiStoreWatcher.Tests.Parsing;

/// <summary>
/// Integration tests for <see cref="CompositeStockParser"/> using real HTML fixtures
/// captured from the Ubiquiti EU store. These tests run all three parsers end-to-end
/// and assert the final stock state produced by the composite.
/// </summary>
[TestFixture]
public class CompositeStockParserTests
{
    private CompositeStockParser _composite = null!;

    [SetUp]
    public void Setup()
    {
        // Construct the composite with the same parser order as production DI:
        // JsonLd (confidence 0.95) → ButtonState (0.85) → TextContent (0.70).
        // The composite stops at the first result with confidence ≥ 0.60.
        IStockParser[] parsers =
        [
            new JsonLdStockParser(NullLogger<JsonLdStockParser>.Instance),
            new ButtonStateParser(),
            new TextContentParser(),
        ];

        _composite = new CompositeStockParser(parsers, NullLogger<CompositeStockParser>.Instance);
    }

    [Test]
    public async Task ParseAsync_RealOutOfStockFixture_ReturnsOutOfStock()
    {
        var html = await File.ReadAllTextAsync(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "real-unas-pro-4-outofstock.html"));

        var result = await _composite.ParseAsync(html);

        // JsonLdStockParser should win with confidence 0.95 on the real fixture.
        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(StockState.OutOfStock),
                "UNAS-Pro-4 OOS fixture must resolve to OutOfStock through the full parser chain");
            Assert.That(result.Confidence, Is.GreaterThanOrEqualTo(0.80));
            Assert.That(result.Strategy, Is.EqualTo("JsonLd"),
                "JsonLdStockParser should be the winning parser (highest confidence, first in chain)");
        });
    }

    [Test]
    public async Task ParseAsync_RealInStockFixture_ReturnsInStock()
    {
        var html = await File.ReadAllTextAsync(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "real-unas-pro-8-instock.html"));

        var result = await _composite.ParseAsync(html);

        // JsonLdStockParser should win with confidence 0.95 on the real fixture.
        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(StockState.InStock),
                "UNAS-Pro-8 InStock fixture must resolve to InStock through the full parser chain");
            Assert.That(result.Confidence, Is.GreaterThanOrEqualTo(0.80));
            Assert.That(result.Strategy, Is.EqualTo("JsonLd"),
                "JsonLdStockParser should be the winning parser (highest confidence, first in chain)");
        });
    }
}
