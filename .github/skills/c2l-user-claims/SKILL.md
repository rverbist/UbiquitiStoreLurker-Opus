---
name: c2l-user-claims
description: 'Maintain Coach2Lead claims identity contract (custom UserInfo claims, producer/consumer paths, refresh/version behavior, and account-switch correctness). Use when adding/changing claims, diagnosing stale identity context, or auditing claims consumers.'
---

# Coach2Lead User Claims

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
- Change claims contract safely without breaking authentication, tenant context, or authorization flows.
- Keep claim producers, refresh paths, and claim consumers consistent.

## Read First
- `references/claims-identity-map.md`
- `checklists/claims-change-checklist.md`

## Core Anchors
- `Coach2Lead/AppConst.cs`
- `Coach2Lead.Web/App/Domain/Claims/IUserInfo.cs`
- `Coach2Lead.Web/App/Domain/Claims/UserInfo.cs`
- `Coach2Lead.Web/App/Domain/Claims/ClaimsIdentityExtensions.cs`
- `Coach2Lead.Web/App/ApplicationUserManager.cs`
- `Coach2Lead.Web/App/Services/ResourceManager.cs`
- `Coach2Lead.Web/Startup/Startup.cs`

## Standard Workflow
1. Update claim constants and `UserInfo` mapping coherently.
2. Update source projections in `ResourceManager.GetUserInfoOrDefault*`.
3. Update write paths: `CreateIdentityAsync` and `UpdateClaimsAsync`.
4. Audit strict/tolerant/direct consumers.
5. Verify account-switch flows still call `SetAccount(...)` then `UpdateClaimsAsync()`.
6. Decide whether to bump `AppConst.ClaimsVersion`.

## Guardrails
- Treat missing scalar claims as strict parse failures by default.
- Do not move full authorization payloads (permissions/features) into custom claims.
- Keep claims aligned to active account-link context.

## Validation
- Sign-in produces parseable `UserInfo` claims.
- Account switch updates account/company/person claim values.
- Claims version mismatch behavior is intentional and verified.
- Strict and tolerant claim readers behave as expected.

## Golden Example
Safe contract change sequence.
1. Add new claim constant in `AppConst.cs`.
2. Add property + `[Claim(...)]` in `UserInfo` and `IUserInfo`.
3. Populate value in `ResourceManager.GetUserInfoOrDefault*`.
4. Verify `CreateIdentityAsync` and `UpdateClaimsAsync` emit the claim.
5. Audit any strict readers and bump `ClaimsVersion` only if required.

## Output Contract
- Claim contract delta.
- Producer and consumer updates.
- Claims version decision.
- Validation scenarios executed.

## Companion Skills
- `c2l-permissions-management`
- `c2l-feature-management`
- `c2l-multi-tenancy-guards`

## Skill-Specific Topics
- `GetClaims<UserInfo>` is strict and throws on missing/duplicate scalar claims.
- `GetClaimsOrDefault<UserInfo>` is tolerant and returns `null` on parse failures.
- Core identity dependencies (`GetUserId`, role claims) must remain intact.
