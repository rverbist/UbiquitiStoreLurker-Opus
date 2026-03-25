# AI-Agent Usage Instructions: EU AI Act Canonical File Set

## Purpose of this Document

This file provides **explicit, non-negotiable instructions for AI agents** (LLMs, rule-based agents, or hybrids) on **how, when, and why** to use the following five canonical artefacts of the EU AI Act corpus:

1. `references/eu-ai-act-en.md`
2. `references/eu-ai-act-en.index.json`
3. `references/eu-ai-act-en.index.schema.json`
4. `references/eu-ai-act-en.enhanced.json`
5. `references/eu-ai-act-en.enhanced.schema.json`

These instructions are designed to:

- prevent hallucination and overreach,
- enforce correct separation of concerns,
- preserve auditability,
- and align with **AICMS-style human-managed compliance systems**.

This document is **instructional, not legal advice**.

---

## Core Principle (Read First)

> **The Markdown text is the law. Everything else is derived, non-authoritative metadata.**

AI agents MUST treat:

- `references/eu-ai-act-en.md` as the **only authoritative source of legal meaning**.
- JSON artefacts as **navigation, annotation, and derivation aids only**.

No agent may infer obligations, permissions, or prohibitions unless they are:

- explicitly present in the Markdown text, or
- deterministically derived and marked as such in `enhanced.json`.

---

## File 1: `references/eu-ai-act-en.md` - Authoritative Legal Text

### What it is

- The full text of Regulation (EU) 2024/1689 (AI Act)
- Structured with stable anchors for:
  - articles, paragraphs, points
  - recitals
  - annexes and sections

### How an AI agent MUST use it

- As the **source of truth** for:
  - legal wording
  - obligations, prohibitions, permissions
  - definitions (Article 3)
- For **quoting, citing, or explaining** legal requirements
- For resolving ambiguity when metadata conflicts exist

### How an AI agent MUST NOT use it

- Do not summarise obligations without citing anchors
- Do not infer structure that is not explicitly present
- Do not treat recitals as operative obligations

---

## File 2: `references/eu-ai-act-en.index.json` - Structural & Lexical Index

### What it is

- A flat graph of all legal nodes (articles, paragraphs, annex items)
- Bidirectional references (`referenced` / `referencing`)
- Terminology and actor indices

### Intended use

AI agents MAY use this file to:

- navigate the regulation efficiently,
- find where a term is defined or used,
- analyse cross-references and dependencies,
- build impact or coverage maps.

### Explicit limitations

AI agents MUST NOT:

- treat index entries as norms,
- infer obligations or permissions from index data,
- assume that actor mentions imply responsibility.

**Index data is descriptive, not normative.**

---

## File 3: `references/eu-ai-act-en.index.schema.json` - Index Contract

### What it is

- The structural contract governing `references/eu-ai-act-en.index.json`

### How an AI agent should use it

- To validate expectations about index structure
- To understand which fields are guaranteed vs optional
- To avoid assuming semantics not present in the schema

### Prohibited use

- Do not use the schema to infer legal meaning
- Do not extrapolate future fields or semantics

---

## File 4: `references/eu-ai-act-en.enhanced.json` - Deterministic Norm Annotations

### What it is

- A **derived, non-authoritative annotation layer**
- Enriches document nodes with:
  - normType (obligation, prohibition, etc.)
  - obligated actors
  - conditions (where / unless / when)
  - applicability (risk, lifecycle)
  - derivation metadata (`derivedFrom`)

### How an AI agent MAY use it

- To assist humans in:
  - locating relevant obligations
  - understanding scope and conditions
  - explaining why a norm applies
- To answer questions like:
  - "Which articles impose obligations on providers?"
  - "Under what conditions does this requirement apply?"

### Mandatory safety rules

AI agents MUST:

- check `derivedFrom` before trusting any normMetadata,
- surface derivation context when explaining norms,
- defer to Markdown text if inconsistencies appear,
- treat missing fields as **unknown**, not false.

### Prohibited use

AI agents MUST NOT:

- decide compliance status,
- infer missing obligations,
- auto-classify real systems or risk levels,
- treat derived metadata as legally binding.

---

## File 5: `references/eu-ai-act-en.enhanced.schema.json` - Norm Annotation Contract

### What it is

- The schema defining what enriched metadata may exist
- Includes explicit non-authoritative constraints

### How an AI agent should use it

- To understand:
  - which metadata fields are guaranteed,
  - which are optional or nullable,
  - what null values mean (absence != permission)
- To validate that enhanced data is structurally sound

### Important reminder

Schema validity != legal correctness.

---

## Correct Usage Patterns (Examples)

### ✅ Correct

- "Article 16(1)(a) requires providers to establish a risk management system (derived as an obligation; source: 'shall')."
- "This obligation applies where the AI system is high-risk, as defined in Article 6 and Annex III."

### ❌ Incorrect

- "Your system is non-compliant with the AI Act."
- "This requirement applies to you automatically."
- "The index indicates this is mandatory."

---

## Conflict Resolution Rule

If any conflict exists:

1. **Markdown text (`.md`) wins**
2. Enhanced metadata is secondary
3. Index data is tertiary

Agents MUST surface conflicts, not resolve them silently.

---

## Intended Deployment Context

These files are designed to support:

- AI-assisted compliance tooling (e.g. AICMS)
- Human-in-the-loop regulatory analysis
- Audit preparation and traceability

They are **not** designed for:

- autonomous enforcement
- legal determinations
- runtime system monitoring

---

## Final Safety Statement

> AI agents using this corpus assist humans by structuring and explaining the EU AI Act.  
> They do not replace legal analysis, regulatory judgment, or organisational responsibility.

---

**End of AI-Agent Instruction File**
