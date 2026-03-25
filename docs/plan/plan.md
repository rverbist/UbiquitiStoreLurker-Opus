# Stock Monitor — Implementation Plan

> **Plan ID**: `storefront-stock-monitor-opus-20260323`
> **Created**: 2026-03-23
> **Updated**: 2026-03-24
> **Status**: Completed (all 35 tasks — 32 original + 3 backlog — completed 2026-03-24)
> **Author**: GitHub Copilot (Claude Opus 4.6)

## 1. Executive Summary

A .NET 10 ASP.NET minimal web application that monitors web storefront product pages for stock availability changes, notifies users via multiple channels, and presents a real-time dashboard. Primary target is Docker (single container + SQLite). The system is single-user, no authentication, HTTP-only behind a reverse proxy.

### Goals

1. **Primary**: Detect stock changes on Ubiquiti EU Store product pages and notify immediately.
2. **Secondary**: Learn containerized .NET web apps, Vue 3, SignalR, Aspire, exotic notification APIs, Prometheus/Grafana, and modern frontend animation techniques.

### Non-Goals

- Multi-user / multi-tenant support
- Authentication or authorization
- HTTPS termination (handled by Traefik)
- Mobile native app
- WhatsApp / Facebook Messenger integration (deferred — prohibitive onboarding, see [notification research](research_findings_notification-providers.yaml))

---

## 2. Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                      Docker Container                       │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐   │
│  │              ASP.NET Core 10 Minimal API             │   │
│  │                                                      │   │
│  │  ┌─────────┐  ┌────────────┐  ┌──────────────────┐  │   │
│  │  │ Vue 3   │  │ Minimal    │  │ SignalR Hub      │  │   │
│  │  │ SPA     │  │ API        │  │ (WebSocket)      │  │   │
│  │  │ (wwwroot│  │ /api/*     │  │ /api/hub/monitor │  │   │
│  │  └─────────┘  └────────────┘  └──────────────────┘  │   │
│  │                      │                  ▲            │   │
│  │                      ▼                  │            │   │
│  │  ┌──────────────────────────────────────┤            │   │
│  │  │         Application Services         │            │   │
│  │  │                                      │            │   │
│  │  │  ┌──────────┐    ┌────────────────┐  │            │   │
│  │  │  │ Poll     │───▶│ Stock Parser   │  │            │   │
│  │  │  │ Scheduler│    │ (AngleSharp)   │  │            │   │
│  │  │  └──────────┘    └────────────────┘  │            │   │
│  │  │       │                  │            │            │   │
│  │  │       ▼                  ▼            │            │   │
│  │  │  ┌──────────┐    ┌────────────────┐  │            │   │
│  │  │  │ Poll     │    │ State Machine  │──┘            │   │
│  │  │  │ Worker   │    │ (Transitions)  │               │   │
│  │  │  └──────────┘    └───────┬────────┘               │   │
│  │  │       │                  │                        │   │
│  │  │       ▼                  ▼                        │   │
│  │  │  ┌──────────┐    ┌────────────────┐              │   │
│  │  │  │ Channel  │    │ Notification   │              │   │
│  │  │  │ (Queue)  │    │ Dispatcher     │              │   │
│  │  │  └──────────┘    └────────────────┘              │   │
│  │  └──────────────────────────────────────────────────┘   │
│  │                      │                                   │
│  │                      ▼                                   │
│  │  ┌──────────────────────────────────────────────────┐   │
│  │  │            SQLite (EF Core 10)                   │   │
│  │  │            /data/ubiquitistorelurker.db                  │   │
│  │  └──────────────────────────────────────────────────┘   │
│  └──────────────────────────────────────────────────────┘   │
│                                                             │
│  Volumes: /data (SQLite), /logs (CLEF files)                │
│  Port: 8080                                                 │
└─────────────────────────────────────────────────────────────┘
```

### Key Data Flow

1. **PollSchedulerService** (BackgroundService) scans SQLite for products where `NextPollDueAtUtc <= UtcNow`, enqueues `PollWorkItem` into a bounded Channel.
2. **PollWorkerService** (BackgroundService) dequeues one item at a time (single reader), executes the HTTP request through a rate-limited and resilient HttpClient pipeline.
3. **StockParser** (AngleSharp) runs a chain of parsers (JSON-LD → button state → text content) to determine stock state.
4. **StateMachine** compares new state against persisted state; if changed, creates a `StockTransition` record.
5. **NotificationDispatcher** fans out to enabled notification providers (Browser Push, Email, SMS, Teams, Discord).
6. **SignalR Hub** broadcasts poll results and state changes to connected web clients in real time.
7. **Vue 3 SPA** renders the product grid, request log, and celebration popup with GSAP animations and canvas-confetti.

---

## 3. Technology Stack

| Layer | Technology | Version | Notes |
|---|---|---|---|
| Runtime | .NET 10 (LTS) | 10.0.5 | GA 2025-11-11, supported through Nov 2028 |
| Web framework | ASP.NET Core 10 Minimal API | 10.0.5 | Built-in validation, SSE, OpenAPI 3.1 |
| ORM | Entity Framework Core 10 (SQLite) | 10.0.5 | Auto-migration, WAL mode, DateTimeOffset converter |
| Database | SQLite | 3.x (bundled) | Single file, WAL mode, volume-mounted |
| Frontend | Vue 3 (Composition API + Vite) | 3.5.x | `<script setup>`, TypeScript, Vue Router |
| Real-time | SignalR | Built-in | Strongly-typed hub, IHubContext injection |
| Animation | GSAP 3.12+ | 3.12.x | Timeline, stagger, elastic/bounce easing |
| Confetti | canvas-confetti | ~6 KB | Worker-offloaded, emoji shapes |
| Logging | Serilog + CLEF | Latest | Console + rolling file, structured JSON-lines |
| Metrics | prometheus-net | 8.2.1 | Custom counters, gauges, histograms + HTTP metrics |
| Health | ASP.NET Core Health Checks | Built-in | Poller heartbeat, DB, volume |
| Resilience | Microsoft.Extensions.Http.Resilience | 9.5.0 | Polly v8: retry, circuit breaker, timeout |
| Rate limiting | System.Threading.RateLimiting | Built-in | TokenBucketRateLimiter via DelegatingHandler |
| HTML parsing | AngleSharp | 1.3.0 | CSS selectors, JSON-LD extraction |
| Browser discovery | Microsoft.Playwright | 1.51.0 | HAR recording, network inspection (dev tool only) |
| Email | MailKit | 4.15.1 | SMTP, TLS, OAuth2 |
| SMS | Twilio | 7.14.3 | REST API via SDK |
| Browser push | Lib.Net.Http.WebPush | 3.3.1 | VAPID, RFC 8030 |
| Testing | NUnit 4.4.0 + NSubstitute 5.x | Latest | Constraint model assertions, interface mocking |
| Observability (dev) | .NET Aspire ServiceDefaults | 9.2+ | OTel + dashboard, optional |
| Container | Docker (Alpine) | - | Multi-stage build, non-root user |

### NuGet Package Manifest

```xml
<!-- src/UbiquitiStoreLurker.Web/UbiquitiStoreLurker.Web.csproj -->
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.*" PrivateAssets="all" />
<PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore" Version="10.0.*" />
<PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="9.*" />
<PackageReference Include="Serilog.AspNetCore" Version="*" />
<PackageReference Include="Serilog.Settings.Configuration" Version="*" />
<PackageReference Include="Serilog.Sinks.Console" Version="*" />
<PackageReference Include="Serilog.Sinks.File" Version="*" />
<PackageReference Include="Serilog.Formatting.Compact" Version="*" />
<PackageReference Include="prometheus-net.AspNetCore" Version="8.*" />
<PackageReference Include="prometheus-net.AspNetCore.HealthChecks" Version="8.*" />
<PackageReference Include="AngleSharp" Version="1.*" />
<PackageReference Include="Lib.Net.Http.WebPush" Version="3.*" />
<PackageReference Include="MailKit" Version="4.*" />
<PackageReference Include="Twilio" Version="7.*" />
```

```xml
<!-- tests/UbiquitiStoreLurker.Tests/UbiquitiStoreLurker.Tests.csproj -->
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
<PackageReference Include="NUnit" Version="4.4.*" />
<PackageReference Include="NUnit3TestAdapter" Version="4.6.*" />
<PackageReference Include="NUnit.Analyzers" Version="4.10.*" />
<PackageReference Include="NSubstitute" Version="5.*" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.*" />
<PackageReference Include="coverlet.collector" Version="6.*" />
```

### npm Package Manifest

```json
{
  "dependencies": {
    "vue": "^3.5",
    "vue-router": "^4",
    "@microsoft/signalr": "^8",
    "gsap": "^3.12",
    "canvas-confetti": "^1"
  },
  "devDependencies": {
    "vite": "^6",
    "@vitejs/plugin-vue": "^5",
    "typescript": "^5.7"
  }
}
```

---

## 4. Solution Structure

```
UbiquitiStoreLurker/
├── UbiquitiStoreLurker.slnx                         # SLNX format (.NET 10 default)
├── Directory.Build.props                     # Shared properties (TFM, nullable, implicit usings)
├── .dockerignore
├── Dockerfile                                # Multi-stage: build → test → runtime
├── docker-compose.yml                        # Production-like single service + aspire-dashboard (Phase 12)
├── docker-compose.override.yml               # Development overrides (dotnet watch)
│
├── src/
│   ├── UbiquitiStoreLurker.Web/
│   │   ├── UbiquitiStoreLurker.Web.csproj
│   │   ├── Program.cs                        # Composition root (DI, middleware, endpoints)
│   │   ├── appsettings.json                  # Base config
│   │   ├── appsettings.Development.json      # Dev overrides
│   │   │
│   │   ├── Data/
│   │   │   ├── UbiquitiStoreLurkerDbContext.cs       # EF Core DbContext
│   │   │   ├── Migrations/                   # EF Core migrations
│   │   │   └── Entities/
│   │   │       ├── Product.cs                # Monitored product (URL, code, name, images, state)
│   │   │       ├── StockCheck.cs             # Individual poll result record
│   │   │       ├── StockTransition.cs        # State change event
│   │   │       ├── NotificationConfig.cs     # Provider instance config
│   │   │       ├── NotificationLog.cs        # Sent notification record
│   │   │       └── AppSettings.cs            # User config (nickname, email, phone, global rate limits)
│   │   │
│   │   ├── Services/
│   │   │   ├── Polling/
│   │   │   │   ├── PollSchedulerService.cs   # BackgroundService: enqueues due items
│   │   │   │   ├── PollWorkerService.cs      # BackgroundService: single-reader channel consumer
│   │   │   │   ├── PollWorkItem.cs           # Channel item record
│   │   │   │   └── PollOptions.cs            # Min/max interval, scheduler scan interval
│   │   │   │
│   │   │   ├── Parsing/
│   │   │   │   ├── IStockParser.cs           # Interface: ParseAsync(html, context) → StockParseResult
│   │   │   │   ├── StockParseResult.cs       # State, confidence, evidence, strategy
│   │   │   │   ├── JsonLdStockParser.cs      # Schema.org Product → offers.availability
│   │   │   │   ├── ButtonStateParser.cs      # add-to-cart/sold-out button state
│   │   │   │   ├── TextContentParser.cs      # Body text pattern matching
│   │   │   │   ├── CompositeStockParser.cs   # Runs parsers in priority order
│   │   │   │   └── ProductInfoExtractor.cs   # og:title, og:image, JSON-LD → Product info
│   │   │   │
│   │   │   ├── StateMachine/
│   │   │   │   ├── StockStateMachine.cs      # Compare states, create transitions
│   │   │   │   └── StockState.cs             # Enum: Unknown, InStock, OutOfStock, Indeterminate
│   │   │   │
│   │   │   ├── Notifications/
│   │   │   │   ├── INotificationProvider.cs  # Interface: SendAsync(context) → result
│   │   │   │   ├── NotificationDispatcher.cs # Fans out to enabled providers
│   │   │   │   ├── BrowserPushProvider.cs    # Web Push via VAPID
│   │   │   │   ├── EmailProvider.cs          # SMTP via MailKit
│   │   │   │   ├── SmsProvider.cs            # Twilio REST API
│   │   │   │   ├── TeamsWebhookProvider.cs   # Adaptive Card POST
│   │   │   │   └── DiscordWebhookProvider.cs # Embed POST
│   │   │   │
│   │   │   └── Health/
│   │   │       ├── PollerHeartbeatCheck.cs   # Last poll age
│   │   │       └── VolumeAccessCheck.cs      # /data writable
│   │   │
│   │   ├── Hubs/
│   │   │   ├── UbiquitiStoreLurkerHub.cs            # Hub<IUbiquitiStoreLurkerClient>
│   │   │   └── IUbiquitiStoreLurkerClient.cs        # Strongly-typed client interface
│   │   │
│   │   ├── Endpoints/
│   │   │   ├── ProductEndpoints.cs           # CRUD for monitored products
│   │   │   ├── SettingsEndpoints.cs          # App config CRUD
│   │   │   ├── NotificationEndpoints.cs      # Notification provider CRUD
│   │   │   └── SetupEndpoints.cs             # Configuration form API
│   │   │
│   │   ├── Http/
│   │   │   ├── BrowserFingerprintHandler.cs  # Sets Chrome UA + full browser headers
│   │   │   ├── UbiquitiCookieHandler.cs      # Seeds + persists EU store cookies
│   │   │   └── ClientSideRateLimitHandler.cs # DelegatingHandler with jittered gap + 429 Retry-After block
│   │   │
│   │   ├── Metrics/
│   │   │   └── UbiquitiStoreLurkerMetrics.cs        # prometheus-net custom metrics
│   │   │
│   │   ├── Telemetry/                        # Phase 12 (planned)
│   │   │   └── UbiquitiStoreLurkerActivities.cs     # ActivitySource("UbiquitiStoreLurker.Web", "1.0.0")
│   │   │
│   │   ├── Infrastructure/
│   │   │   └── ServiceCollectionExtensions.cs # DI registration helpers
│   │   │
│   │   ├── wwwroot/                          # Vite build output (generated)
│   │   │   ├── index.html                    # SPA entry point (fallback)
│   │   │   └── assets/                       # JS, CSS bundles
│   │   │
│   │   └── ClientApp/                        # Vue 3 source (Vite project)
│   │       ├── package.json
│   │       ├── vite.config.ts
│   │       ├── tsconfig.json
│   │       ├── index.html                    # Vite dev entry
│   │       ├── public/
│   │       │   ├── sw.js                     # Service Worker for browser push
│   │       │   └── icons/                    # PWA icons
│   │       ├── src/
│   │       │   ├── main.ts                   # Vue app entry
│   │       │   ├── App.vue                   # Root component
│   │       │   ├── router.ts                 # Vue Router: /, /setup, /monitor
│   │       │   ├── composables/
│   │       │   │   ├── useSignalR.ts         # SignalR connection composable
│   │       │   │   ├── useProducts.ts        # Product state store
│   │       │   │   └── usePushNotifications.ts
│   │       │   ├── views/
│   │       │   │   ├── MonitorView.vue       # Product grid + request log
│   │       │   │   └── SetupView.vue         # Configuration form
│   │       │   ├── components/
│   │       │   │   ├── ProductCard.vue        # Thumbnail card with stock indicator
│   │       │   │   ├── ProductDetail.vue      # Right panel detail view
│   │       │   │   ├── RequestLog.vue         # Real-time request table
│   │       │   │   ├── StockPopup.vue         # "IN STOCK" celebration modal
│   │       │   │   ├── ConfettiOverlay.vue    # canvas-confetti layer
│   │       │   │   ├── SetupForm.vue          # User/rate/notification config
│   │       │   │   └── NotificationProviderConfig.vue
│   │       │   └── styles/
│   │       │       └── ubiquiti-theme.css     # Ubiquiti-inspired light theme
│   │       └── env.d.ts
│   │
│   ├── UbiquitiStoreLurker.ServiceDefaults/         # Phase 12 (planned — dev-only Aspire wiring)
│   │   ├── UbiquitiStoreLurker.ServiceDefaults.csproj
│   │   └── Extensions.cs                    # AddServiceDefaults(): OTel + OTLP exporter
│   │
│   └── UbiquitiStoreLurker.AppHost/                # Phase 12 (planned — Aspire orchestrator)
│       ├── UbiquitiStoreLurker.AppHost.csproj
│       ├── Program.cs                        # SQLite+SQLiteWeb, WaitFor, secrets, Prometheus, Grafana
│       └── Properties/launchSettings.json
│
├── tests/
│   ├── UbiquitiStoreLurker.Tests/
│   │   ├── UbiquitiStoreLurker.Tests.csproj
│   │   ├── Unit/
│   │   │   ├── Parsing/
│   │   │   │   ├── JsonLdStockParserTests.cs
│   │   │   │   ├── ButtonStateParserTests.cs
│   │   │   │   ├── TextContentParserTests.cs
│   │   │   │   └── ProductInfoExtractorTests.cs
│   │   │   ├── StateMachine/
│   │   │   │   └── StockStateMachineTests.cs
│   │   │   ├── Notifications/
│   │   │   │   └── NotificationDispatcherTests.cs
│   │   │   └── Polling/
│   │   │       └── PollSchedulerTests.cs
│   │   ├── Integration/
│   │   │   ├── ApiEndpointTests.cs
│   │   │   ├── HealthCheckTests.cs
│   │   │   └── SignalRHubTests.cs
│   │   ├── Fixtures/                         # Real Ubiquiti HTML + HAR files (Phase 3-D)
│   │   │   ├── fixture-instock.html
│   │   │   ├── fixture-outofstock.html
│   │   │   ├── fixture-malformed.html
│   │   │   └── DISCOVERY.md
│   │   └── Helpers/
│   │       └── TestWebApplicationFactory.cs
│   │
│   └── UbiquitiStoreLurker.AppHostTests/           # Phase 12 (planned — Aspire.Hosting.Testing)
│       └── UbiquitiStoreLurker.AppHostTests.csproj  # 4 NUnit integration tests, requires Docker daemon
│
├── tools/
│   └── UbiquitiStoreLurker.Discovery/               # Playwright discovery CLI (dev tool, not in Docker image)
│       ├── UbiquitiStoreLurker.Discovery.csproj
│       └── Program.cs
│
└── aspire-output/                            # Phase 12: generated by "aspire publish" — gitignored
```

---

## 5. Data Model

### Entity Relationship

```
AppSettings (1) ─────────── singleton config
Product (N) ────┬────────── StockCheck (N per product)
                ├────────── StockTransition (N per product)
                └────────── (state: StockState enum)
NotificationConfig (N) ──── NotificationLog (N per config)
```

### Entities

#### Product

| Column | Type | Notes |
|---|---|---|
| Id | int (PK, auto) | |
| Url | string (unique, required) | Storefront product page URL |
| ProductCode | string (unique) | Immutable after first extraction — fatal error if changes |
| Name | string? | Extracted from page (og:title / JSON-LD) |
| Description | string? | Extracted from page |
| ImageUrl | string? | Primary product image |
| ImageUrls | string? | JSON array of all image URLs |
| CurrentState | StockState | Enum: Unknown=0, InStock=1, OutOfStock=2, Indeterminate=3 |
| PreviousState | StockState | For transition detection |
| IsActive | bool | User can disable polling per product |
| SubscribedEvents | SubscriptionType | Flags enum: None=0, InStock=1, OutOfStock=2, Both=3 |
| NextPollDueAtUtc | DateTimeOffset | Scheduler picks up when <= UtcNow |
| LastPollAtUtc | DateTimeOffset? | |
| LastStateChangeAtUtc | DateTimeOffset? | |
| PollCount | int | Total polls executed |
| ErrorCount | int | Total errors |
| ConsecutiveErrors | int | Reset on success |
| CreatedAtUtc | DateTimeOffset | |
| UpdatedAtUtc | DateTimeOffset | |

#### StockCheck

| Column | Type | Notes |
|---|---|---|
| Id | long (PK, auto) | |
| ProductId | int (FK → Product) | |
| HttpMethod | string | GET |
| RequestUrl | string | |
| HttpStatusCode | int? | |
| DetectedState | StockState | |
| ParserStrategy | string? | Which parser matched |
| ParserConfidence | double? | 0.0–1.0 |
| ParserEvidence | string? | What was matched |
| DurationMs | int | |
| ErrorMessage | string? | |
| IsRetry | bool | |
| RetryAttempt | int | |
| CreatedAtUtc | DateTimeOffset | |

#### StockTransition

| Column | Type | Notes |
|---|---|---|
| Id | int (PK, auto) | |
| ProductId | int (FK → Product) | |
| FromState | StockState | |
| ToState | StockState | |
| DetectedAtUtc | DateTimeOffset | |
| StockCheckId | long (FK → StockCheck) | The check that detected the change |

#### NotificationConfig

| Column | Type | Notes |
|---|---|---|
| Id | int (PK, auto) | |
| ProviderType | string | "BrowserPush", "Email", "Sms", "Teams", "Discord" |
| DisplayName | string | User-friendly label |
| IsEnabled | bool | Only available when fully configured |
| SettingsJson | string | JSON blob: provider-specific config (webhook URL, SMTP settings, etc.) |
| CreatedAtUtc | DateTimeOffset | |
| UpdatedAtUtc | DateTimeOffset | |

#### NotificationLog

| Column | Type | Notes |
|---|---|---|
| Id | long (PK, auto) | |
| NotificationConfigId | int (FK) | |
| StockTransitionId | int (FK) | |
| ProviderType | string | |
| Success | bool | |
| ErrorMessage | string? | |
| SentAtUtc | DateTimeOffset | |

#### AppSettings

| Column | Type | Notes |
|---|---|---|
| Id | int (PK, always 1) | Singleton |
| Nickname | string | |
| Email | string? | Optional |
| Phone | string? | Optional |
| PollIntervalMinSeconds | int | Default: 30 |
| PollIntervalMaxSeconds | int | Default: 90 |
| MaxRetryAttempts | int | Default: 3 |
| RetryBaseDelaySeconds | int | Default: 2 |
| MinDelayBetweenRequestsSeconds | int | Default: 5 |
| VapidPublicKey | string? | Auto-generated on first setup |
| VapidPrivateKey | string? | Auto-generated on first setup |

### Indexes

```csharp
// Product
.HasIndex(p => p.Url).IsUnique();
.HasIndex(p => p.ProductCode).IsUnique();
.HasIndex(p => new { p.IsActive, p.NextPollDueAtUtc }); // Scheduler query

// StockCheck
.HasIndex(c => c.ProductId);
.HasIndex(c => c.CreatedAtUtc);

// StockTransition
.HasIndex(t => t.ProductId);
.HasIndex(t => t.DetectedAtUtc);
```

### Seeding

On first run (empty DB), seed one `AppSettings` row with defaults and one `NotificationConfig` row per provider type (only Browser Push enabled by default).

---

## 6. HTTP Client Pipeline

```
Request → [UserAgentHandler] → [ClientSideRateLimitHandler] → [ResilienceHandler] → [SocketsHttpHandler] → Network
```

### Rate Limiting (Layer 1 — Channel)

- Bounded Channel: capacity 100, SingleWriter, SingleReader
- Only one request executes at a time (serial consumption)

### Rate Limiting (Layer 2 — TokenBucket)

- TokenBucketRateLimiter via DelegatingHandler
- `TokenLimit: 1`, `ReplenishmentPeriod: configurable (default 5s)`
- Returns synthetic 429 if token unavailable (no HTTP call made)

### Resilience (Polly v8)

- **Retry**: 3 attempts, exponential backoff + jitter, base delay 2s
- **429 Retry-After**: Custom DelayGenerator reads `Retry-After` header (seconds or HTTP-date)
- **Circuit Breaker**: 30s sampling, 50% failure ratio, 60s break duration
- **Timeout**: 15s per attempt, 60s total
- **ShouldHandle**: 429, 408, 500, 502, 503, 504 — do NOT retry 403/404

### Respectful Polling

- `User-Agent: UbiquitiStoreLurker/1.0 (+mailto:user@example.com)` — configurable
- `Accept-Encoding: gzip, br` — automatic decompression
- `Accept: text/html`
- Randomized jitter between min/max poll interval (default 30–90s)
- Respect `Retry-After` headers on 429
- Back off on consecutive errors (multiply interval by `2^consecutiveErrors`, cap at 10min)

---

## 7. Stock Detection Engine

### Parser Chain (priority order)

1. **JsonLdStockParser**: Parse `<script type="application/ld+json">` for Schema.org Product → `offers.availability`. Confidence: 0.95. Most reliable when present.
2. **ButtonStateParser**: Query `button, [role='button']` for "Add to Cart" / "Sold Out" text and disabled state. Confidence: 0.85.
3. **TextContentParser**: Scan body text for stock indicator phrases. Confidence: 0.7.

**CompositeStockParser** runs all parsers in order. First result with confidence ≥ 0.7 is accepted. If all return `Indeterminate`, the state is `Indeterminate` and the product is re-polled sooner (halve the interval once).

### Product Info Extraction

On first successful response (bootstrap), extract:

- Name: `meta[property='og:title']` → JSON-LD `name` → `<h1>`
- Code/SKU: `meta[name='product:sku']` → JSON-LD `sku` → URL slug
- Description: `meta[property='og:description']` → JSON-LD `description`
- Images: `meta[property='og:image']` → JSON-LD `image` (array)
- Price: JSON-LD `offers.price` + `priceCurrency`

On subsequent requests, compare extracted `ProductCode` against stored value. If different → **fatal error** (log critical, disable product, notify user). Product code is immutable.

### State Machine

```
         ┌─────────────┐
         │   Unknown    │  (initial state)
         └──────┬───────┘
                │ first successful parse
                ▼
    ┌───────────┴───────────┐
    │                       │
    ▼                       ▼
┌────────┐            ┌──────────┐
│ InStock │◄──────────▶│ OutOfStock│
└────┬───┘  transition └─────┬────┘
     │                       │
     ▼                       ▼
    Notify              Notify (if subscribed)
```

- **Unknown → InStock/OutOfStock**: Initial state discovery. No notification (user chose initial silence).
- **InStock → OutOfStock**: Notify if subscribed to `OutOfStock` events.
- **OutOfStock → InStock**: Notify if subscribed to `InStock` events. This is the **primary use case** — triggers the celebration popup.
- **Indeterminate**: Logged, no state transition created, product scheduled for earlier re-poll.

---

## 8. Notification System

### Provider Abstraction

```csharp
public interface INotificationProvider
{
    string ProviderType { get; }
    Task<NotificationResult> SendAsync(NotificationContext context, CancellationToken ct);
    bool ValidateConfig(string settingsJson, out string? error);
}
```

### NotificationDispatcher

- On state change, queries `NotificationConfig` for enabled providers
- For each, calls `SendAsync` with product info + transition details
- Logs result to `NotificationLog`
- Failures are logged but do not block other providers (fan-out, best-effort)
- No retry on notification failure (avoid spam on transient failures)

### Provider Configuration Matrix

| Provider | Required Settings | Auth Model | Complexity |
|---|---|---|---|
| Browser Push | (auto: VAPID keys) | VAPID | Low |
| Email | SMTP host, port, TLS, username, password, from address | SMTP credentials | Low |
| SMS (Twilio) | Account SID, Auth Token, From phone number | API key | Medium |
| Teams | Webhook URL | Embedded in URL | Low |
| Discord | Webhook URL | Embedded in URL | Low |

### Default Seed

On first run, create one NotificationConfig per provider type:

- Browser Push: enabled = true (no config needed, VAPID auto-generated)
- Email: enabled = false
- SMS: enabled = false
- Teams: enabled = false
- Discord: enabled = false

### Deferred Providers

WhatsApp and Facebook Messenger are **not implemented** in v1. The UI shows them as "Coming Soon" placeholders. Rationale: business verification, template approval, and ongoing compliance overhead is prohibitive for a personal project.

---

## 9. Frontend (Vue 3 + Vite)

### Routing

| Path | Component | Description |
|---|---|---|
| `/` | — | Redirect to `/monitor` |
| `/setup` | SetupView | Configuration form |
| `/monitor` | MonitorView | Dashboard with product grid + request log |

### MonitorView Layout

```
┌──────────────────────────────────────────────────────────────┐
│  Header: "Stock Monitor" + connection status indicator       │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐          │
│  │ Product │ │ Product │ │ Product │ │ Product │   ...     │
│  │  Card   │ │  Card   │ │  Card   │ │  Card   │          │
│  │ (thumb) │ │ (thumb) │ │ (thumb) │ │ (thumb) │          │
│  │ [stock] │ │ [stock] │ │ [stock] │ │ [stock] │          │
│  │ 🔔      │ │         │ │ 🔔      │ │ 🔔      │          │
│  └─────────┘ └─────────┘ └─────────┘ └─────────┘          │
│                                                              │
│  ─ ─ ─ ─ ─ ─ ─ ─ when card clicked ─ ─ ─ ─ ─ ─ ─ ─ ─ ─  │
│                                                              │
│  ┌─────────────────────┐ ┌──────────────────────────────┐   │
│  │  Product Grid       │ │  Detail Panel               │   │
│  │  (shrinks to left)  │ │  - Product info + images    │   │
│  │                     │ │  - Request telemetry        │   │
│  │                     │ │  - Stock telemetry          │   │
│  │                     │ │  - Polling event history    │   │
│  └─────────────────────┘ └──────────────────────────────┘   │
│                                                              │
├──────────────────────────────────────────────────────────────┤
│  Request Log (max 100, recent first)                         │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ Method │ URL │ Product │ Stock │ Status │ Time │ Error │  │
│  ├────────┼─────┼─────────┼───────┼────────┼──────┼───────┤  │
│  │ GET    │ ... │ UNAS-4  │ Out   │ 200    │ 3.2s │       │  │ ← new rows animate in
│  │ GET    │ ... │ UNAS-8  │ In    │ 200    │ 1.8s │       │  │
│  │ ...    │     │         │       │        │      │       │  │
│  └────────────────────────────────────────────────────────┘  │ ← bottom row tumbles off
└──────────────────────────────────────────────────────────────┘
```

### Product Card Design

- Thumbnail image (product image or placeholder)
- Product name label
- Border color: green (#2FB343) = InStock, red (#F44336) = OutOfStock, grey (#BDBDBD) = Unknown/Indeterminate
- Bell icon (🔔) if subscribed to next state change; muted if not
- Click → selected state (elevated, blue accent border), opens detail panel

### Detail Panel

- Full product info (name, code, description, price, all images as carousel)
- **Request telemetry**: total polls, last poll time, next scheduled poll, error count, retry count
- **Stock telemetry**: current state badge, date of last change, days since last change
- **Polling event history**: table sorted recent-first, columns: HTTP method, URL, detected state, status code, duration, error, timestamp
- Updated in real time via SignalR; new rows inserted at top with slide-down animation
- Bottom row exit: GSAP `gsap.to(row, { y: 200, rotation: 15, opacity: 0, duration: 0.5, ease: "power2.in" })` — tumble off screen

### Celebration Popup (StockPopup.vue)

Triggered when a subscribed product transitions from OutOfStock → InStock:

- **Full-screen modal** (z-index above everything except confetti)
- Content: product image (large), product name, **"IT'S IN STOCK!"** (big, bold), "Click anywhere to buy"
- The entire modal is an `<a>` linking to the product page on the store
- **Entrance animation** (PowerPoint 98 energy):
  - GSAP timeline: scale from 0 → 1.3 → 1.0 with elastic ease
  - Simultaneous rotation: 0° → -5° → 5° → 0° (wobble)
  - Text elements fly in from different directions with stagger
  - Star burst / flash overlay on entrance (CSS radial-gradient animated)
  - Sound effect (optional, `<audio>` element, user-click gated)
- **Confetti layer** (z-index ABOVE the popup, the popup is in front of page content):
  - `canvas-confetti` with `particleCount: 200, spread: 160, origin: { y: 0.2 }` on loop while popup is visible
  - Emoji confetti shapes: 🎉🎊🛒💰✨
- Dismiss: click anywhere (navigates to store) or Escape key (closes without navigation)

### Ubiquiti-Inspired Theme

- Font: system font stack (`-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, ...`)
- Background: `#F6F6F8` (light grey)
- Cards: `#FFFFFF` with `box-shadow: 0 1px 3px rgba(0,0,0,0.08)`
- Accent blue: `#0559C9` (Ubiquiti brand blue)
- Success green: `#2FB343`
- Error red: `#F44336`
- Text: `#1A1A1A` primary, `#6C757D` secondary
- Border radius: `8px` on cards, `4px` on buttons
- Max content width: `1200px`, centered

### Vite + ASP.NET Integration

- `vite.config.ts`:
  - `build.outDir: '../../wwwroot'` (output to ASP.NET static files root)
  - `build.emptyOutDir: true`
  - `server.proxy: { '/api': 'http://localhost:5000' }` (dev proxy to Kestrel)
- ASP.NET `Program.cs`:
  - `app.MapStaticAssets()` (serves wwwroot with cache headers)
  - `app.MapFallbackToFile("index.html")` (SPA fallback for Vue Router history mode)
- Build pipeline:
  - `npm run build` in ClientApp → outputs to wwwroot
  - `dotnet publish` includes wwwroot in output
  - Dockerfile: npm install + build in SDK stage, copy wwwroot to runtime stage

---

## 10. Real-Time Communication (SignalR)

### Hub Definition

```csharp
public interface IUbiquitiStoreLurkerClient
{
    Task StockStateChanged(StockStateDto state);
    Task PollCompleted(PollResultDto result);
    Task RequestLogEntry(RequestLogDto entry);
    Task AlertTriggered(AlertDto alert);
}

public class UbiquitiStoreLurkerHub : Hub<IUbiquitiStoreLurkerClient> { }
```

### Server → Client Events

| Event | Trigger | Payload |
|---|---|---|
| `PollCompleted` | Every stock check (success or failure) | Product code, URL, status code, duration, detected state |
| `StockStateChanged` | State transition detected | Product code, from/to state, transition type, timestamp |
| `RequestLogEntry` | Every HTTP request made | Method, URL, product details, stock state, error info |
| `AlertTriggered` | Subscribed in-stock change | Full product info, image URL, store URL, transition |

### Vue 3 Composable

`useSignalR(hubUrl)` → reactive `connectionState`, `on(method, callback)`, `invoke(method, ...args)`

- Auto-reconnect: `[0, 2000, 10000, 30000]` ms delays
- Manual retry on initial connection failure (5s interval)
- onMounted → start, onUnmounted → stop

---

## 11. Observability

### Serilog (CLEF)

- Bootstrap logger → `UseSerilog()` → `UseSerilogRequestLogging()`
- Console sink: CompactJsonFormatter (Docker stdout → collected by Docker log driver)
- File sink: CompactJsonFormatter, rolling daily, 50MB cap, 14 days retained, `/logs/stock-monitor-.clef`
- Structured properties: `ProductCode`, `StockState`, `PollDuration`, `ProviderType`, `HttpStatusCode`
- Level overrides: ASP.NET Core → Warning, EF Core → Warning, SignalR → Information, HttpClient → Warning

### Prometheus Metrics

| Metric | Type | Labels |
|---|---|---|
| `stock_monitor_polls_total` | Counter | product_code, status |
| `stock_monitor_poll_duration_seconds` | Histogram | — |
| `stock_monitor_stock_state` | Gauge | product_code, state_name |
| `stock_monitor_notifications_sent_total` | Counter | provider_type, status |
| `stock_monitor_queue_depth` | Gauge | — |
| `stock_monitor_state_changes_total` | Counter | product_code, from, to |

Plus built-in HTTP metrics from `UseHttpMetrics()`.

Scrape endpoint: `GET /api/metrics` (Prometheus exposition format).

Prometheus config addition (existing stack at `/opt/docker/infra/monitoring/`):

```yaml
- job_name: stock-monitor
  scrape_interval: 15s
  metrics_path: /api/metrics
  static_configs:
    - targets: ['ubiquitistorelurker:8080']
```

### Health Checks

| Endpoint | Tags | Checks |
|---|---|---|
| `/api/health` | ready | DB connectivity, poller heartbeat, volume access |
| `/api/health/live` | live | Always healthy (process alive) |

Docker HEALTHCHECK targets `/api/health/live`.

### Aspire (Development Only) — Phase 12: COMPLETED (2026-03-24)

Phase 12 added .NET Aspire 9.2 as a local development overlay. Production observability (Serilog CLEF + prometheus-net → existing Grafana) is unaffected.

#### Development modes (three options)

| Mode | App | Aspire Dashboard | Grafana | SQLiteWeb |
|---|---|---|---|---|
| `dotnet run` (no Aspire) | `localhost:5000` | — | `grafana.rverbist.io` | — |
| `dotnet run AppHost` | `localhost:5000` | `localhost:15888` (auto) | `localhost:3000` (local container) | dynamic port in Dashboard |
| `docker compose up` (local or Proxmox) | `:8080` / `ubiquitistorelurker.rverbist.io` | `:18888` / `aspire.ubiquitistorelurker.rverbist.io` | `grafana.rverbist.io` (existing lab) | — |

The two Aspire-enabled modes are mutually exclusive — you never have two Dashboards running.

#### Projects to create

- `src/UbiquitiStoreLurker.ServiceDefaults/` — OTel wiring (ASP.NET Core, HttpClient, EF Core, runtime, custom `ActivitySource`)
- `src/UbiquitiStoreLurker.AppHost/` — SQLite resource + `.WithSqliteWeb()`, `.WaitFor(db)`, secrets parameters, local Prometheus + Grafana containers
- `tests/UbiquitiStoreLurker.AppHostTests/` — 4 NUnit integration tests via `Aspire.Hosting.Testing 9.2.*`

#### Wire-in to UbiquitiStoreLurker.Web

```csharp
// Program.cs — before builder.Build()
// No-ops if OTEL_EXPORTER_OTLP_ENDPOINT is not set (safe in production)
builder.AddServiceDefaults();
```

Connection string key renamed: `"Default"` → `"ubiquitistorelurker-db"` to match the Aspire resource name. Dev fallback in `appsettings.Development.json`; production path via Docker volume unchanged.

#### Custom domain instrumentation — 11 ActivitySource spans

`UbiquitiStoreLurker.Web/Telemetry/UbiquitiStoreLurkerActivities.cs` — `ActivitySource("UbiquitiStoreLurker.Web", "1.0.0")`

**HIGH value** (core business pipeline, dark without custom spans):

| Span | Service | Key tags |
|---|---|---|
| `poll.execute` | `PollWorkerService` | `product.id`, `product.url`, `poll.result` |
| `parse.composite` | `CompositeStockParser` | `parser.winner`, `stock.state` |
| `parse.jsonld` | `JsonLdStockParser` | `parser.matched`, `parser.confidence` |
| `parse.button` | `ButtonStateParser` | `parser.matched`, `parser.confidence` |
| `parse.text` | `TextContentParser` | `parser.matched`, `parser.matched_phrase` |
| `state.evaluate` | `StockStateMachine` | `state.from`, `state.to`, `state.changed` |
| `notification.dispatch` | `NotificationDispatcher` | `providers`, `success_count` |
| `notification.send.{type}` | per-provider | `provider.type`, `provider.success` |
| `signalr.broadcast` | `StockHubBroadcaster` | `hub.event`, `product.id` |

**MEDIUM value** (infrastructure helpers):

| Span | Service | When |
|---|---|---|
| `scheduler.scan` | `PollSchedulerService` | every scan; tags: `products_due`, `products_enqueued` |
| `cookie.refresh` | `UbiquitiCookieHandler` | only on seed/persist/reload state change |

EF Core DB queries and all HTTP spans are auto-instrumented — no custom spans needed.

#### Standalone Dashboard in docker-compose (Phase 12)

```yaml
aspire-dashboard:
  image: mcr.microsoft.com/dotnet/aspire-dashboard:9.2
  container_name: aspire-dashboard
  networks:
    proxy:
      ipv4_address: 172.18.2.5
  ports:
    - "18888:18888"    # Dashboard UI
    - "4317:18889"     # OTLP gRPC receiver
  environment:
    - DASHBOARD__UNSECUREDALLOWANONYMOUS=true   # safe: lan-only middleware
    - DOTNET_DASHBOARD_OTLP_ENDPOINT_URL=http://+:18889
  labels:
    - "traefik.http.routers.ubiquitistorelurker-aspire.rule=Host(`aspire.ubiquitistorelurker.rverbist.io`)"
    ...
```

App environment additions:

```yaml
- OTEL_EXPORTER_OTLP_ENDPOINT=http://aspire-dashboard:18889
- OTEL_EXPORTER_OTLP_PROTOCOL=grpc
```

#### Secrets / parameters (AppHost)

| Parameter | Env var injected | First-run command |
|---|---|---|
| `smtp-password` | `Email__Password` | `dotnet user-secrets set "Parameters:smtp-password" "<value>"` |
| `twilio-token` | `Twilio__AuthToken` | `dotnet user-secrets set "Parameters:twilio-token" "<value>"` |
| `discord-webhook` | `Discord__WebhookUrl` | `dotnet user-secrets set "Parameters:discord-webhook" "<value>"` |
| `teams-webhook` | `Teams__WebhookUrl` | `dotnet user-secrets set "Parameters:teams-webhook" "<value>"` |

Connection string injected automatically by `WithReference(db)` — no manual config.

#### Production guard

Dockerfile copies only `UbiquitiStoreLurker.Web/` — ServiceDefaults and AppHost are intentionally excluded. `AddServiceDefaults()` silently no-ops when `OTEL_EXPORTER_OTLP_ENDPOINT` is unset. Production observability continues via prometheus-net → existing Grafana lab stack.

#### Proxmox Grafana — no new containers

On Proxmox, the app wires into the already-running lab stack. No new Prometheus or Grafana containers deploy:

- Prometheus scrape target for `ubiquitistorelurker:8080/api/metrics` already configured in Phase 10
- Grafana dashboard (`ubiquitistorelurker-dashboard.json`) already provisioned in Phase 10
- `grafana.rverbist.io` already shows the ubiquitistorelurker dashboard

---

## 12. Docker

### Dockerfile (Multi-Stage)

```
Stage 1: build (SDK 10.0-alpine)
  - Copy csproj files, restore
  - Copy source, npm install + build (client app)
  - dotnet publish → /app

Stage 2: test (from build)
  - dotnet test --no-build
  - Fail build if tests fail (CI gate)

Stage 3: final (aspnet 10.0-alpine)
  - Create appuser (UID 1654)
  - Create /data, /logs with correct ownership
  - Copy published output
  - USER appuser
  - EXPOSE 8080
  - HEALTHCHECK via wget
  - ENTRYPOINT ["dotnet", "UbiquitiStoreLurker.Web.dll"]
```

### Docker Compose

Production-like deployment matching the existing Proxmox lab infrastructure:

```yaml
services:
  ubiquitistorelurker:
    build: .
    image: ubiquitistorelurker:latest
    container_name: ubiquitistorelurker
    restart: unless-stopped
    networks:
      proxy:
        ipv4_address: 172.18.2.4      # apps tier
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__Default=Data Source=/data/ubiquitistorelurker.db
    volumes:
      - /devpool/docker-volumes/apps/ubiquitistorelurker/data:/data
      - /devpool/docker-volumes/apps/ubiquitistorelurker/logs:/logs
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.ubiquitistorelurker.rule=Host(`ubiquitistorelurker.rverbist.io`)"
      - "traefik.http.routers.ubiquitistorelurker.entrypoints=websecure"
      - "traefik.http.routers.ubiquitistorelurker.tls.certresolver=letsencrypt"
      - "traefik.http.routers.ubiquitistorelurker.middlewares=lan-only@file"
      - "traefik.http.services.ubiquitistorelurker.loadbalancer.server.port=8080"
    healthcheck:
      test: ["CMD", "wget", "--no-verbose", "--tries=1", "--spider", "http://localhost:8080/api/health/live"]
      interval: 30s
      timeout: 5s
      retries: 3
      start_period: 15s

networks:
  proxy:
    external: true
```

Location: `/opt/docker/apps/ubiquitistorelurker/docker-compose.yml`

---

## 13. Testing Strategy

### Coverage Philosophy

Target ~70% coverage. Focus on business logic, not CRUD plumbing.

### Unit Tests (~70% of test suite)

| Test Class | Tests | Priority |
|---|---|---|
| JsonLdStockParserTests | Parse in-stock/out-of-stock/malformed JSON-LD fixtures | High |
| ButtonStateParserTests | Active/disabled button states, various selectors | High |
| TextContentParserTests | Stock phrase matching, edge cases | Medium |
| CompositeStockParserTests | Priority ordering, confidence thresholds | High |
| ProductInfoExtractorTests | og:meta, JSON-LD, missing fields | Medium |
| StockStateMachineTests | All state transitions, no-change, initial discovery | High |
| NotificationDispatcherTests | Fan-out to providers, partial failure handling | High |
| PollSchedulerTests | Due item selection, jitter application, overdue on restart | Medium |
| ProductCodeImmutabilityTests | Code change → exception | High |
| ConfigValidationTests | Invalid URLs, settings range validation | Medium |

### Integration Tests (~25% of test suite)

| Test Class | Tests | Priority |
|---|---|---|
| HealthCheckTests | /api/health → 200, /api/health/live → 200 | High |
| ApiEndpointTests | Products CRUD, Settings CRUD, validation errors | Medium |
| SignalRHubTests | Connect, receive PollCompleted message | Medium |
| DatabaseMigrationTests | Schema creates successfully on in-memory SQLite | Medium |

### Test Fixtures

HTML fixture files (`tests/Fixtures/`) captured from real storefront pages during the Playwright discovery phase. Committed to source control.

### TestWebApplicationFactory

Custom `WebApplicationFactory<Program>` that:

- Replaces SQLite connection with `:memory:` database
- Disables BackgroundServices (PollScheduler, PollWorker)
- Seeds test data

### What We Don't Test

- Individual notification provider HTTP calls (mocked at the interface level)
- Vite/Vue build output (visual testing is manual)
- Playwright discovery tool (manual/exploratory)
- Serilog/Prometheus output format (trust the libraries)

---

## 14. Implementation Phases

### Phase 0: Project Scaffolding ✅

**Goal**: Empty solution that builds, runs, and has a working Docker image.

1. Create solution and project structure per §4
2. `dotnet new web` base → configure Serilog bootstrap, health endpoints
3. Create Dockerfile + docker-compose.yml
4. Create Vue 3 + Vite project in ClientApp/ with empty App.vue
5. Wire Vite build → wwwroot, SPA fallback
6. `docker compose up` → verify health endpoint responds
7. Initial EF Core migration with empty schema
8. NUnit test project scaffold, one green test

**Deliverable**: `docker compose up` → `curl /api/health` → 200 OK.

### Phase 1: Data Model + EF Core ✅

**Goal**: Complete data model with migrations, seeded defaults.

1. Implement all entities from §5
2. Configure DbContext: DateTimeOffset converter, indexes, query filters
3. Create migration `InitialCreate`
4. Auto-migration on startup
5. Seed AppSettings defaults + NotificationConfig per provider
6. Unit tests: model validation, ProductCode immutability
7. Integration test: migration applies, seed data present

**Deliverable**: SQLite DB created on startup with schema and seed data.

### Phase 2: Polling Engine ✅

**Goal**: BackgroundServices poll a URL and persist results.

1. Implement PollSchedulerService (Channel producer)
2. Implement PollWorkerService (Channel consumer)
3. Configure IHttpClientFactory with resilience + rate limiting
4. Implement ClientSideRateLimitHandler
5. Log each request with Serilog structured properties
6. Persist StockCheck records
7. Unit tests: scheduler enqueue logic, rate limiter behavior
8. Manual test with hardcoded URL

**Deliverable**: Polling loop running, StockCheck rows in DB, logs in CLEF.

### Phase 3: Stock Detection ✅

**Goal**: Parse HTML into stock state, extract product info.

1. **Playwright Discovery**: Run discovery tool against both example URLs (in-stock + out-of-stock). Record HAR, capture DOM, identify available API endpoints and stock indicators. Save fixtures.
2. Implement JsonLdStockParser
3. Implement ButtonStateParser
4. Implement TextContentParser
5. Implement CompositeStockParser
6. Implement ProductInfoExtractor
7. Unit tests with captured HTML fixtures
8. Implement StockStateMachine + state transition recording

**Deliverable**: Parser chain correctly identifies stock state from live pages. Test fixtures committed.

### Phase 4: Notification System ✅

**Goal**: State changes trigger notifications through configured providers.

1. Implement INotificationProvider interface
2. Implement BrowserPushProvider (VAPID, Service Worker)
3. Implement EmailProvider (MailKit/SMTP)
4. Implement SmsProvider (Twilio)
5. Implement TeamsWebhookProvider (Adaptive Card)
6. Implement DiscordWebhookProvider (embed)
7. Implement NotificationDispatcher (fan-out)
8. Unit tests: dispatcher logic with NSubstitute mocks
9. Manual test: configure Discord webhook, trigger notification

**Deliverable**: State change → notifications dispatched to enabled providers.

### Phase 5: API Endpoints ✅

**Goal**: Minimal API endpoints for frontend consumption.

1. ProductEndpoints: GET list, GET by ID, POST (add URL), PUT (update settings), DELETE
2. SettingsEndpoints: GET, PUT
3. NotificationEndpoints: GET list, PUT (update config), POST (test send)
4. SetupEndpoints: combined config view + save
5. Request validation via `AddValidation()`
6. Integration tests: CRUD operations, validation error responses

**Deliverable**: Full REST API for all frontend needs, documented via OpenAPI.

### Phase 6: SignalR Real-Time ✅

**Goal**: Server pushes events to connected clients.

1. Implement UbiquitiStoreLurkerHub + IUbiquitiStoreLurkerClient
2. Inject IHubContext into PollWorkerService and NotificationDispatcher
3. Broadcast PollCompleted, StockStateChanged, RequestLogEntry, AlertTriggered
4. Integration test: connect to hub, receive event

**Deliverable**: Events flow from poller → hub → connected clients.

### Phase 7: Vue 3 Frontend — Scaffold + Monitor ✅

**Goal**: Working MonitorView with product grid and request log.

1. Vue Router setup (/, /setup, /monitor)
2. useSignalR composable
3. ProductCard component with stock state border color + bell icon
4. Product grid (responsive CSS grid)
5. ProductDetail panel (slide-in from right)
6. RequestLog table (max 100 rows, newest first)
7. Real-time updates wired to SignalR events
8. Ubiquiti-inspired CSS theme

**Deliverable**: Navigate to /monitor → see products, click for detail, live request log.

### Phase 8: Vue 3 Frontend — Animations + Celebration ✅

**Goal**: Polish animations and the stock-in celebration popup.

1. GSAP: request log row insertion animation (slide-down + fade-in)
2. GSAP: bottom row tumble-off animation (fall + rotate + fade)
3. GSAP: product detail panel slide-in/out
4. GSAP: product card selection emphasis (scale + shadow)
5. StockPopup.vue: in-stock celebration modal
   - GSAP entrance timeline (elastic scale + wobble + stagger text)
   - Star burst overlay (CSS animated radial-gradient)
   - Full-modal anchor link to product page
6. ConfettiOverlay.vue: canvas-confetti loop on popup visibility
7. Modal dismiss: click → navigate, Escape → close

**Deliverable**: Full animation suite, PowerPoint 98 energy popup with confetti.

### Phase 9: Vue 3 Frontend — Setup Page ✅

**Goal**: Configuration form for user settings, rate limits, and notification providers.

1. SetupForm: nickname, email, phone fields
2. Rate limit / retry settings section
3. NotificationProviderConfig: dynamic list of providers, each with:
   - Provider type dropdown
   - Enable/disable toggle (only available when fully configured)
   - Provider-specific settings fields (rendered based on type)
   - "Add provider" button, "Remove" button
4. Product configuration: add URL, enable/disable, subscription events
5. VAPID key display (read-only, auto-generated)
6. Save API call

**Deliverable**: Fully functional setup page persisting to SQLite.

### Phase 10: Prometheus + Grafana ✅

**Goal**: Metrics exported, scrape target configured, dashboard provisioned.

1. Implement UbiquitiStoreLurkerMetrics (counters, gauges, histograms)
2. Wire metrics into poller, dispatcher, state machine
3. `MapMetrics("/api/metrics")`; `UseHttpMetrics()`
4. Health check forwarding to Prometheus
5. Add scrape target to existing Prometheus config
6. Create Grafana dashboard JSON (provisioned via file)
7. Dashboard panels: poll rate, success/error ratio, stock state timeline, notification counts, queue depth

**Deliverable**: Grafana dashboard showing stock monitor operational metrics.

### Phase 11: Browser Push (Service Worker) ✅

**Goal**: Push notifications delivered to browser even when tab is closed.

1. sw.js: push event → showNotification, notificationclick → openWindow
2. Client-side: Notification.requestPermission, pushManager.subscribe
3. Send PushSubscription to server API
4. Server: store subscription, send via Lib.Net.Http.WebPush on state change
5. Handle 410 Gone (expired subscription cleanup)

**Deliverable**: Close the browser tab, get a push notification when stock changes.

### Phase 12: Aspire Integration ✅ (completed 2026-03-24)

**Status**: Completed. All 14 sub-tasks (12-A through 12-N) implemented. Production stack fully operational and unaffected.

| Task ID | Status | Description |
|---|---|---|
| `task-phase-12-a` | ✅ | Create `UbiquitiStoreLurker.ServiceDefaults` project — OTel wiring (ASP.NET Core, HttpClient, EF Core, runtime, custom `ActivitySource`) |
| `task-phase-12-b` | ✅ | Create `UbiquitiStoreLurker.AppHost` project — SQLite + `.WithSqliteWeb()`, `.WaitFor(db)`, 4 secret parameters |
| `task-phase-12-c` | ✅ | Wire `builder.AddServiceDefaults()` into `Program.cs`; rename connection string key `"Default"` → `"ubiquitistorelurker-db"` |
| `task-phase-12-d` | ✅ | Add 11 custom `ActivitySource` spans across the domain pipeline (poll, parsers, state machine, notifications, SignalR) |
| `task-phase-12-e` | ✅ | Verify SQLiteWeb browser shows live data (same SQLite file as app — no code changes) |
| `task-phase-12-f` | ✅ | First-run secrets setup via `dotnet user-secrets` — 4 provider parameters, never in files |
| `task-phase-12-g` | ✅ | Verify `.WaitFor(db)` startup ordering eliminates cold-start DB race condition |
| `task-phase-12-h` | ✅ | 4 NUnit integration tests using `Aspire.Hosting.Testing 9.2.*` (requires Docker daemon) |
| `task-phase-12-i` | ✅ | Local Prometheus + Grafana via `aspire add` (dev only); Proxmox wires existing lab stack — no new containers |
| `task-phase-12-j` | ✅ | Settings migration: connection string key rename, secrets to user-secrets, document migration table |
| `task-phase-12-k` | ✅ | Standalone Aspire Dashboard in `docker-compose.yml` at IP `172.18.2.5`, Traefik at `aspire.ubiquitistorelurker.rverbist.io` |
| `task-phase-12-l` | ✅ | 4 VS Code tasks: Build Docker Image, Deploy — Local Docker, aspire publish, Deploy — Proxmox (docker save \| ssh docker load) |
| `task-phase-12-m` | ✅ | Solution scaffolding: add 3 projects to `UbiquitiStoreLurker.slnx`, add `aspire-output/` to `.gitignore`, update README with 3 dev modes |
| `task-phase-12-n` | ✅ | Production Dockerfile guard — verify no Aspire packages in final image; add explanatory comment |

**Deliverable**: `dotnet run --project UbiquitiStoreLurker.AppHost` → Dashboard shows 11-span domain waterfall traces; SQLiteWeb browser with live data; standalone Dashboard in docker-compose at `aspire.ubiquitistorelurker.rverbist.io`; VS Code deploy tasks; Aspire.Hosting.Testing tests (66/66) pass; production image free of Aspire packages. Dockerfile guard: PASS.

### Phase 13: Polish + Documentation ✅

**Goal**: Production-ready deployment, README, final testing.

1. Review and harden all error handling
2. Validate Docker deployment end-to-end on Proxmox
3. Traefik label configuration for `ubiquitistorelurker.rverbist.io`
4. README.md with setup instructions, screenshots
5. Update AGENTS.md with new container/service
6. Run full test suite, verify coverage targets
7. Manual smoke test: add products, observe polling, trigger state change, verify notifications

**Deliverable**: Production deployment, documented, monitored, operational.

### Post-Delivery Discovery (2026-03-24)

After deployment, a live review session revealed that the stock parsers fail against real Ubiquiti EU store pages despite all tests passing. The root cause: Phase 3 HTML fixtures were synthetic hand-crafted stubs, not captured from the real store as the plan required. Three additional compounding issues were identified in the HTTP client layer.

**Discovery session findings:**

1. Container healthy — 0 restarts, clean since 2026-03-23T22:36:14Z. 8 startup crashes at first boot were a one-time SQLite volume-creation race (self-resolved by restart policy).
2. Two real products added via API (UNAS Pro 4 out-of-stock, UNAS Pro 8 in-stock). After first poll cycle both remained `state=0` (Unknown) — all parsers returned `Indeterminate`.
3. HTTP 429 on the second product — both fired simultaneously due to startup burst (no stagger).
4. User-Agent trivially identifiable as a bot (`UbiquitiStoreLurker/1.0 ...`).
5. No cookie session — the Ubiquiti EU store expects `curr_language`, `curr_store`, and `store_modal_shown` cookies to serve product content correctly.

**Decision**: Halt code changes. Four remediation tasks added below.

### Phase 3-D: Capture Real Fixtures via Playwright ✅

**Goal**: Ground-truth HTML snapshots from the real Ubiquiti EU store.

1. Use Playwright (headless Chromium) to fetch full rendered HTML of both confirmed URLs
2. Capture HAR files documenting network requests, headers, and status codes
3. Manually verify JSON-LD block, button structure, and stock text in the captured HTML
4. Document findings in `tests/UbiquitiStoreLurker.Tests/Fixtures/DISCOVERY.md`
5. **No parser code modified** — observation only

**Deliverable**: Two real HTML fixture files + HAR files saved; DISCOVERY.md written.

**Completed**: 2026-03-24. Confirmed JSON-LD is present and correct on real pages. ButtonStateParser correctly returns Indeterminate (Styled Components hashes). TextContentParser has a priority bug — "Sold Out" must take precedence over "Add to Cart" when both appear.

### Phase 3-R: Real-World Parser Validation and Fix ✅

**Goal**: Replace synthetic fixtures, fix TextContentParser priority bug, validate all parsers against real HTML.

1. Replace synthetic fixture files with real ones captured in Phase 3-D (keep originals renamed `synthetic-*.html`)
2. Verify JsonLdStockParser against real fixtures (attribute selector must match tags with `id` and `data-next-head` attributes)
3. Confirm ButtonStateParser returns Indeterminate for real pages (Styled Components hash classes — expected, not a defect)
4. **Fix TextContentParser priority ordering**: evaluate OutOfStock signals ("Sold Out", "Out of Stock", "Notify Me When Available") before InStock signals ("Add to Cart"). When both appear on the same page, resolve to OutOfStock.
5. Add CompositeStockParser end-to-end integration test against each real fixture
6. Minimum 7 new tests; total suite must remain 50+ all passing
7. Rebuild, redeploy, and verify correct stock detection for both products via manual poll

**Deliverable**: Parser tests updated and passing against real HTML; container redeployed with correct stock detection.

**Dependencies**: Phase 3-D ✅, HTTP Hardening (below)

### Phase 3-C: Validate Unchanged Contracts ✅

**Goal**: Confirm Phase 3-R changes did not break downstream components.

The parser abstractions (`IStockParser`, `StockParseResult`, `CompositeStockParser`) isolate parsers from upper layers. This task makes that assumption verifiable:

1. Full `dotnet test` — zero regressions
2. Manual API verification: `GET /api/products` returns correct states
3. SignalR event validation: Monitor page shows correct stock states on load
4. Prometheus: `stock_checks_total` counter incremented
5. If any regression found: capture in plan.yaml as a new task (no silent patching)

**Deliverable**: Full test suite green; manual API + SignalR + Prometheus validation passes.

**Dependencies**: Phase 3-R

### HTTP Client Hardening ✅

**Goal**: Make the poller indistinguishable from a real Chrome browser session; eliminate 429 responses.

Three sub-areas identified during the discovery session:

**A. Browser Fingerprint** — replaces `UserAgentHandler.cs` (removed) with `BrowserFingerprintHandler.cs`:

- Realistic Chrome 124 User-Agent string (configurable, falls back to Chrome UA if blank)
- Full set of browser request headers: Accept, Accept-Language, Accept-Encoding, Cache-Control, Upgrade-Insecure-Requests, Sec-Fetch-Dest/Mode/Site/User, DNT

**B. Cookie Session Management** — new `UbiquitiCookieHandler.cs`:

- Seeds three required cookies for `eu.store.ui.com`: `curr_language=en`, `curr_store=eu`, `store_modal_shown=true`
- Shared `CookieContainer` singleton for all requests (captures server-set cookies)
- Cookie jar persisted to SQLite (`AppSettings` key: `HttpCookieSession`), reloaded on startup
- Auto-discard and re-seed if persisted cookies older than 24 hours

**C. Rate Limit Safety** — prevent 429 at origin:

- Increase default inter-request gap to 12 s with ±30% jitter (8.4–15.6 s)
- `SemaphoreSlim` + `Task.Delay` pattern instead of `TokenBucketRateLimiter`
- `PollSchedulerService` staggers initial scheduling: `nextPoll = now + (index × gap)` to prevent startup burst
- Server 429 Retry-After: block ALL requests for the specified duration (last-resort guard)
- New Prometheus metrics: `rate_limit_server_429_total`, `poll_request_gap_ms`

**Tests** (7 new unit tests):

- BrowserFingerprintHandler sets Chrome UA and all required browser headers
- UbiquitiCookieHandler seeds three cookies, persists server-set cookies, reloads on startup
- ClientSideRateLimitHandler respects jittered gap and blocks all traffic on 429 with Retry-After

**Deliverable**: All three handlers replaced/added; startup burst staggered; 7 new unit tests pass; container redeployed; no 429 logged after 24 h monitoring.

**Dependencies**: Phase 3-D ✅

### Backlog: Readiness Health Endpoint — `/api/health/ready` ✅ (completed 2026-03-24)

**Goal**: Add a true readiness probe that verifies the app is fully initialised before serving traffic, enabling `.WithHttpProbe(Readiness)` in AppHost.

Currently only `/api/health/live` exists (always 200 if the process is running). AppHost's `.WithHttpHealthCheck("/api/health/ready")` cannot be used because the `/ready` path is missing.

Checks behind the `/ready` tag (all must pass for 200 OK — any failure → 503):

1. **Database reachable** — lightweight EF Core query (`context.AppSettings.AnyAsync()`) confirms SQLite mounted, migrations applied, WAL mode active.
2. **Poller initialised** — `PollingCoordinatorService` has completed its first scan loop (exposed via a small `IReadinessIndicator` flag).

Implementation:

- Register tagged health checks in `Program.cs` (`AddCheck<DatabaseReadinessCheck>("database", tags: ["ready"])`, `AddCheck<PollerReadinessCheck>("poller", tags: ["ready"])`).
- Map `/api/health/live` (predicate: always false — process alive) and `/api/health/ready` (predicate: tag `"ready"`) separately.
- Update AppHost: `.WithHttpHealthCheck("/api/health/live")` (existing) + `.WithHttpProbe(ProbeType.Readiness, "/api/health/ready")` (new).
- Update `docker-compose.yml` healthcheck to use `/api/health/ready`.

New NUnit tests (3):

- `HealthEndpoint_Live_Returns200_WhenProcessRunning`
- `HealthEndpoint_Ready_Returns200_WhenDatabaseAccessible`
- `HealthEndpoint_Ready_Returns503_WhenDatabaseUnavailable`

**Deliverable**: `/api/health/ready` returns 200 when DB + poller ready, 503 otherwise; AppHost and docker-compose updated; 3 tests pass.

**Dependencies**: task-phase-12-b ✅

### Backlog: OTLP Push to Lab OpenTelemetry Collector ✅ (completed 2026-03-24)

**Goal**: Complement the existing pull-based Prometheus scrape (`/api/metrics`) with push-based OTLP telemetry flowing through an OTel Collector sidecar into the lab Prometheus.

Current observability is pull-only: Prometheus scrapes `/api/metrics` every 15 s. Adding an OTel Collector enables traces and metrics to be pushed and forwarded without changing Traefik routing.

**Architecture**:

```
UbiquitiStoreLurker app
  → OTLP gRPC (push)
  → ubiquitistorelurker-otel-collector (172.18.2.6, apps tier)
  → Prometheus remote-write → lab Prometheus (172.18.1.1)
  → Grafana (172.18.1.2) — no dashboard changes

Aspire Dashboard (172.18.2.5) continues to receive OTLP directly on :18889 — unaffected.
```

Deliverable files:

- `otel-collector` service added to `docker-compose.yml` at static IP `172.18.2.6`
- `src/otel-collector.yaml` — OTLP gRPC receiver + Prometheus remote-write exporter + logging exporter
- Lab Prometheus `--web.enable-remote-write-receiver` flag verified/added
- README "Observability — Pull vs Push" section documenting the dual-path architecture

**Deliverable**: OTel Collector healthy; OTLP push confirmed in Prometheus remote-write; Aspire Dashboard traces unaffected; README updated.

**Dependencies**: task-phase-12-k ✅

### Backlog: Aspire Test Secrets — Seed Provider Secrets in AppHostTests ✅ (completed 2026-03-24)

**Goal**: Exercise the notification provider DI chain with realistic (but test-safe) configuration in `AppHostTests`, complementing the existing zero-secrets baseline test.

Current `AppHost_StartsHealthy` (Test 1) passes with no secrets — correct, because all providers are disabled by default in `NotificationConfig` (SQLite). However, DI registration errors caused by missing config would be silently masked.

Changes to `AppHostIntegrationTests.cs`:

```csharp
// Before StartAsync(), inject test-safe placeholder secrets:
appHost.Configuration["Parameters:smtp-password"]   = "test-smtp-password";
appHost.Configuration["Parameters:twilio-token"]    = "test-twilio-token";
appHost.Configuration["Parameters:discord-webhook"] = "http://localhost/discord-test";
appHost.Configuration["Parameters:teams-webhook"]   = "http://localhost/teams-test";
```

New test: `AppHost_WithSeededSecrets_ProvidersRegistered`:

- Start AppHost with the above secrets injected
- `GET /api/settings` → assert provider fields present (DI chain registered)
- `PUT /api/settings/Email__SmtpHost` → `GET` → assert value persisted (Settings API functional)
- Does **not** send a real notification — validates DI and config binding only
- Tagged `[Category("RequiresDocker")]` consistent with existing tests

Existing 5 tests unchanged (zero-secrets baseline remains a first-class test).

**Deliverable**: 1 new test `AppHost_WithSeededSecrets_ProvidersRegistered`; all 5 existing AppHostTests continue to pass.

**Dependencies**: task-phase-12-h ✅

---

## 15. Deployment (Proxmox Docker)

| Property | Value |
|---|---|
| Container name | `ubiquitistorelurker` |
| Image | `ubiquitistorelurker:latest` (local build) |
| Network | `proxy` (external bridge) |
| Static IP | `172.18.2.4` (apps tier) |
| Port | 8080 (internal) |
| Traefik endpoint | `https://ubiquitistorelurker.rverbist.io` |
| Cert resolver | `letsencrypt` (Cloudflare DNS-01, `*.rverbist.io`) |
| Middleware | `lan-only@file` |
| Data volume | `/devpool/docker-volumes/apps/ubiquitistorelurker/data` → `/data` |
| Log volume | `/devpool/docker-volumes/apps/ubiquitistorelurker/logs` → `/logs` |
| Restart policy | `unless-stopped` |
| Health check | `wget /api/health/live` every 30s |

---

## 16. Risks and Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Ubiquiti store changes HTML structure | Parser breaks (Indeterminate) | Multiple parser strategies; indeterminate triggers early re-poll and alert; easy to add new parsers |
| Store blocks the poller (403, CAPTCHA) | No data | **HTTP Hardening task** addresses all three root causes: (1) bot-identifiable User-Agent replaced with Chrome 124 fingerprint; (2) required EU store cookies seeded and persisted via UbiquitiCookieHandler; (3) jittered 12 s inter-request gap with startup burst staggering prevents 429 at origin |
| SQLite corruption on unclean shutdown | Data loss | WAL mode (crash-safe), Docker named volumes on ext4, daily backups of .db + .wal + .shm |
| Synthetic fixtures vs real pages | Parsers pass tests but fail in production | **Discovered in post-delivery review.** Phase 3-D captures real Playwright fixtures; Phase 3-R rewrites tests against them; CompositeStockParser end-to-end test added |
| Notification provider API changes | Notifications fail | Failures are logged, not fatal; each provider isolated; easy to disable/replace |
| Playwright browser binaries bloat Docker image | Large image | Discovery tool is a separate project/CLI, not in the main Docker image |
| VAPID key loss | Push subscriptions invalid | Keys stored in DB, backup includes DB. Re-generation requires all clients to re-subscribe. |
| Vue/Vite ecosystem churn | Build breaks | Pin major versions, use lockfiles (`package-lock.json`) |
| Token/secret exposure | Security | No secrets in source; use Docker env vars or .env file (gitignored); KeepSecretManagerInSecrets |
| WhatsApp / Facebook Messenger not implemented | Fewer notification channels | Deferred to post-v1. UI shows "Coming Soon" placeholders. 5 providers cover primary use cases. |

---

## 17. Research Reference Index

All research findings are in the same directory as this plan:

| File | Topics |
|---|---|
| [research_findings_dotnet10-docker-sqlite-efcore.yaml](research_findings_dotnet10-docker-sqlite-efcore.yaml) | .NET 10 status, Docker images, EF Core SQLite, WAL, migrations, data modeling, container workflow |
| [research_findings_frontend-animations.yaml](research_findings_frontend-animations.yaml) | Vue 3 vs alternatives (scored comparison), GSAP integration, canvas-confetti, Ubiquiti theme, Vite+ASP.NET pipeline |
| [research_findings_notification-providers.yaml](research_findings_notification-providers.yaml) | Browser Push (VAPID), Email (MailKit), SMS (Twilio), Teams (Workflows), Discord (webhooks), WhatsApp (deferred), Facebook (deferred) |
| [research_findings_observability-realtime.yaml](research_findings_observability-realtime.yaml) | SignalR server/client, Serilog CLEF, prometheus-net, health checks, .NET Aspire, Grafana provisioning |
| [research_findings_polling-resilience.yaml](research_findings_polling-resilience.yaml) | BackgroundService + Channels, Polly v8 resilience, rate limiting, AngleSharp HTML parsing, Playwright discovery, stock state machine |
| [research_findings_testing-aspire.yaml](research_findings_testing-aspire.yaml) | NUnit 4 patterns, WebApplicationFactory, NSubstitute, Aspire testing, solution layout, Docker Compose |
