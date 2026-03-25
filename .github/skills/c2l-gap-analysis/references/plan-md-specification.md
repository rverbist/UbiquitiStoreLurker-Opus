---
version: '1.0.0'
---

# plan.md - Authoring Specification

This document defines the **mandatory section structure, per-section content rules, and authoring constraints** for every `plan.md` produced by the `c2l-gap-analysis` skill. It is normative: deviations are bugs, not style choices.

**Author**: `gem-planner` (sole file author for `plan.md`). Input comes from `gem-researcher` verified findings and the task register in `gap-analysis.md`.

**Template**: See [`plan-md-skeleton.md`](plan-md-skeleton.md) for a fill-in-the-blanks starting point.

**Related specifications**:
- [`gap-analysis-specification.md`](gap-analysis-specification.md) - the gap analysis that produces the task register feeding this plan.
- [`plan-yaml-specification.md`](plan-yaml-specification.md) - the machine-readable plan that must stay aligned with this file at all times.
- [`plan-state-contract.md`](plan-state-contract.md) - normative same-day state machine, update protocol, and `plan.md` continuation-update rules.

---

## Derivation Rule

`plan.md` is **derived from `plan.yaml`**, not independently authored. The gem-planner call that produces `plan.md` must treat `plan.yaml` as the single authoritative source of truth for all task data.

**Mandatory derivation steps:**

1. **Read `plan.yaml` first** - before writing a single line of `plan.md`.
2. **Copy task fields verbatim** - for every entry in `plan.yaml tasks[]`, produce the corresponding Section 4 task block using the `id`, `title`, `priority`, `estimated_effort`, and `dependencies` fields copied character-for-character. Do not paraphrase task titles, re-order tasks, or derive values from `gap-analysis.md` when `plan.yaml` already has them.
3. **Task count must match** - the number of task blocks in `plan.md §Section 4` must equal the number of entries in `plan.yaml tasks[]`. A count mismatch is a **hard failure**: stop and report before writing the file.
4. **Dependency graph from `plan.yaml`** - `plan.md §Section 5` Mermaid graph is generated from `plan.yaml tasks[].dependencies`. Every task ID that appears in `plan.yaml` must appear as a node; do not add or remove nodes relative to `plan.yaml`.

**Why derivation, not co-authorship:** Writing both files in the same gem-planner call risks exceeding the subagent response-length limit. Splitting into two calls (Step 3 -> plan.yaml, Step 4 -> plan.md) keeps each call within budget. Drift is prevented by requiring the second call to read the first call's output rather than re-deriving it from research findings.

---

## Canonical Section Order

Every `plan.md` MUST contain these sections, in this order:

| # | Section | Mutability |
|---|---------|-----------|
| - | Document Header Block | Set on creation - do not modify |
| 1 | Status Summary | Replaced in-place on each execution pass |
| 2 | Executive Summary | Set on creation; update only if substance changes |
| 3 | Findings Overview | Set on creation |
| 4 | Task Breakdown | Set on creation; update only if replanning adds or changes tasks |
| 5 | Dependency Graph | Set on creation; full backlog graph - never narrowed per pass |
| 6 | Implementation Notes | Append-only: one sub-section added per execution pass |
| 7 | Verification Checklist | Set on creation; check-off state updated per pass |
| 8 | Scope Decision | Append one run-qualified entry per pass |
| 9 | Run History | Append one entry per pass |

Do not rename, reorder, or omit sections.

---

## Document Header Block

Placed immediately after the H1 title. Formatted as bold-label key-value pairs:

```markdown
**Plan:** `${SCOPE}-gap-analysis-YYYYMMDD`
**Date:** YYYY-MM-DD
**Author:** gem-planner
**Source:** `${PLAN_ROOT}/${SCOPE}-gap-analysis-YYYYMMDD/gap-analysis.md`
```

Rules:
- **Plan** must match `plan_id` in `plan.yaml`.
- **Date** is the date the plan was first created - not updated on continuation passes.
- **Source** is the `gap-analysis.md` whose task register produced this plan.

---

## Section 1 - Status Summary

**Purpose**: Machine-scannable snapshot of current plan state. Replaced in-place on every execution pass.

**Mandatory content**:
1. Overall plan status: `active` or `completed`.
2. Task count breakdown: completed / blocked / deferred / total.
3. Date and run number of the most recent execution pass.

**Mandatory format**:

```markdown
## 1  Status Summary

**Status:** active
**As of:** YYYY-MM-DD (Run N)
**Tasks:** N completed · N blocked · N deferred · N total
```

**Rules**:
- Replace the entire section content on every pass - do not append.
- `Run N` matches the `run_number` in the latest `execution_history` entry in `plan.yaml`.
- Do not include task titles or descriptions here. This section is a one-glance indicator only.
- **Pre-execution initial state** (after scope selection but before the first execution pass): write `**As of:** <YYYY-MM-DD> (Scoped - pending execution)` and `**Status:** active`. Update to `(Run N)` only after the first `execution_history` entry is appended to `plan.yaml`.

---

## Section 2 - Executive Summary

**Purpose**: Narrative description of the plan's goal and current state. Written once on creation and updated only if the substance changes (e.g., scope is materially expanded or a critical discovery changes the goal).

**Mandatory content**:
1. One paragraph: what the plan is for and what gap analysis triggered it.
2. One bullet: the critical path of tasks in execution order (e.g., `T1 -> T2 -> T4`).
3. One bullet: total effort estimate.

**Rules**:
- This section describes the **persistent goal** of the plan - it is not a status update.
- Maximum 250 words.
- Do not update this section on routine continuation passes. Update only when the plan's objective materially changes.

---

## Section 3 - Findings Overview

**Purpose**: Condensed summary of the gap analysis Master Verification Table. Set once on creation from `gap-analysis.md` Section 4.

**Mandatory format**:

```markdown
| Status | Count | Summary |
|--------|-------|---------|
| ✅ Done | N | <One-phrase summary of Done items, or "None"> |
| ⚠️ Partial | N | <One-phrase summary, or "None"> |
| ❌ Missing | N | <One-phrase summary, or "None"> |
| 🔁 Stale | N | <One-phrase summary, or "None"> |
```

**Rules**:
- Counts must match the gap-analysis.md Master Verification Table.
- Do not repeat individual MVT rows - this is a count + headline summary only.
- Do not update on continuation passes unless a material re-research changes the baseline counts.

---

## Section 4 - Task Breakdown

**Purpose**: Human-readable prose description of each task in execution order.

**Mandatory format**: One H3 sub-section per task:

```markdown
### T1 - Task Title

**Priority:** P1 - Critical path
**Effort:** 1-2 days
**Depends on:** Nothing

<One paragraph describing what needs to be done and where. Reference specific file paths and methods.>
```

**Rules**:
- Task IDs and titles must match `gap-analysis.md` Section 6 **and** `plan.yaml` `tasks[].id` / `tasks[].title`. Any divergence is a bug.
- Tasks appear in recommended execution order (T1 first).
- Do not repeat full acceptance criteria here - reference the task ID and point readers to `plan.yaml` or `gap-analysis.md` Section 9.
- Update this section only when replanning adds, removes, or structurally changes tasks.

---

## Section 5 - Dependency Graph

**Purpose**: Visual representation of the full backlog dependency structure for `gem-orchestrator` dispatch planning.

**Mandatory format**: Mermaid `graph TD` diagram.

**Rules**:
- Every task from Section 4 must appear as a node.
- **The graph is never narrowed per continuation pass.** It always represents the full backlog for the dated plan. Scope-specific state is communicated in Section 8 (Scope Decision) and Section 1 (Status Summary).
- P4 (`deferred` priority) tasks appear labeled `[TN - DEFERRED]`.
- If all tasks are independent, write a flat node list with the note "All tasks independent."

---

## Section 6 - Implementation Notes

**Purpose**: Post-execution notes about what was done, how, and any deviations from the plan. Append-only - one sub-section added per execution pass.

**Mandatory format**: One H3 sub-section per execution pass:

```markdown
### Run N - YYYY-MM-DD

#### T1 - Task Title ✅

**Files modified:** `path/to/file1.cs`, `path/to/file2.cshtml`

<Prose describing non-obvious implementation decisions, amendments, or deviations.
Reference specific file paths and method names.>

#### T2 - Task Title 🚫 (Blocked)

<Describe what blocked the task and why.>
```

**Rules**:
- Add one top-level sub-section per executed pass. Do not modify earlier sub-sections.
- Use `✅` for completed tasks and `🚫 (Blocked)` for blocked tasks.
- If a task was implemented exactly as planned with no deviations, write: "Implemented as planned. No deviations."
- If a `failure_modes[].outcome` was recorded in `plan.yaml` for a task, summarize it here.
- `Run N` matches `run_number` in the corresponding `execution_history` entry in `plan.yaml`.

---

## Section 7 - Verification Checklist

**Purpose**: Persistent checklist of all acceptance criteria across all tasks. Set on creation from `gap-analysis.md` Section 9 / `plan.yaml` `tasks[].acceptance_criteria`; check-off state updated on each execution pass.

**Mandatory format**: GitHub-style task list grouped by task:

```markdown
### T1 - Task Title

- [ ] <Criterion 1>
- [ ] <Criterion 2>
- [ ] VS Code task `msbuild:build` succeeds
```

**Rules**:
- Criteria must match `plan.yaml` `tasks[].acceptance_criteria` exactly.
- Mark criteria as `[x]` when verified. Never remove criteria from the list.
- At least one criterion per task must be build-level or runtime-level.
- Do not add criteria on continuation passes unless a replanning pass formally adds them to `plan.yaml` first.

---

## Section 8 - Scope Decision

**Purpose**: Record the scope selection made at the start of each execution pass. Append-only - one run-qualified entry per pass.

**Mandatory format**: One H3 sub-section per pass:

```markdown
### Scope Decision - YYYYMMDD Run N

**In scope:** T1, T2, T3
**Deferred:** T4, T5

**Rationale:** <One paragraph explaining inclusion/exclusion choices. Include the dependency chain validation result.>
```

**Rules**:
- Use `YYYYMMDD Run N` in the heading, matching `run_number` in `plan.yaml` `execution_history`.
- Every task must appear in exactly one of: In scope or Deferred. No task may be unaccounted for.
- The dependency chain validation result must be stated: whether every in-scope task's prerequisites are also in scope (or already completed).

---

## Section 9 - Run History

**Purpose**: Chronological log of all execution passes for traceability. Append-only.

**Mandatory format**:

```markdown
| Run | Date | Selected | Completed | Blocked | Commit |
|-----|------|----------|-----------|---------|--------|
| 1 | YYYY-MM-DD | T1, T2, T3 | T1, T3 | T2 | `abc1234` |
| 2 | YYYY-MM-DD | T4, T5 | T4 | T5 | `def5678` |
```

**Rules**:
- One row per execution pass. Values must agree with `plan.yaml` `execution_history` entries for the corresponding `run_number`.
- `Commit` is the Checkpoint 2 commit hash for that pass. Apply the placeholder rules from [`plan-state-contract.md`](plan-state-contract.md) §Commit Placeholder Contract. This column is non-normative.
- Never modify an existing row.

---

## Global Rules

1. **Alignment with plan.yaml**: `plan.md` and `plan.yaml` must describe the same tasks, ordering, scope, and dependencies. If either is updated, the other must be updated in the same pass. Any divergence is a bug.
2. **Mutability contract**: Follow the per-section mutability rules defined above. Sections marked "Set on creation" must not change unless explicitly justified by a replanning pass. Sections marked "Append-only" accumulate entries over time - older entries are never modified or removed.
3. **Commit bundling**: `plan.md` updates must be included in the same Checkpoint 2 commit as the corresponding `plan.yaml` updates, implementation changes, and walkthrough file. See [`plan-state-contract.md`](plan-state-contract.md) for the normative commit protocol.
4. **No scope narrowing of the Dependency Graph**: Section 5 always shows the full backlog for the dated plan - do not redraw it to reflect only the current pass's scope.
5. **`plan.md` is an authoring artifact, not an external status dashboard**: Summary information for human readers lives in the walkthrough files. `plan.md` captures the plan's evolution for `gem-planner` and `gem-orchestrator`.
