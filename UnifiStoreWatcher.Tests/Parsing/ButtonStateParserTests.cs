using AngleSharp;
using UnifiStoreWatcher.Web.Data.Entities;
using UnifiStoreWatcher.Web.Services.Parsing;

namespace UnifiStoreWatcher.Tests.Parsing;

[TestFixture]
public class ButtonStateParserTests
{
    private ButtonStateParser _parser = null!;

    [SetUp]
    public void Setup() => _parser = new ButtonStateParser();

    private static async Task<AngleSharp.Dom.IDocument> ParseHtmlAsync(string html)
    {
        var context = BrowsingContext.New(Configuration.Default);
        return await context.OpenAsync(req => req.Content(html));
    }

    [Test]
    public async Task DetectsInStock_WhenAddToCartButtonIsEnabled()
    {
        var html = await File.ReadAllTextAsync(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "fixture-instock-button.html"));
        var doc = await ParseHtmlAsync(html);

        var result = await _parser.ParseAsync(doc);

        Assert.That(result.State, Is.EqualTo(StockState.InStock));
        Assert.That(result.Confidence, Is.GreaterThanOrEqualTo(0.80));
    }

    [Test]
    public async Task DetectsOutOfStock_WhenButtonIsDisabled()
    {
        var html = await File.ReadAllTextAsync(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "fixture-outofstock-button.html"));
        var doc = await ParseHtmlAsync(html);

        var result = await _parser.ParseAsync(doc);

        Assert.That(result.State, Is.EqualTo(StockState.OutOfStock));
    }

    [Test]
    [TestCase("<body><button disabled>Add to Cart</button></body>", StockState.OutOfStock)]
    [TestCase("<body><button>Add to Cart</button></body>", StockState.InStock)]
    [TestCase("<body><button disabled>Sold Out</button></body>", StockState.OutOfStock)]
    public async Task DetectsState_ForInlineHtml(string bodyHtml, StockState expectedState)
    {
        var html = $"<html><head></head>{bodyHtml}</html>";
        var doc = await ParseHtmlAsync(html);

        var result = await _parser.ParseAsync(doc);

        Assert.That(result.State, Is.EqualTo(expectedState));
    }

    // The real Ubiquiti OOS page has a disabled "Sold Out" <button> that precedes the
    // notify-me "Add to Cart" button in the DOM. ButtonStateParser correctly returns
    // OutOfStock by matching the earlier out-of-stock button (not Indeterminate).
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
                "Disabled 'Sold Out' button should be detected before the notify-me 'Add to Cart' button");
            Assert.That(result.Confidence, Is.GreaterThanOrEqualTo(0.80));
        });
    }

    // The real Ubiquiti InStock page has an enabled "Add to Cart" <button> and no
    // disabled or out-of-stock buttons. ButtonStateParser returns InStock.
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
                "Enabled 'Add to Cart' button should be detected as InStock");
            Assert.That(result.Confidence, Is.GreaterThanOrEqualTo(0.80));
        });
    }
}
