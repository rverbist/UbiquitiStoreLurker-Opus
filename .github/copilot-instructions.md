# UniFiStoreWatcher — Copilot Instructions

## Build & Test

| Action | Command |
|--------|---------|
| Build | `dotnet build UniFiStoreWatcher.slnx` |
| Run web app | `dotnet run --project UniFiStoreWatcher.Web/UniFiStoreWatcher.Web.csproj` |
| Test | `dotnet test` |
| Frontend dev | `npm run dev` (from `UniFiStoreWatcher.Web/ClientApp/`) |
| Frontend build | `npm run build` (outputs to `../wwwroot`) |

> **Solution format is `.slnx`** (new experimental format) — not `.sln`. Always reference `UniFiStoreWatcher.slnx` explicitly; tools that glob for `*.sln` will not find it.

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

**Key folders under `UniFiStoreWatcher.Web/`:**

| Folder | Purpose |
|--------|---------|
| `Services/Polling/` | `PollSchedulerService` + `PollWorkerService` (BackgroundServices) |
| `Services/Parsing/` | `IStockParser` pipeline; `CompositeStockParser` stops at confidence ≥ 0.60 |
| `Services/StateMachine/` | Pure transition evaluator returning `TransitionResult` record |
| `Services/Notifications/` | `NotificationDispatcher` fan-out; `BrowserPushProvider`, Email, SMS, Teams, Discord |
| `Http/` | Delegating handlers — must stay in chain for requests to succeed |
| `Endpoints/` | Minimal API extensions: `MapProductEndpoints`, `MapSettingsEndpoints`, etc. |
| `Hubs/` | `UniFiStoreWatcherHub` (file: `UbiquitiStoreLurkerHub.cs`) at `/UniFiStoreWatcher-hub` |
| `Data/` | EF Core + SQLite; auto-migrates on startup; WAL mode via interceptor |
| `Metrics/` | Prometheus counters/gauges at `/api/metrics` |
| `Telemetry/` | Single `ActivitySource("UniFiStoreWatcher.Web", "1.0.0")` |

**Two frontend UIs coexist:**
- **Primary** (Claude dashboard): `wwwroot/claude/index.html` — served at `/`, `/monitor`, `/v2/monitor`
- **Legacy** (Vue SPA): `wwwroot/index.html` — served at `/v1/monitor`; built by Vite pipeline

## Tech Stack

- **.NET 10** (`net10.0`) — cutting edge, pre-release packages expected
- **SQLite** with WAL mode; EF Core code-first migrations
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

### `DateTimeOffset` in SQLite
All `DateTimeOffset` properties are stored as `long` (UTC ticks) via a global EF converter applied in `OnModelCreating`. Raw SQL queries against the database will see integer columns, not ISO timestamps.

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
- Uses a named shared in-memory SQLite connection (`Mode=Memory;Cache=Shared`) kept alive by `_keepAliveConnection`
- `[OneTimeSetUp]` / `[OneTimeTearDown]` — one factory per fixture class

### Unit tests
- Construct SUT manually with `NullLogger<T>.Instance`
- DB-dependent unit tests: `new SqliteConnection("Data Source=:memory:")`, open, `EnsureCreated()` — no migrations in unit tests
- In-memory SQLite ignores WAL mode; this is expected and safe for tests

### Assertion style
Use `Assert.Multiple(() => { ... })` for grouping. `NUnit.Framework` is a global implicit using. Test method names use underscores (CA1707 suppressed).

### HTML fixtures
`Fixtures/` contains real captured pages from the Ubiquiti EU store. These are the primary oracle for parser correctness — prefer adding a fixture file over mocking HTTP responses.

## Key Pitfalls

- **Hub file/class name mismatch**: `UbiquitiStoreLurkerHub.cs` contains `class UniFiStoreWatcherHub`. Search by class name, not file name.
- **`NotificationDispatcher` concurrent writes**: `DispatchAsync` fires `Task.WhenAll` for providers but writes audit logs sequentially to avoid `DbContext` thread-safety violations. Do not parallelize the log-write loop.
- **WAL via interceptor, not connection string**: `SqliteWalModeInterceptor` runs `PRAGMA journal_mode=WAL` on every connection. In-memory test DBs silently ignore it — this is fine.
- **Wildcard NuGet versions on Serilog**: `Version="*"` resolves to latest at restore time; combined with .NET 10 preview this can cause unexpected churn.
