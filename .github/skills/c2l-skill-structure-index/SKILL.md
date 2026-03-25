---
name: c2l-skill-structure-index
description: 'Maintain canonical structure and inventory consistency for all c2l-* skills, including frontmatter, section order, TOC integrity, anchor validity, and sync with AGENTS.md. Use when adding, updating, or auditing C2L skills.'
---

# C2L Skill Structure Index

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
- Keep all `c2l-*` skills structurally consistent and easy for agents to parse.
- Prevent drift between skill folders, AGENTS inventory, and skill index listings.

## Read First
- `AGENTS.md`
- `.vscode/scripts/skills/Lint-Skills.ps1`

## Core Anchors
- `.github/skills/c2l-*/SKILL.md`
- `.github/skills/c2l-skill-structure-index/SKILL.md`
- `AGENTS.md`
- `.vscode/scripts/skills/Lint-Skills.ps1`

## Standard Workflow
1. Enumerate all `c2l-*` skill directories in `.github/skills/`.
2. Validate frontmatter (`name`, `description`) and skill/folder name alignment.
3. Validate canonical H2 sections are present and in order (extra H2s between them are allowed).
4. Validate anchored file references and companion-skill links.
5. Sync inventories in this index and in `AGENTS.md`.
6. Run `.vscode/scripts/skills/Lint-Skills.ps1` and resolve all reported issues.

## Guardrails
- Do not keep stale skill names or paths in AGENTS or indexed-skill lists.
- Keep canonical sections present and in order to preserve predictable agent parsing. Extra skill-specific H2s between canonical ones are allowed.
- Keep examples concrete and anchored to real files where possible.

## Validation
- Every `c2l-*` folder has a `SKILL.md` with valid frontmatter.
- All canonical H2 sections are present and in the correct relative order (extra H2s allowed between them).
- TOC entries match H2 headings exactly.
- AGENTS skill table and indexed-skill list match actual folders.
- Lint script exits with status code `0`.

## Golden Example
Typical skill-maintenance pass:
1. Add a new `c2l-*` skill folder with `SKILL.md`.
2. Add the skill row in `AGENTS.md`.
3. Add the skill path in this file's indexed list.
4. Run `.vscode/scripts/skills/Lint-Skills.ps1`.
5. Fix any reported path, section-order, or inventory mismatches before commit.

## Output Contract
- Skill folders touched.
- Inventory changes in `AGENTS.md` and this index.
- Lint results summary.
- Any unresolved references or intentional exceptions.

## Companion Skills
- `c2l-solution-orientation`
- `c2l-build-run-debug`
- `c2l-new-area-module`

## Skill-Specific Topics
- Canonical section order for `c2l-*` skills (all must be present, in this relative order; extra H2s between them are allowed):
  1. `## Objective`
     - `### When to Invoke` *(optional - recommended for skills with complex trigger matching; lists >=5 trigger phrases or keywords for agent auto-discovery)*
  2. `## Read First`
  3. `## Core Anchors`
  4. `## Standard Workflow`
  5. `## Guardrails`
  6. `## Validation`
  7. `## Golden Example`
  8. `## Output Contract`
  9. `## Companion Skills`
  10. `## Skill-Specific Topics`
- Indexed skills:
  - `.github/skills/c2l-breeze-webapi/SKILL.md`
  - `.github/skills/c2l-build-run-debug/SKILL.md`
  - `.github/skills/c2l-chrome-devtools/SKILL.md`
  - `.github/skills/c2l-controller-base-architecture/SKILL.md`
  - `.github/skills/c2l-d3-angularjs-directives/SKILL.md`
  - `.github/skills/c2l-ef6-migrations/SKILL.md`
  - `.github/skills/c2l-ef6-models/SKILL.md`
  - `.github/skills/c2l-eu-ai-act/SKILL.md`
  - `.github/skills/c2l-feature-management/SKILL.md`
  - `.github/skills/c2l-gap-analysis/SKILL.md`
  - `.github/skills/c2l-messaging-pipeline/SKILL.md`
  - `.github/skills/c2l-multi-tenancy-guards/SKILL.md`
  - `.github/skills/c2l-new-area-module/SKILL.md`
  - `.github/skills/c2l-permissions-management/SKILL.md`
  - `.github/skills/c2l-repository-pattern/SKILL.md`
  - `.github/skills/c2l-skill-structure-index/SKILL.md`
  - `.github/skills/c2l-solution-orientation/SKILL.md`
  - `.github/skills/c2l-user-claims/SKILL.md`
  - `.github/skills/c2l-webjobs-routines/SKILL.md`
