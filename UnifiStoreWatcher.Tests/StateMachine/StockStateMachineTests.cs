using Microsoft.Extensions.Logging.Abstractions;
using UnifiStoreWatcher.Web.Data.Entities;
using UnifiStoreWatcher.Web.Services.Parsing;
using UnifiStoreWatcher.Web.Services.StateMachine;

namespace UnifiStoreWatcher.Tests.StateMachine;

[TestFixture]
public class StockStateMachineTests
{
    private StockStateMachine _machine = null!;

    [SetUp]
    public void Setup() =>
        _machine = new StockStateMachine(NullLogger<StockStateMachine>.Instance);

    private static Product CreateProduct(
        StockState current = StockState.Unknown,
        SubscriptionType subscription = SubscriptionType.InStock) =>
        new()
        {
            Id = 1,
            Url = "https://store.ui.com/eu/en/products/test",
            CurrentState = current,
            SubscribedEvents = subscription,
        };

    private static StockCheck CreateCheck(long id = 1) =>
        new() { Id = id, ProductId = 1 };

    [Test]
    public void FirstDiscovery_UnknownToInStock_NoNotification()
    {
        var product = CreateProduct(StockState.Unknown, SubscriptionType.InStock);
        var parse = new StockParseResult(StockState.InStock, 0.95, "JsonLd", "InStock");

        var result = _machine.Evaluate(product, parse, CreateCheck());

        Assert.Multiple(() =>
        {
            Assert.That(result.StateChanged, Is.True);
            Assert.That(result.ShouldNotify, Is.False, "Initial state discovery must not trigger notification");
            Assert.That(result.Transition!.FromState, Is.EqualTo(StockState.Unknown));
            Assert.That(result.Transition.ToState, Is.EqualTo(StockState.InStock));
        });
    }

    [Test]
    public void OutOfStockToInStock_WithInStockSubscription_Notifies()
    {
        var product = CreateProduct(StockState.OutOfStock, SubscriptionType.InStock);
        var parse = new StockParseResult(StockState.InStock, 0.95, "JsonLd", "InStock");

        var result = _machine.Evaluate(product, parse, CreateCheck());

        Assert.Multiple(() =>
        {
            Assert.That(result.StateChanged, Is.True);
            Assert.That(result.ShouldNotify, Is.True, "OutOfStock→InStock should notify when subscribed");
        });
    }

    [Test]
    public void InStockToOutOfStock_WithoutOutOfStockSubscription_DoesNotNotify()
    {
        var product = CreateProduct(StockState.InStock, SubscriptionType.InStock);
        var parse = new StockParseResult(StockState.OutOfStock, 0.95, "JsonLd", "OutOfStock");

        var result = _machine.Evaluate(product, parse, CreateCheck());

        Assert.Multiple(() =>
        {
            Assert.That(result.StateChanged, Is.True);
            Assert.That(result.ShouldNotify, Is.False, "Should not notify when OutOfStock is not subscribed");
        });
    }

    [Test]
    public void SameState_NoTransition()
    {
        var product = CreateProduct(StockState.InStock);
        var parse = new StockParseResult(StockState.InStock, 0.95, "JsonLd", "InStock");

        var result = _machine.Evaluate(product, parse, CreateCheck());

        Assert.Multiple(() =>
        {
            Assert.That(result.StateChanged, Is.False);
            Assert.That(result.ShouldNotify, Is.False);
            Assert.That(result.Transition, Is.Null);
        });
    }

    [Test]
    public void Indeterminate_NoTransition_NoNotification()
    {
        var product = CreateProduct(StockState.OutOfStock);
        var parse = new StockParseResult(StockState.Indeterminate, 0.0, "Composite", null);

        var result = _machine.Evaluate(product, parse, CreateCheck());

        Assert.Multiple(() =>
        {
            Assert.That(result.StateChanged, Is.False);
            Assert.That(result.ShouldNotify, Is.False);
        });
    }
}
