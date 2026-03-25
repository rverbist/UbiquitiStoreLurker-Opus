# Coach2Lead - Copilot Instructions

Coach2Lead - multi-tenant SaaS for organizational coaching, competence/risk management, and EU AI Act compliance (AICMS).
.NET Framework 4.8 · ASP.NET MVC 5 · Web API 2 · AngularJS 1.8.2 · Breeze.js · EF6 Code First · SQL Server.
See `AGENTS.md` for complete agent instructions, build commands, project structure, and coding conventions.

## Critical Rules

- **NEVER** hand-author EF6 migration files - ALWAYS use the `ef6:add-migration` task.
- **NEVER** delete or modify existing migration files in `Coach2Lead.Web/Migrations/`.
- Every entity query **MUST** be company-scoped via `IHaveCompany`. NEVER bypass multi-tenancy.
- **NEVER** commit secrets, connection strings, or API keys.
- **NEVER** use Angular 2+ patterns in this AngularJS 1.x codebase.
- **ASK** before changing database schema, adding NuGet packages, or modifying shared infrastructure.

## Skills - Read Before Working

19 skills in `.github/skills/` - **read the relevant SKILL.md before domain-specific work:**

| Skill | Purpose |
|---|---|
| `c2l-solution-orientation` | Start here for codebase orientation |
| `c2l-build-run-debug` | Build, run, debug locally |
| `c2l-breeze-webapi` | Breeze ResourceController + ContextProvider |
| `c2l-ef6-migrations` | EF6 migration workflow |
| `c2l-ef6-models` | Entity design patterns |
| `c2l-controller-base-architecture` | Controller hierarchy and managers |
| `c2l-multi-tenancy-guards` | Tenant isolation enforcement |
| `c2l-new-area-module` | Scaffold new Area modules |
| `c2l-permissions-management` | Permission lifecycle |
| `c2l-feature-management` | Feature flags |
| `c2l-webjobs-routines` | Background jobs |
| `c2l-messaging-pipeline` | Mail/SMS pipeline |
| `c2l-d3-angularjs-directives` | D3 chart directives |
| `c2l-user-claims` | Identity claims contract |
| `c2l-eu-ai-act` | EU AI Act legal reference API |
| `c2l-repository-pattern` | ResourceManager data-access patterns |
| `c2l-chrome-devtools` | Browser automation for local testing |
| `c2l-gap-analysis` | Gap analysis and task planning |
| `c2l-skill-structure-index` | Skill structure validation and inventory sync |

## Knowledgebase

Deep-dive docs in `docs/knowledgebase/solution/`:
- `01-codebase-architecture.md` - Full architecture, tech stack, project structure
- `02-copilot-skills-inventory.md` - Complete skills inventory with descriptions
- `03-azure-production-environment.md` - Azure production resources
- `04-staging-nightly-environments.md` - On-prem staging/nightly environments
- `05-source-control-github.md` - GitHub repo metadata and conventions
