# Permission System Map (Coach2Lead)

<!-- toc:start -->
<details>
  <summary>
   <strong>Table of Contents</strong> Click to open
  </summary>

- [1) Entity relationship model](#1-entity-relationship-model)
- [2) Where permissions are defined and registered](#2-where-permissions-are-defined-and-registered)
- [3) Role-assigned vs user-assigned permissions](#3-role-assigned-vs-user-assigned-permissions)
- [4) Backend permission checking flow](#4-backend-permission-checking-flow)
- [5) Claims-based authentication tie-in](#5-claims-based-authentication-tie-in)
- [6) Tenant/account switching interaction](#6-tenantaccount-switching-interaction)
- [7) How permissions are communicated to client](#7-how-permissions-are-communicated-to-client)
- [8) How client queries/enforces permissions](#8-how-client-queriesenforces-permissions)
- [9) Claims-to-role mapping from external IdP claims](#9-claims-to-role-mapping-from-external-idp-claims)
- [10) Golden examples](#10-golden-examples)
  - [Example A: Add a new permission and wire it end-to-end](#example-a-add-a-new-permission-and-wire-it-end-to-end)
  - [Example B: Role-level assignment mutation pattern (server)](#example-b-role-level-assignment-mutation-pattern-server)
  - [Example C: User-level assignment mutation pattern that avoids duplicates](#example-c-user-level-assignment-mutation-pattern-that-avoids-duplicates)
  - [Example D: Tenant/account switch safe flow](#example-d-tenantaccount-switch-safe-flow)
- [11) Known pitfalls](#11-known-pitfalls)
- [12) Fast audit commands](#12-fast-audit-commands)

</details>
<!-- toc:end -->
For deep claims payload and lifecycle details, cross-load:
1. `.github/skills/c2l-user-claims/SKILL.md`
2. `.github/skills/c2l-user-claims/references/claims-identity-map.md`

## 1) Entity relationship model

Use this mental model:
`ApplicationUser` (identity user)
-> `ApplicationAccountLink` (user + account/company membership; active + primary flags)
-> `ApplicationAccountLinkRole` (membership of a user link in account roles)
-> `ApplicationAccountRole` (role in one account; can be default/admin and optionally mapped to IdP claim name)
-> `ApplicationAccountLinkRolePermission` (permission keys granted to a role)
-> `ApplicationAccountLinkPermission` (permission keys granted directly to a user link)
Key files:
- `Coach2Lead.Web/App/Models/ApplicationAccountLink.cs`
- `Coach2Lead.Web/App/Models/ApplicationAccountRole.cs`
- `Coach2Lead.Web/App/Models/ApplicationAccountLinkRole.cs`
- `Coach2Lead.Web/App/Models/ApplicationAccountLinkRolePermission.cs`
- `Coach2Lead.Web/App/Models/ApplicationAccountLinkPermission.cs`
Schema/migrations:
- `Coach2Lead.Web/Migrations/201910111336057_AppPermisions.cs`
- `Coach2Lead.Web/Migrations/202303301625342_AddedSomeIndexes.cs`
Notes:
- `PermissionKey` is a string key.
- Effective permissions are additive union of:
  - Role permissions
  - User-direct permissions
- No explicit deny row/type exists.

## 2) Where permissions are defined and registered

Base definitions:
- Constants: `Coach2Lead.Web/App/Domain/Permissions/Permission.cs`
- Tree and metadata: `Coach2Lead.Web/App/Domain/Permissions/Permissions.cs`
- Node type: `Coach2Lead.Web/App/Domain/Permissions/ApplicationPermission.cs`
Module-specific registration pattern:
- Example: `Coach2Lead.Web/Areas/Metrics/Authorization.cs`
  - `MetricsPermission` constants
  - `MetricsPermissionProvider.Setup(Permissions permissions)`
  - Calls `permissions.Update()`
- Bootstrapped in `Coach2Lead.Web/Global.asax.cs` via:
  - `Metrics.MetricsPermissionProvider.Setup(Permissions.Instance);`
Codegen integration:
- `Coach2Lead.Web/App/Domain/CodeGen/Writer/Templates/IntegrationTemplate.cs`
  - Generates `*PermissionProvider.Setup(...)` with `permissions.Update()`

## 3) Role-assigned vs user-assigned permissions

Role assignment:
- Managed by `ManageResourceController.EditRole(...)`
- Diffs existing `ApplicationAccountLinkRolePermission` rows against DTO keys.
- File: `Coach2Lead.Web/Areas/Manage/Controllers/API/ManageResourceController.cs:1004`
User-direct assignment:
- Managed by `ManageResourceController.EditUser(...)`
- Computes `permissions = dto.Permissions.Except(rolePermissions)` first, so user-direct set excludes role-derived keys.
- File: `Coach2Lead.Web/Areas/Manage/Controllers/API/ManageResourceController.cs:1270`
Role membership for users:
- Add/remove via:
  - `AddUserToRole(...)`
  - `RemoveUserFromRole(...)`
- File: `Coach2Lead.Web/Areas/Manage/Controllers/API/ManageResourceController.cs:1046`
Default role setup:
- `SetupDefaultPermissions()` resets roles/links, creates `Admin` + `User` roles, applies `Permissions.GetDefaultRolePermissionKeys()`, assigns all active users to default role.
- File: `Coach2Lead.Web/Areas/Manage/Controllers/API/ManageResourceController.cs:1181`

## 4) Backend permission checking flow

Main runtime check:
- `ApplicationUserManager.HasPermissions(requireAll, values...)`
- File: `Coach2Lead.Web/App/ApplicationUserManager.cs:273`
Logic summary:
1. Read current `UserInfo` from OWIN claims (`AccountLinkId` context).
2. If `userInfo.IsAdministrator` => allow all.
3. Query current `ApplicationAccountLink` by `AccountLinkId`.
4. Union role permissions and user-direct permissions.
5. Evaluate `requireAll` or `any`.
Filter attributes:
- HTTP:
  - `Coach2Lead.Web/Filters/HTTP/Action/PermissionAttribute.cs`
  - `Coach2Lead.Web/Filters/HTTP/Action/FeatureAttribute.cs`
- MVC:
  - `Coach2Lead.Web/Filters/MVC/Action/PermissionAttribute.cs`
  - `Coach2Lead.Web/Filters/MVC/Action/FeatureAttribute.cs`
The attributes validate that keys exist in `Permissions.Instance`/`Features.Instance` and delegate to user manager checks.

## 5) Claims-based authentication tie-in

Claims payload type:
- `UserInfo` in `Coach2Lead.Web/App/Domain/Claims/UserInfo.cs`
- Claims include: user/account/accountLink/company/person/local-admin/culture/version.
- It does **not** include full permission list.
Claims generation/update:
- Build at sign-in: `ApplicationUserManager.CreateIdentityAsync(...)`
- Refresh in-session: `ApplicationUserManager.UpdateClaimsAsync()`
- File: `Coach2Lead.Web/App/ApplicationUserManager.cs:34`, `:63`
Claims serialization helpers:
- `Coach2Lead.Web/App/Domain/Claims/ClaimsIdentityExtensions.cs`
- `Coach2Lead.Web/App/Domain/Claims/ClaimAttribute.cs`
Claims version invalidation:
- `Startup` validates `UserInfo.Version == AppConst.ClaimsVersion`; otherwise signs out/rejects identity.
- File: `Coach2Lead.Web/Startup/Startup.cs:57`

## 6) Tenant/account switching interaction

Account switch implementation:
- DB-level switch: `ApplicationDbContext.SetAccount(userId, accountId)` executes stored proc `SetAccount`.
- File: `Coach2Lead.Web/App/Persistence/ApplicationDbContext.Coach2Lead.cs:203`
Service-level switch:
- `ResourceManager.SetAccount(...)` validates membership in active account and notifies SignalR group.
- File: `Coach2Lead.Web/App/Services/ResourceManager.cs:478`
User-facing endpoints call both switch + claims refresh:
- `Coach2LeadResourceController.ChangeAccount(...)` (`SetAccount` + `UpdateClaimsAsync`)
  - `Coach2Lead.Web/Controllers/API/Coach2LeadResourceController.cs:280`
- `MeController.SetAccount(...)`
  - `Coach2Lead.Web/Controllers/MeController.cs:399`
- `HomeController.JoinSession(...)`
  - `Coach2Lead.Web/Controllers/HomeController.cs:186`
- `DiagnoseController.Company/Account(...)`
  - `Coach2Lead.Web/Controllers/DiagnoseController.cs`
Why this matters:
- Permission checks run against account-link context from claims (`AccountLinkId`), so stale claims after account switch will give incorrect authorization.

## 7) How permissions are communicated to client

Server payload:
- `Coach2LeadResourceController.GetApplicationContext()`
  - Loads role names, feature keys, and effective permission keys (role + user direct), unless admin.
  - Returns `ApplicationContextViewModel` with `Roles`, `Permissions`, `Features`, `IsLocalAdministrator`, `IsAdministrator`.
- File: `Coach2Lead.Web/Controllers/API/Coach2LeadResourceController.cs:41`
Client bootstrap:
- `ApplicationResourceFactory.getApplicationContext()` queries endpoint.
  - File: `Coach2Lead.Web/Areas/Surveys/Angular/coach2lead/factories/ApplicationResourceFactory.js:21`
- `ApplicationService.requestApplicationContext()` stores arrays as lookup maps.
  - File: `Coach2Lead.Web/Areas/Surveys/Angular/coach2lead/services/ApplicationService.js:84`

## 8) How client queries/enforces permissions

Constants:
- `Permissions` constant in:
  - `Coach2Lead.Web/Areas/Surveys/Angular/coach2lead/services/PermissionsService.js`
Runtime checks:
- `ApplicationService.userHasPermission(permission)`
- `ApplicationService.userHasPermissions(array)`
- `ApplicationService.checkPermission(permission)` (throws + modal)
- File: `Coach2Lead.Web/Areas/Surveys/Angular/coach2lead/services/ApplicationService.js:228`
Route guards:
- `StateAuthenticationFactory` checks `state.data.permissions` and `state.data.features`.
- File: `Coach2Lead.Web/Areas/Surveys/Angular/coach2lead/factories/StateAuthenticationFactory.js:17`
Route declaration example:
- `Actions` states specify `data.permissions: [permissions.C2L_ACTIONS_...]`
- File: `Coach2Lead.Web/Areas/Actions/Angular/actions/actions.routes.js:28`
Manage permissions UI:
- Controller flatten/check/edit flow:
  - `Coach2Lead.Web/Areas/Manage/Angular/manage/controllers/SettingsPermissionsController.js`
- Resource calls:
  - `findRoles`, `findUsers`, `editRole`, `editUser`, `getPermissionOptions`, etc.
  - `Coach2Lead.Web/Areas/Manage/Angular/manage/factories/ManageResourceFactory.js:148`

## 9) Claims-to-role mapping from external IdP claims

Two related patterns exist:
Legacy/company-specific sign-on:
- Example `SignOnController.Contec.cs` assigns account roles where:
  - `role.IsDefault` OR `role.ClaimName` matches incoming external roles.
- File: `Coach2Lead.Web/Controllers/SignOnController.Contec.cs:197`
OIDC flow:
- `ConnectController` links user to company/account, assigns default roles, then synchronizes role membership from claim values mapped to `ApplicationAccountRole.ClaimName`.
- File: `Coach2Lead.Web/Controllers/ConnectController.cs:499`
This is claims-based role synchronization, not direct permission claims injection.

## 10) Golden examples

### Example A: Add a new permission and wire it end-to-end

1. Add constant:
- `Coach2Lead.Web/App/Domain/Permissions/Permission.cs`
1. Register in tree:
- `Coach2Lead.Web/App/Domain/Permissions/Permissions.cs` inside `GetDefaultPermissions()`
1. Add client constant:
- `Coach2Lead.Web/Areas/Surveys/Angular/coach2lead/services/PermissionsService.js`
1. Use in route/UI:
- Route `data.permissions` in `*.routes.js`
- Or `application.checkPermission(...)` in domain service/controller
1. Assign to role/user:
- Through Manage UI or Manage API (`EditRole`/`EditUser`)
1. Validate:
- Log in with non-admin user in same tenant, verify denial/allow flows.

### Example B: Role-level assignment mutation pattern (server)

Reference implementation:
- `Coach2Lead.Web/Areas/Manage/Controllers/API/ManageResourceController.cs:1004`
Pattern:
1. Load role with current permission rows.
2. Compute set difference against incoming keys.
3. Add missing `ApplicationAccountLinkRolePermission`.
4. Remove obsolete rows.
5. Save once.

### Example C: User-level assignment mutation pattern that avoids duplicates

Reference implementation:
- `Coach2Lead.Web/Areas/Manage/Controllers/API/ManageResourceController.cs:1270`
Pattern:
1. Load user link with roles + role permissions + direct permissions.
2. Compute `rolePermissions`.
3. Keep direct keys only for `dto.Permissions.Except(rolePermissions)`.
4. Diff against direct rows, add/remove, save.

### Example D: Tenant/account switch safe flow

Reference implementations:
- `Coach2Lead.Web/Controllers/API/Coach2LeadResourceController.cs:280`
- `Coach2Lead.Web/Controllers/MeController.cs:399`
Pattern:
1. Validate user membership in target active account.
2. Call `ResourceManager.SetAccount(userId, accountId)`.
3. Call `UserManager.UpdateClaimsAsync()` to refresh `UserInfo` claims.
4. Reload client application context.

## 11) Known pitfalls

- Server/client key drift: key must match exactly across C#, DB rows, and JS constants.
- Admin bypass confusion: global/local admins bypass key checks in client and server.
- Attribute assumptions: `PermissionAttribute` infrastructure exists; confirm actual usage in target area.
- Typo legacy: some existing keys use historical spelling (`SECURTY`) and must remain stable unless coordinated migration is done.

## 12) Fast audit commands

Run these from repo root:
```powershell
rg -n "class Permission|class Permissions|GetDefaultPermissions|GetDefaultRolePermissionKeys" Coach2Lead.Web/App/Domain/Permissions
```
```powershell
rg -n "FindRoles|EditRole|FindUsers|EditUser|GetPermissionOptions|SetupDefaultPermissions" Coach2Lead.Web/Areas/Manage/Controllers/API/ManageResourceController.cs
```
```powershell
rg -n "HasPermissions|CheckPermissions|HasFeatures|CheckFeatures" Coach2Lead.Web/App/ApplicationUserManager.cs
```
```powershell
rg -n "GetApplicationContext|ChangeAccount" Coach2Lead.Web/Controllers/API/Coach2LeadResourceController.cs
```
```powershell
rg -n "userHasPermission|checkPermission|requestApplicationContext" Coach2Lead.Web/Areas/Surveys/Angular/coach2lead/services/ApplicationService.js
```
```powershell
rg -n "\[(Permission|Feature)\b" Coach2Lead.Web -g "*.cs"
```
The last query is useful to verify actual attribute usage in your target area instead of assuming the filter infrastructure is active.
