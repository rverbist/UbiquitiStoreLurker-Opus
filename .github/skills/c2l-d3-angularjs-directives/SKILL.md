---
name: c2l-d3-angularjs-directives
description: 'Document modern Coach2Lead d3.js usage inside AngularJS directives, including chart factory draw/update lifecycle, isolate scope bindings, data join enter/merge/exit, tooltip wiring, transitions, and resize/layout orchestration. Use when creating or refactoring d3 visualizations in AngularJS directives, wiring chart lifecycle behavior, or aligning new charts to existing Coach2Lead conventions.'
---

# Coach2Lead D3 AngularJS Directives

<!-- toc:start -->
<details>
  <summary>
   <strong>Table of Contents</strong> Click to open
  </summary>

- [Objective](#objective)
- [Read First](#read-first)
- [Core Anchors](#core-anchors)
- [Standard Workflow](#standard-workflow)
- [Guardrails](#guardrails)
- [Validation](#validation)
- [Golden Example](#golden-example)
- [Output Contract](#output-contract)
- [Companion Skills](#companion-skills)
- [Skill-Specific Topics](#skill-specific-topics)

</details>
<!-- toc:end -->

## Objective
- Implement and refactor D3 visualizations in AngularJS directives using established Coach2Lead chart conventions.
- Prefer modern patterns used across active modules.
- Keep legacy D3 v3 API variants out of default implementation choices.

## Read First
- `Coach2Lead.Web/Areas/Competences/Angular/competences/directives/EvaluationSkillChart.js`
- `Coach2Lead.Web/Areas/Surveys/Angular/surveys/directives/ManagementTaskChart.js`
- `Coach2Lead.Web/Areas/Strategy/Angular/strategy/directives/processesRankingChart2.js`
- `Coach2Lead.Web/Areas/Tasks/Angular/tasks/directives/organizationalChartTree.js`
- `Coach2Lead.Web/Areas/Metrics/Angular/metrics/directives/indicator-trend-chart.js`
- `Coach2Lead.Web/Areas/Risks/Angular/risks/directives/risk-summary-chart.js`
- `Coach2Lead.Web/Areas/Providers/Angular/providers/directives/providerEvaluationSurveyEvolutionChart.js`

## Core Anchors
- `Coach2Lead.Web/Startup/BundleConfig.cs` (D3 and `d3.tip` bundle wiring)
- `Coach2Lead.Web/Scripts/d3/d3.js` (current bundled D3 runtime)
- `Coach2Lead.Web/Scripts/d3/d3.tip.js` (tooltip plugin used by chart directives)
- `Coach2Lead.Web/Content/site.css` (`.d3-tip` and tooltip style classes)
- `Coach2Lead.Web/Areas/Metrics/Angular/metrics/directives/indicator-trend-chart.js` (modern resize cleanup pattern)

## Standard Workflow
1. Pick the closest chart family first: bar/line axis chart, pie/radial chart, drag-based quadrant chart, or hierarchy chart.
2. Define directive contract with `restrict: 'EA'`, isolate `scope`, and a focused `link` orchestration.
3. Use chart lifecycle split:
   - `draw(element)` creates scales, root `svg.surface`, static groups, and tooltip attachment.
   - `update(scope)` binds data and performs incremental visual updates.
4. Rebuild the surface idempotently in `draw`:
   - `d3.select(element[0]).select('svg.surface').remove();`
   - append a fresh `svg.surface`.
5. Apply D3 data join update flow in `update`:
   - selection `.data(...)`
   - `.enter()` to create
   - `.merge(...)` to update
   - `.exit().remove()` for stale elements.
6. Add tooltip behavior with `d3.tip()`:
   - `.call(tooltip)` on the root surface.
   - show/hide on pointer events for bars, nodes, paths, and markers.
7. Wire redraw triggers:
   - scope watchers for data-bound properties.
   - layout event handling (`layoutChanged` or module-specific equivalent).
   - resize handling (prefer add/remove listener cleanup when possible).
8. Keep callbacks explicit through `&` bindings for click/dblclick/interaction outputs.

## Guardrails
- Keep `restrict: 'EA'` and isolate `scope` for chart directives.
- Preserve array-style AngularJS dependency injection.
- Do not introduce legacy APIs (`d3.layout.*`, `d3.svg.*`) in new chart work.
- Keep `draw` side effects self-contained and repeatable.
- Do not leave stale DOM on redraw; always remove previous `svg.surface`.
- Prefer explicit listener cleanup on `$destroy` for new or touched resize handlers.

## Validation
- Confirm frontmatter has valid `name` and a keyword-rich `description` (what + when).
- Confirm this section order remains canonical for C2L skills.
- Confirm each implementation includes:
  - `draw` + `update` lifecycle split,
  - isolate `scope`,
  - data join with exit cleanup,
  - tooltip wiring,
  - resize/layout redraw behavior.
- Confirm at least one modern cleanup pattern is considered when wiring resize listeners.
- Confirm legacy v3 files are not used as the primary template.

## Golden Example
Canonical modern pattern for a D3 directive with draw/update split and listener cleanup.
```javascript
(function(angular, d3) {
    'use strict';

    angular.module('example')
        .factory('ExampleChartCtrl', [function() {
            return function() {
                var surface, group, tooltip;

                this.draw = function(element) {
                    d3.select(element[0]).select('svg.surface').remove();

                    tooltip = d3.tip()
                        .attr('class', 'd3-tip')
                        .offset([-5, 0])
                        .html(function(event, d) { return d.label; });

                    surface = d3.select(element[0])
                        .append('svg')
                        .attr('class', 'surface')
                        .attr('width', element.width())
                        .attr('height', 240)
                        .call(tooltip);

                    group = surface.append('g').attr('class', 'chart-data');
                };

                this.update = function(scope) {
                    var points = scope.model || [];
                    var selection = group.selectAll('circle.point').data(points);

                    var enter = selection.enter()
                        .append('circle')
                        .attr('class', 'point')
                        .attr('r', 4)
                        .on('mouseover', function(event, d) { tooltip.show(event, d); })
                        .on('mouseout', function(event, d) { tooltip.hide(event, d); });

                    selection.merge(enter)
                        .transition(d3.transition().duration(250))
                        .attr('cx', function(d) { return d.x; })
                        .attr('cy', function(d) { return d.y; });

                    selection.exit().remove();
                };
            };
        }])
        .directive('exampleChart', ['$timeout', 'ExampleChartCtrl', function($timeout, ExampleChartCtrl) {
            return {
                restrict: 'EA',
                scope: { model: '=', onSelect: '&' },
                link: function(scope, element) {
                    var chart = new ExampleChartCtrl();
                    var redraw = function() { chart.draw(element); chart.update(scope); };
                    var onResize = function() { redraw(); };

                    scope.$watch('model', function() { chart.update(scope); }, true);
                    scope.$on('layoutChanged', function() { $timeout(redraw, 150); });
                    window.addEventListener('resize', onResize);
                    scope.$on('$destroy', function() { window.removeEventListener('resize', onResize); });

                    redraw();
                }
            };
        }]);
})(angular, d3);
```

## Output Contract
- Chart family selected and why it matches the requested visualization.
- Anchor file copied from and any deliberate deviations.
- Directive contract: scope bindings and callback outputs.
- Lifecycle wiring: what happens in `draw` vs `update`.
- Data join strategy (`enter`/`merge`/`exit`) and transition behavior.
- Resize/layout handling strategy and cleanup behavior.
- Any cross-layer impacts (if backend/API/schema changes are required, list them explicitly).

## Companion Skills
- `c2l-solution-orientation`
- `c2l-new-area-module`
- `c2l-build-run-debug`
- `c2l-breeze-webapi`
- `c2l-multi-tenancy-guards`

## Skill-Specific Topics
- Common modern variants to prefer:
  - Axis + bars/lines with factory-controller split (`EvaluationSkillChart`, `providerEvaluationSurveyEvolutionChart`, `riskSummaryChart`).
  - Pie/radial segment charts (`ManagementTaskChart`, `pieChart`).
  - Interactive drag quadrants (`processesRankingChart2`, `processRankingChart`).
  - Hierarchy tree using modern API (`organizationalChartTree`).
  - Safer resize cleanup pattern (`indicator-trend-chart`).
- Typical project conventions observed in modern directive contexts:
  - `restrict: 'EA'` and isolate scope are standard.
  - Most charts attach `d3.tip()` and use `.call(tooltip)` on root surface.
  - Most update paths use D3 transitions for animated state change.
- Legacy references (out of scope for primary guidance):
  - `Coach2Lead.Web/Areas/Processes/Angular/processes/directives/locationChart.js`
  - `Coach2Lead.Web/Areas/Tasks/Angular/tasks/directives/organizationalChartRadial.js`
  - `Coach2Lead.Web/Areas/Tasks/Angular/tasks/directives/organizationalChartCartesian.js`
  - These are compatibility references only; do not use as baseline for new chart implementations.
