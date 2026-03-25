---
version: '1.0.0'
---

# plan.yaml - Authoring Specification

This document defines the **mandatory schema, field contracts, and authoring constraints** for every `plan.yaml` produced by the `c2l-gap-analysis` skill. It is normative: deviations are bugs, not style choices.

**Author**: `gem-planner` (sole file author for `plan.yaml`). Input comes from `gem-researcher` verified findings and the task register in `gap-analysis.md`.

**Template**: See [`plan-yaml-skeleton.yaml`](plan-yaml-skeleton.yaml) for a fill-in-the-blanks starting point.

**Related specifications**:

- [`research-findings-specification.md`](research-findings-specification.md) - normative schema for the `gem-researcher` artifacts that feed this plan.
- [`gap-analysis-specification.md`](gap-analysis-specification.md) - the gap analysis that produces the task register feeding this plan.
- [`plan-state-contract.md`](plan-state-contract.md) - normative same-day state machine, task status values, `scope_history` / `execution_history` schemas, and `plan.md` update contract. This specification extends those schemas with the full file-level structure.
- [`plan-md-specification.md`](plan-md-specification.md) - mandatory section structure and per-section authoring rules for `plan.md`.

---

## Top-Level Metadata Fields

Every `plan.yaml` MUST contain these top-level fields, in this order:

| Field | Type | Required | Rules |
| ----- | ---- | -------- | ----- |
| `plan_id` | string | Mandatory | Must match the directory name: `${SCOPE}-gap-analysis-${YYYYMMDD}`. |
| `plan_path` | string | Mandatory | Relative path from repo root: `${PLAN_ROOT}/${SCOPE}-gap-analysis-${YYYYMMDD}/plan.yaml`. |
| `source_document` | string | Mandatory | Relative path to the gap analysis: `${PLAN_ROOT}/${SCOPE}-gap-analysis-${YYYYMMDD}/gap-analysis.md`. |
| `objective` | string (multiline) | Mandatory | One paragraph describing the overall goal of the plan. Use YAML `\|` block scalar. |
| `created_at` | string | Mandatory | ISO date `"YYYY-MM-DD"` (quoted). The date the plan was first created. |
| `created_by` | string | Mandatory | Always `gem-planner`. |
| `status` | string | Mandatory | One of: `active`, `completed`. Set to `active` on creation; set to `completed` when all tasks reach `completed` or `blocked`. |
| `research_confidence` | string | Mandatory | One of: `high`, `medium`, `low`. Reflects the confidence level of the `gem-researcher` findings that fed this plan. |

---

## Summary Fields

| Field            | Type               | Required  | Rules |
| ---------------- | ------------------ | --------- | ----- |
| `tldr`           | string (multiline) | Mandatory | 3-8 line executive summary. Use YAML `\|` block scalar. Must state: task count, critical path, total effort estimate, execution phases, and scope gate status. |
| `open_questions` | list of strings    | Mandatory | Unresolved questions that may affect task execution. Empty list `[]` if none. |

---

## Scope Fields

### Current format: `scope_history` (array)

New plans MUST use the `scope_history` array format. One entry is appended at scope-selection time for each execution pass.

```yaml
scope_history:
  - timestamp: "YYYY-MM-DDTHH:mm:ss"
    in_scope: [T1, T2]
    out_of_scope: [T3, T4]
```

See [`plan-state-contract.md`](plan-state-contract.md) for the normative schema and rules.

### Legacy format: `scope_decision` (single object) - READ ONLY

Older plans (e.g., `aicms-gap-analysis-20260320`) use a single-object `scope_decision` field:

```yaml
scope_decision:
  date: "YYYY-MM-DD"
  in_scope: [task-001, task-002, task-005]
  out_of_scope: [task-003, task-004, task-006]
  dependency_waivers:
    - task: task-005
      waived_dependency: task-004
      reason: "..."
  execution_order:
    wave_1: [task-001, task-002]
    wave_2: [task-005]
```

**This format is legacy.** It is retained for reading historical plans but MUST NOT be generated for new runs. New plans use `scope_history` arrays instead. Back-filling existing plans to the array format is not required - they are historical artifacts.

The `dependency_waivers` and `execution_order` sub-fields were specific to the single-object format and do not carry over to `scope_history`. Dependency waivers, when needed in the current format, are recorded in task-level `description` fields and in `plan.md` Scope Decision sections.

---

## Execution History

```yaml
execution_history:
  - run_number: 1
    run_id: "YYYYMMDD-HHmmss"
    started_at: "YYYY-MM-DDTHH:mm:ss"
    completed_at: "YYYY-MM-DDTHH:mm:ss"
    selected_tasks: [T1, T2]
    completed_tasks: [T1]
    blocked_tasks: [T2]
    commit: "<sha>"
    walkthrough: "${PLAN_ROOT}/${SCOPE}-gap-analysis-YYYYMMDD/walkthrough-completion-YYYYMMDD-HHmmss.md"
```

See [`plan-state-contract.md`](plan-state-contract.md) for the normative schema, `run_number` rules, `RUN_ID` freezing semantics, and commit placeholder contract.

---

## Pre-Mortem

| Field                               | Type            | Required  | Rules |
| ----------------------------------- | --------------- | --------- | ----- |
| `pre_mortem`                        | object          | Mandatory | Contains `overall_risk_level`, `critical_failure_modes`, and `assumptions`. |
| `pre_mortem.overall_risk_level`     | string          | Mandatory | One of: `low`, `medium`, `high`, `critical`. |
| `pre_mortem.critical_failure_modes` | list of objects | Mandatory | At least one entry. Each entry has the fields below. |
| `pre_mortem.assumptions`            | list of strings | Mandatory | Explicit assumptions underlying the plan. At least one entry. |

### Failure mode object fields

| Field        | Type   | Required  | Rules |
| ------------ | ------ | --------- | ----- |
| `scenario`   | string | Mandatory | Concrete description of what could go wrong. Reference specific tasks, files, or patterns. |
| `likelihood` | string | Mandatory | One of: `low`, `medium`, `high`. |
| `impact`     | string | Mandatory | One of: `low`, `medium`, `high`. |
| `mitigation` | string | Mandatory | Concrete action to reduce risk. Must be actionable, not vague. |
| `outcome`    | string | Optional  | Filled post-execution if the failure mode was triggered. Describes what happened and how it was resolved. |

---

## Implementation Specification

| Field                                            | Type               | Required  | Rules |
| ------------------------------------------------ | ------------------ | --------- | ----- |
| `implementation_specification`                   | object             | Mandatory | Contains `code_structure`, `affected_areas`, `component_details`, `dependencies`, and `integration_points`. |
| `implementation_specification.code_structure`    | string (multiline) | Mandatory | High-level file/directory map of where changes land. Group by backend/frontend. Use YAML `\|` block scalar. |
| `implementation_specification.affected_areas`    | list of strings    | Mandatory | One entry per directory or area affected, with a brief note on the nature of changes. |
| `implementation_specification.component_details` | list of objects    | Mandatory | One entry per significant component. |
| `implementation_specification.dependencies`      | list of objects    | Mandatory | Cross-task dependency declarations. |
| `implementation_specification.integration_points`| list of strings    | Mandatory | Notes on how tasks interact or share state at runtime. |

### Component detail object fields

| Field            | Type            | Required  | Rules |
| ---------------- | --------------- | --------- | ----- |
| `component`      | string          | Mandatory | Component name with task reference, e.g., `"Classification wizard (T6)"`. |
| `responsibility` | string          | Mandatory | One sentence describing this component's purpose. |
| `interfaces`     | list of strings | Mandatory | Concrete API endpoints, Angular services, or UI touchpoints this component exposes or consumes. |

### Dependency object fields

| Field          | Type   | Required  | Rules |
| -------------- | ------ | --------- | ----- |
| `component`    | string | Mandatory | Task or component being described. |
| `relationship` | string | Mandatory | Free text describing the dependency: what depends on what and why. |

---

## Tasks Array

The `tasks` field is a YAML list of task objects. Each task represents one unit of implementation work from the gap analysis task register.

### Task field contract

Every task object MUST contain these fields:

| Field                 | Type                 | Required  | Rules |
| --------------------- | -------------------- | --------- | ----- |
| `id`                  | string               | Mandatory | Task identifier matching `gap-analysis.md` Section 6, e.g., `T1`, `T3a`. Must be unique within the plan. |
| `title`               | string               | Mandatory | Short task title (max 80 chars). Must match the corresponding task card title in `gap-analysis.md`. |
| `description`         | string (multiline)   | Mandatory | Detailed description of the work. Must cite specific files/methods. Use YAML `\|` block scalar. |
| `agent`               | string               | Mandatory | The agent responsible for execution. For this skill, task execution values are fixed to `gem-implementer`. Any other value is invalid unless the skill contract is revised. |
| `priority`            | string               | Mandatory | One of: `critical`, `high`, `medium`, `low`, `deferred`. Apply the exact P0-P4 mapping from [`plan-state-contract.md`](plan-state-contract.md) §Priority Mapping. |
| `status`              | string               | Mandatory | One of: `deferred`, `pending`, `completed`, `blocked`. See [Task Status Contract](#task-status-contract). |
| `in_scope`            | boolean              | Mandatory | `true` if selected for the current execution pass; `false` otherwise. Initially `false` for all tasks (scope gate). |
| `dependencies`        | list of strings      | Mandatory | Task IDs this task depends on. Empty list `[]` if independent. |
| `context_files`       | map (string -> string)| Mandatory | Keys are file paths (relative to repo root); values describe why/how the file is relevant to this task. |
| `estimated_effort`    | string               | Mandatory | One of: `small` (< 1 day), `medium` (1-3 days), `large` (3+ days). |
| `estimated_files`     | integer              | Mandatory | Approximate number of files to create or modify. |
| `estimated_lines`     | integer              | Mandatory | Approximate number of lines to add or change. |
| `focus_area`          | string               | Mandatory | Short label for the functional area. It must match the human-readable `focus_area` value from exactly one source `research_findings_<focus-area>.yaml` file and be reused consistently across every task derived from that research artifact. |
| `verification`        | list of strings      | Mandatory | Steps to verify the task was implemented correctly. At least one entry. |
| `acceptance_criteria` | list of strings      | Mandatory | Falsifiable criteria for completion. At least one must be build-level or runtime-level. Must align with `gap-analysis.md` Section 9 criteria for this task. |
| `failure_modes`       | list of objects      | Mandatory | Predicted failure scenarios. At least one entry. Uses the same object format as `pre_mortem.critical_failure_modes`. |
| `tech_stack`          | list of strings      | Mandatory | Technologies used by this task (e.g., `"AngularJS 1.x"`, `"Razor views (cshtml)"`). |
| `test_coverage`       | string or null       | Mandatory | Description of test coverage, or `null` if no tests are added. |
| `requires_review`     | boolean              | Mandatory | Whether the task needs post-implementation review. |
| `review_depth`        | string               | Mandatory | One of: `lightweight`, `standard`, `deep`. Determines review thoroughness. |
| `security_sensitive`  | boolean              | Mandatory | Whether the task touches authentication, authorization, or data access patterns. |

---

## Task Status Contract

Task statuses are defined normatively in [`plan-state-contract.md`](plan-state-contract.md).
Valid values: `deferred`, `pending`, `completed`, `blocked`.
Unknown or missing task statuses make the plan invalid.

---

## Global Rules

1. **Alignment with plan.md**: `plan.yaml` and `plan.md` must describe the same tasks, ordering, scope, and dependencies. If either is updated, the other must be updated in the same pass. Any divergence is a bug.
2. **Scope gate**: All tasks are created with `status: deferred` and `in_scope: false`. This prevents `gem-orchestrator` from entering Phase 3 automatically. Tasks are promoted to `pending` / `in_scope: true` only after user scope selection.
3. **`plan_id` = directory name**: The `plan_id` must always match the directory name (`${SCOPE}-gap-analysis-${YYYYMMDD}`).
4. **Task IDs are stable**: Once assigned, a task ID never changes within a plan. If a task is split, use suffixed IDs (e.g., `T3a`, `T3b`).
5. **No orphan tasks**: Every task must trace to at least one MVT row in `gap-analysis.md` Section 4.
6. **History is append-only**: `scope_history` and `execution_history` entries are appended, never modified or removed.
7. **Build verification**: At least one task's `acceptance_criteria` must include a solution build check: VS Code task `msbuild:build` succeeds.
8. **No non-spec fields**: Do not add fields to task objects beyond those listed in the Task field contract above (e.g., do not add `complexity`, `effort_days`, or other aliases for existing fields like `estimated_effort`). Extra fields inflate file size and create ambiguity.
