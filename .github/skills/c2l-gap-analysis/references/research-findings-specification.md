---
version: '1.0.0'
---

# research_findings_<focus-area>.yaml - Authoring Specification

This document defines the mandatory schema and usage rules for each `research_findings_<focus-area>.yaml` artifact produced by `gem-researcher` during the `c2l-gap-analysis` workflow. These files are the primary structured input to `gem-planner` and the continuity record used during targeted re-verification on same-day continuation.

**Author**: `gem-researcher` (sole file author for `research_findings_<focus-area>.yaml`).

**Template**: See [`research-findings-skeleton.yaml`](research-findings-skeleton.yaml) for a fill-in-the-blanks starting point.

**Primary consumers**:
- `gem-planner` - reads these files as the authoritative structured findings input when authoring `gap-analysis.md`, `plan.yaml`, and `plan.md`.
- `gem-researcher` - re-reads the relevant file during targeted re-verification to understand what was already checked and what delta must be re-verified.

---

## Filename Convention

The canonical pattern is:

```text
research_findings_<focus-area>.yaml
```

Rules:
- `<focus-area>` is a filesystem-safe identifier unique within the dated plan directory.
- Prefer lowercase kebab-case for new files, for example `research_findings_backend-api-lifecycle.yaml`.
- Historical files that use underscores remain valid read-only artifacts.
- The filename token does not need to equal the human-readable top-level `focus_area` value, but it must describe the same scope slice.

---

## Top-Level Fields

Every `research_findings_<focus-area>.yaml` MUST contain these top-level fields, in this order:

| Field | Type | Required | Rules |
|-------|------|----------|-------|
| `plan_id` | string | Mandatory | Must match the dated plan directory name, for example `${SCOPE}-gap-analysis-20260322`. |
| `objective` | string | Mandatory | One sentence describing what this research slice verifies. |
| `focus_area` | string | Mandatory | Human-readable label for the functional slice covered by this artifact. Reuse this exact value when populating `plan.yaml` task `focus_area` fields derived from this artifact. |
| `created_at` | string | Mandatory | ISO date `YYYY-MM-DD` in quotes. |
| `created_by` | string | Mandatory | Always `gem-researcher`. |
| `status` | string | Mandatory | Always `completed` when the artifact is handed to `gem-planner`. Draft or partial states are not valid hand-off artifacts. |
| `tldr` | string (multiline) | Mandatory | Concise bullet summary of the main findings. |
| `research_metadata` | object | Mandatory | Methodology, tools, scope, and confidence metadata. |
| `items` | list of objects | Mandatory | One entry per task, planning item, or grouped verification unit in this focus area. At least one entry. |
| `files_analyzed` | list of objects | Mandatory | Concrete files inspected while producing these findings. At least one entry. |
| `additional_findings` | list of objects | Optional | Extra observations that do not map cleanly to a single primary item. |
| `patterns_found` | list of objects | Optional | Cross-cutting patterns observed during research. |
| `open_questions` | Optional | list of strings | Unresolved questions that emerged during research. Include when uncertainty affects task planning. The gem-researcher agent mode marks this as required - include it when relevant research uncertainty exists; omit it only when no material open questions remain. |

---

## research_metadata Fields

| Field | Type | Required | Rules |
|-------|------|----------|-------|
| `methodology` | string | Mandatory | Brief description of how the research was performed. |
| `tools_used` | list of strings | Mandatory | Tool names actually used, for example `read_file`, `grep_search`, `list_dir`. |
| `scope` | string | Mandatory | Directories, modules, or planning documents inspected. |
| `confidence` | string | Mandatory | One of: `high`, `medium`, `low`. |
| `coverage` | integer | Mandatory | Approximate percentage coverage of the intended focus area, from 0 to 100. |

---

## items Object Fields

Each entry in `items` MUST contain:

| Field | Type | Required | Rules |
|-------|------|----------|-------|
| `id` | string | Mandatory | Stable identifier for the item being assessed, for example `T4`, `GAP-003`, or another scope-local identifier. |
| `title` | string | Mandatory | Short human-readable title. |
| `status` | string | Mandatory | One of: `Done`, `Partial`, `Missing`, `Stale`. |
| `evidence` | list or map | Mandatory | Concrete evidence collected from the codebase. Use a list for flat evidence and a map of lists when evidence is naturally grouped by concern. |
| `detail` | string (multiline) | Mandatory | Narrative explanation of what was found, what is missing, and why the status is justified. |

Rules:
- `status` values are prose findings states used by `gem-researcher`; `gem-planner` maps them into the formal `gap-analysis.md` status presentation.
- Evidence must cite concrete files, methods, properties, routes, migrations, tests, or search locations.
- Do not record document-only conclusions. If an item could not be verified against the live codebase, stop and resolve that blocker before finalizing the artifact.

---

## files_analyzed Object Fields

Each entry in `files_analyzed` MUST contain:

| Field | Type | Required | Rules |
|-------|------|----------|-------|
| `file` | string | Mandatory | Basename or display name of the file. |
| `path` | string | Mandatory | Repository-relative path to the file inspected. |
| `purpose` | string | Mandatory | Why this file was inspected. |
| `key_elements` | list of objects | Mandatory | Important functions, patterns, routes, or sections examined. At least one entry. |
| `language` | string | Mandatory | Primary language or file type, for example `csharp`, `JavaScript`, `Markdown`. |
| `lines` | string or integer | Mandatory | Approximate line count or line range context. |

Each `key_elements` entry MUST contain `element`, `type`, `location`, and `description`.

---

## additional_findings Object Fields

Each entry in `additional_findings`, when present, MUST contain:

| Field | Type | Required | Rules |
|-------|------|----------|-------|
| `id` | string | Mandatory | Stable identifier such as `AF-1`. |
| `title` | string | Mandatory | Short title. |
| `detail` | string (multiline) | Mandatory | Description of the extra finding and why it matters. |

---

## patterns_found Object Fields

Each entry in `patterns_found`, when present, MUST contain:

| Field | Type | Required | Rules |
|-------|------|----------|-------|
| `category` | string | Mandatory | Broad grouping such as `structure`, `architecture`, `security`. |
| `pattern` | string | Mandatory | Short label for the pattern. |
| `description` | string | Mandatory | What the pattern is and why it matters. |
| `examples` | list of objects | Mandatory | Concrete examples with file/location/snippet. |
| `prevalence` | string | Mandatory | One of: `rare`, `common`, `widespread`. |

Each example object MUST contain `file`, `location`, and `snippet`.

---

## Global Rules

1. These files are evidence artifacts, not final deliverables. They may be detailed and verbose if that improves planner accuracy.
2. Every claim must be grounded in live-code verification. The minimum valid basis is file inspection.
3. Use repository-relative paths throughout so downstream artifacts can copy evidence consistently.
4. Keep status wording stable: `Done`, `Partial`, `Missing`, `Stale`.
5. Historical researcher artifacts remain valid read-only inputs even if newer files adopt stricter filename formatting.