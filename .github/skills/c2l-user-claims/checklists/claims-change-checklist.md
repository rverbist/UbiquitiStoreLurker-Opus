# Claims Change Checklist (Coach2Lead)

<!-- toc:start -->
<details>
  <summary>
   <strong>Table of Contents</strong> Click to open
  </summary>

- [1) Scope and contract change](#1-scope-and-contract-change)
- [2) Definitions updated consistently](#2-definitions-updated-consistently)
- [3) Producers updated](#3-producers-updated)
- [4) Readers audited](#4-readers-audited)
- [5) Account-switch refresh preserved](#5-account-switch-refresh-preserved)
- [6) Claims version decision recorded](#6-claims-version-decision-recorded)
- [7) Security and multi-tenant implications checked](#7-security-and-multi-tenant-implications-checked)
- [8) Smoke scenarios executed and recorded](#8-smoke-scenarios-executed-and-recorded)
- [9) Suggested verification commands](#9-suggested-verification-commands)

</details>
<!-- toc:end -->
Use this checklist for any custom claims contract or claim consumption change.

## 1) Scope and contract change

- [ ] Change scope is defined (new claim, renamed claim, removed claim, type change, or consumer-only change).
- [ ] Impacted authentication flows are listed (interactive sign-in, remote/basic, account switch, cookie revalidation).
- [ ] Backward compatibility decision is explicit (compatible vs requires version bump).

## 2) Definitions updated consistently

- [ ] Claim constants updated in `Coach2Lead/AppConst.cs`.
- [ ] `IUserInfo` contract updated in `Coach2Lead.Web/App/Domain/Claims/IUserInfo.cs` (if needed).
- [ ] `UserInfo` mapping updated in `Coach2Lead.Web/App/Domain/Claims/UserInfo.cs` with `[Claim(...)]`.
- [ ] Computed properties (`IsGlobalAdministrator`, `IsAdministrator`) remain coherent with contract.

## 3) Producers updated

- [ ] `ResourceManager.GetUserInfoOrDefault*` projections updated in `Coach2Lead.Web/App/Services/ResourceManager.cs`.
- [ ] Claim write path updated in `Coach2Lead.Web/App/ApplicationUserManager.cs`:
  - [ ] `CreateIdentityAsync`
  - [ ] `UpdateClaimsAsync`
- [ ] Optional/null handling reviewed for scalar claim parsing behavior.

## 4) Readers audited

- [ ] Strict readers (`GetClaims<UserInfo>`) reviewed for new/changed requirements.
- [ ] Tolerant readers (`GetClaimsOrDefault<UserInfo>`) reviewed for failure/`null` handling.
- [ ] Direct claim reads (`FindFirst(AppConst.*Claim)`) reviewed (for example `HtmlExtensions`).
- [ ] Indirect resource-context paths reviewed (`UserResourceManager.GetCurrentUserInfo()` usage).

## 5) Account-switch refresh preserved

- [ ] Switch call sites preserve sequence:
  - [ ] `SetAccount(...)`
  - [ ] `UpdateClaimsAsync()`
- [ ] Verified call sites:
  - [ ] `Coach2LeadResourceController.ChangeAccount`
  - [ ] `MeController.SetAccount`
  - [ ] `HomeController.JoinSession`
  - [ ] `DiagnoseController.Company`
  - [ ] `DiagnoseController.Account`

## 6) Claims version decision recorded

- [ ] Decision recorded: keep or increment `AppConst.ClaimsVersion`.
- [ ] If incremented, expected sign-out/re-auth behavior is documented.
- [ ] Startup validation path still reflects intended behavior.

## 7) Security and multi-tenant implications checked

- [ ] Claims still represent active account-link context.
- [ ] No cross-tenant leakage through stale claims.
- [ ] No sensitive authorization data moved into claims inadvertently (permissions/features remain DB-driven).
- [ ] Role and `GetUserId()` dependencies remain valid for authorization attributes and identity helpers.

## 8) Smoke scenarios executed and recorded

- [ ] Standard sign-in: custom claims present and parseable with expected types/values.
- [ ] Account switch: account/company/person claims update immediately after switch.
- [ ] Claims version mismatch: old identity rejected and user signed out.
- [ ] Missing claim behavior: strict parse throws, tolerant parse returns `null`.
- [ ] Core authorization dependency sanity:
  - [ ] `GetUserId()` resolves correctly
  - [ ] `[Authorize(Roles = ...)]` paths still behave correctly
- [ ] Non-claim payload sanity: permissions/features still sourced through `GetApplicationContext()` DB queries.

## 9) Suggested verification commands

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
