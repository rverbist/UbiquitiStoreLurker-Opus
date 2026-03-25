---
name: c2l-repository-pattern
description: 'Apply Coach2Lead repository data-access patterns with ResourceManager and UserResourceManager, including company scoping, ISecuredAccess enforcement, and controller integration. Use when writing or debugging queries, reviewing tenant isolation, or fixing access-control behavior.'
---

# Coach2Lead Repository Pattern

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
- Implement query logic that is tenant-safe and access-safe by default.
- Choose the correct manager (`ResourceManager` vs `UserResourceManager`) for each scenario.

## Read First
- `Coach2Lead.Web/App/Services/ResourceManager.cs`
- `Coach2Lead.Web/App/Services/UserResourceManager.cs`

## Core Anchors
- `Coach2Lead.Web/App/ApplicationControllerBase.cs`
- `Coach2Lead.Web/App/ApplicationApiControllerBase.cs`
- `Coach2Lead/Interfaces/IHaveCompany.cs`
- `Coach2Lead/Interfaces/ISecuredAccess.cs`
- `Coach2Lead.Web/App/Utils/Extensions/QueryableExtensions.cs` (for `WhereIf` pattern)

## Standard Workflow
1. Choose access scope: use `UserResourceManager` for user-facing flows and `ResourceManager` for system/admin/background operations.
2. Apply company filter on the closest navigation path to `CompanyId`.
3. Apply access restriction checks for `ISecuredAccess<T>` entities when user is not admin.
4. Apply soft-delete/archive predicates when domain requires it.
5. Use no-tracking scopes for read-heavy queries.

## Guardrails
- Never return unscoped company data from user-facing query paths.
- Prefer typed methods in `UserResourceManager` over ad-hoc controller-level filtering.
- Dispose impersonation/no-tracking scopes correctly.

## Validation
- Confirm SQL/logical filter includes expected company constraint.
- Confirm admin/non-admin behavior differs only where intended.
- Confirm restricted entity returns honor `AccessRestricted` + `ResourceRights` logic.

## Golden Example
Canonical secured query pattern.
```csharp
public IQueryable<Audit> Audits()
{
    var current = GetCurrentUserInfo();
    return Query<Audit>()
        .Where(s => s.CompanyId == current.CompanyId)
        .WhereIf(!current.IsAdministrator,
            s => !s.AccessRestricted ||
                 s.ResourceRights.Any(r => r.OwnerId == current.PersonId));
}
```

## Output Contract
- Entity/query touched.
- Company scoping path used.
- Access restriction strategy.
- Soft-delete/archive filters added or preserved.

## Companion Skills
- `c2l-multi-tenancy-guards`
- `c2l-breeze-webapi`
- `c2l-ef6-models`
- `c2l-webjobs-routines`

## Skill-Specific Topics
- Background routines commonly iterate active accounts first, then use user-scoped managers for company-safe logic.
- Attachment security inherits from parent entity security and should be filtered using parent navigation.
- `WithoutTracking`/`WithoutChangeTracking` scopes are the preferred performance switches for read and bulk operations.
