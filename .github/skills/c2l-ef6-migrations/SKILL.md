---
name: c2l-ef6-migrations
description: 'Safely evolve Coach2Lead database schema using EF6 Code First migrations in Coach2Lead.Web/Migrations. Use when model/context changes require columns, indexes, constraints, or data-shape updates across environments.'
---

# Coach2Lead EF6 Migrations

<!-- toc:start -->
<details>
  <summary>
   <strong>Table of Contents</strong> Click to open
  </summary>

- [Objective](#objective)
- [Read First](#read-first)
- [Core Anchors](#core-anchors)
- [Standard Workflow](#standard-workflow)
- [Guardrails](#guardrails)
- [Validation](#validation)
- [Golden Example](#golden-example)
- [Output Contract](#output-contract)
- [Companion Skills](#companion-skills)
- [Skill-Specific Topics](#skill-specific-topics)
  - [Running ef6.exe from CLI](#running-ef6exe-from-cli)
    - [Config Path Resolution (Critical)](#config-path-resolution-critical)
    - [Add-Migration](#add-migration)
    - [Update-Database](#update-database)
    - [List Migrations](#list-migrations)
  - [VS Code Tasks](#vs-code-tasks)
  - [VS Package Manager Console](#vs-package-manager-console)

</details>
<!-- toc:end -->

## Objective
- Produce reliable, reviewable migrations for schema changes.
- Keep environment deployment predictable and reversible.

## Read First
- `Coach2Lead.Web/Migrations/Configuration.cs`
- `Coach2Lead/AppConfig.cs` - dynamic connection string resolution via `ConfigurationManager`

## Core Anchors
- `Coach2Lead.Web/Migrations/`
- `Coach2Lead.Web/App/Persistence/ApplicationDbContext*.cs`
- `Coach2Lead/Models/**`
- `.vscode/scripts/ef6/` - PowerShell automation scripts
- `packages/EntityFramework.*/tools/net45/any/ef6.exe` - CLI tool

## Standard Workflow
1. Implement model/context change.
2. Build the solution.
3. Run Add-Migration (via VS Code task, CLI, or Package Manager Console).
4. Review migration for tenant keys, indexes, and unintended destructive operations.
5. Run Update-Database locally.
6. Record migration intent for reviewers.

## Guardrails
- Keep `AutomaticMigrationsEnabled = false` behavior.
- Do not modify historical migrations unless a deliberate migration strategy requires it.
- Prefer additive changes and explicit data backfill steps for risky transitions.
- Multi-step data migrations (add nullable column, backfill, alter to non-null) are safer than single-step non-null column additions on tables with existing data.

## Validation
- Migration compiles and applies on a clean/local database.
- Up/down paths are coherent for rollback expectations.
- Runtime paths touching changed schema still load and save.

## Golden Example
Typical migration pass:
1. Add a nullable `ComplianceDeadline` column to `AiSystem`.
2. Build solution.
3. Run `ef6:add-migration` → `AddAiSystemComplianceDeadline`.
4. Review generated migration: confirm additive-only, no data-loss operations.
5. Run `ef6:update-database` locally.
6. Verify runtime loads and saves for the changed entity.

## Output Contract
- Migration name.
- Schema delta summary.
- Risk notes (destructive/data-move/no-risk).
- Verification steps executed.

## Companion Skills
- `c2l-ef6-models`
- `c2l-build-run-debug`
- `c2l-multi-tenancy-guards`

## Skill-Specific Topics

### Running ef6.exe from CLI

#### Config Path Resolution (Critical)

`ef6.exe` creates a child AppDomain and sets its `ConfigurationFile` from the `--config` parameter. **Relative paths are resolved relative to the AppDomain's base directory** (the directory containing the `--assembly` DLL), NOT relative to the current working directory.

This means:
- `--config Coach2Lead.Web/Web.config` resolves as Coach2Lead.Web/bin/Coach2Lead.Web/Web.config — **WRONG**
- `--config "$(pwd)/Coach2Lead.Web/Web.config"` resolves correctly — **RIGHT**

**Always use an absolute path for `--config`.**

When `--config` resolves to a nonexistent path, `ConfigurationManager` silently returns empty results. Coach2Lead's `AppConfig.DatabaseConnectionStringName` then fails with: `No connection string named 'Db' found in the configuration.` - because `AppSettings["Database"]` returned null and the fallback chain produced the empty-string connection name `"Db"`.

#### Add-Migration

```bash
# From the solution root
packages/EntityFramework.6.5.1/tools/net45/any/ef6.exe migrations add <MigrationName> \
  --force \
  --project-dir Coach2Lead.Web \
  --assembly Coach2Lead.Web/bin/Coach2Lead.Web.dll \
  --config "$(pwd)/Coach2Lead.Web/Web.config" \
  --connection-string "<connection string from Web.config>" \
  --connection-provider "System.Data.SqlClient" \
  --verbose
```

The `--connection-string` parameter provides the actual database connection for model diffing. Use the `TestingDbRelease` connection string value from Web.config.

#### Update-Database

```bash
packages/EntityFramework.6.5.1/tools/net45/any/ef6.exe database update \
  --assembly Coach2Lead.Web/bin/Coach2Lead.Web.dll \
  --config "$(pwd)/Coach2Lead.Web/Web.config" \
  --connection-string "<connection string>" \
  --connection-provider "System.Data.SqlClient" \
  --verbose
```

#### List Migrations

```bash
packages/EntityFramework.6.5.1/tools/net45/any/ef6.exe migrations list \
  --assembly Coach2Lead.Web/bin/Coach2Lead.Web.dll \
  --config "$(pwd)/Coach2Lead.Web/Web.config" \
  --connection-string "<connection string>" \
  --connection-provider "System.Data.SqlClient"
```

### VS Code Tasks

The VS Code tasks (`ef6:add-migration`, `ef6:update-database`, etc.) invoke PowerShell scripts in `.vscode/scripts/ef6/`. These scripts use `Resolve-ProjectInfo` which builds absolute paths via `Join-Path` and `Resolve-Path`, so they pass correct absolute paths to `--config` automatically.

### VS Package Manager Console

Works because `devenv.exe` loads the **startup project's** config into the AppDomain. The `-Project` parameter selects the migration assembly, but config resolution uses whichever project is set as startup. Ensure Coach2Lead.Web is set as the startup project.
