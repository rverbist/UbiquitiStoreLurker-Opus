---
name: c2l-eu-ai-act
description: 'Use and extend the EU AI Act legal reference API in the AICMS module. Covers the singleton C# service, Breeze endpoints, AngularJS resource factory, AiActService, and reusable directives (definition tooltip, node panel).'
---

# EU AI Act API Stack

## Objective

Provide programmatic access to the full EU AI Act (Regulation 2024/1689) from the AICMS module. The corpus (2,651-line markdown) and its enhanced index (1,283 nodes, 68 definitions) are loaded once as embedded resources into a singleton service, then exposed through Breeze, an AngularJS resource factory, a caching service, and reusable directives.

## Architecture Overview

```
.github/skills/eu-ai-act-navigator/references/
  eu-ai-act-en.md                    Authoritative legal text (embedded resource)
  eu-ai-act-en.enhanced.json         Normative annotations + graph (embedded resource)
  eu-ai-act-en.enhanced.schema.json  JSON schema (embedded resource)
        |
        v
EuAiActGraphService  (C# singleton, loads at startup)
        |
        v
AicmsResourceController.AiAct.cs  (10 Breeze HTTP GET endpoints)
        |
        v
AicmsResourceFactory  (10 Breeze EntityQuery methods)
        |
        v
AiActService  (AngularJS service with caching)
        |
        v
Directives: aiActDefinitionTooltip, aiActNodePanel
```

## Core Files

| Layer | File |
|-------|------|
| Interface + DTOs | `Coach2Lead.Web/App/Domain/AICMS/Regulation/EuAiActGraphContracts.cs` |
| Service (main) | `Coach2Lead.Web/App/Domain/AICMS/Regulation/EuAiActGraphService.cs` |
| Service (models) | `Coach2Lead.Web/App/Domain/AICMS/Regulation/EuAiActGraphService.Models.cs` |
| Service (build) | `Coach2Lead.Web/App/Domain/AICMS/Regulation/EuAiActGraphService.Build.cs` |
| Service (resolve) | `Coach2Lead.Web/App/Domain/AICMS/Regulation/EuAiActGraphService.Resolve.cs` |
| Service (clone) | `Coach2Lead.Web/App/Domain/AICMS/Regulation/EuAiActGraphService.Clone.cs` |
| Breeze controller | `Coach2Lead.Web/Areas/AICMS/Controllers/API/AicmsResourceController.AiAct.cs` |
| Resource factory | `Coach2Lead.Web/Areas/AICMS/Angular/aicms/aicms.resources.js` |
| JS service | `Coach2Lead.Web/Areas/AICMS/Angular/aicms/services/AiAct.service.js` |
| Domain service | `Coach2Lead.Web/Areas/AICMS/Angular/aicms/aicms.service.js` (exposes `domain.aiAct`) |
| Tooltip directive | `Coach2Lead.Web/Areas/AICMS/Angular/aicms/directives/aiActDefinitionTooltip.js` |
| Panel directive | `Coach2Lead.Web/Areas/AICMS/Angular/aicms/directives/aiActNodePanel.js` |
| Panel template | `Coach2Lead.Web/Areas/AICMS/Views/Directive/AiActNodePanel.cshtml` |
| Directive controller | `Coach2Lead.Web/Areas/AICMS/Controllers/DirectiveController.cs` |

## Layer 1: C# Singleton Service

### Registration

```csharp
// Global.asax.cs
ServiceLocator.Configure(services =>
{
    services.AddSingleton<IEuAiActGraphService, EuAiActGraphService>();
});
ServiceLocator.GetRequiredService<IEuAiActGraphService>(); // eager load
```

### IEuAiActGraphService Interface

| Method | Return | Description |
|--------|--------|-------------|
| `GetDocumentSummary()` | `EuAiActDocumentSummaryDto` | Document overview: metadata, chapters, annexes, recitals |
| `GetNode(anchor)` | `EuAiActNodeDetailDto` | Full node detail with source markdown, cross-refs, definitions |
| `FindNodes(query)` | `EuAiActNodeSummaryDto[]` | Filtered search by type, normType, actor, term, text, parent |
| `FindDefinitions(term, limit)` | `EuAiActDefinitionSummaryDto[]` | Search definitions by label or description text |
| `GetDefinition(anchor)` | `EuAiActDefinitionSummaryDto` | Single definition by anchor (e.g. `"definition-1"`) |
| `GetIndexKeys()` | `EuAiActIndexKeysDto` | Available filter values: types, normTypes, actors, terms |
| `GetNodeChildren(anchor)` | `EuAiActNodeSummaryDto[]` | Direct children of a node (one level) |
| `GetNodeMarkdown(anchor)` | `string` | Source markdown text for a node (lightweight) |
| `FindObligations(actor, limit)` | `EuAiActNodeSummaryDto[]` | Shortcut: obligations filtered by actor |
| `FindProhibitions(limit)` | `EuAiActNodeSummaryDto[]` | All prohibition nodes (Article 5 etc.) |

### EuAiActNodeQuery (for FindNodes)

| Property | Type | Description |
|----------|------|-------------|
| `Anchor` | `string` | Exact anchor lookup (bypasses all other filters) |
| `ParentAnchor` | `string` | Filter to children of this parent |
| `Type` | `string` | Node type: `Chapter`, `Article`, `ArticleParagraph`, `ArticlePoint`, `Annex`, `Recital` |
| `NormType` | `string` | Norm classification: `obligation`, `prohibition`, `permission`, `definition`, `exception`, etc. |
| `Actor` | `string` | Applicable actor: `provider`, `deployer`, `importer`, `distributor`, etc. |
| `Term` | `string` | Defined terminology used in the node |
| `Text` | `string` | Free-text search across anchor, heading, title, summary, keywords, source markdown |
| `IncludeRecitals` | `bool` | Include recital nodes in results (default: false) |
| `Limit` | `int?` | Max results (default: 100, max: 250) |

### Key DTOs

**EuAiActNodeSummaryDto** - lightweight node representation:
`anchor`, `type`, `line`, `endLine`, `depth`, `parentAnchor`, `heading`, `title`, `fullTitle`, `terminology[]`, `graphMetrics`, `structuralMetadata`, `normMetadata`, `summaryText`, `keywords[]`, `penaltyExposure`, `ambiguitySignal`, `temporalScope`, `childCount`, `referencedCount`, `referencingCount`

**EuAiActNodeDetailDto** - extends summary with:
`parentNode`, `childNodes[]`, `referencedNodes[]`, `referencingNodes[]`, `referencesWithContext[]`, `terminologyDefinitions[]`, `sourceMarkdown`

**EuAiActDefinitionSummaryDto**:
`anchor`, `label`, `description`, `line`, `referencedByNodes[]`

**EuAiActIndexKeysDto**:
`types[]`, `normTypes[]`, `actors[]`, `terms[]`

## Layer 2: Breeze Endpoints

Base route: `/breeze/AicmsResource/`

| HTTP Method | Endpoint | Parameters | Returns |
|-------------|----------|------------|---------|
| GET | `GetAiActDocumentSummary` | - | `EuAiActDocumentSummaryDto` |
| GET | `GetAiActNode` | `?anchor=article-9` | `EuAiActNodeDetailDto` |
| GET | `FindAiActNodes` | `?type=Article&actor=provider&normType=obligation&text=...&limit=50` | `EuAiActNodeSummaryDto[]` |
| GET | `FindAiActDefinitions` | `?term=ai+system&limit=25` | `EuAiActDefinitionSummaryDto[]` |
| GET | `GetAiActDefinition` | `?anchor=definition-1` | `EuAiActDefinitionSummaryDto` |
| GET | `GetAiActIndexKeys` | - | `EuAiActIndexKeysDto` |
| GET | `GetAiActNodeChildren` | `?anchor=article-9` | `EuAiActNodeSummaryDto[]` |
| GET | `GetAiActNodeMarkdown` | `?anchor=article-9` | `string` |
| GET | `FindAiActObligations` | `?actor=provider&limit=100` | `EuAiActNodeSummaryDto[]` |
| GET | `FindAiActProhibitions` | `?limit=100` | `EuAiActNodeSummaryDto[]` |

All responses use camelCase JSON serialization.

## Layer 3: AngularJS Resource Factory

**Module:** `aicms` via `AicmsResourceFactory`

| Method | Breeze Query | Returns |
|--------|-------------|---------|
| `getAiActDocumentSummary()` | `resources.one(...)` | Promise\<object\> |
| `getAiActNode(anchor)` | `resources.one(...)` | Promise\<object\> |
| `findAiActNodes(queryParameters)` | `resources.many(...)` | Promise\<array\> |
| `findAiActDefinitions(term, limit)` | `resources.many(...)` | Promise\<array\> |
| `getAiActDefinition(anchor)` | `resources.one(...)` | Promise\<object\> |
| `getAiActIndexKeys()` | `resources.one(...)` | Promise\<object\> |
| `getAiActNodeChildren(anchor)` | `resources.many(...)` | Promise\<array\> |
| `getAiActNodeMarkdown(anchor)` | `resources.one(...)` | Promise\<string\> |
| `findAiActObligations(actor, limit)` | `resources.many(...)` | Promise\<array\> |
| `findAiActProhibitions(limit)` | `resources.many(...)` | Promise\<array\> |

## Layer 4: AiActService (Recommended Entry Point)

**Inject:** `'AiActService'` or access via `domain.aiAct` from `AicmsDomainService`.

### Cached Methods (loaded once, static data)

| Method | Description |
|--------|-------------|
| `getDocumentSummary()` | Cached. Returns document overview. |
| `getIndexKeys()` | Cached. Returns available filter keys for dropdowns. |

### Query Methods (pass-through)

| Method | Parameters | Description |
|--------|------------|-------------|
| `getNode(anchor)` | `'article-9'` | Full node detail with markdown and cross-refs |
| `getDefinition(anchor)` | `'definition-1'` | Single definition by anchor |
| `getNodeChildren(anchor)` | `'article-9'` | Direct child nodes |
| `getNodeMarkdown(anchor)` | `'article-9'` | Raw markdown text |
| `findNodes(query)` | `{ type, normType, actor, term, text, includeRecitals, limit }` | Filtered node search |
| `findDefinitions(term, limit)` | `'ai system', 25` | Definition search |
| `findObligations(actor, limit)` | `'provider', 100` | Obligations for an actor |
| `findProhibitions(limit)` | `100` | All prohibition nodes |
| `clearCache()` | - | Reset cached data |

### Usage from Controllers

```javascript
// Via dependency injection
angular.module('aicms').controller('MyController', ['AiActService', function(aiAct) {
    aiAct.getIndexKeys().then(function(keys) {
        $scope.actors = keys.actors;     // ['provider', 'deployer', ...]
        $scope.normTypes = keys.normTypes; // ['obligation', 'prohibition', ...]
    });

    aiAct.findNodes({ actor: 'provider', normType: 'obligation', limit: 50 })
        .then(function(nodes) { $scope.obligations = nodes; });
}]);

// Via domain service
$scope.domain.aiAct.getNode('article-9').then(function(node) {
    console.log(node.sourceMarkdown);
});
```

## Layer 5: Directives

### aiActDefinitionTooltip

**Type:** Attribute directive
**Module:** `aicms`
**Dependencies:** `AiActService`, `$sce`

Shows a popover with the EU AI Act definition (label + description) on mouse hover. Accepts either a definition anchor or a term label.

```html
<!-- By term label -->
<span ai-act-definition-tooltip="'ai system'">AI system</span>

<!-- By definition anchor -->
<span ai-act-definition-tooltip="'definition-1'">AI system</span>

<!-- With dynamic binding -->
<span ai-act-definition-tooltip="selectedTerm">{{selectedTerm}}</span>
```

**Scope bindings:**
- `term` (`=aiActDefinitionTooltip`): string - definition anchor (`definition-N`) or term label

**Behavior:**
- If term starts with `definition-`, calls `AiActService.getDefinition(anchor)`
- Otherwise calls `AiActService.findDefinitions(term, 1)` to match by label
- Renders popover with `label`, `anchor` badge, and `description`
- Appends a small info icon when definition is resolved

### aiActNodePanel

**Type:** Element directive
**Module:** `aicms`
**Dependencies:** `AiActService`
**Template:** `/AICMS/Directive/AiActNodePanel`

Renders a collapsible panel displaying an EU AI Act article/provision with its heading, metadata badges, terminology, source markdown, and sub-provisions.

```html
<!-- Basic usage (collapsed by default, loads on expand) -->
<ai-act-node-panel anchor="'article-9'"></ai-act-node-panel>

<!-- Start expanded -->
<ai-act-node-panel anchor="'article-9'" collapsed="false"></ai-act-node-panel>

<!-- Dynamic binding -->
<ai-act-node-panel anchor="selectedAnchor"></ai-act-node-panel>
```

**Scope bindings:**
- `anchor` (`=`): string - node anchor (e.g. `article-9`, `article-9-2-a`, `annex-iii`)
- `collapsed` (`=?`): boolean - initial collapsed state (default: `true`)

**Panel content (when expanded):**
- Summary text
- Metadata badges: norm type, penalty tier, applicable-from date
- Terminology labels
- Source markdown in `<pre>` block
- Child nodes list with type badge and heading

## Anchor Naming Convention

| Type | Pattern | Example |
|------|---------|---------|
| Article | `article-{number}` | `article-9` |
| Paragraph | `article-{number}-{paragraph}` | `article-9-2` |
| Point | `article-{number}-{paragraph}-{point}` | `article-9-2-a` |
| Definition | `definition-{number}` | `definition-1` |
| Annex | `annex-{roman}` | `annex-iii` |
| Recital | `recital-{number}` | `recital-47` |
| Chapter | `chapter-{roman-or-number}` | `chapter-iii` |

## Guardrails

- The markdown text is the law. All JSON metadata is derived and non-authoritative.
- Do not use enhanced metadata to make compliance decisions - it is for navigation and reference only.
- `null` fields mean "unknown", not "not applicable" or "permission granted".
- Provisions marked with `ambiguitySignal.level = "vague"` or `"delegated"` require human judgment.
- Temporal scope matters - many provisions have different effective dates per Article 113.

## Companion Skills

- `eu-ai-act-navigator` - full corpus documentation, schema reference, and query snippets
- `c2l-breeze-webapi` - Breeze controller patterns and save pipeline
- `c2l-solution-orientation` - project structure and module layout
