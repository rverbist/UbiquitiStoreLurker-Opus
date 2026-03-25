---
name: c2l-permissions-management
description: 'Implement and debug Coach2Lead permissions end-to-end: permission key definition, role/user assignment, backend enforcement, account-switch correctness, and Angular route/action checks. Use when adding/changing permissions or diagnosing access-control behavior.'
---

# Coach2Lead Permissions Management

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
- Keep permission behavior consistent across server, persistence, and client enforcement.
- Avoid authorization regressions during permission key and assignment changes.

## Read First
- `references/permission-system-map.md`
- If claims contract is involved, also read `.github/skills/c2l-user-claims/references/claims-identity-map.md`.

## Core Anchors
- `Coach2Lead.Web/App/Domain/Permissions/Permission.cs`
- `Coach2Lead.Web/App/Domain/Permissions/Permissions.cs`
- `Coach2Lead.Web/App/ApplicationUserManager.cs`
- `Coach2Lead.Web/Areas/Manage/Controllers/API/ManageResourceController.cs`
- `Coach2Lead.Web/Areas/Surveys/Angular/coach2lead/services/PermissionsService.js`
- `Coach2Lead.Web/Areas/Surveys/Angular/coach2lead/services/ApplicationService.js`

## Standard Workflow
1. Add/update permission key constant and registration node.
2. Update defaults if permission should be in baseline role setup.
3. Ensure role/user assignment paths in Manage API remain coherent.
4. Enforce server checks with `CheckPermissions`/`HasPermissions` in sensitive operations.
5. Align client constants and route/action guards.
6. Verify account-switch refresh behavior and resulting effective permissions.

## Guardrails
- Keep key strings identical across C#, DB rows, and JS constants.
- Do not rely on client-only permission checks for sensitive server operations.
- Respect admin bypass logic intentionally; do not duplicate it inconsistently.

## Validation
- Role-based and user-direct assignment scenarios pass.
- Disabled permission blocks route/action/backend path.
- Enabled permission allows path.
- Account switching updates effective permissions as expected.

## Golden Example
Server guard plus client route guard pair.
1. Add backend check: `UserManager.CheckPermissions(true, Permission.C2L_MANAGE_USERS);`.
2. Add route guard: `data.permissions: [permissions.C2L_MANAGE_USERS]` in route definition.
3. Verify non-admin account without key is blocked on both server and client.

## Output Contract
- Permission keys changed.
- Assignment paths changed (role/user).
- Backend and client enforcement points.
- Smoke scenarios executed.

## Companion Skills
- `c2l-user-claims`
- `c2l-feature-management`
- `c2l-multi-tenancy-guards`

## Skill-Specific Topics
- `EditUser` stores direct permissions excluding role-derived keys by design.
- Historical key spellings (for example `SECURTY`) may be intentionally stable; treat renames as migration work.
