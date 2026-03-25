# Feature System Map (Coach2Lead)

<!-- toc:start -->
<details>
  <summary>
   <strong>Table of Contents</strong> Click to open
  </summary>

- [1) Entity model and tenant persistence](#1-entity-model-and-tenant-persistence)
- [2) Where features are defined and registered](#2-where-features-are-defined-and-registered)
- [3) Backend feature check flow](#3-backend-feature-check-flow)
- [4) Claims and tenant/account switching tie-in](#4-claims-and-tenantaccount-switching-tie-in)
- [5) How features are communicated to the client](#5-how-features-are-communicated-to-the-client)
- [6) How client queries and enforces features](#6-how-client-queries-and-enforces-features)
- [7) Feature admin API and UI](#7-feature-admin-api-and-ui)
- [8) Golden examples](#8-golden-examples)
  - [Example A: Core feature tree and defaults](#example-a-core-feature-tree-and-defaults)
  - [Example B: Module-specific registration (Metrics)](#example-b-module-specific-registration-metrics)
  - [Example C: Backend enforcement path](#example-c-backend-enforcement-path)
  - [Example D: Tenant-level enable/disable/reset behavior](#example-d-tenant-level-enabledisablereset-behavior)
  - [Example E: Claims refresh during account switching](#example-e-claims-refresh-during-account-switching)
  - [Example F: Client feature gating (route + runtime checks)](#example-f-client-feature-gating-route--runtime-checks)
- [9) Known pitfalls and checks](#9-known-pitfalls-and-checks)
- [10) Fast audit commands](#10-fast-audit-commands)

</details>
<!-- toc:end -->
For deep claims payload and lifecycle details, cross-load:
1. `.github/skills/c2l-user-claims/SKILL.md`
2. `.github/skills/c2l-user-claims/references/claims-identity-map.md`

## 1) Entity model and tenant persistence

Use this mental model:
`ApplicationUser`
-> `ApplicationAccountLink` (active account context for user)
-> `ApplicationAccount`
-> `ApplicationAccountFeature` (`FeatureKey` rows per account)
Key files:
- `Coach2Lead.Web/App/Models/ApplicationAccountFeature.cs`
- `Coach2Lead.Web/App/Models/ApplicationAccount.cs`
- `Coach2Lead.Web/App/Models/ApplicationAccountLink.cs`
Schema/migrations:
- `Coach2Lead.Web/Migrations/202006250605109_Coach2LeadFeatures.cs`
- `Coach2Lead.Web/Migrations/202303301625342_AddedSomeIndexes.cs`
Notes:
- Feature assignments are tenant/account-level, not global user-level.
- `FeatureKey` is a string and is indexed/non-null in later migration.

## 2) Where features are defined and registered

Base definitions:
- Constants: `Coach2Lead.Web/App/Domain/Features/Feature.cs`
- Tree/node model: `Coach2Lead.Web/App/Domain/Features/ApplicationFeature.cs`
- Root/default registration: `Coach2Lead.Web/App/Domain/Features/Features.cs` (`GetDefaultFeatures`)
Dynamic/module registration pattern:
- Example provider: `Coach2Lead.Web/Areas/Metrics/Authorization.cs` (`MetricsFeatureProvider.Setup`)
- Bootstrapped in `Coach2Lead.Web/Global.asax.cs`:
  - `Metrics.MetricsFeatureProvider.Setup(Features.Instance);`
Important behavior:
- `Features.Instance` is singleton runtime registry.
- New module nodes require `features.Update()` after tree mutation.

## 3) Backend feature check flow

Main runtime check:
- `Coach2Lead.Web/App/ApplicationUserManager.cs`
  - `CheckFeatures(bool requireAll, params string[] values)`
  - `HasFeatures(bool requireAll, params string[] values)`
Logic summary:
1. Read `UserInfo` from OWIN claims (`AccountLinkId` context).
2. Query current `ApplicationAccountLink`.
3. Resolve assigned account feature keys (`link.Account.Features.Select(f => f.FeatureKey)`).
4. Evaluate `all` or `any` based on `requireAll`.
5. Throw user-facing exception when required features are missing (`CheckFeatures`).
Filter attribute infrastructure:
- MVC: `Coach2Lead.Web/Filters/MVC/Action/FeatureAttribute.cs`
- WebApi: `Coach2Lead.Web/Filters/HTTP/Action/FeatureAttribute.cs`
Attribute behavior:
- Validates feature keys against `Features.Instance`.
- Calls user manager feature checks.
Observed note:
- Search for `"[Feature("` currently returns no usages in controllers, so most enforcement appears to be explicit checks and client route gating.

## 4) Claims and tenant/account switching tie-in

Claims payload:
- `Coach2Lead.Web/App/Domain/Claims/UserInfo.cs`
  - includes account-link/company/person/admin/version context
  - does not include full feature list
Claims generation/update:
- `ApplicationUserManager.CreateIdentityAsync(...)` builds C2L claims.
- `ApplicationUserManager.UpdateClaimsAsync()` refreshes claims in-session.
- Claims values are sourced through `ResourceManager.GetUserInfo*`.
Claims version validation:
- `Coach2Lead.Web/Startup/Startup.cs`
  - cookie validation rejects identities when `userInfo.Version != AppConst.ClaimsVersion`.
Tenant/account switching:
- API flow: `Coach2LeadResourceController.ChangeAccount(int id)`
- MVC flow: `MeController.SetAccount(int id)`
- Both call:
  1. `ResourceManager.SetAccount(userId, accountId)` (membership + active-account validation)
  2. `UserManager.UpdateClaimsAsync()`
Why this matters:
- Feature checks use account-link claims context; stale claims after switch can authorize against the wrong account.

## 5) How features are communicated to the client

Server payload:
- `Coach2LeadResourceController.GetApplicationContext()`
  - loads current account feature keys from `ApplicationAccountFeature`
  - returns `ApplicationContextViewModel.Features` as `string[]`
View model contract:
- `ApplicationContextViewModel` in `Coach2LeadResourceController.cs` includes:
  - `Roles`
  - `Permissions`
  - `Features`
  - admin flags and identity objects
Client bootstrap:
- `ApplicationResourceFactory.getApplicationContext()` fetches context.
- `ApplicationService.requestApplicationContext()` stores feature keys in hash map (`instance.features`).

## 6) How client queries and enforces features

Constants:
- `Features` constant in `Coach2Lead.Web/Areas/Surveys/Angular/coach2lead/services/PermissionsService.js`
Runtime checks:
- `ApplicationService.hasFeature(feature)`
- `ApplicationService.hasFeatures(features)`
- `ApplicationService.checkFeature(feature)` (modal + exception)
Route enforcement:
- `StateAuthenticationFactory` checks `state.data.features` using `application.hasFeatures(...)`.
Menu/item visibility:
- `coach2lead.module.js` hides menu entries when `item.features` are not present.
Account-switch client refresh:
- `ApplicationService` watches `instance.accountLink`.
- On change, calls `resources.changeAccount(...)`, then `requestApplicationContext()`, then broadcasts `Coach2Lead::UserAccountChanged`.
- App module listens and reloads current state.

## 7) Feature admin API and UI

Manage API endpoints:
- `FindFeatures` (returns tree-derived `FeatureDto` list with `Enabled` flag per account)
- `EnableFeature(key)`
- `DisableFeature(key)`
- `SetupDefaultFeatures()`
Server file:
- `Coach2Lead.Web/Areas/Manage/Controllers/API/ManageResourceController.cs`
DTO/query models:
- `Coach2Lead.Web/Areas/Manage/Controllers/ViewModels/FeatureDto.cs`
- `Coach2Lead.Web/Areas/Manage/Controllers/ViewModels/FindFeaturesQueryParameters.cs`
Angular integration:
- Factory calls:
  - `findFeatures`
  - `enableFeature`
  - `disableFeature`
  - `setupDefaultFeatures`
  in `Coach2Lead.Web/Areas/Manage/Angular/manage/factories/ManageResourceFactory.js`
- Controller:
  - `Coach2Lead.Web/Areas/Manage/Angular/manage/controllers/SettingsFeaturesController.js`
- Route permission:
  - `Coach2Lead.Web/Areas/Manage/Angular/manage/manage.routes.js` (`permissions.C2L_MANAGE_FEATURES`)

## 8) Golden examples

### Example A: Core feature tree and defaults

Files:
- `Coach2Lead.Web/App/Domain/Features/Feature.cs`
- `Coach2Lead.Web/App/Domain/Features/Features.cs`
Pattern:
1. Add new string key in `Feature.cs`.
2. Add node in `GetDefaultFeatures()` under root `C2L` (or relevant child).
3. Provide clear name/description for manage UI and diagnostics.

### Example B: Module-specific registration (Metrics)

Files:
- `Coach2Lead.Web/Areas/Metrics/Authorization.cs`
- `Coach2Lead.Web/Global.asax.cs`
Pattern:
1. Define module feature constants (for example `C2L.METRICS`).
2. Add feature nodes in `MetricsFeatureProvider.Setup(Features features)`.
3. Call `features.Update()`.
4. Bootstrap provider during app start.

### Example C: Backend enforcement path

Files:
- `Coach2Lead.Web/App/ApplicationUserManager.cs`
- `Coach2Lead.Web/Filters/HTTP/Action/FeatureAttribute.cs`
- `Coach2Lead.Web/Filters/MVC/Action/FeatureAttribute.cs`
Pattern:
1. Validate target feature key exists in `Features.Instance`.
2. Call `CheckFeatures(requireAll, keys...)`.
3. Apply explicit checks in service/controller methods handling sensitive operations.

### Example D: Tenant-level enable/disable/reset behavior

File:
- `Coach2Lead.Web/Areas/Manage/Controllers/API/ManageResourceController.cs`
Pattern:
1. Load current account and account feature rows.
2. Resolve canonical feature via `Features.Instance.Get(key)`.
3. Add/remove `ApplicationAccountFeature` rows.
4. Save once.
5. For baseline reset, keep only the base set (`Feature.C2L`).

### Example E: Claims refresh during account switching

Files:
- `Coach2Lead.Web/Controllers/API/Coach2LeadResourceController.cs`
- `Coach2Lead.Web/Controllers/MeController.cs`
- `Coach2Lead.Web/App/Services/ResourceManager.cs`
Pattern:
1. Validate user belongs to target active account.
2. Call `ResourceManager.SetAccount(...)`.
3. Call `UserManager.UpdateClaimsAsync()`.
4. Reload application context on client.

### Example F: Client feature gating (route + runtime checks)

Files:
- `Coach2Lead.Web/Areas/Exchange/Angular/exchange/exchange.routes.js`
- `Coach2Lead.Web/Areas/Processes/Angular/processes/processes.routes.js`
- `Coach2Lead.Web/Areas/Metrics/Angular/metrics/metrics.routes.js`
- `Coach2Lead.Web/Areas/Exchange/Angular/exchange/services/ExchangeDomainService.js`
- `Coach2Lead.Web/Areas/Actions/Angular/actions/services/ActionsDomainService.js`
- `Coach2Lead.Web/Areas/Audits/Angular/audits/services/AuditsDomainService.js`
Pattern:
1. Add route guard using `data.features`.
2. Add runtime action guard using `checkFeature`/`hasFeature`.
3. Keep permission checks and feature checks separate and intentional.

## 9) Known pitfalls and checks

1. Client-only checks are insufficient for sensitive operations.
2. Feature keys must match exactly across C#, DB, and JS constants.
3. Attribute infrastructure exists, but verify actual usage before assuming it is active.
4. Account switch requires claims refresh; missing refresh creates stale authorization behavior.
5. `ApplicationService.getObjectFeatures` currently references `userHasFeature`, while primary methods are `hasFeature/hasFeatures`; avoid copying inconsistent helper usage blindly.

## 10) Fast audit commands

Run from repo root:
```powershell
rg -n "class Feature|GetDefaultFeatures|class Features" Coach2Lead.Web/App/Domain/Features
```
```powershell
rg -n "MetricsFeatureProvider|Setup\\(Features\\.Instance\\)" Coach2Lead.Web/Areas/Metrics/Authorization.cs Coach2Lead.Web/Global.asax.cs
```
```powershell
rg -n "HasFeatures|CheckFeatures|ApplicationAccountFeature|FeatureAttribute" Coach2Lead.Web/App/ApplicationUserManager.cs Coach2Lead.Web/Filters
```
```powershell
rg -n "GetApplicationContext|ChangeAccount|SetAccount\\(|UpdateClaimsAsync" Coach2Lead.Web/Controllers/API/Coach2LeadResourceController.cs Coach2Lead.Web/Controllers/MeController.cs Coach2Lead.Web/App/Services/ResourceManager.cs
```
```powershell
rg -n "hasFeature\\(|hasFeatures\\(|checkFeature\\(|state\\.data\\.features" Coach2Lead.Web/Areas/Surveys/Angular/coach2lead Coach2Lead.Web/Areas/Exchange Coach2Lead.Web/Areas/Processes Coach2Lead.Web/Areas/Metrics
```
```powershell
rg -n "\\[Feature\\(" Coach2Lead.Web -g "*.cs"
```
