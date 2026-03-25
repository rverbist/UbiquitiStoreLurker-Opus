# AI-Agent Navigation & Query Snippets for EU AI Act JSON Corpus

This document provides **reference code snippets** that AI agents are expected to generate anyway, but in a **safe, canonical, copy-pasteable form**.

The snippets demonstrate **how to search, traverse, and validate** the following files:

- `../references/eu-ai-act-en.index.json`
- `../references/eu-ai-act-en.enhanced.json`
- (schemas used implicitly for validation assumptions)

The snippets are **illustrative, not normative**. They do not infer legal meaning.

## Before You Use These Snippets

**Read [AGENTS.MD](../AGENTS.MD) first** to understand:

- Which file to use for which purpose (index vs. enhanced vs. markdown)
- Safety rules and limitations
- What AI agents MUST NOT do with this data
- Conflict resolution hierarchy

This file provides **how** to query. AGENTS.MD provides **when, why, and whether** you should.

---

## General Conventions

- Anchors (e.g. `article-16-1-a`) are the **primary keys**
- All navigation is **anchor-based**
- Agents MUST treat missing fields as *unknown*, not false

---

## Python Examples

### Load enhanced.json

```python
import json

with open("eu-ai-act-en.enhanced.json", "r", encoding="utf-8") as f:
    doc = json.load(f)
```

---

### Find all obligations for a given actor

```python
def find_obligations_for_actor(doc, actor_key):
    results = []
    for section in (doc.get("chapters", []) + doc.get("annexes", [])):
        stack = [section]
        while stack:
            node = stack.pop()
            norm = node.get("normMetadata")
            if norm and norm.get("normType") == "obligation":
                for actor in norm.get("obligatedActors") or []:
                    if actor.get("actor") == actor_key:
                        results.append(node["anchor"])
            for child in node.get("children") or []:
                child_node = find_node_by_anchor(doc, child)
                if child_node:
                    stack.append(child_node)
    return results
```

---

### Retrieve derivation evidence for a node

```python
def get_derivation(node):
    norm = node.get("normMetadata")
    if not norm:
        return None
    return norm.get("derivedFrom")
```

---

## JavaScript Examples

### Load enhanced.json

```js
import fs from "fs";

const doc = JSON.parse(
  fs.readFileSync("eu-ai-act-en.enhanced.json", "utf-8")
);
```

---

### Find all nodes with conditions

```js
function findConditionalNodes(doc) {
  const results = [];

  function walk(node) {
    if (node.normMetadata?.conditions) {
      results.push(node.anchor);
    }
    (node.children || []).forEach(anchor => {
      const child = findNodeByAnchor(doc, anchor);
      if (child) walk(child);
    });
  }

  [...(doc.chapters || []), ...(doc.annexes || [])].forEach(walk);
  return results;
}
```

---

## Enhanced.json Specific Examples

### Find all prohibitions with actors

```python
def find_prohibitions_with_actors(doc):
    results = []
    for section in (doc.get("chapters", []) + doc.get("annexes", [])):
        stack = [section]
        while stack:
            node = stack.pop()
            norm = node.get("normMetadata")
            if norm and norm.get("normType") == "prohibition":
                actors = norm.get("obligatedActors") or []
                if actors:
                    results.append({
                        "anchor": node["anchor"],
                        "actors": [a["actor"] for a in actors],
                        "source": [a["source"] for a in actors],
                        "ruleId": norm.get("derivedFrom", {}).get("ruleIds", [])
                    })
            for child in node.get("children") or []:
                child_node = find_node_by_anchor(doc, child)
                if child_node:
                    stack.append(child_node)
    return results
```

### Filter nodes by condition type

```js
function findNodesWithCondition(doc, conditionType) {
  const results = [];

  function walk(node) {
    const conditions = node.normMetadata?.conditions;
    if (conditions && conditions[conditionType]?.length > 0) {
      results.push({
        anchor: node.anchor,
        conditions: conditions[conditionType].map(c => ({
          text: c.text,
          span: c.span
        }))
      });
    }
    (node.children || []).forEach(anchor => {
      const child = findNodeByAnchor(doc, anchor);
      if (child) walk(child);
    });
  }

  [...(doc.chapters || []), ...(doc.annexes || [])].forEach(walk);
  return results;
}

// Usage: findNodesWithCondition(doc, "where")
// Returns all nodes with "where" conditions
```

### Validate derivation metadata completeness

```python
def validate_norm_derivation(doc):
    errors = []
    for section in (doc.get("chapters", []) + doc.get("annexes", [])):
        stack = [section]
        while stack:
            node = stack.pop()
            norm = node.get("normMetadata")
            if norm:
                derived = norm.get("derivedFrom")
                if not derived:
                    errors.append(f"{node['anchor']}: normMetadata without derivedFrom")
                elif not derived.get("method") or not derived.get("ruleVersion"):
                    errors.append(f"{node['anchor']}: incomplete derivedFrom")
            for child in node.get("children") or []:
                child_node = find_node_by_anchor(doc, child)
                if child_node:
                    stack.append(child_node)
    return errors
```

### Extract all inherited actors

```js
function findInheritedActors(doc) {
  const inherited = [];

  function walk(node) {
    const actors = node.normMetadata?.obligatedActors || [];
    actors.forEach(actor => {
      if (actor.source.startsWith("inherited:")) {
        inherited.push({
          anchor: node.anchor,
          actor: actor.actor,
          source: actor.source,
          confidence: actor.confidence
        });
      }
    });
    (node.children || []).forEach(anchor => {
      const child = findNodeByAnchor(doc, anchor);
      if (child) walk(child);
    });
  }

  [...(doc.chapters || []), ...(doc.annexes || [])].forEach(walk);
  return inherited;
}
```

---

## Index.json Navigation Examples

### Find all articles referencing Annex III

```python
def find_annex_iii_references(index):
    return [
        node["anchor"]
        for node in index["nodes"]
        if "annex-iii" in (node.get("referenced") or [])
    ]
```

---

### Find definition usage (Article 3)

```js
function findDefinitionUsage(index, definitionAnchor) {
  return index.definitions
    .find(d => d.anchor === definitionAnchor)
    ?.referenced || [];
}
```

---

## Safety Pattern: Conflict Resolution

```python
def resolve_conflict(md_text, enhanced_node):
    # Always prefer markdown
    if md_text:
        return md_text
    return enhanced_node.get("normMetadata")
```

---

## What AI Agents MUST NOT Do

- Do not classify real systems
- Do not infer missing obligations
- Do not treat index data as normative
- Do not ignore `derivedFrom` metadata

---

## Intended Usage

These snippets are designed for:

- AI-assisted compliance tooling
- Interactive exploration
- Human-in-the-loop reasoning

They are **not** intended for autonomous enforcement.

---

**End of Code Snippet Reference**
