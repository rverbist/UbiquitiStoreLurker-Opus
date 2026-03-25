---
name: c2l-new-area-module
description: 'Scaffold a new Coach2Lead module as an MVC Area + AngularJS SPA + Breeze/EF6 API stack using existing Area conventions. Use when introducing a new business module or extending a module with new routed screens and ResourceController endpoints.'
---

# Coach2Lead New Area Module

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
- Add a new Area that matches existing module architecture end-to-end.
- Deliver minimal scaffolding that is routable, queryable, and tenant-safe.

## Read First
- `docs/knowledgebase/domain/scaffolding/feature-scaffolding-guide.md`
- `docs/knowledgebase/domain/scaffolding/breeze-api-layer.md`
- `docs/knowledgebase/domain/scaffolding/angularjs-routes-pages-controllers.md`

## Core Anchors
- `Coach2Lead.Web/Areas/Actions/` (reference implementation)
- `Coach2Lead.Web/Startup/HttpRouteConfig.cs`
- `Coach2Lead.Web/App/ApplicationApiControllerBase.cs`
- `Coach2Lead.Web/App/ApplicationControllerBase.cs`
- `Coach2Lead.Web/App/Services/UserResourceManager.cs`

## Standard Workflow
1. Create Area folder and `<Module>AreaRegistration.cs`.
2. Add shell MVC controller/view for Area entry.
3. Add Breeze API pair: `<Module>ContextProvider.cs` and `<Module>ResourceController.cs`.
4. Add Angular module + routes + base controller/service/factory structure.
5. Wire menu/navigation using existing module patterns.
6. If persistent entities are added, wire DbContext and migration.

## Guardrails
- Reuse base classes (`ApplicationControllerBase`, `ApplicationApiControllerBase`).
- Keep AngularJS DI array-annotated.
- Enforce company scoping in every query/save path.

## Validation
- Area route resolves and shell view loads.
- Angular route resolves and controller initializes.
- Breeze endpoint returns data/metadata without authorization or scoping regressions.
- Migration exists when schema changes are introduced.

## Golden Example
Scaffold baseline files for `Areas/Example`.
1. `Coach2Lead.Web/Areas/Example/ExampleAreaRegistration.cs`
2. `Coach2Lead.Web/Areas/Example/Controllers/ExampleController.cs`
3. `Coach2Lead.Web/Areas/Example/Views/Example/Index.cshtml`
4. `Coach2Lead.Web/Areas/Example/Controllers/API/ExampleContextProvider.cs`
5. `Coach2Lead.Web/Areas/Example/Controllers/API/ExampleResourceController.cs`
6. `Coach2Lead.Web/Areas/Example/Angular/example/example.module.js`
7. `Coach2Lead.Web/Areas/Example/Angular/example/example.routes.js`

## Output Contract
- New Area file list.
- Route and endpoint summary.
- Tenant scoping statement.
- DB impact (`none` or migration name).

## Companion Skills
- `c2l-breeze-webapi`
- `c2l-ef6-models`
- `c2l-multi-tenancy-guards`

## Skill-Specific Topics
- Use `Areas/Actions` conventions for folder naming, ResourceController patterns, and Angular route conventions.
- Keep module-specific authorization/feature wiring consistent with existing `Authorization.cs` patterns when present.
