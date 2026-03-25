# Phase 12 Completion Walkthrough — Aspire Integration (Learning Edition)

**Plan:** storefront-stock-monitor-opus-20260323
**Completed:** 2026-03-24
**Tasks:** 14 completed (task-phase-12-a through task-phase-12-n)
**Test suite:** 66/66 passing; Dockerfile guard: PASS; Docker image health check: 200 OK

---

## Overview

Phase 12 added .NET Aspire 9/13 as a **local development overlay** on top of the already-complete production UbiquitiStoreLurker application. The production stack (Serilog CLEF, prometheus-net, Grafana lab) is entirely unaffected. Aspire runs in parallel as an alternative local dev path.

Two new modes are now available alongside the existing plain `dotnet run` and Docker Compose modes:

| Mode | Command | Observability |
|---|---|---|
| Plain dotnet run | `dotnet run --project src/UbiquitiStoreLurker.Web` | Grafana lab only |
| **Aspire AppHost** | `dotnet run --project src/UbiquitiStoreLurker.AppHost` | Aspire Dashboard + local Grafana + SQLiteWeb |
| Docker Compose (local) | `docker compose up -d` | Aspire Dashboard sidecar at :18888 |
| Docker Compose (Proxmox) | VS Code task → Deploy — Proxmox | Aspire Dashboard at aspire.ubiquitistorelurker.rverbist.io |

---

## What Was Built

### New Projects

**`src/UbiquitiStoreLurker.ServiceDefaults/`**
Class library for shared OpenTelemetry wiring. Registers:

- Tracing: ASP.NET Core + HttpClient + EF Core + custom `UbiquitiStoreLurker.Web` ActivitySource
- Metrics: ASP.NET Core + HttpClient + Runtime + `UbiquitiStoreLurker.Web` meter
- OTLP exporter: conditional on `OTEL_EXPORTER_OTLP_ENDPOINT` env var (no-ops silently in production)
- Default health check (`self`, tagged `live`)

**`src/UbiquitiStoreLurker.AppHost/`**
.NET Aspire orchestrator. On `dotnet run`:

- Starts `ubiquitistorelurker-db` (SQLite resource) with SQLiteWeb sidecar
- Starts `ubiquitistorelurker` (UbiquitiStoreLurker.Web), `.WaitFor(db)` enforces startup order
- Injects `ConnectionStrings__ubiquitistorelurker-db` automatically via `.WithReference(db)`
- Injects 4 provider secrets from `dotnet user-secrets` as environment variables
- Starts local `prom/prometheus` (auto-scrapes app at :5000/api/metrics)
- Starts local `grafana/grafana` with ubiquitistorelurker dashboard pre-provisioned

**`tests/UbiquitiStoreLurker.AppHostTests/`**
4 integration tests requiring Docker (`[Category("RequiresDocker")]`):

1. `AppHost_StartsHealthy` — verifies `/api/health/live` returns 200 after full boot
2. `AppHost_AddProduct_PollCycle_PersistsStockCheck` — end-to-end against real SQLite
3. `AppHost_SignalR_ConnectAndReceiveEvent` — SignalR hub event delivery
4. `AppHost_SqliteWeb_Accessible` — SQLiteWeb sidecar reachable

Run them: `dotnet test tests/UbiquitiStoreLurker.AppHostTests/ --filter "Category=RequiresDocker"`
Skip in CI: `--filter "Category!=RequiresDocker"`

### Changes to UbiquitiStoreLurker.Web

**Connection string key renamed:** `"Default"` → `"ubiquitistorelurker-db"` everywhere:

- `appsettings.json`, `appsettings.Development.json`, `Program.cs` (`GetConnectionString`), `docker-compose.yml` env var

**`builder.AddServiceDefaults()` added** to `Program.cs` — wires OTel tracing, metrics, and OTLP exporter. Completely inert in production (no env var → no exporter → no cost).

**`src/UbiquitiStoreLurker.Web/Telemetry/UbiquitiStoreLurkerActivities.cs`** — new `ActivitySource("UbiquitiStoreLurker.Web", "1.0.0")` shared by all 11 custom spans.

**11 custom spans** added across the business pipeline:

| Span | Location | Key Tags |
|---|---|---|
| `poll.execute` | PollWorkerService | product.id, product.url, poll.result |
| `parse.composite` | CompositeStockParser | parser.winner, stock.state |
| `parse.jsonld` | JsonLdStockParser | parser.matched, parser.confidence |
| `parse.button` | ButtonStateParser | parser.matched, parser.confidence |
| `parse.text` | TextContentParser | parser.matched, parser.matched_phrase |
| `state.evaluate` | StockStateMachine | state.from, state.to, state.changed, state.should_notify |
| `notification.dispatch` | NotificationDispatcher | providers, success_count, failure_count |
| `notification.send.{type}` | Each provider (5) | provider.type, provider.success, provider.error |
| `signalr.broadcast` | StockHubBroadcaster | hub.event, product.id |
| `scheduler.scan` | PollSchedulerService | products_due, products_enqueued |
| `cookie.refresh` | UbiquitiCookieJar | cookie.action (seed/persist/reload) |

In the Aspire Dashboard Traces tab, a single poll cycle shows the full waterfall: `poll.execute` → HTTP fetch (auto) → `parse.composite` → `parse.*` children → `state.evaluate` → `notification.dispatch` → per-provider children → `signalr.broadcast`. EF Core DB operations appear automatically as siblings via `EntityFrameworkCoreInstrumentation`.

### Changes to docker-compose.yml

Added `aspire-dashboard` service:

- Image: `mcr.microsoft.com/dotnet/aspire-dashboard:9.2`
- Static IP: `172.18.2.5` (apps tier, next after `172.18.2.4` ubiquitistorelurker)
- OTLP gRPC receiver on internal port 18889
- Traefik → `aspire.ubiquitistorelurker.rverbist.io` with `lan-only@file` middleware
- `ubiquitistorelurker` service gains `OTEL_EXPORTER_OTLP_ENDPOINT=http://aspire-dashboard:18889`

### Other Deliverables

**Dockerfile** — added `COPY UbiquitiStoreLurker.ServiceDefaults/` lines in restore + build phases; guard comment above final stage; `grep -i aspire` against the image returns nothing (PASS).

**`src/.vscode/tasks.json`** — 4 VS Code tasks under Terminal → Run Task:

1. `UbiquitiStoreLurker: Build Docker Image`
2. `UbiquitiStoreLurker: Deploy — Local Docker`
3. `UbiquitiStoreLurker: Aspire Publish`
4. `UbiquitiStoreLurker: Deploy — Proxmox` (docker save → ssh docker load, no registry)

**`UbiquitiStoreLurker.slnx`** — updated to include ServiceDefaults, AppHost, AppHostTests.

**`.gitignore`** — `aspire-output/` added.

**`README.md`** — "Development Modes" section, first-run secrets commands, SQLiteWeb entity list, migration notes table.

---

## Key Technical Deviations from Original Plan

| Plan spec | Actual implementation | Reason |
|---|---|---|
| `Aspire.Hosting.AppHost` 9.2.* | 13.2.* | Aspire 9 workload deprecated on .NET 10 SDK; 13.x is the NuGet-only model |
| `IsAspireHost=true` | omitted | Triggers deprecated workload path on .NET 10 |
| `IsAspireProjectResource` | Required on `UbiquitiStoreLurker.Web` ProjectReference | Aspire 13.x target needs metadata to generate proxy class |
| `Aspire.Hosting` in ServiceDefaults | omitted | KubernetesClient transitive CVE; unused in ServiceDefaults code |
| `ConfigureOpenTelemetryTracerProvider` on `IHostApplicationBuilder` | `builder.Services.AddOpenTelemetry().WithTracing/WithMetrics` | Extension not present in OTel 1.15.0 |
| `aspire add prometheus/grafana` packages | `AddContainer("prom/prometheus")` / `AddContainer("grafana/grafana")` | No `Aspire.Hosting.Prometheus/Grafana` NuGet packages exist in 13.x |
| `CommunityToolkit.Aspire.Hosting.SQLite` `*` | `13.1.1` | Latest stable pinned |
| `[Timeout]` in AppHostTests | `[CancelAfter]` + `CancellationToken` | `[Timeout]` obsolete in NUnit 4 on .NET 10 |
| `WaitForResourceHealthyAsync` on app | `ResourceNotificationService.WaitForResourceHealthyAsync` | Extension not on app object in Aspire.Hosting.Testing 13.2 |

---

## Development Mode Quick Reference

### Aspire AppHost (first run on a new machine)

```bash
# Set secrets once (never committed)
dotnet user-secrets set "Parameters:smtp-password"   "<value>" --project src/UbiquitiStoreLurker.AppHost
dotnet user-secrets set "Parameters:twilio-token"    "<value>" --project src/UbiquitiStoreLurker.AppHost
dotnet user-secrets set "Parameters:discord-webhook" "<value>" --project src/UbiquitiStoreLurker.AppHost
dotnet user-secrets set "Parameters:teams-webhook"   "<value>" --project src/UbiquitiStoreLurker.AppHost

# Run
dotnet run --project src/UbiquitiStoreLurker.AppHost
```

Access points:

- App: <http://localhost:5000>
- Aspire Dashboard: <http://localhost:15888>
- Grafana (local container): <http://localhost:3000>
- SQLiteWeb: Dashboard → Resources → click `ubiquitistorelurker-db-sqliteweb` link

### Docker Compose with Aspire Dashboard

```bash
docker compose up -d
```

- App: <http://localhost:8080> / <https://ubiquitistorelurker.rverbist.io>
- Aspire Dashboard: <http://localhost:18888> / <https://aspire.ubiquitistorelurker.rverbist.io>

### Deploy to Proxmox

VS Code → Terminal → Run Task → **UbiquitiStoreLurker: Deploy — Proxmox**
(streams image via `docker save | ssh docker load`, no registry required)

---

## Next Steps

Phase 12 was scoped as a learning exercise. Possible follow-up improvements:

- **App-level health check tag `ready`** — add `/api/health/ready` endpoint (currently only `/live` exists) to enable `.WithHttpHealthCheck("/api/health/ready")` in AppHost for a richer readiness probe
- **Aspire Dashboard auth** — `DASHBOARD__UNSECUREDALLOWANONYMOUS` is acceptable behind `lan-only@file`; if Tailscale exit nodes are added, consider enabling token auth
- **OTLP to lab Prometheus** — currently Prometheus scrapes `/api/metrics` (pull); adding OTLP push to an OTel Collector would complement the pull model
- **`dotnet user-secrets` integration test** — Test 1 currently passes with empty secrets (providers disabled by NotificationConfig in DB); consider seeding test secrets for richer coverage
