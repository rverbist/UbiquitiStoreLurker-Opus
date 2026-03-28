# MSSQL Docker Cleanup

## Objective

Replace SQLite with SQL Server end-to-end, move runtime database configuration to env files, add trusted forwarded-header support for Traefik on the `proxy` network, and simplify frontend hosting so the built `wwwroot/index.html` app is the only SPA entrypoint.

## Deployment Facts

- Docker network: `proxy`
- Proxy subnet: `172.18.0.0/16`
- Trusted Traefik IP: `172.18.2.4`
- Host repo path: `/opt/docker/apps/unifistorewatcher`
- Runtime mount: `/opt/docker/apps/unifistorewatcher/logs` -> `/logs`
- SQL Server host: `RV-WEBSERVER`
- Production DB: `unifistorewatcher`
- Development DB: `unifistorewatcher-dev`

## Scope

In scope:
- Remove SQLite from production code and tests.
- Standardize on `ConnectionStrings:UniFiStoreWatch-db`.
- Move DB configuration into `.env`, `.env.Development`, `.env.Production`, and `.env.Example`.
- Add SQL Server support and regenerate EF Core migrations.
- Add `UseForwardedHeaders` for Traefik.
- Remove stale Claude shell routing and legacy UI aliases.
- Add Dockerfile, `.dockerignore`, and compose deployment files.

Out of scope:
- Removing OpenAPI endpoints.
- Removing health or metrics endpoints.
- Removing Product, Settings, Notification, Push, or SignalR endpoints.
- Adding a dedicated SQL Server test container.

## Plan

### Phase 1: Configuration and secrets

1. Remove the `ConnectionStrings` section from `UnifiStoreWatcher.Web/appsettings.json` and `UnifiStoreWatcher.Web/appsettings.Development.json`.
2. Create `.env`, `.env.Development`, `.env.Production`, and `.env.Example`.
3. Add `.env`, `.env.Development`, and `.env.Production` to `.gitignore`.

### Phase 2: EF provider migration

1. Replace `Microsoft.EntityFrameworkCore.Sqlite` with `Microsoft.EntityFrameworkCore.SqlServer` and add `Microsoft.EntityFrameworkCore.InMemory`.
2. Delete SQLite-only production code:
   - `UnifiStoreWatcher.Web/Data/SqliteWalModeInterceptor.cs`
   - `UnifiStoreWatcher.Web/Data/DateTimeOffsetToBinaryConverter.cs`
3. Replace the SQLite migration set with a fresh SQL Server migration.

### Phase 3: Runtime pipeline cleanup

1. Update `UnifiStoreWatcher.Web/Program.cs` to use SQL Server for runtime and EF InMemory for tests.
2. Add `UseForwardedHeaders` right after app creation and trust proxy IP `172.18.2.4` with `ForwardLimit = 1`.
3. Remove the stale Claude shell routing and legacy aliases `/monitor`, `/v1/monitor`, and `/v2/monitor`.
4. Change SPA fallback to the built-in `wwwroot/index.html` app.

### Phase 4: Secondary cleanup

1. Update `UnifiStoreWatcher.Web/Http/UbiquitiCookieJar.cs` so cookie persistence no longer depends on a SQLite file path or the wrong connection-string key.
2. Update `UnifiStoreWatcher.Web/Services/Health/DatabaseReadinessCheck.cs` comments and wording to be provider-neutral.
3. Update `.github/copilot-instructions.md` to remove SQLite-specific repository guidance.

### Phase 5: Test migration

1. Replace SQLite-backed setup in `UnifiStoreWatcher.Tests/TestApiFactory.cs` with EF InMemory setup.
2. Remove direct `Microsoft.Data.Sqlite` usage from API, data, notification, and polling tests.
3. Normalize remaining test configuration to `ConnectionStrings:UniFiStoreWatch-db`.

### Phase 6: Docker artifacts

1. Add a root Dockerfile.
2. Add a root `.dockerignore`.
3. Add a root compose file using:
   - `env_file: .env.Production`
   - external network `proxy`
   - internal port `8080`
   - only the `/logs` bind mount
   - Traefik labels for `usw.rverbist.io`

### Phase 7: Verification

1. `dotnet build UnifiStoreWatcher.slnx`
2. `dotnet ef migrations add InitialCreate --project UnifiStoreWatcher.Web --startup-project UnifiStoreWatcher.Web`
3. `dotnet test`
4. Search for `claude/index.html`, `/monitor`, `/v1/monitor`, and `/v2/monitor` in runtime code.
5. `docker build -t unifistorewatcher .`
6. `docker compose up -d`
7. Validate `/api/health/live` and `/` behind Traefik.

## Decisions

- Only the legacy UI aliases are removed.
- Product, Settings, Notification, Push, SignalR, health, metrics, and OpenAPI endpoints remain.
- SQL Server is external, so no `/data` volume is required.
- Traefik is trusted by fixed proxy IP `172.18.2.4` on the `proxy` network.
- EF Core InMemory replaces SQLite-backed tests for this migration.

## Risks

- EF Core InMemory is less relationally faithful than SQL Server.
- Cookie persistence needs an explicit path now that the DB is not file-backed.
- Fresh SQL Server migrations may reveal provider-specific schema differences.

## Next Step

Execute the repository changes and then validate the Docker deployment behind Traefik.
