# Stock Monitor — Completion Walkthrough

**Plan ID**: storefront-stock-monitor-opus-20260323
**Completed**: 2026-03-23
**Tests**: 50 passing, 0 failing
**Container**: Running at `172.18.2.4:8080` (healthy)
**URL**: `https://ubiquitistorelurker.rverbist.io`

---

## What Was Built

A full-stack stock monitoring application for the Ubiquiti EU store, deployed as a Docker container behind Traefik on the Proxmox home-lab.

### Backend (.NET 10 / ASP.NET Core)

- **REST API** — Products CRUD, stock history, settings, notification configs, push subscriptions, metrics
- **Polling Engine** — Background scheduler + worker with jitter, rate limiting (1 req/5s), Polly resilience
- **Stock Detection** — 3-parser chain: JSON-LD (0.95) → Button state (0.85) → Text content (0.70)
- **State Machine** — Unknown→anything = no notification (initial discovery); OutOfStock→InStock = notify
- **Notifications** — Email (MailKit), SMS (Twilio), Teams (Adaptive Card), Discord (embed), Browser Push (VAPID)
- **SignalR Hub** — `/ubiquitistorelurker-hub` broadcasting `StockStatusChanged`, `PollCycleCompleted`, `PollErrorOccurred`, `PollStarted`
- **Prometheus Metrics** — Poll rate, transition rate, notification rate, poll duration p50/p95, active products gauge
- **OpenAPI** — `/openapi/v1.json` (ASP.NET Core 10 built-in)

### Frontend (Vue 3 + Vite + TypeScript)

- **Monitor page** — ProductCard grid with Ubiquiti dark theme (#006FFF accent, dark navy background)
- **Real-time updates** — SignalR client updates product cards live without page reload
- **Celebration** — GSAP animations + canvas-confetti burst + StockPopup modal on InStock transitions
- **Setup page** — Global settings form, notification provider cards (JSON config + enable toggle), browser push subscription
- **GSAP animations** — Slide-in on mount, tumble-off on delete, badge pulse on state change

### Infrastructure

- **SQLite WAL mode** — Zero-config persistence at `/data/ubiquitistorelurker.db`
- **Docker** — Alpine-based multi-stage build; non-root user (UID 10001); health check via wget
- **Traefik** — `ubiquitistorelurker.rverbist.io`, websecure, letsencrypt, `lan-only@file` middleware
- **Prometheus scrape** — Job added to `/opt/docker/infra/monitoring/config/prometheus/prometheus.yml`
- **Grafana dashboard** — Provisioned to `/opt/docker/infra/monitoring/dashboards/ubiquitistorelurker.json`

---

## Task Summary

| Task | Description | Tests Added |
|---|---|---|
| 0 | Project scaffolding (SLNX, Vite, Dockerfile) | 1 |
| 1 | Data model + EF Core + migrations | 6 |
| 2 | Polling engine (scheduler + worker + rate limit) | 5 |
| 3 | Stock detection (3 parsers + state machine) | 16 |
| 4 | Notification system (5 providers + dispatcher) | 3 |
| 5 | REST API endpoints + OpenAPI | 11 |
| 6 | SignalR hub + broadcaster | 3 |
| 7 | Vue 3 frontend scaffold + monitor page | — (build) |
| 8 | GSAP animations + StockPopup + confetti | — (build) |
| 9 | Setup page + notification provider config | — (build) |
| 10 | Prometheus metrics + Grafana dashboard | 3 |
| 11 | Browser push (VAPID + service worker) | 2 |
| 12 | Aspire integration | skipped (optional) |
| 13 | Polish + README + deployment | — |

**Total: 50 tests, 0 failures**

---

## How to Use

### Add a product

1. Navigate to `https://ubiquitistorelurker.rverbist.io`
2. Paste a Ubiquiti EU store URL (e.g., `https://eu.store.ui.com/eu/en/products/udr`)
3. Click **Add Product** — the poller will check it within 30–90 seconds

### Configure notifications

1. Navigate to **Setup**
2. Enable Email/Discord/SMS and paste credentials as JSON
3. Enable **Browser Push** to receive desktop notifications even when the tab is closed

### Monitor deployment

```bash
docker ps --filter name=ubiquitistorelurker
docker logs ubiquitistorelurker --tail 50
curl https://ubiquitistorelurker.rverbist.io/health
```

---

## Next Steps

- **Phase 12 (skipped)**: .NET Aspire integration for local observability dashboard
- Add more product URLs from the Ubiquiti EU store
- Tune poll intervals per-product once the DB schema supports overrides
- Consider adding a Plex-style "discovery" search to find product URLs automatically
