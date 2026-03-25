# Claims Identity Map (Coach2Lead)

<!-- toc:start -->
<details>
  <summary>
   <strong>Table of Contents</strong> Click to open
  </summary>

- [1) Claims model definitions](#1-claims-model-definitions)
- [2) Persisted custom claims matrix (12 claims)](#2-persisted-custom-claims-matrix-12-claims)
- [3) Claim serialization/parsing behavior](#3-claim-serializationparsing-behavior)
- [4) Claim lifecycle](#4-claim-lifecycle)
- [5) Account switching and refresh call sites](#5-account-switching-and-refresh-call-sites)
- [6) Core (non-custom) identity claims depended on by app code](#6-core-non-custom-identity-claims-depended-on-by-app-code)
- [7) What is not persisted in custom claims](#7-what-is-not-persisted-in-custom-claims)
- [8) Consumer map by category](#8-consumer-map-by-category)
- [9) Failure modes and diagnostics](#9-failure-modes-and-diagnostics)
- [10) Repeatable audit commands](#10-repeatable-audit-commands)
- [11) Validation scenarios](#11-validation-scenarios)

</details>
<!-- toc:end -->

## 1) Claims model definitions

Primary custom claims model files:
1. `Coach2Lead.Web/App/Domain/Claims/ClaimAttribute.cs`
2. `Coach2Lead.Web/App/Domain/Claims/IHaveClaims.cs`
3. `Coach2Lead.Web/App/Domain/Claims/IUserInfo.cs`
4. `Coach2Lead.Web/App/Domain/Claims/UserInfo.cs`
5. `Coach2Lead.Web/App/Domain/Claims/ClaimsIdentityExtensions.cs`
Claim URI constants and version value:
1. `Coach2Lead/AppConst.cs`
Core writer/refresh hooks:
1. `Coach2Lead.Web/App/ApplicationUserManager.cs`
Source-of-truth population queries:
1. `Coach2Lead.Web/App/Services/ResourceManager.cs` (`GetUserInfoOrDefault` overloads)

## 2) Persisted custom claims matrix (12 claims)

All 12 custom claims map to `UserInfo` properties with `[Claim(AppConst.*Claim)]`.
| Claim constant | Claim URI | `UserInfo` property (type) | Source fields (`ResourceManager.GetUserInfoOrDefault*`) | Writer path | Representative readers/usages |
| - | - | - | - | - | - |
| `AppConst.UserIdClaim` | `https://app.coach2lead.be/2023/05/25/identity/claims/userid` | `UserInfo.UserId` (`string`) | `l.User.Id` | `ApplicationUserManager.CreateIdentityAsync`, `ApplicationUserManager.UpdateClaimsAsync` | `ApplicationControllerBase.GetUserInfo()`, `ApplicationHub.GetUserInfo()`, log-context providers |
| `AppConst.UserNameClaim` | `https://app.coach2lead.be/2023/05/25/identity/claims/username` | `UserInfo.UserName` (`string`) | `l.User.Name` | `ApplicationUserManager.CreateIdentityAsync`, `ApplicationUserManager.UpdateClaimsAsync` | `UserDto`, `ApplicationSession`, log-context providers |
| `AppConst.AccountLinkIdClaim` | `https://app.coach2lead.be/2023/05/25/identity/claims/accountlinkid` | `UserInfo.AccountLinkId` (`int`) | `l.Id` | `ApplicationUserManager.CreateIdentityAsync`, `ApplicationUserManager.UpdateClaimsAsync` | `ApplicationUserManager.HasFeatures`, `ApplicationUserManager.HasPermissions`, `SetLastActivity` calls |
| `AppConst.AccountIdClaim` | `https://app.coach2lead.be/2023/05/25/identity/claims/accountid` | `UserInfo.AccountId` (`int`) | `l.Account.Id` | `ApplicationUserManager.CreateIdentityAsync`, `ApplicationUserManager.UpdateClaimsAsync` | `UserResourceManager.GetApplicationAccountId()` and account-scoped flows |
| `AppConst.AccountNameClaim` | `https://app.coach2lead.be/2023/05/25/identity/claims/accountname` | `UserInfo.AccountName` (`string`) | `l.Account.Name` | `ApplicationUserManager.CreateIdentityAsync`, `ApplicationUserManager.UpdateClaimsAsync` | Account-context logging and user context operations |
| `AppConst.CompanyIdClaim` | `https://app.coach2lead.be/2023/05/25/identity/claims/companyid` | `UserInfo.CompanyId` (`int`) | `l.Account.Company.Id` | `ApplicationUserManager.CreateIdentityAsync`, `ApplicationUserManager.UpdateClaimsAsync` | `UserResourceManager.CompanyQuery`, tenant-scoped queries |
| `AppConst.CompanyNameClaim` | `https://app.coach2lead.be/2023/05/25/identity/claims/companyname` | `UserInfo.CompanyName` (`string`) | `l.Account.Company.Name` | `ApplicationUserManager.CreateIdentityAsync`, `ApplicationUserManager.UpdateClaimsAsync` | `ApplicationSession`, `ApplicationSessionState`, log-context providers |
| `AppConst.PersonIdClaim` | `https://app.coach2lead.be/2023/05/25/identity/claims/personid` | `UserInfo.PersonId` (`int`) | `l.Person.Id` | `ApplicationUserManager.CreateIdentityAsync`, `ApplicationUserManager.UpdateClaimsAsync` | `HtmlExtensions.GetPersonId`, `ResourceManager.GetApplicationPersonId()` consumers |
| `AppConst.PersonNameClaim` | `https://app.coach2lead.be/2023/05/25/identity/claims/personname` | `UserInfo.PersonName` (`string`) | `l.Person.Name` | `ApplicationUserManager.CreateIdentityAsync`, `ApplicationUserManager.UpdateClaimsAsync` | `UserDto`, `ApplicationSession`, audit/log contexts |
| `AppConst.IsLocalAdministratorClaim` | `https://app.coach2lead.be/2023/05/25/identity/claims/islocaladministrator` | `UserInfo.IsLocalAdministrator` (`bool`) | `l.Roles.Any(r => r.AccountRole.IsAdmin)` | `ApplicationUserManager.CreateIdentityAsync`, `ApplicationUserManager.UpdateClaimsAsync` | `HtmlExtensions.IsLocalAdministrator`, admin checks via `IUserInfo.IsAdministrator` |
| `AppConst.CultureClaim` | `https://app.coach2lead.be/2023/05/25/identity/claims/culture` | `UserInfo.Culture` (`string`) | `l.Culture ?? l.User.Culture ?? l.Account.Culture` | `ApplicationUserManager.CreateIdentityAsync`, `ApplicationUserManager.UpdateClaimsAsync` | `HtmlExtensions.L(...)`, localization behavior |
| `AppConst.VersionClaim` | `https://app.coach2lead.be/2023/05/25/identity/claims/version` | `UserInfo.Version` (`int`) | `AppConst.ClaimsVersion` | `ApplicationUserManager.CreateIdentityAsync`, `ApplicationUserManager.UpdateClaimsAsync` | `Startup` cookie validation (`Version` mismatch rejection) |
Notes:
1. `UserInfo.IsGlobalAdministrator` and `UserInfo.IsAdministrator` are computed properties and are not persisted as claims.
2. All custom claim values are built from active account-link context returned by `ResourceManager.GetUserInfoOrDefault*`.

## 3) Claim serialization/parsing behavior

Serialization (`IHaveClaims.GetClaims()`):
1. Reflection scans properties on the instance type.
2. Only properties with `[ClaimAttribute]` are included.
3. Scalar property: omitted if value is `null`; otherwise claim value uses `ToString()`.
4. Array property: each non-`null` element becomes one claim with the same claim type.
Parsing (`ClaimsIdentityExtensions.GetClaimsInternal<T>`):
1. Reflection scans target properties with `[ClaimAttribute]`.
2. Scalar claim contract:
- Missing claim -> throw `InvalidOperationException`.
- Duplicate claim -> throw `InvalidOperationException`.
- Single claim value converted via `Convert.ChangeType`.
3. Array claim contract:
- Zero or more claims allowed.
- Values converted to element type and stored as array.
4. `GetClaims<T>` surfaces exceptions.
5. `GetClaimsOrDefault<T>` catches all exceptions and returns `null`.
Null-handling implications:
1. Nullable/optional scalar claims should be designed carefully; omitted scalars break strict parsing.
2. String claims with `null` values are not emitted and therefore treated as missing by strict readers.

## 4) Claim lifecycle

1. Sign-in creation:
- `ApplicationUserManager.CreateIdentityAsync` starts with base identity (`UserManager`), then replaces custom claim types with current `UserInfo` claims.
- If no `UserInfo` is resolved, identity is returned without custom claims.
2. Cookie persistence and revalidation:
- Cookie auth is configured in `Startup`.
- `SecurityStampValidator.OnValidateIdentity` runs on interval (`TimeSpan.FromMinutes(10)`), and identity is regenerated through `CreateIdentityAsync`.
3. Version invalidation:
- `Startup` parses `UserInfo` via `GetClaimsOrDefault<UserInfo>()`.
- If parse fails (`null`) or `userInfo.Version != AppConst.ClaimsVersion`, identity is rejected and signed out.
4. In-session refresh:
- `ApplicationUserManager.UpdateClaimsAsync` recomputes `UserInfo`, replaces existing custom claims, and sets `AuthenticationResponseGrant` with `IsPersistent = true`.

## 5) Account switching and refresh call sites

These call sites follow the same pattern: switch active account then refresh claims.
1. `Coach2Lead.Web/Controllers/API/Coach2LeadResourceController.cs` (`ChangeAccount`)
2. `Coach2Lead.Web/Controllers/MeController.cs` (`SetAccount`)
3. `Coach2Lead.Web/Controllers/HomeController.cs` (`JoinSession`)
4. `Coach2Lead.Web/Controllers/DiagnoseController.cs` (`Company`)
5. `Coach2Lead.Web/Controllers/DiagnoseController.cs` (`Account`)
Common sequence:
1. `ResourceManager.SetAccount(userId, accountId)`
2. `await UserManager.UpdateClaimsAsync()`

## 6) Core (non-custom) identity claims depended on by app code

1. Name identifier:
- `GetUserId()` usage depends on ASP.NET Identity `NameIdentifier` claim.
- Used across controllers/services and directly in startup OIDC flow (`ClaimTypes.NameIdentifier`).
2. Role claims:
- `[Authorize(Roles = ...)]` depends on role claims and role principal behavior.
- Example role-gated paths exist in MVC and API controllers.

## 7) What is not persisted in custom claims

Not persisted in `UserInfo` claim payload:
1. Effective permission lists.
2. Effective feature lists.
3. Role list arrays.
These are queried from DB and returned to client context through:
1. `Coach2LeadResourceController.GetApplicationContext()`

## 8) Consumer map by category

Strict readers (`GetClaims<UserInfo>`):
1. `ApplicationControllerBase.GetUserInfo()`
2. `ApplicationHub.GetUserInfo()`
Tolerant readers (`GetClaimsOrDefault<UserInfo>`):
1. `OwinExtensions.GetUserInfo()`
2. `Startup` cookie validation path
3. `OwinCoach2LeadClaimsLogContextProvider`
Direct claim reads (`FindFirst(AppConst.*Claim)`):
1. `HtmlExtensions.L(...)` -> `CultureClaim`
2. `HtmlExtensions.GetPersonId()` -> `PersonIdClaim`
3. `HtmlExtensions.IsLocalAdministrator()` -> `IsLocalAdministratorClaim`
Indirect broad usage:
1. `UserResourceManager.GetCurrentUserInfo()` hydrates from claims/user id context and drives tenant/account filters across many queries.

## 9) Failure modes and diagnostics

1. Missing scalar claim:
- `GetClaims<UserInfo>` throws (`Claim '...' not found`).
- `GetClaimsOrDefault<UserInfo>` returns `null`.
2. Duplicate scalar claim:
- Strict parse throws (`Claim '...' is found more than once`).
3. Type conversion failure:
- Strict parse throws conversion exception.
- Tolerant parse returns `null`.
4. Claims version mismatch:
- Startup rejects identity and signs user out.
5. No active primary account link:
- `ResourceManager.GetUserInfoOrDefault(user.Id)` may return `null`.
- Identity may be issued without custom claims; strict readers then fail and tolerant readers return `null`.

## 10) Repeatable audit commands

Run from repo root:
```powershell
rg -n "\[Claim\(|IUserInfo|ClaimsIdentityExtensions|UpdateClaimsAsync|ClaimsVersion" Coach2Lead.Web
```
```powershell
rg -n "GetClaims<UserInfo>|GetClaimsOrDefault<UserInfo>|FindFirst\(AppConst\..*Claim\)" Coach2Lead.Web
```
```powershell
rg -n "GetUserId\(|Authorize\(Roles" Coach2Lead.Web
```
```powershell
rg -n "SetAccount\(|UpdateClaimsAsync\(" Coach2Lead.Web/Controllers Coach2Lead.Web/App/Services
```

## 11) Validation scenarios

1. Standard sign-in:
- Assert all expected custom claims exist and parse to expected types.
2. Account switch:
- Trigger switch flow and confirm account/company/person claim values change.
3. Claims version mismatch:
- Force mismatch and verify cookie identity is rejected.
4. Missing claim behavior:
- Validate strict parse throw and tolerant parse `null` behavior.
5. Authorization dependency sanity:
- Validate `GetUserId()` flows and role-based authorize checks still pass.
6. Non-claim payload sanity:
- Validate `GetApplicationContext()` still drives permissions/features from DB context.
