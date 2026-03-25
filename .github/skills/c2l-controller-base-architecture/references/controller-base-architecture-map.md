# Controller Base Architecture Map (Coach2Lead)

<!-- toc:start -->
<details>
  <summary>
   <strong>Table of Contents</strong> Click to open
  </summary>

- [1) MVC Base Contract (ApplicationControllerBase)](#1-mvc-base-contract-applicationcontrollerbase)
- [2) API Base Contract (ApplicationApiControllerBase)](#2-api-base-contract-applicationapicontrollerbase)
- [3) Breeze Save Pipeline (ApplicationContextProvider)](#3-breeze-save-pipeline-applicationcontextprovider)
- [4) Resource Layers](#4-resource-layers)
  - [4.1 ResourceManager](#41-resourcemanager)
  - [4.2 UserResourceManager](#42-userresourcemanager)
- [5) Exposed Manager API Surface](#5-exposed-manager-api-surface)
  - [5.1 CacheService (`ApplicationCacheService`)](#51-cacheservice-applicationcacheservice)
  - [5.2 ErrorService (`ApplicationErrorService`)](#52-errorservice-applicationerrorservice)
  - [5.3 LogService (`ApplicationLogService`)](#53-logservice-applicationlogservice)
  - [5.4 MailService (`ApplicationMailService`)](#54-mailservice-applicationmailservice)
  - [5.5 RoleManager (`ApplicationRoleManager`)](#55-rolemanager-applicationrolemanager)
  - [5.6 SignInManager (`ApplicationSignInManager`)](#56-signinmanager-applicationsigninmanager)
  - [5.7 TextService (`ApplicationTextService`)](#57-textservice-applicationtextservice)
  - [5.8 UserManager (`ApplicationUserManager`)](#58-usermanager-applicationusermanager)
- [6) OWIN Registration Source](#6-owin-registration-source)
- [7) Known Pitfalls](#7-known-pitfalls)
- [8) Repeatable Audit Commands](#8-repeatable-audit-commands)

</details>
<!-- toc:end -->

## 1) MVC Base Contract (ApplicationControllerBase)

Primary file:
- `Coach2Lead.Web/App/ApplicationControllerBase.cs`
Behavior:
1. Inherits `Controller`.
2. Creates lazy logger per controller type (`ApplicationLogService.CreateLogger(GetType().Name)`).
3. Creates lazy `UserResourceManager` from claims `UserInfo` first, then falls back to user id lookup.
4. Exposes protected OWIN-resolved services/managers:
- `DbContext`
- `LogService`
- `ErrorService`
- `TextService`
- `MailService`
- `CacheService`
- `UserManager`
- `RoleManager`
- `SignInManager`
5. Claims-based identity helpers:
- `GetUserInfo()` uses strict `GetClaims<UserInfo>()` parsing.
- `GetUserId()` uses `User?.Identity?.GetUserId()`.

## 2) API Base Contract (ApplicationApiControllerBase)

Primary file:
- `Coach2Lead.Web/App/ApplicationApiControllerBase.cs`
Behavior:
1. Inherits `ApiController`.
2. Decorated with `[Authorize]` and `[BreezeController]`.
3. Holds `ContextProvider` and a module discriminator used by module-scoped action endpoints.
4. Uses lazy logger and lazy `UserResourceManager` similarly to MVC base, but anchored to `contextProvider.Context`.
5. Exposes the same protected OWIN-resolved services/managers as MVC base.
6. OWIN context propagation:
- `Initialize(HttpControllerContext)` calls `ContextProvider.SetOwinContext(...)`.
7. Shared Breeze endpoints:
- `Metadata()` delegates to `ContextProvider.Metadata()`.
- `SaveChanges(JObject saveBundle)` delegates to `ContextProvider.SaveChangesAsync(...)` and logs via `ErrorService.SaveServerBreezeException(...)` on failure.
8. Shared utility endpoint surface includes attachment, access-right, dimension, company/person lookup, and module-aware action query helpers.

## 3) Breeze Save Pipeline (ApplicationContextProvider)

Primary file:
- `Coach2Lead.Web/App/ApplicationContextProvider.cs`
Core mechanics:
1. Type-based allow/deny map:
- `AllowSave<T>(...)`
- `PreventSave<T>(...)`
2. Type-based save hooks:
- `BeforeSave<T>(...)`
- `AfterSave<T>(...)`
3. Default behavior is deny:
- `BeforeSaveEntity(...)` returns `false` when no allow/prevent entry matches the entity type.
4. Internal pre-save behavior:
- Clears auditable fields for new `IAuditable` entities.
- Blocks `IStaticable` changes by forcing unchanged state and logging warning.
5. Context defaults in `CreateContext()`:
- `ChangeTrackingMode = Full`
- `ProxyCreationEnabled = false`
- `LazyLoadingEnabled = false`
- `CurrentUser` set from OWIN when available.
6. Async after-save dispatch:
- `AfterSaveEntities(...)` calls `AfterSaveEntitiesAsync(...)`, then per-entity handlers.
Important implementation split:
1. Hook-map mode: controllers configure `ContextProvider.AllowSave/BeforeSave/AfterSave` in constructor.
2. Custom provider mode: area `*ContextProvider` overrides `BeforeSaveEntity(...)` with explicit `if (entityInfo.Entity is X) return true;` whitelist.
3. If custom provider override does not call `base.BeforeSaveEntity(...)`, map/hook behaviors in `ApplicationContextProvider` are bypassed for that provider.

## 4) Resource Layers

### 4.1 ResourceManager

Primary file:
- `Coach2Lead.Web/App/Services/ResourceManager.cs`
Responsibilities:
1. Generic EF operations:
- `Query(...)`, `Find(...)`, `Add(...)`, `Remove(...)`, `SaveChanges(...)`, `Reload(...)`.
2. Tracking-control scopes:
- `WithoutTracking()`
- `WithoutChangeTracking()`
3. Application identity/account helpers:
- `GetApplicationUser/Account/Company/Person*`
- `GetUserInfo*` projections.
4. Account switching:
- `SetAccount(userId, accountId)` validates membership/activity, calls `_dbContext.SetAccount(...)`, and emits SignalR `userAccountChanged` message.

### 4.2 UserResourceManager

Primary files:
- `Coach2Lead.Web/App/Services/UserResourceManager.cs`
- `Coach2Lead.Web/Areas/Metrics/UserResourceManager.cs` (partial extension)
Responsibilities:
1. Wraps `ResourceManager` with current-user context (`IUserInfo`/user id).
2. Provides tenant-safe `IQueryable<T>` catalog across domain entities.
3. Applies secured-access restrictions for many `ISecuredAccess`-style entities via:
- `WhereIf(!current.IsAdministrator, !AccessRestricted || ResourceRights.Any(...))`
4. Supports impersonation scopes:
- `WithImpersonation(userId)`
- `WithImpersonation(userId, companyId)`
- `WithImpersonation(personId)`
5. Includes archive/remove helper operations for selected aggregates.

## 5) Exposed Manager API Surface

These managers/services are exposed through protected properties on both:
- `Coach2Lead.Web/App/ApplicationControllerBase.cs`
- `Coach2Lead.Web/App/ApplicationApiControllerBase.cs`

### 5.1 CacheService (`ApplicationCacheService`)

Primary file:
- `Coach2Lead.Web/App/ApplicationCacheService.cs`
Service lifecycle:
1. `OwinCreate(...)`
2. `OwinDispose(...)`
Related interfaces in same file:
1. `IApplicationCacheService`:
- `Get<TKey, TValue>(cacheName, key, factory, dependencies)`
- `Invalidate(dependencies)`
2. `ICacheManager` / `ICache` / `ITypedCache<TKey, TValue>` / `ICacheDependency`.
Implementation status:
1. `CacheManager.GetCache(...)` and typed overloads currently throw `NotImplementedException`.
2. Treat cache manager API as incomplete for runtime usage.

### 5.2 ErrorService (`ApplicationErrorService`)

Primary file:
- `Coach2Lead.Web/App/ApplicationErrorService.cs`
Documented methods:
1. `SaveException(...)`
2. `SaveServerException(...)`
3. `SaveServiceException(...)`
4. `SaveClientException(...)`
5. `SaveServerBreezeException(...)`
6. `SaveServerAuthException(...)`
7. `SaveServerMvcException(...)`
8. `SaveServerApiException(...)`
9. `SaveServerUncaughtException(...)`
Supporting utility:
1. `FormatNameValueCollection(...)`
Operational note:
1. Error save path persists `Coach2LeadError` and then sends notification mail (`ApplicationMailService.Create().SendCoach2LeadError(...)`).

### 5.3 LogService (`ApplicationLogService`)

Primary file:
- `Coach2Lead.Web/App/ApplicationLogService.cs`
Documented methods:
1. `CreateLogger(name)`
2. `CreateLogger(name, options)`
Lifecycle:
1. `OwinCreate(...)`
2. `OwinDispose(...)`

### 5.4 MailService (`ApplicationMailService`)

Primary file:
- `Coach2Lead.Web/App/ApplicationMailService.cs`
Documented methods:
1. `SendAsync(IdentityMessage)`
2. `SendQueuedMailMessages()`
3. `SendMail(int id)`
4. `SendMail(MailMessage mail)`
5. `QueueMail(MailMessage mail)`
6. `SendCoach2LeadError(Coach2LeadError error, Exception exception)`
Operational behavior:
1. Queue + worker friendly APIs exist (`QueueMail`, `SendQueuedMailMessages`).
2. Send path branches by company settings and special Graph API branch for specific company id.

### 5.5 RoleManager (`ApplicationRoleManager`)

Primary file:
- `Coach2Lead.Web/App/ApplicationRoleManager.cs`
Documented C2L surface:
1. `OwinCreate(...)` factory.

### 5.6 SignInManager (`ApplicationSignInManager`)

Primary file:
- `Coach2Lead.Web/App/ApplicationSignInManager.cs`
Documented C2L surface:
1. `OwinCreate(...)` factory.

### 5.7 TextService (`ApplicationTextService`)

Primary file:
- `Coach2Lead.Web/App/ApplicationTextService.cs`
Documented methods:
1. `SendAsync(IdentityMessage)`
2. `SendQueuedTextMessages()`
3. `SendText(int id)`
4. `SendText(TextMessage text)`
5. `QueueText(TextMessage text)`
Operational behavior:
1. Queue + worker friendly APIs exist (`QueueText`, `SendQueuedTextMessages`).
2. Transport uses Twilio and environment redirect/disable switches in `AppConfig`.

### 5.8 UserManager (`ApplicationUserManager`)

Primary file:
- `Coach2Lead.Web/App/ApplicationUserManager.cs`
C2L-specific method surface:
1. `UpdateClaimsAsync()`
2. Password lifecycle overrides:
- `RemovePasswordAsync(...)`
- `AddPasswordAsync(...)`
- `ChangePasswordAsync(...)`
- `ResetPasswordAsync(...)`
3. `InvalidatePasswordAsync(...)`
4. `GeneratePasswordAsync(...)`
5. Feature checks:
- `CheckFeatures(...)`
- `HasFeatures(...)`
6. Permission checks:
- `CheckPermissions(...)`
- `HasPermissions(...)`

## 6) OWIN Registration Source

Registration file:
- `Coach2Lead.Web/Startup/Startup.cs`
Per-request OWIN registrations:
1. `ApplicationDbContext`
2. `ApplicationLogService`
3. `ApplicationErrorService`
4. `ApplicationMailService`
5. `ApplicationTextService`
6. `ApplicationCacheService`
7. `ApplicationUserManager`
8. `ApplicationRoleManager`
9. `ApplicationSignInManager`
This registration is the source for controller-base protected properties that resolve services from `Request.GetOwinContext().Get<T>()`.

## 7) Known Pitfalls

1. Mixed `DbContext` usage in API flows:
- `ApplicationApiControllerBase` exposes `DbContext` (request OWIN context) and `ResourceManager` built from `ContextProvider.Context`.
- Some code paths explicitly note context mismatch risk.
2. Attachment security TODO:
- `UserResourceManager.Attachments(...)` includes a TODO about secured attachments retrievable by id.
3. Cache runtime incompleteness:
- `CacheManager.GetCache*` methods are not implemented.
4. Error logging side effects:
- Error persistence triggers error mail send within `ApplicationErrorService.Save(...)`.
5. Save pipeline confusion:
- Custom `*ContextProvider.BeforeSaveEntity(...)` implementations that skip `base.BeforeSaveEntity(...)` bypass hook-map configuration.

## 8) Repeatable Audit Commands

Run from repo root:
```powershell
rg -n "ApplicationControllerBase|ApplicationApiControllerBase|ApplicationContextProvider" Coach2Lead.Web/Areas Coach2Lead.Web/App -g "*.cs"
```
```powershell
rg -n "CacheService|ErrorService|LogService|MailService|RoleManager|SignInManager|TextService|UserManager" Coach2Lead.Web/App/ApplicationControllerBase.cs Coach2Lead.Web/App/ApplicationApiControllerBase.cs
```
```powershell
rg -n "BeforeSaveEntity\\(|AllowSave\\(|PreventSave\\(|BeforeSave<|AfterSave<" Coach2Lead.Web/Areas Coach2Lead.Web/App -g "*.cs"
```
```powershell
rg -n "WithImpersonation|WithoutTracking|WhereIf\\(!current\\.IsAdministrator|CompanyId == current\\.CompanyId" Coach2Lead.Web/App/Services/UserResourceManager.cs Coach2Lead.Web/Areas/Metrics/UserResourceManager.cs
```
```powershell
rg -n "CreatePerOwinContext<Application(Log|Error|Mail|Text|Cache|UserManager|RoleManager|SignInManager|DbContext)" Coach2Lead.Web/Startup/Startup.cs
```
