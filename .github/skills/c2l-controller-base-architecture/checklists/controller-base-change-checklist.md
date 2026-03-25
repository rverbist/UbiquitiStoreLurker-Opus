# Controller Base Change Checklist (Coach2Lead)

<!-- toc:start -->
<details>
  <summary>
   <strong>Table of Contents</strong> Click to open
  </summary>

- [1) Base Class and Constructor Wiring](#1-base-class-and-constructor-wiring)
- [2) OWIN Manager Dependencies](#2-owin-manager-dependencies)
- [3) Tenant and Access Filters](#3-tenant-and-access-filters)
- [4) Breeze Save Mode and Whitelist](#4-breeze-save-mode-and-whitelist)
- [5) Side Effects and Operational Paths](#5-side-effects-and-operational-paths)
- [6) Smoke Verification](#6-smoke-verification)

</details>
<!-- toc:end -->
Use this checklist for controller changes that touch MVC/API base classes, Breeze save behavior, or manager-service calls.

## 1) Base Class and Constructor Wiring

- [ ] Base class selected intentionally:
  - [ ] `ApplicationControllerBase` for MVC shell/view actions.
  - [ ] `ApplicationApiControllerBase` for API/Breeze actions.
- [ ] API controller constructor calls `: base(new <Area>ContextProvider(), Module.<X>)`.
- [ ] Controller attributes are correct for surface (`[Authorize]`, `[BreezeController]`, role restrictions where needed).

## 2) OWIN Manager Dependencies

- [ ] All used manager properties are available through base class (`CacheService`, `ErrorService`, `LogService`, `MailService`, `RoleManager`, `SignInManager`, `TextService`, `UserManager`).
- [ ] Calls to manager methods are intentional and side effects are understood.
- [ ] If account/claims context changes, follow account switch + claim refresh pattern (`SetAccount(...)` then `UpdateClaimsAsync()` where relevant).

## 3) Tenant and Access Filters

- [ ] User-facing queries use `UserResourceManager` paths by default.
- [ ] Company scoping is explicit (`CompanyId == current.CompanyId` or equivalent navigation path).
- [ ] `AccessRestricted` / `ResourceRights` filters are preserved for non-admin paths.
- [ ] Any impersonation scope (`WithImpersonation(...)`) is disposed in-request.

## 4) Breeze Save Mode and Whitelist

- [ ] Save mode chosen explicitly:
  - [ ] Hook-map mode (`AllowSave/PreventSave/BeforeSave/AfterSave`).
  - [ ] Custom provider whitelist override (`BeforeSaveEntity`).
- [ ] Save whitelist includes all expected entity types and no unsafe extras.
- [ ] If provider overrides `BeforeSaveEntity`, behavior around `base.BeforeSaveEntity(...)` is intentional and documented.

## 5) Side Effects and Operational Paths

- [ ] Error-path persistence/mail side effects reviewed (`ApplicationErrorService`).
- [ ] Messaging side effects reviewed (`ApplicationMailService`, `ApplicationTextService` queue/send semantics).
- [ ] Logging usage reviewed (`ApplicationLogService.CreateLogger(...)` context and noise level).
- [ ] Cache usage reviewed with current limitations (`CacheManager.GetCache*` not implemented).

## 6) Smoke Verification

- [ ] Breeze metadata endpoint works (`Metadata()`).
- [ ] Representative query endpoint works and remains tenant-safe.
- [ ] Representative save endpoint works and follows expected save mode.
- [ ] Admin and non-admin behavior is validated for one secured entity path.
- [ ] No regression in route/controller resolution for changed area.
