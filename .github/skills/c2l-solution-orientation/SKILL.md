---
name: c2l-solution-orientation
description: 'Orient quickly in Coach2Lead by mapping projects, runtime entry points, MVC Area + AngularJS + Breeze request flow, and safe edit targets. Use when starting an unfamiliar feature, triaging a bug, or planning cross-layer changes.'
---

# Coach2Lead Solution Orientation

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

</details>
<!-- toc:end -->

## Objective
- Produce a concrete edit map quickly: `Area -> Angular -> API/ContextProvider -> domain model -> migration`.
- Find the nearest working pattern before writing code.

## Read First
- `AGENTS.md`
- `docs/knowledgebase/domain/c2l-codebase-overview.md`

## Core Anchors
- `Coach2Lead.sln`
- `Coach2Lead/AppConfig.cs`
- `Coach2Lead.Web/Global.asax.cs`
- `Coach2Lead.Web/Startup/Startup.cs`
- `Coach2Lead.Web/Startup/HttpRouteConfig.cs`
- `Coach2Lead.Web/App/ApplicationContextProvider.cs`
- `Coach2Lead.Web/App/Persistence/ApplicationDbContext*.cs`
- `Coach2Lead.Web/Areas/Actions/`
- `Coach2Lead.Web.Jobs/Functions.cs`

## Standard Workflow
1. Identify the target module/Area and classify the task as backend, frontend, or both.
2. Trace entry route: Razor shell + Angular route + API endpoint.
3. Trace data path: `ResourceController -> ContextProvider/query -> UserResourceManager/DbContext`.
4. Decide whether schema change is required; if yes, plan EF6 migration in `Coach2Lead.Web/Migrations/`.
5. List exact files to edit before implementation.

## Guardrails
- Follow an existing nearby Area pattern; do not introduce a parallel structure.
- Keep tenant/company scope explicit in all data paths.
- Preserve AngularJS array-style DI and existing naming conventions.

## Validation
- Verify route and shell file resolution in the target Area.
- Verify endpoint/controller and query path exist and are wired.
- Verify migration requirement is explicitly `yes` or `no`.

## Golden Example
Example: locate where to change Metrics list behavior.
1. Route: `Coach2Lead.Web/Areas/Metrics/Angular/metrics/metrics.routes.js`.
2. Controller/service: `Coach2Lead.Web/Areas/Metrics/Angular/metrics/...`.
3. API endpoint: `Coach2Lead.Web/Areas/Metrics/Controllers/API/MetricsResourceController.cs`.
4. Query/provider: `Coach2Lead.Web/Areas/Metrics/Controllers/API/MetricsResourceController.cs` (uses `ApplicationContextProvider`) and `Coach2Lead.Web/App/Services/UserResourceManager.cs`.
5. Schema touch check: if model changes, add migration in `Coach2Lead.Web/Migrations/`.

## Output Contract
- Target Area/module.
- Exact files to edit.
- Data path summary.
- DB impact (`none` or migration name).
- Smoke route/API to verify.

## Companion Skills
- `c2l-build-run-debug`
- `c2l-breeze-webapi`
- `c2l-repository-pattern`
- `c2l-webjobs-routines`

## Skill-Specific Topics
- Shared Angular base module lives under `Coach2Lead.Web/Areas/Surveys/Angular/coach2lead/`.
- New team members should use `Areas/Actions` as the reference module for structure and conventions.
