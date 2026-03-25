---
name: c2l-controller-base-architecture
description: 'Document and apply Coach2Lead controller foundations across ApplicationControllerBase, ApplicationApiControllerBase, ApplicationContextProvider, ResourceManager, UserResourceManager, and Breeze save/query patterns. Use when designing or reviewing MVC/API controller changes, manager usage, or save-pipeline behavior.'
---

# Coach2Lead Controller Base Architecture

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
- Implement or review MVC/API/Breeze controller changes without breaking tenant scoping, save behavior, or manager side effects.
- Make controller-base decisions explicit: base class, save pipeline mode, resource manager scope, and OWIN manager usage.

## Read First
- `references/controller-base-architecture-map.md`
- `checklists/controller-base-change-checklist.md`

## Core Anchors
- `Coach2Lead.Web/App/ApplicationControllerBase.cs`
- `Coach2Lead.Web/App/ApplicationApiControllerBase.cs`
- `Coach2Lead.Web/App/ApplicationContextProvider.cs`
- `Coach2Lead.Web/App/Services/ResourceManager.cs`
- `Coach2Lead.Web/App/Services/UserResourceManager.cs`
- `Coach2Lead.Web/App/Domain/Caching/ApplicationCacheService.cs`
- `Coach2Lead.Web/App/ApplicationErrorService.cs`
- `Coach2Lead.Web/App/ApplicationLogService.cs`
- `Coach2Lead.Web/App/ApplicationMailService.cs`
- `Coach2Lead.Web/App/ApplicationRoleManager.cs`
- `Coach2Lead.Web/App/ApplicationSignInManager.cs`
- `Coach2Lead.Web/App/ApplicationTextService.cs`
- `Coach2Lead.Web/App/ApplicationUserManager.cs`

## Standard Workflow
1. Choose controller base:
   - MVC view/shell controller -> `ApplicationControllerBase`.
   - API/Breeze endpoint controller -> `ApplicationApiControllerBase`.
2. Choose save mode explicitly:
   - Hook-map mode: `ContextProvider.AllowSave/PreventSave/BeforeSave/AfterSave`.
   - Custom override mode: `*ContextProvider.BeforeSaveEntity(...)` whitelist.
3. Choose data-scope manager explicitly:
   - User-facing endpoint/query -> `UserResourceManager`.
   - System/background/admin data flow -> `ResourceManager` only with explicit tenant checks.
4. Validate OWIN manager usage and side effects:
   - `ErrorService` persistence + error mail.
   - `MailService` and `TextService` queue/send behavior.
   - `UserManager` claims refresh and permission/feature checks.
5. Validate query lifecycle behavior:
   - Read-heavy queries use `using (ResourceManager.WithoutTracking())`.
   - Any impersonation scope is disposed in the same request flow.

## Guardrails
- Never expose unscoped tenant data in user-facing API or MVC flows.
- Prefer `UserResourceManager` query methods for user-facing flows; avoid re-implementing company filters in controllers.
- Always dispose `WithoutTracking`, `WithoutChangeTracking`, and impersonation scopes.
- If a custom `*ContextProvider` overrides `BeforeSaveEntity` and does not call `base.BeforeSaveEntity(...)`, do not assume `AllowSave/BeforeSave/AfterSave` hook maps run.
- Treat `ApplicationCacheService` runtime cache API as incomplete in current state (`CacheManager.GetCache*` throws `NotImplementedException`).

## Validation
- Controller inheritance is correct for task intent.
- Save mode is explicit and consistent with module conventions.
- Tenant and secured-access filters are enforced at query roots.
- OWIN manager calls are intentional and side effects are understood.
- Read/query paths apply tracking mode intentionally.

## Golden Example
Area API controller using `ApplicationApiControllerBase`, hook-map save mode, and tenant-safe query.
```csharp
[Authorize]
[BreezeController]
public class ExampleResourceController : ApplicationApiControllerBase
{
    public ExampleResourceController()
        : base(new ApplicationContextProvider(), Module.None)
    {
        ContextProvider.AllowSave<ExampleEntity>();
    }

    [HttpGet]
    [EnableBreezeQuery(MaxExpansionDepth = 0)]
    public IQueryable<ExampleEntity> ExampleEntities()
    {
        using (ResourceManager.WithoutTracking())
        {
            return ResourceManager.Query<ExampleEntity>()
                                  .Where(e => e.CompanyId == ResourceManager.GetCurrentUserInfo().CompanyId);
        }
    }
}
```

## Output Contract
- Controller base choice and rationale.
- Save mode choice and enforcement points.
- Resource manager scope choice and tenant/access filters.
- OWIN manager touchpoints and side effects.
- Verification outcomes for metadata/query/save behavior.

## Companion Skills
- `c2l-breeze-webapi`
- `c2l-repository-pattern`
- `c2l-multi-tenancy-guards`
- `c2l-user-claims`
- `c2l-messaging-pipeline`
- `c2l-new-area-module`

## Skill-Specific Topics
- `ApplicationApiControllerBase.Initialize(...)` pushes OWIN context into `ApplicationContextProvider`.
- `ApplicationControllerBase` and `ApplicationApiControllerBase` expose the same protected manager set: `CacheService`, `ErrorService`, `LogService`, `MailService`, `RoleManager`, `SignInManager`, `TextService`, `UserManager`.
- `UserResourceManager` is partial and is extended by module-specific files such as `Coach2Lead.Web/Areas/Metrics/UserResourceManager.cs`.
