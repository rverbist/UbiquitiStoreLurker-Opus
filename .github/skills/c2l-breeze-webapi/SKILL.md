---
name: c2l-breeze-webapi
description: 'Implement or modify Coach2Lead Breeze WebApi endpoints (ResourceController + ContextProvider) while preserving save pipeline hooks, authorization, and tenant scoping. Use when adding queries, changing save behavior, or diagnosing metadata/query/save issues.'
---

# Coach2Lead Breeze WebApi

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
- Change Breeze endpoints safely without breaking query semantics or save pipeline behavior.
- Keep tenant and authorization logic centralized and explicit.

## Read First
- `docs/knowledgebase/domain/scaffolding/breeze-api-layer.md`
- `Coach2Lead.Web/App/ApplicationContextProvider.cs`

## Core Anchors
- `Coach2Lead.Web/Startup/HttpRouteConfig.cs`
- `Coach2Lead.Web/App/ApplicationApiControllerBase.cs`
- `Coach2Lead.Web/App/ApplicationContextProvider.cs`
- `Coach2Lead.Web/App/Persistence/ApplicationDbContext*.cs`
- `Coach2Lead.Web/Areas/<Module>/Controllers/API/*ContextProvider.cs`
- `Coach2Lead.Web/Areas/<Module>/Controllers/API/*ResourceController.cs`

## Standard Workflow
1. Locate the nearest module `*ResourceController` and `*ContextProvider` pattern.
2. Implement query endpoints as `IQueryable<T>` with `EnableBreezeQuery` where appropriate.
3. Apply company/access scoping in provider/query composition before exposing results.
4. If save behavior changes, use existing `ApplicationContextProvider` hook maps.
5. Align metadata/entity constructor expectations on the Angular side if entity shape changes.

## Guardrails
- Do not introduce a parallel save pipeline.
- Do not return unscoped `Query<T>()` for company data.
- Do not assume Breeze `.expand(...)` semantics in server-side routines.

## Validation
- Metadata endpoint returns expected entity shape.
- Query endpoint enforces tenant and access restrictions.
- Save path runs expected hooks and persists correctly.
- Angular client reads data without serialization/circular reference regressions.

## Golden Example
Minimal tenant-safe query endpoint pattern.
```csharp
[HttpGet]
[EnableBreezeQuery(MaxExpansionDepth = 0)]
public IQueryable<Indicator> Indicators()
{
    using (ResourceManager.WithoutTracking())
    {
        return ResourceManager.PerformanceIndicators();
    }
}
```

## Output Contract
- Endpoints changed.
- Scoping and authorization strategy.
- Save hook impact.
- Client metadata impact.

## Companion Skills
- `c2l-repository-pattern`
- `c2l-multi-tenancy-guards`
- `c2l-webjobs-routines`

## Skill-Specific Topics
- Breeze route pattern is `breeze/{controller}/{action}`.
- Client-side metadata usage is anchored in `Coach2Lead.Web/Areas/Surveys/Angular/coach2lead/coach2lead.metadata.js` and related resource factories.
