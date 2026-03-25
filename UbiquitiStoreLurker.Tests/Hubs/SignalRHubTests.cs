using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using UbiquitiStoreLurker.Web.Data.Entities;
using UbiquitiStoreLurker.Web.Hubs;

namespace UbiquitiStoreLurker.Tests.Hubs;

[TestFixture]
public class SignalRHubTests
{
    private TestApiFactory _factory = null!;

    [OneTimeSetUp]
    public void Setup()
    {
        _factory = new TestApiFactory();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _factory.Dispose();
    }

    [Test]
    public async Task Hub_ConnectsSuccessfully()
    {
        var hubConnection = new HubConnectionBuilder()
            .WithUrl("http://localhost/ubiquitistorelurker-hub", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        await hubConnection.StartAsync();

        Assert.That(hubConnection.State, Is.EqualTo(HubConnectionState.Connected));

        await hubConnection.StopAsync();
        await hubConnection.DisposeAsync();
    }

    [Test]
    public async Task Hub_ReceivesStockStatusChanged_WhenBroadcast()
    {
        var hubConnection = new HubConnectionBuilder()
            .WithUrl("http://localhost/ubiquitistorelurker-hub", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        var tcs = new TaskCompletionSource<StockStatusChanged>(TaskCreationOptions.RunContinuationsAsynchronously);
        hubConnection.On<StockStatusChanged>("StockStatusChanged", evt => tcs.TrySetResult(evt));

        await hubConnection.StartAsync();

        var broadcaster = _factory.Services.GetRequiredService<StockHubBroadcaster>();
        var expected = new StockStatusChanged(
            ProductId: 1,
            Url: "https://eu.store.ui.com/test",
            ProductName: "Test Product",
            FromState: StockState.Unknown,
            ToState: StockState.InStock,
            DetectedAtUtc: DateTimeOffset.UtcNow);

        await broadcaster.BroadcastStockStatusChangedAsync(expected);

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(5000));
        Assert.That(completed, Is.EqualTo(tcs.Task), "Timed out waiting for StockStatusChanged event");

        var received = tcs.Task.Result;
        Assert.That(received.ProductId, Is.EqualTo(1));
        Assert.That(received.ToState, Is.EqualTo(StockState.InStock));

        await hubConnection.StopAsync();
        await hubConnection.DisposeAsync();
    }

    [Test]
    public async Task Hub_ReceivesPollCycleCompleted_WhenBroadcast()
    {
        var hubConnection = new HubConnectionBuilder()
            .WithUrl("http://localhost/ubiquitistorelurker-hub", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        var tcs = new TaskCompletionSource<PollCycleCompleted>(TaskCreationOptions.RunContinuationsAsynchronously);
        hubConnection.On<PollCycleCompleted>("PollCycleCompleted", evt => tcs.TrySetResult(evt));

        await hubConnection.StartAsync();

        var broadcaster = _factory.Services.GetRequiredService<StockHubBroadcaster>();
        var expected = new PollCycleCompleted(
            ProductId: 2,
            Url: "https://eu.store.ui.com/product2",
            Success: true,
            HttpStatusCode: 200,
            DurationMs: 150,
            CompletedAtUtc: DateTimeOffset.UtcNow);

        await broadcaster.BroadcastPollCycleCompletedAsync(expected);

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(5000));
        Assert.That(completed, Is.EqualTo(tcs.Task), "Timed out waiting for PollCycleCompleted event");
        Assert.That(tcs.Task.Result.HttpStatusCode, Is.EqualTo(200));

        await hubConnection.StopAsync();
        await hubConnection.DisposeAsync();
    }
}
