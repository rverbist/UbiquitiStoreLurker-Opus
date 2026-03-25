---
version: '1.0.0'
---

# Plan State Contract

> **Phase reference:** For phase naming across skill documents, see the [Phase Reference Map](workflow-details.md#phase-reference-map) in workflow-details.md.

This document defines the **normative state machine** for same-day gap-analysis runs, `plan.yaml` task statuses, run history fields, and artifact ownership. It is the compact reference for the workflow state model used by the `c2l-gap-analysis` skill.

## Purpose

- Keep the main skill file focused on workflow.
- Define one unambiguous same-day state model for `gem-orchestrator` and `gem-planner`.
- Prevent duplicate same-day runs, malformed plan states, and mismatched `plan.yaml` / `plan.md` updates.

## Artifact Set

The dated directory `${PLAN_ROOT}/${SCOPE}-gap-analysis-${YYYYMMDD}/` is the **single same-day artifact set** for the scope.

Possible artifact shapes:

| Shape | Meaning |
| ----- | ------- |
| `gap-analysis.md` only | Same-day run closed with no remaining implementation work |
| `gap-analysis.md` + `plan.yaml` + `plan.md` | Same-day plan exists and may be active or exhausted |
| `plan.md` without `plan.yaml`, missing statuses, unknown statuses | Invalid artifact set; repair before continuing |

No second fresh same-day run is created for the same scope/date.

## Artifact Ownership

> Canonical artifact ownership table lives in **SKILL.md § Output Contract**. Do not duplicate it here.

## Machine Formats

- Use `YYYYMMDD` for **directory names** (`${SCOPE}-gap-analysis-YYYYMMDD`), `plan_id`, and run-qualified headings such as `Scope Decision - YYYYMMDD Run N`.
- Use `YYYYMMDD-HHmmss` for **`RUN_ID`** and walkthrough filenames (`walkthrough-completion-YYYYMMDD-HHmmss.md`). `RUN_ID` is a timestamp frozen **once** at the start of Phase 4 using local system time. It is a finer-grained identifier within a plan day - multiple execution passes on the same day each get a unique `RUN_ID`.
- Use `YYYY-MM-DD` only in human-readable prose fields and ISO-like timestamps.

## Priority Mapping

This table is the canonical cross-artifact mapping between the task priorities used in `gap-analysis.md` / `plan.md` and the YAML values stored in `plan.yaml`.

| Gap-analysis / plan.md | plan.yaml | Meaning |
| ---------------------- | --------- | ------- |
| `P0` | `critical` | Immediate correction required. Current state is broken, misleading, or prevents basic use. |
| `P1` | `high` | Critical path to a usable end-to-end workflow. |
| `P2` | `medium` | Important for completeness, but does not block the basic workflow. |
| `P3` | `low` | Valuable, but not on the MVP path. |
| `P4` | `deferred` | Explicitly deferred or optional. Not implemented in the current run. |

## Same-Day States

The dated directory may be in exactly one of these states:

| State | Detection rule | Required behavior |
| ----- | -------------- | ----------------- |
| `closed-no-gap` | `gap-analysis.md` exists and `plan.yaml` does not | Report the existing no-gap result and stop |
| `active-plan` | `plan.yaml` exists and at least one task is `deferred` or `pending` | Resume at scope selection or execution |
| `exhausted-plan` | `plan.yaml` exists and all tasks are `completed` or `blocked` | Report the exhausted same-day plan and stop |
| `invalid` | Missing required artifacts or invalid task state data | Stop and repair before continuing |
| `new-run` | Dated directory does not exist | Start full workflow |

## Task Status Contract

Every task in `plan.yaml` must carry exactly one of these statuses:

| Status | Meaning | Default selection behavior |
| ------ | ------- | -------------------------- |
| `deferred` | Known work not yet approved for the current execution pass | Selectable only if dependencies are satisfied |
| `pending` | Approved for the current execution pass and ready for dispatch | Resume execution; do not show as selectable |
| `completed` | Completed within this plan history | Historical context only |
| `blocked` | Attempted or reviewed but not proceeding without explicit reconsideration | Historical context only unless user explicitly asks to reconsider it |

Unknown or missing task statuses make the plan invalid.

## Transition Summary

```text
new-run
  -> closed-no-gap     (research/planning finds no remaining work)
  -> active-plan       (plan.yaml created with deferred tasks)

active-plan
  -> active-plan       (same-day continuation with deferred or pending work still remaining)
  -> exhausted-plan    (all tasks become completed or blocked)
  -> invalid           (artifacts drift or schema becomes malformed)

closed-no-gap
  -> closed-no-gap     (same-day re-invocation reports existing result)

exhausted-plan
  -> exhausted-plan    (same-day re-invocation reports existing result)
```

## Scope Selection Rules

When the same-day state is `active-plan`, partition tasks into these groups:

| Group | Rule | Selectable |
| ----- | ---- | ---------- |
| Completed | `status: completed` | No |
| Blocked | `status: blocked` | No, unless explicitly reconsidered |
| Available now | `status: deferred` and all dependencies are `completed`, OR all unmet dependencies are also being selected in the same scope pass forming a valid acyclic chain | Yes |
| Not yet unblocked | `status: deferred` and has unmet dependencies not being selected in the same pass | No |

> **Dependency-chain selection**: A deferred task is selectable if (a) all its dependencies are already in `completed` status, **or** (b) all of its unmet dependencies are also being selected in the same scope pass and together they form a valid acyclic dependency chain. Selecting a task whose dependency is neither completed nor also in-scope for the same pass is rejected with a prompt to also include the prerequisite.

Blocked-task reconsideration is a special case, not a normal scope pick.

## Reconsidering Blocked Tasks

Before a blocked task can re-enter scope:

1. The user must explicitly ask to reconsider it.
2. `gem-researcher` must perform targeted re-verification.
3. Only after successful re-verification may `gem-planner` move it back into the selectable set.
4. The reconsideration must be recorded in both `plan.yaml` and `plan.md`.

## Targeted Re-Verification Triggers

Targeted re-verification is required before a task enters scope when any of the following apply:

1. Its dependency was completed outside this plan's normal status transitions.
2. Relevant code changed since the original research pass.
3. The task was previously `blocked` and is being reconsidered.
4. The branch head moved materially since the plan was authored.

## Same-Day Continuation Contract

When the dated directory is in the `active-plan` state:

1. Skip the full research and planning phases. Resume at scope selection or execution.
2. Read `plan.yaml` and validate task states before presenting scope.
3. Partition tasks using the Scope Selection Rules in this file.
4. Reconsider blocked tasks only through the blocked-task path plus targeted re-verification.
5. Apply the Two-Phase Update Protocol in this file for every execution pass.
6. Skip Checkpoint 1 when planning artifacts are already committed for the same dated directory.
7. Generate exactly one walkthrough file per execution pass using the frozen `RUN_ID`.

## plan.yaml History Contract

`gem-planner` initialises `scope_history: []` and `execution_history: []` as **empty arrays** when first creating `plan.yaml`. These fields are never absent from a valid `plan.yaml`. Neither field is pre-populated - entries are appended at separate lifecycle phases.

```yaml
scope_history:
  - timestamp: "YYYY-MM-DDTHH:mm:ss"
    in_scope: [T1, T2]
    out_of_scope: [T3, T4]

execution_history:
  - run_number: 1
    run_id: "YYYYMMDD-HHmmss"
    started_at: "YYYY-MM-DDTHH:mm:ss"
    completed_at: "YYYY-MM-DDTHH:mm:ss"
    selected_tasks: [T1, T2]
    completed_tasks: [T1]
    blocked_tasks: [T2]
    commit: "<post-commit: git log -1 --format=%H>"  # placeholder written at walkthrough generation; see Commit Placeholder Contract
    walkthrough: "${PLAN_ROOT}/${SCOPE}-gap-analysis-YYYYMMDD/walkthrough-completion-YYYYMMDD-HHmmss.md"
```

Rules:

- Append one `scope_history` entry at scope-selection time for each execution pass.
- Append one `execution_history` entry when the walkthrough is generated.
- `run_number` is persisted and must match the `Run N` heading used in `plan.md`.
- `RUN_ID` is frozen once at Phase 4 start and reused consistently in walkthrough filename and history fields.
- `run_id` and `started_at` must both derive from the **same frozen `Get-Date` call** at Phase 4 start. `started_at` is the ISO-8601 representation of that same moment (`YYYY-MM-DDTHH:mm:ss`). Setting `started_at` to midnight (`T00:00:00`) or any placeholder value is a hard failure - it must reflect the real clock time captured by `Get-Date`.

## Commit Placeholder Contract

Because the walkthrough and updated plan artifacts are part of the Checkpoint 2 commit bundle, the final commit hash is unknowable when Phase B writes those files.

- Use the literal placeholder `<post-commit: git log -1 --format=%H>` for `execution_history[].commit`, the walkthrough header `Commit`, and the `plan.md` Run History `Commit` field before the Checkpoint 2 commit is created.
- Artifacts remain valid if this placeholder is still present in the committed bundle.
- Backfilling the real hash after commit is optional. If you backfill it, update every placeholder occurrence for that run in one follow-up pass so the artifacts stay consistent.

## plan.md Update Contract

The normative section structure and per-section authoring rules live in [`plan-md-specification.md`](plan-md-specification.md). This section defines the update protocol only.

On continuation passes, `gem-planner` must update:

- `Status Summary` - replaced in place.
- `Scope Decision` - append one run-qualified section per pass.
- `Run History` - append one entry per pass.

Other sections change only when targeted re-verification or replanning changes their substance.

The Dependency Graph remains the **full backlog graph** for the dated plan and is not rewritten to show only the currently selected subset.

## Two-Phase Update Protocol

`plan.yaml` and `plan.md` updates are split across two lifecycle phases per execution pass:

| Phase | When | What `gem-planner` writes |
| ----- | ---- | ------------------------ |
| **A - Scope selection** | Before execution begins (after user confirms scope) | Append `scope_history` entry; add Scope Decision sub-section to `plan.md` Section 8; flip selected tasks from `deferred` -> `pending` in `plan.yaml`. |
| **B - Post-execution** | After `gem-implementer` reports results and before the Checkpoint 2 commit is created | Append `execution_history` entry; replace Status Summary in-place; append Run History entry. Apply the Commit Placeholder Contract in this file for `commit` fields. The walkthrough file is generated separately by `gem-orchestrator` after these plan-artifact writes and before the Checkpoint 2 commit. |

**Rules:**

- Do not merge Phase A and Phase B into a single write - the scope decision must be persisted before any task execution begins.
- Phase A is skipped on resumed executions where the scope was already committed in a prior pass (tasks are already `pending`).
- Phase B is never applied partially - all three plan-artifact writes happen in one `gem-planner` pass before the Checkpoint 2 `git commit`.

## Historical Compatibility

- Historical plans remain read-only artifacts.
- Back-filling older `scope_decision`-style plans is not required.
- This contract applies to future runs and to any plan artifacts newly rewritten under the hardened workflow.
