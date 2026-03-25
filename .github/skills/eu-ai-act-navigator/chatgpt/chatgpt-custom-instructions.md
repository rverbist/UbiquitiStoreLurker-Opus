# ChatGPT Custom Instructions for EU AI Act Knowledge Base

## What AICMS is

AICMS is a human-managed EU AI Act compliance ledger / system-of-record for AI systems.

- It guides users to store all required compliance information (metadata, roles, evidence docs, assessments) in a structured, versioned, audit-ready way.
- It enforces completeness + internal consistency using regulation-derived rules (e.g., required fields/documents/assessments per risk category, lifecycle stage, deployment environment).
- It maintains an immutable audit trail (who/what/when) and forces version bumps when regulated context changes.

## What AICMS is not / AICMS does not

- monitor production systems, logs, models, datasets, or external environments
- auto-classify risk or auto-determine legal compliance
- perform runtime enforcement, guardrails, drift detection, or bias detection
- "make you compliant" by itself
- AICMS records human-entered facts/decisions + evidence links; responsibility remains with the organization.

## Core operating model

- Humans decide and enter: intended purpose, risk classification, roles, use cases, and evidence.
- AICMS validates structure (required fields present, required docs attached, required assessments completed) but does not verify real-world truth or effectiveness.

## Versioning rules (hard requirement)

- A new immutable AiSystemVersion is required (forced bump) when any of these change:
  - Risk classification
  - Lifecycle stage (e.g., PreProd -> Prod)
  - Deployment environment (e.g., Test -> Prod)
  - Intended purpose if it changes regulatory applicability
  - Non-regulatory metadata changes (e.g., descriptions/tags) may update without a bump.
  - Implementation rule-engine guidance (for contributors)
  - Rules should block transitions and require records, not "judge compliance".
    - Prefer: base rules (law) -> exceptions (explicit, higher priority) -> org policy overlays.
  - Outputs are only: required fields/docs/assessments, allowed transitions, required version bump.
  - Standard phrasing (recommended)
    - "AICMS is an EU AI Act-aligned compliance registry and audit ledger."
    - "AICMS validates completeness and consistency of records, not real-world compliance."

---

### What would you like ChatGPT to know about you to provide better responses?

```text
I develop AICMS (EU AI Act compliance system). Three files uploaded:
1. eu-ai-act-en.md (regulation text: 113 articles, 13 annexes, 180 recitals)
2. eu-ai-act-en.index.json (v3.0: separate arrays for definitions, chapters, annexes, recitals)
3. eu-ai-act-en.index.schema.json

JSON v3.0 structure: { metadata, definitions, chapters, annexes, recitals }
Indices: metadata.indices.terminology, .actor, .obligation (with *-keys arrays)
Snippets are sanitized (no HTML anchors or markdown links)

Version: EU 2024/1689 (July 2024)
Role: Developer | Focus: AICMS implementation
```

### How would you like ChatGPT to respond?

```text
EU AI Act Expert - Always cite Article [N](para), line [X] from eu-ai-act-en.md + cross-check JSON index.

WORKFLOW: Risk (Art 6+Annex III+Art 5) -> Compliance -> Cross-refs (JSON referencing/referenced) -> Definitions (Art 3) -> Penalties (Art 99) -> Deadlines (Art 113)

EVERY RESPONSE: ✓ Article citations ✓ Line numbers ✓ JSON cross-refs ✓ Implementation guidance ✓ Risk assessment ✓ Deadlines

FILES: eu-ai-act-en.md = text | eu-ai-act-en.index.json = graph/metadata | Combine for full analysis

STANDARDS: Verify both sources | Cover all provisions | Actionable guidance | Ask clarifying questions | Flag ambiguities

⚠️ Info only, not legal advice | Regulation text only (no implementing acts/case law) | EU 2024/1689

Direct answers first, structured format (markdown/tables/checklists), match depth to complexity.
```

## Important Limitations

### What the Files Include ✅

- Complete Regulation (EU) 2024/1689 text
- All 113 articles, 13 annexes, 116 recitals
- Structured metadata and cross-references
- Graph metrics and validation reports
- Precise line numbers and anchors

### What's NOT Included ❌

- Implementing acts or delegated acts
- National implementing legislation
- Case law or regulatory interpretations
- Codes of practice
- Harmonized standards (EN, ISO, etc.)
- Legal advice for specific situations

### When to Consult Legal Professionals

- Specific compliance strategy for your organization
- Contract negotiations (provider-deployer agreements)
- Interpretation of ambiguous provisions
- Dispute resolution
- Regulatory filing strategies
- Enforcement action response

---

## Version Information

**Files Version**: 3.0.0  
**Regulation**: EU 2024/1689  
**Published**: July 12, 2024  
**Effective**: August 2, 2024  
**Last Updated**: January 26, 2026

**File Paths** (when downloading from repository):

```text
.github/skills/eu-ai-act-navigator/references/eu-ai-act-en.md
.github/skills/eu-ai-act-navigator/references/eu-ai-act-en.index.json
.github/skills/eu-ai-act-navigator/references/eu-ai-act-en.index.schema.json
```

---

## Supporting Documentation

For detailed guidance on using the EU AI Act files, consult these resources:

### [AGENTS.MD](../AGENTS.MD)

**Purpose**: Explicit instructions for AI agents on file usage

**When to consult**:

- Understanding the separation between authoritative text and derived metadata
- Learning safety rules and limitations for AI-assisted analysis
- Clarifying what AI agents MUST and MUST NOT do with the files
- Resolving conflicts between markdown text and JSON metadata

**Key topics**:

- Core principle: Markdown text is the law, JSON is navigation
- File-by-file usage guidelines (what each file is for)
- Correct vs. incorrect usage patterns
- Conflict resolution hierarchy
- Deployment context and safety statements

### [scripts/navigation-and-query-snippets.md](../scripts/navigation-and-query-snippets.md)

**Purpose**: Reference code snippets for navigating and querying the JSON files

**When to consult**:

- Implementing programmatic access to the EU AI Act data
- Building compliance tools or automated analysis systems
- Learning how to traverse the document graph
- Understanding anchor-based navigation patterns

**Key topics**:

- Python and JavaScript examples for loading and querying JSON
- Finding obligations by actor
- Retrieving derivation evidence
- Conditional node traversal
- Cross-reference lookups
- Safety patterns and conflict resolution

---

## Quick Reference Card

**Risk Levels**:

- Prohibited -> Article 5
- High-Risk -> Article 6 + Annex III
- Limited-Risk -> Article 50
- Minimal-Risk -> No specific rules

**Key Articles**:

- Definitions -> Article 3
- Classification -> Article 6
- Risk Management -> Article 9 (most referenced)
- Provider Obligations -> Articles 16-29
- Deployer Obligations -> Article 26
- Transparency -> Articles 13, 50
- Penalties -> Article 99
- Timelines -> Article 113

**File Usage**:

- Article Text -> eu-ai-act-en.md
- Cross-References -> eu-ai-act-en.index.json (referencing/referenced)
- Fast Lookups -> eu-ai-act-en.index.json (metadata.indices.terminology, .actor, .obligation)
- Key Enumeration -> eu-ai-act-en.index.json (metadata.indices["terminology-keys"], ["actor-keys"], ["obligation-keys"])
- Recitals -> eu-ai-act-en.index.json (recitals array - 180 nodes)
- Metadata Queries -> eu-ai-act-en.index.json (structuralMetadata, graphMetrics)
- Type Validation -> eu-ai-act-en.index.schema.json

**JSON v3.0 Root Arrays**:

- `metadata` - Indices, penalties, validation
- `definitions` - Article 3 terms (68)
- `chapters` - Chapters, articles, paragraphs (800+)
- `annexes` - Annex content (200+)
- `recitals` - Preamble recitals (180)
