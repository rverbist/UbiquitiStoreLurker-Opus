using AngleSharp;
using UbiquitiStoreLurker.Web.Data.Entities;
using UbiquitiStoreLurker.Web.Services.Parsing;

namespace UbiquitiStoreLurker.Tests.Parsing;

[TestFixture]
public class TextContentParserTests
{
    private TextContentParser _parser = null!;

    [SetUp]
    public void Setup() => _parser = new TextContentParser();

    private static async Task<AngleSharp.Dom.IDocument> ParseHtmlAsync(string html)
    {
        var context = BrowsingContext.New(Configuration.Default);
        return await context.OpenAsync(req => req.Content(html));
    }

    [Test]
    public async Task DetectsOutOfStock_WhenBodyContainsOutOfStockPhrase()
    {
        var html = await File.ReadAllTextAsync(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "fixture-outofstock-text.html"));
        var doc = await ParseHtmlAsync(html);

        var result = await _parser.ParseAsync(doc);

        Assert.That(result.State, Is.EqualTo(StockState.OutOfStock));
        Assert.That(result.Confidence, Is.GreaterThanOrEqualTo(0.65));
    }

    [Test]
    public async Task ReturnsIndeterminate_WhenNoStockPhrasesFound()
    {
        var html = await File.ReadAllTextAsync(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "fixture-indeterminate.html"));
        var doc = await ParseHtmlAsync(html);

        var result = await _parser.ParseAsync(doc);

        Assert.That(result.State, Is.EqualTo(StockState.Indeterminate));
    }

    // The real OOS page contains both "Sold Out" and "Add to Cart" text.
    // OutOfStockPhrases are checked first so "sold out" wins over "add to cart".
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
                "'sold out' phrase must take priority over 'add to cart' when both are present");
            Assert.That(result.Confidence, Is.GreaterThanOrEqualTo(0.65));
        });
    }

    // The real InStock page contains "Add to Cart" but no out-of-stock phrases.
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
                "'add to cart' / 'available' should resolve to InStock when no OOS signal present");
            Assert.That(result.Confidence, Is.GreaterThanOrEqualTo(0.60));
        });
    }
}
