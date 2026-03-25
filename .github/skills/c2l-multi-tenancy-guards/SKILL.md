---
name: c2l-multi-tenancy-guards
description: 'Apply and verify Coach2Lead multi-tenancy protections across entity design, EF queries, Breeze endpoints, and background jobs. Use when implementing or reviewing company-scoped behavior and preventing cross-tenant reads/writes.'
---

# Coach2Lead Multi Tenancy Guards

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
- Prevent cross-tenant data leaks and cross-tenant mutation paths.
- Make tenant scoping explicit and testable.

## Read First
- `Coach2Lead/Interfaces/IHaveCompany.cs`
- `Coach2Lead.Web/App/ApplicationContextProvider.cs`

## Core Anchors
- `Coach2Lead.Web/App/Services/UserResourceManager.cs`
- `Coach2Lead.Web/App/Services/ResourceManager.cs`
- `Coach2Lead.Web/Areas/<Module>/Controllers/API/*ContextProvider.cs`
- `Coach2Lead.Web/Areas/<Module>/Controllers/API/*ResourceController.cs`
- `Coach2Lead.Web/App/Jobs/*`

## Standard Workflow
1. Verify entity scope: identify whether entity is company-scoped.
2. Verify query scope: ensure `CompanyId` filter exists at the nearest usable navigation level.
3. Verify save scope: reject cross-tenant FK payloads in save pipeline/providers.
4. Verify background routine scope: root company data through active-account patterns.
5. Verify UI/account switch behavior does not reuse stale tenant data.

## Guardrails
- Treat missing company filter as a security defect.
- Do not rely on client checks for tenant enforcement.
- Keep account-switch plus claims refresh paths intact for context correctness.

## Validation
- Execute at least one non-admin and one admin scenario in different tenants.
- Check generated SQL/query shape for company predicates.
- Verify save attempts with foreign-tenant IDs are rejected.

## Golden Example
Tenant-safe child-entity query pattern.
```csharp
public IQueryable<RiskIssue> RiskIssues()
{
    var current = GetCurrentUserInfo();
    return Query<RiskIssue>()
        .Where(s => s.RiskAssessmentItem.RiskAssessment.CompanyId == current.CompanyId);
}
```

## Output Contract
- Tenant boundaries validated.
- Query and save enforcement points.
- Any remaining risk hotspots.

## Companion Skills
- `c2l-repository-pattern`
- `c2l-breeze-webapi`
- `c2l-webjobs-routines`

## Skill-Specific Topics
- For routines, start from `ApplicationAccount.IsActive` before traversing to company-owned entities.
- For secured entities, combine tenant filter with `AccessRestricted` checks.
