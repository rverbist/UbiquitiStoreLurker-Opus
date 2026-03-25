# Coach2Lead background routines map

<!-- toc:start -->
<details>
  <summary>
   <strong>Table of Contents</strong> Click to open
  </summary>

- [Source-of-truth anchors](#source-of-truth-anchors)
- [Runtime topology](#runtime-topology)
- [Reusable routine skeleton](#reusable-routine-skeleton)
- [Pattern catalog](#pattern-catalog)
  - [1. Status-change notification producer](#1-status-change-notification-producer)
  - [2. Domain mail producer](#2-domain-mail-producer)
  - [3. Queue consumer](#3-queue-consumer)
  - [4. Diagnose execution path](#4-diagnose-execution-path)
- [EF Include vs Breeze expand](#ef-include-vs-breeze-expand)
  - [Server routine (EF6 Include)](#server-routine-ef6-include)
  - [Client query (BreezeJS expand)](#client-query-breezejs-expand)
- [Troubleshooting matrix](#troubleshooting-matrix)
- [Quick validation checklist for new routine docs/code](#quick-validation-checklist-for-new-routine-docscode)
- [Test cases and scenarios](#test-cases-and-scenarios)

</details>
<!-- toc:end -->
Use this file as the detailed technical map for `c2l-webjobs-routines`.

## Source-of-truth anchors

| Anchor | Responsibility |
| - | - |
| `Coach2Lead.Web/App/Jobs/ICoach2LeadRoutine.cs:10` | Routine contract (`Log` + `Run`). |
| `Coach2Lead.Web.Jobs/Functions.cs:16` | Shared routine execution wrapper with exception handling and error persistence. |
| `Coach2Lead.Web.Jobs/CronExpression.cs:5` | Central cron expression constants. |
| `Coach2Lead.Web.Jobs/Program.cs:13` | WebJobs host setup (`UseTimers`, `RunAndBlock`). |
| `Coach2Lead.Web/Global.asax.cs:46` | Self-host timer setup for in-web-app execution path. |
| `Coach2Lead/AppConfig.cs:77` | `SelfHostBackgroundJobs` and background timer interval. |
| `Coach2Lead.Web/Controllers/DiagnoseController.cs:53` | Manual admin routine runner (`Jobs`). |
| `Coach2Lead.Web/App/Jobs/Action Plan Management/ActionPlanManagementRoutine.cs:120` | Active-account-scoped query with EF Include and notification/mail flow. |
| `Coach2Lead.Web/App/Jobs/SendMessagesRoutine.cs:13` | Queue consumer routine that drains outbound mail/text queues. |
| `Coach2Lead.Web/App/ApplicationMailService.cs:73` | Mail queue send pipeline entry point. |
| `Coach2Lead.Web/App/ApplicationTextService.cs:95` | Text queue send pipeline entry point. |
| `Coach2Lead.Web/App/Services/Coach2LeadMailService.cs:22` | Domain-specific mail producer pattern (`QueueMail` from rendered content). |
| `Coach2Lead.Web/Areas/Surveys/Angular/coach2lead/factories/ResourceFactory.js:22` | BreezeJS client-side `.expand(include)` pattern. |

## Runtime topology

1. Producer routines run on schedule and queue side effects.
2. Queue consumer routine (`SendMessagesRoutine`) sends queued mail/text.
3. WebJobs host is the primary execution path.
4. Optional self-host loop exists in web app (`Global.asax`) for selected environments.
5. Diagnose controller provides manual execution path for admin smoke testing.

## Reusable routine skeleton

```csharp
public class ExampleRoutine : Coach2LeadRoutine
{
    public override async Task Run()
    {
        using var context = ApplicationDbContext.Create();

        var items = await context.Set<ApplicationAccount>()
                                 .Where(account => account.IsActive)
                                 .SelectMany(account => account.Company.Actions)
                                 .Where(a => !a.Notified)
                                 .Include(a => a.AssignedToGroup.Select(l => l.AssignedTo))
                                 .ToArrayAsync();

        foreach (var item in items)
        {
            // In-app notification
            foreach (var link in item.AssignedToGroup)
            {
                context.Set<Coach2LeadNotification>().Add(new Coach2LeadNotification
                {
                    Person = link.AssignedTo,
                    SourceType = "ActionPlanItem",
                    SourceId = item.Id,
                    Date = DateTime.UtcNow,
                    Title = "Example notification",
                    Message = "Example background routine notification."
                });
            }

            // Queue outbound message (producer)
            await Coach2LeadMailService.SendActionReminderMail(item);

            // Idempotency flag
            item.Notified = true;
        }

        await context.SaveChangesAsync();
    }
}
```
Notes:
1. Use EF `Include(...)` for relationships needed in routine logic.
2. Queue mail/text in producer routines; do not bypass queue consumer model unless explicitly required.
3. Always set and persist idempotency flags.

## Pattern catalog

### 1. Status-change notification producer

Reference: `Coach2Lead.Web/App/Jobs/Action Plan Management/ActionPlanManagementRoutine.cs:120`
What it shows:
1. Root tenant scope via active accounts.
2. EF Includes for role-linked recipients.
3. Combined in-app notification + queued mail.
4. State flags + save to prevent duplicates.

### 2. Domain mail producer

Reference: `Coach2Lead.Web/App/Services/Coach2LeadMailService.cs:22`
What it shows:
1. Build content from `/Mail/*` endpoints.
2. Build `MailMessage` recipient set.
3. Queue through `ApplicationMailService.QueueMail`.

### 3. Queue consumer

References:
1. `Coach2Lead.Web/App/Jobs/SendMessagesRoutine.cs:13`
2. `Coach2Lead.Web/App/ApplicationMailService.cs:73`
3. `Coach2Lead.Web/App/ApplicationTextService.cs:95`
What it shows:
1. Dedicated routine drains queued outbound mail/text.
2. Transport send behavior is centralized in services.

### 4. Diagnose execution path

Reference: `Coach2Lead.Web/Controllers/DiagnoseController.cs:53`
What it shows:
1. Manual run entry for admin.
2. Useful for smoke checks when validating a new routine.

## EF Include vs Breeze expand

Use the right pattern in the right layer.

### Server routine (EF6 Include)

```csharp
var reminders = await context.Set<ApplicationAccount>()
                             .Where(account => account.IsActive)
                             .SelectMany(account => account.Company.Actions)
                             .Include(a => a.AssignedToGroup.Select(l => l.AssignedTo))
                             .ToArrayAsync();
```
Pattern source: `Coach2Lead.Web/App/Jobs/Action Plan Management/ActionPlanManagementRoutine.cs:120`

### Client query (BreezeJS expand)

```javascript
const query = new breeze.EntityQuery('Modules')
    .expand(include);
```
Pattern source: `Coach2Lead.Web/Areas/Surveys/Angular/coach2lead/factories/ResourceFactory.js:22`
Rule:
1. Use EF `Include(...)` in C# server routines.
2. Use Breeze `.expand(...)` in Angular client query composition.
3. Do not assume one API behaves like the other.

## Troubleshooting matrix

| Symptom | Likely cause | Verify | Fix |
| - | - | - | - |
| Duplicate notifications or mails | Missing idempotency flags or flags not persisted | Check `Notified`/`Sent`/`Handled` transitions and `SaveChangesAsync()` | Set flags inside loop and persist changes |
| Routine never fires | Missing TimerTrigger wiring or wrong schedule | Check `Functions.cs` trigger method and `CronExpression` value | Add trigger method and correct cron constant |
| Queue keeps growing | Consumer routine not running | Check `SendMessagesRoutine` trigger and runtime host | Ensure consumer trigger runs in target environment |
| Missing related data in routine | Missing EF `Include(...)` | Compare access path vs query includes | Add required Include paths |
| Works in Diagnose but not on schedule | Manual path updated but TimerTrigger path not updated | Compare `DiagnoseController.Jobs` vs `Functions.cs` registration | Register routine in timer host |
| Tenant leakage risk | Query not rooted through active accounts | Inspect query root for `ApplicationAccount.IsActive` chain | Root company data queries through active accounts |

## Quick validation checklist for new routine docs/code

1. Routine class implements `ICoach2LeadRoutine`.
2. Trigger exists in `Functions.cs` and uses `Run(routine)`.
3. Cron schedule is explicit and reviewable.
4. Company-scoped data starts from active-account chain.
5. Notifications and queue writes are idempotent.
6. Queue consumer path is known and enabled.
7. Diagnose path is updated when manual smoke testing is needed.

## Test cases and scenarios

1. Skill trigger test:
   - Prompts like "add background routine", "TimerTrigger", "WebJobs routine", "queue mail from routine", and "Include vs Breeze expand" should map to this skill.
2. Traceability test:
   - Each key requirement maps to at least one explicit section plus a source anchor in this file or `SKILL.md`.
3. Consumer usability test:
   - A consumer should be able to implement a new routine from the documented workflow without making architecture decisions.
4. Safety test:
   - Company-scoped routine queries should demonstrate active-account root scoping.
5. Queue semantics test:
   - Documentation should clearly separate producer routines (queue) from consumer routines (send/drain).
6. Formatting/validity test:
   - Skill frontmatter remains valid (`name`, `description` only), relative reference path is correct, and links/anchors are readable.
