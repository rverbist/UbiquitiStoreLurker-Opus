---
name: c2l-webjobs-routines
description: 'Design, implement, and troubleshoot Coach2Lead background routines across WebJobs and self-host execution paths, including TimerTrigger wiring, active-account scoping, notification and queue side effects, and EF Include usage. Use when adding or debugging scheduled routines.'
---

# Coach2Lead WebJobs Routines

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
- Deliver background routines that are schedule-correct, tenant-safe, and idempotent.
- Keep producer/consumer side effects (notifications, mail/text) consistent.

## Read First
- `references/background-routines-map.md`

## Core Anchors
- `Coach2Lead.Web/App/Jobs/ICoach2LeadRoutine.cs`
- `Coach2Lead.Web.Jobs/Functions.cs`
- `Coach2Lead.Web.Jobs/CronExpression.cs`
- `Coach2Lead.Web.Jobs/Program.cs`
- `Coach2Lead.Web/Global.asax.cs`
- `Coach2Lead.Web/Controllers/DiagnoseController.cs`
- `Coach2Lead.Web/App/Services/Coach2LeadMailService.cs`
- `Coach2Lead.Web/App/Jobs/SendMessagesRoutine.cs`

## Standard Workflow
1. Create routine class in `Coach2Lead.Web/App/Jobs/` implementing `Run()`.
2. Acquire context with `ApplicationDbContext.Create()`.
3. Scope company data through active-account root query where applicable.
4. Load required relations with EF `Include(...)`.
5. Apply side effects (notifications, queue writes) and set idempotency flags.
6. `SaveChangesAsync()`.
7. Wire trigger in `Functions.cs` with shared `Run(routine)` wrapper and cron constant.
8. Add self-host/diagnose registration when required.

## Guardrails
- Avoid duplicate side effects by persisting idempotency flags.
- Do not bypass queue model when queue-based delivery is expected.
- Do not mix Breeze client `.expand(...)` assumptions with server routine query code.

## Validation
- Trigger wiring and cron value verified.
- Tenant scoping verified for company data.
- Duplicate send/notify protection verified.
- Manual diagnose path tested when used.

## Golden Example
Routine loop pattern for tenant-safe processing.
```csharp
using var context = ApplicationDbContext.Create();
var items = await context.Set<ApplicationAccount>()
    .Where(a => a.IsActive)
    .SelectMany(a => a.Company.Actions)
    .Where(x => !x.Notified)
    .ToArrayAsync();

foreach (var item in items)
{
    item.Notified = true;
}

await context.SaveChangesAsync();
```

## Output Contract
- Routine class + trigger mapping.
- Schedule and execution surfaces updated.
- Tenant safety and idempotency strategy.
- Side effects summary (notifications/mail/text).

## Companion Skills
- `c2l-build-run-debug`
- `c2l-multi-tenancy-guards`
- `c2l-repository-pattern`

## Skill-Specific Topics
- Producer routines should queue outbound messages; `SendMessagesRoutine` drains queues.
- WebJobs host is primary scheduler; self-host path is fallback/config-driven.
- Use EF `Include(...)` in routines, Breeze `.expand(...)` only in client queries.
