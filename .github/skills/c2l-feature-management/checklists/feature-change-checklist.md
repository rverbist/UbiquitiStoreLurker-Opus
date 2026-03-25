# Feature Change Checklist (Coach2Lead)

<!-- toc:start -->
<details>
  <summary>
   <strong>Table of Contents</strong> Click to open
  </summary>

- [1) Scope and design](#1-scope-and-design)
- [2) Definition and registration](#2-definition-and-registration)
- [3) Backend enforcement](#3-backend-enforcement)
- [4) Claims and account switching](#4-claims-and-account-switching)
- [5) Server-to-client contract](#5-server-to-client-contract)
- [6) Client constants and gating](#6-client-constants-and-gating)
- [7) Manage API/UI behavior](#7-manage-apiui-behavior)
- [8) Required test scenarios](#8-required-test-scenarios)
- [9) PR readiness](#9-pr-readiness)

</details>
<!-- toc:end -->
Use this checklist for any feature-switch change.

## 1) Scope and design

- [ ] Target area/module identified.
- [ ] Feature classified as global/root or module-specific.
- [ ] Backend enforcement decision recorded (default is required).
- [ ] Tenant/account impact documented (which accounts should have feature).
- [ ] DB schema impact evaluated.
- [ ] If schema touched, EF6 migration added under `Coach2Lead.Web/Migrations/`.

## 2) Definition and registration

- [ ] Feature key added/updated in `Coach2Lead.Web/App/Domain/Features/Feature.cs` (or module feature constants).
- [ ] Feature tree registration added/updated:
  - [ ] `Coach2Lead.Web/App/Domain/Features/Features.cs` or
  - [ ] module provider (for example `Areas/Metrics/Authorization.cs`)
- [ ] `features.Update()` called after dynamic tree mutation.
- [ ] Key naming is consistent across C#, JS constants, and persisted rows.

## 3) Backend enforcement

- [ ] Sensitive backend operations enforce feature checks (`ApplicationUserManager.CheckFeatures` or equivalent explicit guard).
- [ ] Optional filter usage (`FeatureAttribute`) validated against existing area conventions.
- [ ] Invalid key behavior confirmed to fail fast.
- [ ] Multi-tenant/account scoping validated in affected queries/services.

## 4) Claims and account switching

- [ ] Account switch path includes both:
  - [ ] `ResourceManager.SetAccount(...)`
  - [ ] `UserManager.UpdateClaimsAsync()`
- [ ] Claims version behavior unchanged or intentionally updated (`AppConst.ClaimsVersion` path).
- [ ] Feature behavior verified after tenant/account switch.

## 5) Server-to-client contract

- [ ] `GetApplicationContext()` includes expected account feature keys.
- [ ] `ApplicationContextViewModel.Features` contract remains valid for client consumers.
- [ ] If Manage endpoints changed, contract impact is documented.

## 6) Client constants and gating

- [ ] Feature constants added/updated in `PermissionsService.js` (`Features` constant).
- [ ] Route gating uses `data.features` where required.
- [ ] Menu visibility paths evaluate `application.hasFeatures`.
- [ ] Action/service paths use `application.checkFeature` or `application.hasFeature` as intended.
- [ ] AngularJS DI remains array-annotated/minification-safe.

## 7) Manage API/UI behavior

- [ ] `FindFeatures` reflects expected `Enabled` and `IsActive` states.
- [ ] `EnableFeature` persists assignment for current account.
- [ ] `DisableFeature` removes assignment for current account.
- [ ] `SetupDefaultFeatures` resets to intended baseline.
- [ ] Manage route permission (`C2L_MANAGE_FEATURES`) still enforced.

## 8) Required test scenarios

- [ ] Disabled feature account:
  - [ ] backend call/path blocked
  - [ ] client route/menu/action blocked or hidden
- [ ] Enabled feature account:
  - [ ] backend call/path allowed
  - [ ] client route/menu/action available
- [ ] Tenant/account switching:
  - [ ] switching changes effective feature behavior immediately
  - [ ] no stale context after switch
- [ ] Unknown feature key:
  - [ ] guard/validation fails fast
- [ ] Manage flow:
  - [ ] toggle feature and verify `ApplicationAccountFeature` persistence
- [ ] Default reset:
  - [ ] baseline features restored as expected
- [ ] Regression:
  - [ ] unrelated routes/modules unchanged

## 9) PR readiness

- [ ] PR includes changed files and rationale.
- [ ] Reviewer focus calls out tenant scoping, backend guard, and account-switch behavior.
- [ ] DB impact declared (`none` or migration name + summary).
- [ ] UI smoke-test routes/views listed.
