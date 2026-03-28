# UnifiStoreWatcher — Copilot Instructions

## Build & Test

| Action | Command |
|--------|---------|
| Build | `dotnet build UnifiStoreWatcher.slnx` |
| Run web app | `dotnet run --project UnifiStoreWatcher.Web/UnifiStoreWatcher.Web.csproj` |
| Test | `dotnet test` |
| Frontend dev | `npm run dev` (from `UnifiStoreWatcher.Web/ClientApp/`) |
| Frontend build | `npm run build` (outputs to `../wwwroot`) |

> **Solution format is `.slnx`** (new experimental format) — not `.sln`. Always reference `UnifiStoreWatcher.slnx` explicitly; tools that glob for `*.sln` will not find it.

Default runtime port: **5248**. The Vite dev proxy targets `localhost:5000` — update `vite.config.ts` or `launchSettings.json` if running both simultaneously.

## Architecture

```
PollSchedulerService → Channel<PollWorkItem> → PollWorkerService
                                                      ↓
                               IHttpClientFactory ("UniFiStoreWatchPoller")
                               [BrowserFingerprintHandler → UbiquitiCookieHandler
                                → ClientSideRateLimitHandler → StandardResilienceHandler]
                                                      ↓
                                          CompositeStockParser
                                    (JsonLd 0.95 → Button 0.85 → Text 0.70)
                                                      ↓
                                           StockStateMachine
                                                      ↓
                             NotificationDispatcher + StockHubBroadcaster (SignalR)
```

**Key folders under `UnifiStoreWatcher.Web/`:**

| Folder | Purpose |
|--------|---------|
| `Services/Polling/` | `PollSchedulerService` + `PollWorkerService` (BackgroundServices) |
| `Services/Parsing/` | `IStockParser` pipeline; `CompositeStockParser` stops at confidence ≥ 0.60 |
| `Services/StateMachine/` | Pure transition evaluator returning `TransitionResult` record |
| `Services/Notifications/` | `NotificationDispatcher` fan-out; `BrowserPushProvider`, Email, SMS, Teams, Discord |
| `Http/` | Delegating handlers — must stay in chain for requests to succeed |
| `Endpoints/` | Minimal API extensions: `MapProductEndpoints`, `MapSettingsEndpoints`, etc. |
| `Hubs/` | `UnifiStoreWatcherHub` (file: `UbiquitiStoreLurkerHub.cs`) at `/UnifiStoreWatcher-hub` |
| `Data/` | EF Core + SQL Server; auto-migrates on startup |
| `Metrics/` | Prometheus counters/gauges at `/api/metrics` |
| `Telemetry/` | Single `ActivitySource("UnifiStoreWatcher.Web", "1.0.0")` |

**Single frontend UI:**
- **Vue SPA**: `wwwroot/index.html` — served as default SPA fallback; built by Vite pipeline

## Tech Stack

- **.NET 10** (`net10.0`) — cutting edge, pre-release packages expected
- **SQL Server** on `RV-WEBSERVER`; EF Core code-first migrations
- **Vue 3.5 + Vite 8 + TypeScript** in `ClientApp/`; **Pinia 3**, **vue-router 4.6**, `@microsoft/signalr`
- **NUnit 4 + NSubstitute 5** in the test project
- `TreatWarningsAsErrors=true`, `Nullable=enable`, `AnalysisLevel=latest-recommended` — solution-wide

## Conventions

### Logging
Use **`[LoggerMessage]` source-generated methods** in `sealed partial class` services. Never call `logger.LogInformation(...)` directly.

```csharp
// Correct
sealed partial class MyService(ILogger<MyService> logger)
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Polling {ProductId}")]
    private partial void LogPolling(string productId);
}
```

### `IStockParser` registration pattern
Each concrete parser is registered **twice**: once as its concrete type, once as `IStockParser` via a factory delegate. This allows both direct injection and `IEnumerable<IStockParser>` enumeration.

### `DateTimeOffset` in SQL Server
`DateTimeOffset` properties are stored natively as `datetimeoffset(7)` — no custom EF converter is applied.

### `SubscriptionType` is a `[Flags]` enum
Use `.HasFlag(...)` comparisons. Setting `SubscribedEvents = 0` means no notifications ever fire.

### HTTP handler chain ordering
`AddHttpMessageHandler` calls are outermost-first. **Do not reorder or remove `BrowserFingerprintHandler`** — the Ubiquiti store will return 403/429 without the full Chrome header set. `ClientSideRateLimitHandler` uses a `SemaphoreSlim(1,1)` to serialize all outbound requests globally.

### DI lifetimes
- `NotificationDispatcher` — **Scoped** (creates its own `DbContext` scope internally)
- `INotificationProvider` implementations — **Singleton**
- `StockHubBroadcaster` — **Singleton** (thin wrapper around `IHubContext<T>` for background service access)

### `PollCount` vs `ErrorCount`
`PollCount == 0` is the "never polled" sentinel used for startup stagger. `ErrorCount` increments independently. Keep both counters accurate when touching poll state.

## Testing

Tests mirror production namespaces: `Api/`, `Http/`, `Hubs/`, `Parsing/`, `Polling/`, `StateMachine/`, `Notifications/`.

### Integration tests (`TestApiFactory`)
- Inherits `WebApplicationFactory<Program>`
- **Override `CreateHost`, not `ConfigureWebHost`** — `Program.cs` calls `MigrateAsync()` before `ConfigureWebHost` hooks fire; the override must intercept `DeferredHostBuilder`
- Sets `ConnectionStrings:UniFiStoreWatch-db` to an `InMemory:<guid>` value so `Program.cs` switches to `UseInMemoryDatabase` and calls `EnsureCreatedAsync` instead of `MigrateAsync`
- `[OneTimeSetUp]` / `[OneTimeTearDown]` — one factory per fixture class

### Unit tests
- Construct SUT manually with `NullLogger<T>.Instance`
- DB-dependent unit tests: `new DbContextOptionsBuilder<UnifiStoreWatcherDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options`, then `EnsureCreated()` — no migrations in unit tests
- Each test uses a unique database name for full isolation

### Assertion style
Use `Assert.Multiple(() => { ... })` for grouping. `NUnit.Framework` is a global implicit using. Test method names use underscores (CA1707 suppressed).

### HTML fixtures
`Fixtures/` contains real captured pages from the Ubiquiti EU store. These are the primary oracle for parser correctness — prefer adding a fixture file over mocking HTTP responses.

## Key Pitfalls

- **Hub file/class name mismatch**: `UbiquitiStoreLurkerHub.cs` contains `class UnifiStoreWatcherHub`. Search by class name, not file name.
- **`NotificationDispatcher` concurrent writes**: `DispatchAsync` fires `Task.WhenAll` for providers but writes audit logs sequentially to avoid `DbContext` thread-safety violations. Do not parallelize the log-write loop.
- **Cookie persistence**: `UbiquitiCookieJar` reads `CookieJar:PersistPath` from config. Set `CookieJar__PersistPath=/logs/http-cookies.json` in production (`.env.Production` or compose `environment`). If unset, cookies are re-seeded from scratch on each restart (still functional, slightly slower on first poll).
- **Connection string key**: Always use `ConnectionStrings:UniFiStoreWatch-db` (env var: `ConnectionStrings__UniFiStoreWatch-db`). Other names (`Default`, `UnifiStoreWatcher-db`) are dead.
- **Wildcard NuGet versions on Serilog**: `Version="*"` resolves to latest at restore time; combined with .NET 10 preview this can cause unexpected churn.
