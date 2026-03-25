---
name: c2l-feature-management
description: 'Implement and troubleshoot Coach2Lead tenant-level feature switches end-to-end: feature key definition/registration, account feature persistence, backend enforcement, application-context exposure, and Angular route/action gating. Use when adding/changing feature flags or debugging feature-access behavior.'
---

# Coach2Lead Feature Management

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
- Deliver feature-flag changes that remain tenant-safe and predictable across server and client.
- Keep account-switch and context refresh behavior aligned with feature enforcement.

## Read First
- `references/feature-system-map.md`
- If claims behavior is involved, also read `.github/skills/c2l-user-claims/references/claims-identity-map.md`.

## Core Anchors
- `Coach2Lead.Web/App/Domain/Features/Feature.cs`
- `Coach2Lead.Web/App/Domain/Features/Features.cs`
- `Coach2Lead.Web/App/Models/ApplicationAccountFeature.cs`
- `Coach2Lead.Web/App/ApplicationUserManager.cs`
- `Coach2Lead.Web/Controllers/API/Coach2LeadResourceController.cs`
- `Coach2Lead.Web/Areas/Manage/Controllers/API/ManageResourceController.cs`
- `Coach2Lead.Web/Areas/Surveys/Angular/coach2lead/services/ApplicationService.js`

## Standard Workflow
1. Add/update feature key and registration node.
2. Decide baseline/default enable strategy per tenant.
3. Ensure backend feature checks protect sensitive operations.
4. Ensure feature keys are returned through `GetApplicationContext()`.
5. Align client constants and route/action/menu gating.
6. Validate account switch refresh path and resulting feature map.

## Guardrails
- Keep key names consistent across C#, DB, and JS.
- Do not rely on client-only feature checks for sensitive backend behavior.
- Keep tenant/account context explicit when toggling features.

## Validation
- Feature disabled account is blocked on intended paths.
- Feature enabled account is allowed.
- Manage enable/disable/reset endpoints mutate expected rows.
- Account switch updates effective feature behavior without stale context.

## Golden Example
Add a module feature and gate a route.
1. Add key constant in module or `Feature.cs`.
2. Register node in feature tree (`Features.cs` or module provider).
3. Add route guard: `data.features: [features.C2L_EXAMPLE]`.
4. Add server check in sensitive API path.
5. Verify behavior in one enabled and one disabled account.

## Output Contract
- Feature key(s) and registration path.
- Backend enforcement points.
- Client gating points.
- Tenant rollout and smoke-test result.

## Companion Skills
- `c2l-user-claims`
- `c2l-permissions-management`
- `c2l-multi-tenancy-guards`

## Skill-Specific Topics
- `SetupDefaultFeatures` behavior defines baseline account feature state and should be treated as rollout policy.
- Feature attributes exist, but explicit server checks are preferred when area usage is inconsistent.
