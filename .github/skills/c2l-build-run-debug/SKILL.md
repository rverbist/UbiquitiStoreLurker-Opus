---
name: c2l-build-run-debug
description: 'Build, run, and debug Coach2Lead locally on Windows using repo tasks/scripts (MSBuild restore/build, IIS Express HTTPS run, WebJobs host run, and CLI run). Use when setting up local execution, reproducing runtime bugs, or verifying implementation changes.'
---

# Coach2Lead Build Run Debug

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
- Produce a repeatable local run path for Web, Jobs, and CLI.
- Confirm the change under the correct runtime configuration.

## Read First
- `.vscode/tasks.json`
- `.vscode/launch.json`
- `.vscode/scripts/msbuild/Invoke-MSBuild.ps1`

## Core Anchors
- `Coach2Lead.sln`
- `Coach2Lead.Web/Global.asax.cs`
- `Coach2Lead.Web.Jobs/Program.cs`
- `Coach2Lead.Web.Jobs/Functions.cs`
- `Coach2Lead.Web.Jobs/CronExpression.cs`
- `Coach2Lead/AppConfig.cs`

## Standard Workflow
1. Run `msbuild:restore`.
2. Run `msbuild:build` (default `Debug|Any CPU`).
3. Run `iisexpress:start` (it already depends on `iisexpress:ensure-config` and `iisexpress:setup-ssl`).
4. Open `https://localhost:44300/` and test the target flow.
5. If required, run `Coach2Lead.Web.Jobs` and verify trigger wiring in `Functions.cs`.
6. If required, run `Coach2Lead CLI` launch profile.

## Guardrails
- Keep `Platform="Any CPU"` unless you intentionally update solution settings.
- Do not commit local secrets/connection strings.
- Treat build/runtime errors as environment or config issues first, not immediate code defects.

## Validation
- Build passes without restore gaps.
- Web starts on expected HTTPS URL.
- Target flow reproduces and/or resolves.
- Jobs/CLI path is verified when touched.

## Golden Example
Minimal debug loop for a web bug.
1. `msbuild:restore`.
2. `msbuild:build`.
3. Start IIS Express and attach launch profile `Coach2Lead Web (IIS Express)`.
4. Reproduce at `https://localhost:44300/`.
5. Capture failing controller/action and map to Area files before patching.

## Output Contract
- Commands/tasks executed.
- URL/entrypoint used.
- Config assumptions (environment/database).
- Reproduction result or fix verification result.

## Companion Skills
- `c2l-solution-orientation`
- `c2l-webjobs-routines`
- `c2l-ef6-migrations`

## Skill-Specific Topics
- `Invoke-MSBuild.ps1` uses `/p:RestorePackagesConfig=true` for `packages.config` restore flow.
- If jobs appear idle, verify both cron constant and `Functions.cs` trigger method mapping.
- **`"Any CPU"` vs `AnyCPU`**: MSBuild uses two distinct identifiers for the same logical platform:
  - `.sln` CLI (`/p:Platform=...`): `"Any CPU"` - with a space, must be quoted in shell. Passing `AnyCPU` (no space) silently matches no solution configuration.
  - `.csproj` XML (`<Platform>`, `<PlatformTarget>`, and PropertyGroup `Condition` attributes): `AnyCPU` - no space. This is the MSBuild project format convention.
  - MSBuild maps between them automatically when transitioning from solution to project evaluation.
