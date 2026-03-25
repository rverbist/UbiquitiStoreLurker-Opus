---
version: '1.0.0'
---

# Workflow Details

This document contains operational protocols, edge-case procedures, and error templates that support the standard workflow defined in SKILL.md §Standard Workflow.

## Phase Reference Map

The following table is the **canonical mapping** between phase naming schemes used across
c2l-gap-analysis reference files. When a file uses numbered or letter phases, refer here
to identify the corresponding descriptive phase name.

| Descriptive Name (canonical) | Numbered Phase | Letter Phase | Description |
|---|---|---|---|
| Research Phase | Phase 1 | - | gem-researcher agents gather codebase evidence |
| Planning Phase | Phase 2 | - | gem-planner produces gap-analysis.md, plan.yaml, plan.md |
| Scope Selection | Phase 3 | Phase A | User reviews deferred tasks; selects in-scope set for a run |
| Execution Phase | Phase 4 | Phase B (post-execution update) | gem-implementer executes in-scope tasks; plan.md updated after |

> **Deprecation notice:** The numbered (Phase 1-4) and letter (Phase A/B) forms are legacy
> naming from early versions of these reference files. New text should always use the
> Descriptive Name column. Existing uses are preserved for backward compatibility but should
> be updated opportunistically.

## Source Control Checkpoints

Detailed protocol for the three mandatory git checkpoints that ensure clean commit boundaries.

> Referenced by SKILL.md §Standard Workflow (Steps 0-4).

Three mandatory git checkpoints ensure clean commit boundaries:

| Checkpoint | When | Commit message pattern | Notes |
| ---------- | ---- | ---------------------- | ----- |
| 0 | Before research | User choice: commit / stash / ignore | Plugin check -> branch check -> git-status prompt; stash ref tracked for Checkpoint 2 |
| 1 | After scope selection | `docs(${SCOPE}): gap analysis and scoped plan YYYYMMDD` | Only planning artifacts; skip if nothing to commit |
| 2 | After execution complete | `feat(${SCOPE}): implement gap analysis plan YYYYMMDD` | Restore stash if applicable; skip if all tasks deferred |

**Edge cases:**

- **Nothing to commit at Checkpoint 1:** All files were already committed (e.g., interrupted re-run or same-day continuation with no new planning artifacts). Skip the commit.
- **Nothing implemented at Checkpoint 2:** All tasks were deferred or no tasks existed. Skip the commit. Still restore stash if one exists.
- **Stash pop conflicts:** Alert the user with the conflict details. Do not auto-resolve. The user can `git stash show -p` to review and merge manually.
- **Checkpoint 0 on clean tree:** No prompt needed - proceed directly.
- **Same-day continuation:** Checkpoint 1 is always skipped (planning artifacts already committed). Checkpoint 2 runs normally after each implementation pass. Multiple Checkpoint 2 commits on the same dated directory are expected.
- **Walkthrough not yet generated at Checkpoint 2:** Generate it first using the already-frozen `RUN_ID`. Never omit the walkthrough from a Checkpoint 2 commit when an execution pass occurred.
- **`git stash push` fails at Checkpoint 0:** **Hard stop.** Report the git error verbatim. Do not proceed under any option - including Ignore. Ask the user to resolve the failure (check for locked index, insufficient disk space, or a rejected hook) and re-invoke from Checkpoint 0. Do not proceed with a dirty working tree that has no stash ref.

## Same-Day Continuation

Detailed protocol for resuming an active plan within the same dated directory on the same calendar day.

> Referenced by SKILL.md §Standard Workflow. State model is defined in `plan-state-contract.md` (loaded at skill activation).

The normative same-day continuation behavior is defined in `plan-state-contract.md` in these sections:

- `Same-Day States` - detection rules for `new-run`, `active-plan`, `exhausted-plan`, `closed-no-gap`, `invalid`
- `Scope Selection Rules` - which tasks are selectable and which are not
- `Reconsidering Blocked Tasks` - special-case path requiring targeted re-verification
- `Targeted Re-Verification Triggers` - when re-verification is mandatory before scope entry
- `Same-Day Continuation Contract` - the six-step resumption checklist
- `Two-Phase Update Protocol` - how plan.yaml and plan.md are updated in the same pass

Use this file for operator reminders and edge cases only.

**Operator reminders:**

1. **Checkpoint 1 is skipped** on continuation because the planning artifacts are already committed.
2. **Checkpoint 2 still runs** after each implementation pass. Use targeted staging - never `git add -A` (see SKILL.md §Checkpoint 2 targeted-staging rule):

   ```bash
   git add ${PLAN_ROOT}/${SCOPE}-gap-analysis-YYYYMMDD/
   git add <implementation-files...>
   git commit -m "feat(${SCOPE}): implement gap analysis plan YYYYMMDD"
   ```

   Review `git status` after staging to confirm no unrelated files are included before committing.

3. `gem-orchestrator` generates one new walkthrough file for the pass using the frozen `RUN_ID`.
4. Targeted re-verification reads the corresponding `research_findings_<focus-area>.yaml` first; that artifact must follow [research-findings-specification.md](research-findings-specification.md).

**What does NOT happen on continuation:**

- `gem-researcher` does not perform a full research re-run.
- `gem-planner` does not re-author `gap-analysis.md`.
- The Dependency Graph is not narrowed to the current scope.
- A second fresh same-day analysis is not created.

**Targeted re-verification is required** before a task enters scope when the plan-state contract says its prerequisites, history, or branch movement make the earlier verification stale. When performing targeted re-verification, `gem-researcher` reads the corresponding `research_findings_<focus-area>.yaml` file first to understand what was already verified, then checks only the delta against the live codebase - a full re-research is not needed.

## New Analysis After Prior Work

Protocol for starting a fresh analysis on a new calendar date when a completed or exhausted prior analysis exists.

> Referenced by the Research Phase when prior dated directories exist. Distinct from [Same-Day Continuation](#same-day-continuation).
>
> **Note**: This section describes starting a fresh analysis for a new calendar date while a completed or exhausted prior analysis exists in an earlier dated directory. This is distinct from [Same-Day Continuation](#same-day-continuation), which resumes an active plan within the same dated directory on the same calendar day.

When a prior gap analysis exists (e.g., `${SCOPE}-gap-analysis-20260318`) and you need to run a new one:

1. **New directory:** create `${SCOPE}-gap-analysis-${YYYYMMDD}` with today's date. Do not overwrite the prior run.
2. **Lineage:** in the new `gap-analysis.md`, reference the prior analysis in the Document Lineage section: which tasks were completed in the prior run, which were deferred, and what (if anything) has changed since.
3. **Prior deferred tasks:** re-verify them - they may have been implemented outside the plan, or requirements may have changed.
4. **New gaps:** the fresh verification may surface items that weren't in the prior analysis.
5. **plan.yaml:** new `plan_id` (today's date). Prior completed tasks are not repeated - only the delta.

## Execution Failure Recovery

Protocol for handling task failures reported by `gem-implementer` during Phase 3.

> Referenced by SKILL.md §Standard Workflow (Phase 3).

When `gem-implementer` reports a task failure during Phase 3:

1. `gem-orchestrator` follows its standard retry/replan logic (retry once, then redelegate to `gem-planner` if structural).
2. **Before retry:** re-verify the task's preconditions against the live codebase. A dependency may have been implemented differently than expected.
3. **If replanning is needed:** `gem-planner` updates both `plan.yaml` and `plan.md` in the same pass (alignment rule applies).
4. **If a task is abandoned:** set its status to `blocked` with a reason. Do not set it to `deferred` - that status is reserved for scope decisions. The walkthrough should document what was blocked and why.

## Error Message Templates

Canonical error messages for HARD STOP and escalation conditions used throughout the skill workflow.

> Referenced by SKILL.md §Standard Workflow.

Use these exact messages for the HARD STOP and escalation conditions in this skill:

| Condition | Message template |
| --------- | ---------------- |
| Plugin not found (Step 0a) | `HARD STOP - gem-team agent files not detected under [resolved agentPlugins root]. Required files: gem-orchestrator.md, gem-researcher.md, gem-planner.md, gem-implementer.md. Checked recursively, including packaged plugin layouts such as github.com/github/awesome-copilot/plugins/gem-team/agents/. Install or repair the gem-team extension for this VS Code channel and re-invoke from Step 0a.` |
| Dirty working tree detected (Step 0c) | `DIRTY WORKTREE - Uncommitted changes detected. Choose one: (1) Commit them, (2) Stash them as pre-gap-analysis-YYYYMMDD, or (3) Ignore and continue on the dirty tree. Record the choice before proceeding.` |
| Branch is not `main` (Step 0b) | `BRANCH CONFIRMATION REQUIRED - Current branch is [branch], not main. Confirm whether to proceed on this branch before continuing the gap-analysis workflow.` |
| `git stash push` fails (Checkpoint 0) | `HARD STOP - git stash push failed: [verbatim error]. Do not proceed with a dirty working tree. Resolve the failure (check for locked index, insufficient disk space, or a rejected hook) and re-invoke from Checkpoint 0.` |
| Invalid same-day artifact set | `HARD STOP - Invalid artifact set in [directory]: [describe specific anomaly]. Repair the plan state manually before re-invoking.` |
| Item cannot be verified against codebase | `HARD STOP - [item] could not be verified against the live codebase. Minimum valid verification basis is File inspection. Narrow the scope or restore access to the relevant files before completing the analysis.` |
| Ambiguous classification (Step 3) | `AMBIGUOUS CLASSIFICATION - [item] matches criteria for both [status A] and [status B]. Defaulting to lower-confidence status [status A]. See Verification Notes for conditions that would confirm [status B].` |

## gem-planner Length-Limit Recovery

Protocol for handling response-length-limit failures from `gem-planner` during Planning Phase.

> Referenced by SKILL.md §Planning Phase (Step 5 - Alignment check).

A length-limit failure occurs when a gem-planner subagent returns a truncated or partial output, or reports that the response limit was exceeded.

**Step 1 - Check the budget rule first.**  
Verify the orchestrator's prompt complied with the 150-line inline-content limit defined in SKILL.md §Planning Phase. If inline research content exceeded 150 lines: write that content to a disk file, update the prompt to pass only the file path, and retry the same step.

**Step 2 - If the prompt was within budget, split the output.**  
If plan.yaml was the file being produced and the task count is large (>12 tasks), split into two gem-planner calls:
- Call A: produce tasks T1-T(N/2), writing a partial `plan-part-a.yaml`
- Call B: produce tasks T(N/2+1)-TN, writing a partial `plan-part-b.yaml`
- Orchestrator merges the two outputs into `plan.yaml`, preserving all top-level metadata fields from Call A

**Step 3 - Retry once after applying the fix.**  
Each recovery attempt (budget-rule fix or output-split) counts as one attempt. After two failed attempts on the same step, stop and report to the user - do not attempt a third retry unilaterally.

**Hard rule - orchestrator authorship is forbidden.**  
The orchestrator (`gem-orchestrator` or the main chat agent) **MUST NOT** author `plan.yaml` or `plan.md` directly as a fallback, regardless of how many gem-planner attempts have failed. These files require gem-planner's sole authorship to ensure schema compliance, pre-mortem content, and task-field completeness. If two budget-rule-compliant gem-planner attempts both fail, stop and report:

> `HARD STOP - gem-planner failed to produce [file] after two attempts with compliant prompts. Prompt size: [N] lines inline. Output size estimate: [M] tasks. Diagnose whether output-splitting is needed and retry, or ask the user to reduce the task count before planning.`

## gem-researcher Invocation - Schema Override

Protocol documenting the schema conflict between the gem-researcher agent mode's built-in output schema and the skill-specific `research-findings-specification.md`, and the selected remediation.

> Referenced by SKILL.md §Research Phase §Schema override for gem-researcher output.

**Root cause documented:** The gem-researcher agent mode defines a built-in `research_format_guide`
that structurally conflicts with `research-findings-specification.md`. Specifically:
- The mode schema does not define the mandatory `items` root key
- The mode schema makes `patterns_found` required; the spec makes it optional
- The mode schema adds `related_architecture` and `domain_security_considerations` sections not in the spec

**Remediation - Option A (selected):** The orchestrating agent must pass the mandatory schema
inline in every gem-researcher prompt. See SKILL.md §Research Phase §Schema override for
gem-researcher output for the exact block to include.

**Why not Option B:** Modifying `research-findings-specification.md` to match the mode schema
would change the `items` mandatory key, which is the canonical handoff contract for gem-planner.
Preserving the skill-specific schema is safer than reconciling to the mode schema.

**Known limitation:** If the gem-researcher mode schema is enforced at the system-prompt level
and overrides all user instructions, gem-researcher outputs may still deviate. In that case,
the orchestrating agent must validate the schema of each research findings file before proceeding
to the Planning Phase and correct any schema violations (root key `gap_table` -> `items`).

**open_questions field:** The gem-researcher agent mode marks `open_questions` as REQUIRED.
The c2l-gap-analysis research-findings spec marks it as **Optional**. Resolution: include
`open_questions` when research uncertainty materially affects planning; omit it when all
findings are conclusive. Omitting it does not violate the skill spec even if the mode
marks it required.
