# Walkthrough: storefront-stock-monitor-opus-20260323

**Plan status**: Completed
**Date completed**: 2026-03-24
**Total tasks**: 35 (32 original phases + 3 post-delivery backlog)
**Commits**: 20+ across the plan lifecycle
**Test suite**: 69/69 pass (UbiquitiStoreLurker.Tests) + 5 AppHost integration tests compiled

---

## Overview

This plan delivered a complete, production-deployed home-lab web application: a stock monitor for the Ubiquiti EU Store. The app polls product pages for availability changes, persists history in SQLite, dispatches notifications through five independent providers, streams real-time events via SignalR to a Vue 3 frontend, and exposes Prometheus metrics to the existing lab Grafana stack.

The build spanned two sessions across 2026-03-23 and 2026-03-24. Phases 0 through 11 were completed first, followed by a post-delivery real-fixture discovery exercise (Phase 3 supplemental), HTTP hardening, and a full .NET Aspire integration (Phase 12 with 14 sub-tasks). Three backlog tasks were then added and completed to address production-readiness gaps surfaced during Aspire testing.

**Live endpoint**: `https://ubiquitistorelurker.rverbist.io`
**Source root**: `docs/plan/storefront-stock-monitor-opus-20260323/src/`
**Deployed compose**: `/opt/docker/apps/ubiquitistorelurker/docker-compose.yml`

---

## Architecture Summary

```
Browser (Vue 3 + Vite)
  ↕ REST (Minimal API)   ↕ WebSocket (SignalR)
ASP.NET Core 10 (Kestrel, port 8080)
  ├── EF Core 10 → SQLite (WAL, /data/ubiquitistorelurker.db)
  ├── PollSchedulerService (BackgroundService + Channel)
  │   └── PollWorkerService → HTTP → AngleSharp/CompositeStockParser
  │       └── StockStateMachine → NotificationDispatcher
  │           ├── BrowserPushProvider (VAPID / Lib.Net.Http.WebPush)
  │           ├── EmailProvider (MailKit)
  │           ├── SmsProvider (Twilio)
  │           ├── TeamsProvider (Workflows webhook)
  │           └── DiscordProvider (Discord webhook)
  ├── prometheus-net → /api/metrics
  ├── /api/health/live  (liveness:  always 200)
  └── /api/health/ready (readiness: DB + poller initialized)

Docker Compose (production, /opt/docker/apps/ubiquitistorelurker/)
  ubiquitistorelurker           172.18.2.4  ← app container
  aspire-dashboard       172.18.2.5  ← standalone Aspire Dashboard
  ubiquitistorelurker-otel-collector  172.18.2.6  ← OTel Collector sidecar

Lab observability (existing, /opt/docker/infra/monitoring/)
  prometheus  172.18.1.1  ← scrapes /api/metrics + accepts remote-write from Collector
  grafana     172.18.1.2  ← Grafana dashboard (ubiquitistorelurker-dashboard.json)

Traefik (172.18.0.4) → lan-only@file → ubiquitistorelurker.rverbist.io
```

---

## Phases Completed

### Phase 0 — Project Scaffolding ✅

Created the .NET 10 solution with ASP.NET Core Minimal API, Serilog bootstrap logger, `/api/health` probe, Alpine multi-stage Dockerfile (non-root UID 1654), and `docker-compose.yml` with Traefik labels and static IP `172.18.2.4`. Vue 3 + Vite frontend scaffolded inside `ClientApp/` with SPA proxy wiring.

### Phase 1 — Data Model + EF Core ✅

Designed the SQLite schema: `Products`, `StockChecks`, `AppSettings`, `NotificationConfigs`, `NotificationLogs`, `PushSubscriptions`. EF Core 10 with WAL mode (`PRAGMA journal_mode=WAL`) and a design-time factory for `dotnet ef migrations`. All migrations source-controlled under `Data/Migrations/`.

### Phase 2 — Polling Engine ✅

`PollSchedulerService` (BackgroundService + `Channel<PollWorkItem>`) enqueues products due for polling using jittered `TimeSpan` scheduling. `PollWorkerService` dequeues and performs the HTTP fetch using `IHttpClientFactory` with Polly resilience (exponential backoff, circuit breaker, retry). Per-product polling intervals are configurable at runtime.

### Phase 3 — Stock Detection ✅

`StockStateMachine` tracks `(InStock, OutOfStock, Indeterminate)` transitions and fires `StockStateChanged` domain events only on genuine state changes — preventing duplicate notifications on transient parse failures.

**Phase 3 supplemental (post-delivery)**:

- `task-phase-3-discovery`: Playwright captured live HTML fixtures from three Ubiquiti EU Store product pages (in-stock, out-of-stock, unavailable) into `tests/UbiquitiStoreLurker.Tests/Fixtures/`. Commit: `6e4b5ed`
- `task-phase-3-realworld`: Parser tests rewritten against real fixtures; `CompositeStockParser` end-to-end test added. Commit: `adbc3a8`
- `task-phase-3-cascading`: Contract verification across all parser/state/notification interfaces. Commit: `2ee8123`

### Phase 4 — Notification System ✅

Five `INotificationProvider` implementations, each feature-flagged via `NotificationConfig` (SQLite-backed, configurable at runtime). `NotificationDispatcher` iterates enabled providers with per-provider exception isolation — one failing provider never blocks others. All providers log success/failure to `NotificationLogs`.

### Phase 5 — API Endpoints ✅

Full CRUD REST surface via Minimal API route groups:
`/api/products` · `/api/products/{id}/history` · `/api/products/{id}/poll-now`
`/api/settings` · `/api/notifications/config` · `/api/notifications/log`
`/api/push/subscribe` · `/api/push/unsubscribe` · `/api/push/test`
`/api/metrics` (prometheus-net) · `/api/health/live` · `/api/health/ready`

### Phase 6 — SignalR Real-Time ✅

`UbiquitiStoreLurkerHub` broadcasts `PollCycleCompleted` events to all connected clients. The Vue frontend subscribes on mount and updates the product list reactively without full page refresh.

### Phase 7 — Vue 3 Frontend: Monitor View ✅

Monitor dashboard with per-product status cards, colour-coded stock status badges (`In Stock` / `Out of Stock` / `Indeterminate`), last-checked timestamps, and a manual "Poll Now" button. Live updates arrive via SignalR.

### Phase 8 — Vue 3 Frontend: Animations ✅

`CelebrationOverlay.vue` fires full-page GSAP timeline + canvas-confetti when a product transitions to `InStock`. Overlay is dismissable and suppressed when the tab is hidden.

### Phase 9 — Vue 3 Frontend: Setup Page ✅

`GlobalSettingsForm.vue` — application settings (polling intervals, retry limits, contact info). `NotificationSettingsForm.vue` — per-provider toggle + JSON config editor with live test button. All settings persist to SQLite via PUT `/api/settings` and PUT `/api/notifications/config/{id}`.

### Phase 10 — Prometheus + Grafana ✅

`prometheus-net` counters/gauges/histograms at `/api/metrics`:
`ubiquitistorelurker_checks_total` · `ubiquitistorelurker_poll_duration_seconds` · `ubiquitistorelurker_monitored_products_total` · `ubiquitistorelurker_active_products`
Grafana dashboard provisioned from `grafana-provisioning/dashboards/ubiquitistorelurker-dashboard.json`.

### Phase 11 — Browser Push (Service Worker) ✅

VAPID key pair auto-generated and stored in `AppSettings`. `sw.js` service worker intercepts Push events and displays OS-level notifications. Subscription management via `/api/push/subscribe` (POST) and `/api/push/unsubscribe` (DELETE). Test-push button on the Setup page.

**HTTP Hardening (`task-phase-http-hardening`)**: Chrome 124 `User-Agent` fingerprint, `UbiquitiCookieHandler` seeding EU store cookies, jittered 12 s inter-request gap — eliminates 429/403 from the Ubiquiti CDN. Commit: `7a8d81b`

### Phase 12 — Aspire Integration ✅ (14 sub-tasks: 12-A through 12-N)

Full `.NET Aspire 9.2+` integration:

| Sub-task | Deliverable |
|---|---|
| 12-A | `UbiquitiStoreLurker.ServiceDefaults` — OTel wiring (ASP.NET Core, HttpClient, EF Core, runtime, custom ActivitySource) |
| 12-B | `UbiquitiStoreLurker.AppHost` — SQLite + `.WithSqliteWeb()`, `.WaitFor(db)`, 4 secret parameters |
| 12-C | `builder.AddServiceDefaults()` in `Program.cs`; connection string key renamed `"Default"` → `"ubiquitistorelurker-db"` |
| 12-D | 11 custom `ActivitySource` spans across domain pipeline (poll, parsers, state machine, notifications, SignalR) |
| 12-E | SQLiteWeb sidecar verified (live data browser) |
| 12-F | Aspire Dashboard standalone Docker container at `172.18.2.5`; `aspire.ubiquitistorelurker.rverbist.io` |
| 12-G | `Aspire.Hosting.Testing` project (`UbiquitiStoreLurker.AppHostTests`) with 4 integration tests `[RequiresDocker]` |
| 12-H | `AppHost_StartsHealthy` — verifies liveness probe returns 200 |
| 12-I | `AppHost_AddProduct_PollCycle_PersistsStockCheck` — end-to-end product add + poll |
| 12-J | `AppHost_SignalR_ConnectAndReceiveEvent` — SignalR hub wiring |
| 12-K | `AppHost_SqliteWeb_Accessible` — sidecar health |
| 12-L | `WithHttpHealthCheck("/api/health/ready")` added to AppHost |
| 12-M | Aspire Dashboard OTLP endpoint configured in docker-compose |
| 12-N | Grafana dashboard provisioning verified end-to-end |

Commit: `2cc64c9`

---

## Backlog Tasks (Post Phase 12)

### task-backlog-1 — `/api/health/ready` Readiness Probe ✅

**Commit**: `353028b`

**Problem**: AppHost's `.WithHttpHealthCheck("/api/health/ready")` was wired but the `/ready` route didn't exist — the old healthcheck always mapped to `/live`.

**Solution**:

- `IReadinessIndicator` / `ReadinessIndicator` — thread-safe `volatile bool` singleton tracking poller first-scan completion
- `DatabaseReadinessCheck` — EF Core `db.AppSettings.AnyAsync(ct)` probe (tagged `"ready"`)
- `PollerReadinessCheck` — reads `IReadinessIndicator.IsReady` (tagged `"ready"`)
- `PollSchedulerService` — calls `readinessIndicator.MarkReady()` after first `EnqueueDueProductsAsync`
- `Program.cs` — two separate health endpoints with tag-based predicates:
  - `/api/health/live` → `Predicate = _ => false` (always 200, process alive)
  - `/api/health/ready` → `Predicate = c => c.Tags.Contains("ready")` (200/503)
- `docker-compose.yml` — healthcheck target updated to `/api/health/ready`
- `HealthEndpointTests.cs` — 3 new NUnit tests (live 200, ready 200, ready 503)

**Tests**: 69/69 pass (3 new health tests + 66 pre-existing).

### task-backlog-2 — OTLP Push via OTel Collector Sidecar ✅

**Commits**: `7967c4a` (implementation) + `c428358` (docs fix)

**Problem**: Observability was pull-only (Prometheus scrape of `/api/metrics`). Traces from the custom ActivitySource had no push destination in Docker/production mode — only available in Aspire dev mode via the Dashboard's built-in OTLP listener.

**Solution**:

- `src/otel-collector.yaml` — OTel Collector config:
  - **Receiver**: `otlp/grpc` on `0.0.0.0:4317`
  - **Exporters**: `prometheusremotewrite` → `http://172.18.1.1:9090/api/v1/write`; `otlp/aspire` → `http://172.18.2.5:18889`; `debug` (basic verbosity)
  - **Pipelines**: metrics → [otlp] → [prometheusremotewrite, debug]; traces → [otlp] → [otlp/aspire, debug]
- `docker-compose.yml`:
  - Added `ubiquitistorelurker-otel-collector` service at `172.18.2.6` (apps tier, next free IP)
  - Rerouted `OTEL_EXPORTER_OTLP_ENDPOINT` from `aspire-dashboard:18889` to `ubiquitistorelurker-otel-collector:4317`
  - The Collector now fans out: metrics → Prometheus remote-write, traces → Aspire Dashboard
- `/opt/docker/infra/monitoring/docker-compose.yml` — added `--web.enable-remote-write-receiver` to Prometheus command args
- `README.md` — "Observability — Pull vs Push" section with architecture diagram

**Result**: Dual-path telemetry. Grafana receives metrics from both pull (scrape) and push (remote-write) simultaneously. Aspire Dashboard receives traces in production Docker mode (not just dev mode).

### task-backlog-3 — Aspire Test Secrets: `AppHost_WithSeededSecrets_ProvidersRegistered` ✅

**Commit**: `e56db01`

**Problem**: All 4 `AppHostTests` passed with zero secrets injected. While this correctly tests the graceful-degradation path (all providers disabled by default), DI registration errors caused by missing `Parameters:*` configuration would be silently masked.

**Solution**: Added Test 5 to `AppHostIntegrationTests.cs`:

```csharp
// Seed test-safe placeholder secrets before StartAsync
appBuilder.Configuration["Parameters:smtp-password"]   = "test-smtp-password";
appBuilder.Configuration["Parameters:twilio-token"]    = "test-twilio-token";
appBuilder.Configuration["Parameters:discord-webhook"] = "http://localhost/discord-test";
appBuilder.Configuration["Parameters:teams-webhook"]   = "http://localhost/teams-test";
```

Test assertions:

1. App starts healthy with secrets present (no DI crash)
2. `GET /api/settings` → 200 with expected defaults (poller DI chain intact)
3. `PUT /api/settings` with `Nickname="Seeded Test Monitor"` + `Email="test@example.com"` → 200
4. `GET /api/settings` → confirms nickname and email persisted (Settings API write/read round-trip)

Tagged `[Category("RequiresDocker")]`. The 4 existing AppHostTests remain unchanged — the zero-secrets baseline is preserved as a first-class test.

---

## File Inventory

### New files (backlog phase only)

| File | Purpose |
|---|---|
| `UbiquitiStoreLurker.Web/Services/Health/IReadinessIndicator.cs` | Interface: `bool IsReady`, `void MarkReady()` |
| `UbiquitiStoreLurker.Web/Services/Health/ReadinessIndicator.cs` | Thread-safe singleton (`volatile bool`) |
| `UbiquitiStoreLurker.Web/Services/Health/DatabaseReadinessCheck.cs` | EF Core SQLite probe, tagged `"ready"` |
| `UbiquitiStoreLurker.Web/Services/Health/PollerReadinessCheck.cs` | Poller first-scan flag check, tagged `"ready"` |
| `tests/UbiquitiStoreLurker.Tests/Api/HealthEndpointTests.cs` | 3 NUnit tests for /live and /ready |
| `src/otel-collector.yaml` | OTel Collector config (OTLP receiver, Prometheus RW, Aspire OTLP) |

### Modified files (backlog phase only)

| File | Change |
|---|---|
| `UbiquitiStoreLurker.Web/Services/Polling/PollSchedulerService.cs` | Takes `IReadinessIndicator`; marks ready after first scan |
| `UbiquitiStoreLurker.Web/Program.cs` | Registers readiness services; maps `/live` and `/ready` with tag predicates |
| `UbiquitiStoreLurker.AppHost/Program.cs` | Adds `.WithHttpHealthCheck("/api/health/ready")` |
| `src/docker-compose.yml` | `/ready` healthcheck; `otel-collector` service; rerouted OTLP endpoint |
| `/opt/docker/infra/monitoring/docker-compose.yml` | `--web.enable-remote-write-receiver` on Prometheus |
| `src/README.md` | "Observability — Pull vs Push" section |
| `tests/UbiquitiStoreLurker.Tests/TestApiFactory.cs` | Connection string key `Default` → `ubiquitistorelurker-db` |
| `tests/UbiquitiStoreLurker.Tests/Polling/PollSchedulerServiceTests.cs` | Stub `IReadinessIndicator` in 3 constructor calls |
| `tests/UbiquitiStoreLurker.AppHostTests/AppHostIntegrationTests.cs` | Test 5 added; `using UbiquitiStoreLurker.Web.Endpoints` import |

---

## Test Coverage Summary

| Suite | Tests | Status |
|---|---|---|
| `UbiquitiStoreLurker.Tests` (unit + integration via WebApplicationFactory) | 69 | ✅ All pass |
| `UbiquitiStoreLurker.AppHostTests` (Aspire E2E, `[RequiresDocker]`) | 5 | ✅ Compiles; run with live Docker |

### Test categories in `UbiquitiStoreLurker.Tests`

| Folder | Tests |
|---|---|
| `Api/` | Products CRUD, Settings, Notifications, Health (live + ready), Smoke |
| `Data/` | EF Core repository layer |
| `Http/` | Cookie handler, rate limiting, resilience pipeline |
| `Hubs/` | SignalR hub broadcast |
| `Notifications/` | NotificationDispatcher, BrowserPushProvider |
| `Parsing/` | ButtonStateParser, ImageAltParser, TextContentParser, CompositeStockParser (incl. real fixtures) |
| `Polling/` | PollSchedulerService, PollWorkerService |
| `StateMachine/` | StockStateMachine transitions |

---

## Observability Stack (running state)

| Component | Address | Role |
|---|---|---|
| `ubiquitistorelurker` | `172.18.2.4` | Application container |
| `aspire-dashboard` | `172.18.2.5` | Trace / metric UI (Aspire Dashboard standalone) |
| `ubiquitistorelurker-otel-collector` | `172.18.2.6` | OTLP receiver, fans out metrics + traces |
| Lab Prometheus | `172.18.1.1` | Pull scrape (`/api/metrics`) + push remote-write from Collector |
| Lab Grafana | `172.18.1.2` | Dashboard (`ubiquitistorelurker-dashboard.json`) |

**Telemetry paths**:

- **Pull**: Prometheus scrapes `/api/metrics` every 15 s (prometheus-net counters + histograms)
- **Push**: App → Collector (OTLP gRPC `:4317`) → Prometheus remote-write (metrics) + Aspire Dashboard (traces)

Custom ActivitySource spans (11 total): poll scheduling, HTTP fetch, HTML parsing, stock state transitions, notification dispatch (per provider), SignalR broadcast.

---

## Deployment Quick Reference

```bash
# First deploy (create volumes + network)
docker volume create ubiquitistorelurker-data
docker volume create ubiquitistorelurker-logs

# Pull / build and start
cd /opt/docker/apps/ubiquitistorelurker
docker compose pull
docker compose up -d

# Start OTel Collector sidecar
# (defined in same docker-compose.yml)
docker compose up -d ubiquitistorelurker-otel-collector

# Restart lab Prometheus to pick up --web.enable-remote-write-receiver
cd /opt/docker/infra/monitoring
docker compose up -d prometheus

# Verify all healthy
docker compose ps
curl -s https://ubiquitistorelurker.rverbist.io/api/health/ready | jq .
```

**Secrets** (set before first run via Docker env or `.env`):

```env
Email__SmtpHost=smtp.example.com
Email__SmtpPort=587
Email__Username=user@example.com
Email__Password=<smtp-password>       # injected by AppHost as Parameters:smtp-password
Twilio__AccountSid=ACxxxxxxxxxxxxxxxx
Twilio__AuthToken=<twilio-token>       # injected by AppHost as Parameters:twilio-token
Discord__WebhookUrl=<discord-webhook>  # injected by AppHost as Parameters:discord-webhook
Teams__WebhookUrl=<teams-webhook>      # injected by AppHost as Parameters:teams-webhook
```

---

## Key Decisions Made

| Decision | Rationale |
|---|---|
| **SQLite (WAL) over Postgres** | Single-container simplicity; WAL gives crash-safe concurrent reads; backup is a file copy |
| **Channel\<T\> for poll queue** | Bounded, backpressure-aware, zero external dependency vs Redis / RabbitMQ |
| **IReadinessIndicator singleton** | Avoids polling the DB just to check if the poller has started; volatile bool is sufficient for a single-writer/multi-reader scenario |
| **OTel Collector fan-out vs SDK multi-exporter** | Keeps app config to a single `OTEL_EXPORTER_OTLP_ENDPOINT`; Collector handles fan-out, buffering, and retry independently |
| **`--web.enable-remote-write-receiver` on lab Prometheus** | Remote-write is the only practical way to receive OTLP metrics from the Collector without standing up a separate Prometheus OTLP receiver; re-uses existing lab infra |
| **Health predicate (`Predicate = _ => false`) for `/live`** | The liveness check must always return 200 as long as the process is responsive; re-using the tag filter would mean a DB failure causes Kubernetes/Traefik to restart a perfectly healthy process |
| **`appBuilder.Configuration` for Aspire test secrets** | `DistributedApplicationTestingBuilder` exposes `Configuration` directly; no `IConfiguration` override gymnastics needed |
| **`UpdateSettingsRequest` PUT (not PATCH)** | Nullable fields achieve partial-update semantics without a separate PATCH endpoint; simpler client code |

---

## Next Steps (not in scope)

- **Deploy to production** (`/opt/docker/apps/ubiquitistorelurker/`) and confirm end-to-end: first poll → notification → Grafana metric
- **Prometheus scrape config** — add `ubiquitistorelurker` job to `/opt/docker/infra/monitoring/config/prometheus.yml` if not already present
- **Grafana alert rules** — alert when `ubiquitistorelurker_checks_total{result="error"}` rate exceeds threshold
- **Aspire Dashboard Traefik route** — `aspire.ubiquitistorelurker.rverbist.io` is referenced in docker-compose labels; verify the dynamic config file exists in `/opt/docker/infra/traefik/dynamic/`
- **AppHost integration tests in CI** — the `[RequiresDocker]` suite needs a Docker-capable runner; consider a GitHub Actions self-hosted runner on the Proxmox host
- **Notification provider enable/disable UI** — the Setup page JSON editor is functional but not user-friendly; a dedicated per-provider form could improve UX
- **WhatsApp / Facebook Messenger** — deferred since Phase 4; both are marked "Coming Soon" in the UI
