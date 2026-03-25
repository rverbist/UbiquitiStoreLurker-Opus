---
name: eu-ai-act-navigator
description: Expert guidance for navigating the EU AI Act (Regulation 2024/1689). Use when users ask about AI compliance, high-risk systems, prohibited practices, conformity assessment, penalties, or specific articles from the EU AI regulation. Provides structured access to the complete regulation with cross-references and metadata.
---

# EU AI Act Navigator Skill

This skill provides comprehensive knowledge and navigation capabilities for the EU Artificial Intelligence Act (Regulation 2024/1689), the world's first comprehensive AI regulation.

## When to Use This Skill

Use this skill when the user requests:

- **Compliance guidance** for AI systems under EU regulation
- **Risk classification** of AI systems (prohibited, high-risk, limited-risk, minimal-risk)
- **Article lookup** or interpretation from the EU AI Act
- **Cross-reference tracing** between articles, annexes, and definitions
- **Penalty information** for non-compliance
- **Timeline and deadlines** for implementation
- **Definition clarification** for AI-specific terminology
- **Provider or deployer obligations** under the regulation
- **Conformity assessment** procedures
- **Documentation requirements** for high-risk AI systems

## Supporting Documentation

For detailed implementation guidance, consult these resources:

### [AGENTS.MD](AGENTS.MD)
**Purpose**: AI-agent usage instructions and safety rules

**When to use**:
- Before implementing any automated EU AI Act analysis
- When deciding which file to use for a specific task
- To understand limitations and prohibited operations
- For conflict resolution between markdown and JSON

**Key content**:
- Core principle: Markdown is authoritative, JSON is derived
- Per-file usage rules (MUST/MUST NOT patterns)
- Safety constraints for norm extraction and compliance checks
- Deployment context for AI-assisted compliance tooling

### [scripts/navigation-and-query-snippets.md](scripts/navigation-and-query-snippets.md)
**Purpose**: Code examples for navigating JSON files

**When to use**:
- Building compliance tools that consume the JSON files
- Implementing search, filtering, or graph traversal
- Learning anchor-based navigation patterns
- Generating queries for obligations, actors, or conditions

**Key content**:
- Python and JavaScript examples
- Finding nodes by criteria (actor, norm type, conditions)
- Derivation metadata retrieval
- Cross-reference lookups
- Safety patterns and validation

---

## Knowledge Base Location

The EU AI Act documentation is stored in three interconnected files:

1. **Primary Document**: [references/eu-ai-act-en.md](references/eu-ai-act-en.md)
   - Full regulation text with 2,651 lines
   - Complete table of contents with collapsible sections
   - Glossary of 68 defined terms with quick-reference links
   - All 113 articles with anchor IDs
   - All 13 annexes
   - 116 recitals

2. **JSON Index**: [references/eu-ai-act-en.index.json](references/eu-ai-act-en.index.json)
   - 1,103 nodes (chapters, articles, sections, annexes, recitals)
   - 644 cross-references with context
   - Complete metadata and validation reports
   - Graph metrics (in-degree, out-degree, dependency chains)
   - Structural metadata (obligations, permissions, prohibitions)
   - Pre-computed indices with keys arrays for fast enumeration
   - Sanitized snippets (human-readable, no HTML/markdown artifacts)
   - Line numbers for precise location

3. **JSON Schema**: [references/eu-ai-act-en.index.schema.json](references/eu-ai-act-en.index.schema.json)
   - Type definitions for programmatic access
   - Validation rules
   - Property specifications

## How to Navigate the Regulation

### Step 1: Understand the Structure

The EU AI Act is organized into:

- **Recitals (1-116)**: Legislative context and reasoning
- **13 Chapters**:
  - Chapter I: General Provisions (Articles 1-4)
  - Chapter II: Prohibited AI Practices (Article 5)
  - Chapter III: High-Risk AI Systems (Articles 6-51)
  - Chapter IV: Transparency Obligations (Article 50)
  - Chapter V: General-Purpose AI Models (Articles 51-56)
  - Chapter VI: Measures in Support of Innovation (Articles 57-62)
  - Chapter VII: Governance (Articles 63-75)
  - Chapter VIII: EU Database for High-Risk AI Systems (Article 76)
  - Chapter IX: Post-Market Monitoring, Information Sharing, Market Surveillance (Articles 77-87)
  - Chapter X: Codes of Conduct (Articles 95-96)
  - Chapter XI: Delegation and Committee Procedure (Articles 97-101)
  - Chapter XII: Penalties (Article 99)
  - Chapter XIII: Final Provisions (Articles 102-113)
- **13 Annexes**: Technical details, lists, procedures

### Step 2: Look Up Articles by Anchor ID

All articles have consistent anchor IDs:

- Articles: `#article-{number}` (e.g., `#article-9`)
- Article paragraphs: `#article-{number}-{paragraph}` (e.g., `#article-9-2`)
- Article points: `#article-{number}-{paragraph}-{point}` (e.g., `#article-9-2-a`)
- Definitions: `#definition-{number}` (e.g., `#definition-1`)
- Annexes: `#annex-{number}` (e.g., `#annex-iii`)
- Recitals: `#recital-{number}` (e.g., `#recital-5`)
- Recital points: `#recital-{number}-{point}` (e.g., `#recital-5-a`)

### Step 3: Use the JSON Index for Advanced Queries

The JSON index uses a restructured format with separate root arrays:

```javascript
// Structure: { metadata, definitions, chapters, annexes, recitals }

// Use pre-computed indices for fast lookups
const allTerms = doc.metadata.indices["terminology-keys"];
const providerArticles = doc.metadata.indices.actor["provider"];
const shallObligations = doc.metadata.indices.obligation["shall"];

// Example: Find all articles with obligations for "provider"
const nodes = [...doc.chapters, ...doc.annexes];
nodes.filter(n => 
  n.type === "Article" && 
  n.structuralMetadata?.applicableTo?.includes("provider") &&
  n.structuralMetadata?.hasObligations === true
)

// Example: Find articles that reference Article 9
nodes.filter(n => 
  n.referencing?.includes("article-9")
)

// Example: Access recitals directly
doc.recitals.filter(r => r.type === "Recital")
```

### Step 4: Check Cross-References

The JSON index contains:

- **`referencing`**: Articles this article references
- **`referenced`**: Articles that reference this article
- **`referencesWithContext`**: Detailed reference metadata with:
  - `targetId`: The referenced anchor
  - `contextType`: Relationship type (compliance, exception, supplement, etc.)
  - `snippet`: Human-readable text (sanitized, no HTML anchors or markdown links)
  - `context`: Surrounding text for classification
  - `line`: Source line number

## Key Concepts and Definitions

### Risk Categories

1. **Prohibited AI** (Article 5): Systems that manipulate behavior, social scoring, real-time biometric identification in public spaces (with exceptions)

2. **High-Risk AI** (Article 6 + Annex III): Systems in critical areas like:
   - Biometric identification and categorization
   - Critical infrastructure
   - Education and vocational training
   - Employment and worker management
   - Essential services (law enforcement, migration, justice)
   - AI components of regulated products

3. **Limited-Risk AI** (Article 50): Systems requiring transparency (chatbots, emotion recognition, deepfakes)

4. **Minimal-Risk AI**: Everything else (no specific obligations)

### Key Actors

1. **Provider** (Definition 3): Develops or has developed AI systems; places on market
2. **Deployer** (Definition 4): Uses AI system under their authority
3. **Importer** (Definition 6): Places on market AI from third country
4. **Distributor** (Definition 7): Makes available on market
5. **Operator** (Definition 8): Collective term for provider, deployer, etc.

### Important Article Clusters

**High-Risk System Requirements (Articles 8-15)**:
- Article 8: Compliance framework
- Article 9: Risk management system ⭐ (most referenced)
- Article 10: Data and data governance
- Article 11: Technical documentation
- Article 12: Record-keeping
- Article 13: Transparency and information to deployers
- Article 14: Human oversight
- Article 15: Accuracy, robustness, cybersecurity

**Provider Obligations (Articles 16-29)**:
- Article 16: Quality management system
- Article 17: Documentation and record-keeping
- Article 26: Deployer obligations

**Conformity Assessment (Articles 43-49)**:
- Article 43: Conformity assessment procedures
- Article 46: EU declaration of conformity

**Penalties (Article 99)**:
- €35M or 7% turnover: Prohibited practices (Article 5)
- €15M or 3% turnover: Other violations
- €7.5M or 1.5% turnover: Incorrect information

## Common Query Patterns

### 1. Classification Query
**User asks**: "Is my chatbot high-risk?"

**Steps**:
1. Check Article 6 for classification rules
2. Review Annex III for high-risk categories
3. Check Article 6(3) for exceptions
4. Provide risk level and applicable obligations

### 2. Compliance Requirements Query
**User asks**: "What do I need to comply with for a high-risk AI system?"

**Steps**:
1. Confirm high-risk classification (Article 6 + Annex III)
2. List requirements from Articles 8-15
3. Add provider obligations from Articles 16-29
4. Include conformity assessment (Articles 43-49)
5. Reference Annex IV (technical documentation)

### 3. Timeline Query
**User asks**: "When does this come into force?"

**Steps**:
1. Check Article 113 for entry into force dates
2. Key dates:
   - Regulation enters into force: 20 days after publication
   - Prohibited practices: 6 months after entry
   - High-risk systems: 36 months after entry
   - General-purpose AI: 24 months after entry

### 4. Definition Query
**User asks**: "What is a deployer?"

**Steps**:
1. Look up definition in Article 3
2. Reference anchor: `#definition-4`
3. Provide full definition text
4. Show related articles using `terminology` field in JSON

### 5. Cross-Reference Query
**User asks**: "What articles reference Article 9?"

**Steps**:
1. Look up Article 9 in JSON index
2. Check `referenced` array
3. Provide list with article titles
4. Optionally show context from `referencesWithContext`

## Response Templates

### Article Lookup Response
```markdown
## Article [NUMBER]: [TITLE]

**Location**: Line [LINE] in eu-ai-act-en.md
**Chapter**: [CHAPTER]
**Section**: [SECTION if applicable]

### Summary
[Brief 1-2 sentence summary]

### Full Text
[Quote relevant paragraphs]

### Cross-References
**References**: [List of articles this article cites]
**Referenced by**: [List of articles citing this one]

### Related Definitions
[List any terminology used from Article 3]
```

### Compliance Checklist Response
```markdown
## Compliance Checklist: [AI System Type]

**Risk Classification**: [Prohibited/High-Risk/Limited-Risk/Minimal-Risk]

### Requirements:
- [ ] Article [N]: [Requirement name]
  - Summary: [Brief description]
  - Deadline: [If applicable]
  
### Documentation Required:
- [ ] Technical documentation (Annex IV)
- [ ] EU declaration of conformity (Article 46)
- [ ] [Other requirements]

### Penalties for Non-Compliance:
Up to [AMOUNT] or [PERCENTAGE]% of global turnover
```

### Definition Response
```markdown
## Definition: [TERM]

**Anchor**: #definition-[NUMBER]
**Line**: [LINE NUMBER]

### Official Definition
> [Full definition text from Article 3]

### Used in Articles:
[List of articles that reference this term based on JSON index]

### Related Terms:
[Link to related definitions]
```

## Advanced Features

### 1. Graph Analysis
Use the JSON index to perform graph analysis:

- **Most referenced article**: Use `validation.statistics.mostReferencedArticle`
- **Dependency depth**: Use `graphMetrics.dependencyChainLength`
- **Compliance paths**: Traverse `referencing` and `referenced` arrays

### 2. Structural Filtering
Filter by structural metadata:

```javascript
// Find all articles with obligations
nodes.filter(n => n.structuralMetadata?.hasObligations === true)

// Find articles with specific obligation verbs
nodes.filter(n => 
  n.structuralMetadata?.obligationVerbs?.includes("shall")
)

// Find articles applicable to specific actors
nodes.filter(n => 
  n.structuralMetadata?.applicableTo?.includes("provider")
)
```

### 3. Pre-computed Index Lookups
Use indices for O(1) lookups:

```javascript
// Get all available keys first
const terms = doc.metadata.indices["terminology-keys"];      // ["ai system", "deployer", ...]
const actors = doc.metadata.indices["actor-keys"];           // ["deployer", "provider", ...]
const obligations = doc.metadata.indices["obligation-keys"]; // ["shall", "must", "have to"]

// Then lookup by key
const providerArticles = doc.metadata.indices.actor["provider"];
const shallArticles = doc.metadata.indices.obligation["shall"]; // 108 articles
```

### 4. Contextual Reference Analysis
Use `referencesWithContext` for understanding why articles reference each other:

```javascript
// Get context of references (snippets are sanitized for readability)
article.referencesWithContext.forEach(ref => {
  console.log(`References ${ref.targetId}`);
  console.log(`Type: ${ref.contextType}`); // compliance, exception, etc.
  console.log(`Snippet: ${ref.snippet}`);  // Human-readable, no HTML/markdown
});
```

### 5. Recital Access
Recitals are now first-class citizens:

```javascript
// Access all 180 recitals
const recitals = doc.recitals;

// Find recitals with cross-references
const referencingRecitals = doc.recitals.filter(r => r.referencing?.length > 0);

// Get recital points
const recitalPoints = doc.recitals.filter(r => r.type === "RecitalPoint");
```

## Data Quality Metrics

The documentation has been verified for quality:

- **Completeness**: 113/113 articles (100%)
- **Reference Integrity**: 0 broken references
- **Validation**: All checks passed
- **Line Accuracy**: All anchor IDs verified
- **Cross-Reference Count**: 644 tracked references
- **Graph Metrics**: Fully computed for all nodes

## Important Notes

1. **Line Numbers**: Always use line numbers from JSON index for precise location
2. **Anchor Links**: All markdown anchors are lowercase with hyphens
3. **Versioning**: This is Regulation 2024/1689 (final published version)
4. **Effective Date**: Entered into force August 2, 2024
5. **Language**: English (EN) version
6. **Scope**: Applies to EU market and certain third-country providers

## Limitations

- This skill covers the regulation text only, not:
  - Implementing acts or delegated acts
  - National implementing legislation
  - Case law or interpretations
  - Codes of practice
  - Harmonized standards
- For legal advice, always consult qualified legal professionals
- Regulation is subject to updates via implementing acts

## Best Practices for Responses

1. **Cite precisely**: Always include article numbers and line references
2. **Show context**: Provide surrounding text for clarity
3. **Link related content**: Reference cross-references and definitions
4. **Be comprehensive**: For compliance questions, cover all applicable obligations
5. **Use structured format**: Employ checklists, tables, and clear headings
6. **Validate claims**: Use JSON index to verify cross-references
7. **Consider applicability**: Check Article 2 scope before applying provisions
8. **Note exceptions**: Many articles have carve-outs and exceptions

## Example Workflows

### Workflow 1: Classify an AI System
1. Read user's description of AI system
2. Check Article 6 classification rules
3. Match against Annex III categories
4. Apply Article 6(3) exceptions if relevant
5. Provide risk classification with rationale
6. List applicable obligations based on risk level

### Workflow 2: Generate Compliance Roadmap
1. Confirm AI system classification
2. Extract requirements from Articles 8-15
3. Add provider obligations from Articles 16-29
4. Include conformity assessment requirements
5. Map to Annex IV documentation requirements
6. Add timeline from Article 113
7. Format as actionable checklist with deadlines

### Workflow 3: Answer Legal Question
1. Identify relevant article(s) using semantic search
2. Load full article text from markdown
3. Check cross-references using JSON index
4. Review related definitions from Article 3
5. Provide comprehensive answer with citations
6. Include any relevant recitals for context

## File Access Examples

### Read Primary Document
```markdown
Read lines 704-727 from `references/eu-ai-act-en.md` for Article 9
```

### Query JSON Index
```markdown
Search `references/eu-ai-act-en.index.json` for:
- Node with anchor "article-9"
- All nodes where type === "Article" and structuralMetadata.hasObligations === true
```

### Validate Against Schema
```markdown
Verify JSON structure against `references/eu-ai-act-en.index.schema.json`
```

---

## Quick Reference

**Most Important Articles**:
- Article 3: Definitions
- Article 5: Prohibited AI practices
- Article 6: Classification of high-risk AI
- Article 9: Risk management system (most referenced)
- Article 26: Deployer obligations
- Article 43: Conformity assessment
- Article 99: Penalties
- Article 113: Entry into force

**Most Referenced Annex**: Annex III (High-risk AI systems) - referenced 37 times

**Total Structure**:
- 113 Articles
- 13 Chapters
- 13 Annexes
- 68 Definitions
- 180 Recitals (extracted as nodes)
- 1,103 Total Nodes
- 644 Cross-References

**JSON Structure** (v3.0):
- `metadata`: Indices, penalties, validation
- `definitions`: Article 3 terms
- `chapters`: Chapters, articles, paragraphs
- `annexes`: Annex content
- `recitals`: Preamble recitals

**Regulation Details**:
- Number: EU 2024/1689
- Published: July 12, 2024
- Effective: August 2, 2024
- Scope: European Union + certain third countries
- Official Journal: L series 2024/1689
