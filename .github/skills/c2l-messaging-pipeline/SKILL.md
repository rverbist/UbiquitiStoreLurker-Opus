---
name: c2l-messaging-pipeline
description: 'Document, trace, and troubleshoot Coach2Lead mail and text messaging end-to-end, including company-scoped message aggregates, SMTP settings resolution, queue/send routines, Twilio inbound callback handling, proposal SMS vote processing, and mail view rendering. Use when debugging undelivered or unprocessed messages, explaining messaging architecture, or implementing mail/text pipeline changes.'
---

# Coach2Lead Messaging Pipeline

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
- Explain and troubleshoot Coach2Lead messaging across model, queue, transport, and response-handling layers.
- Preserve company scoping, queue semantics, and response idempotency when changing mail/text behavior.
- Help agents quickly locate where messages are created, queued, rendered, sent, and reconciled.

## Read First
- `references/messaging-domain-map.md`
- `references/messaging-runtime-flows.md`
- `references/messaging-troubleshooting.md`

## Core Anchors
- Models
  - `Coach2Lead/Models/MailMessage.cs`
  - `Coach2Lead/Models/MailAttachment.cs`
  - `Coach2Lead/Models/MailRecipient.cs`
  - `Coach2Lead/Models/RecipientType.cs`
  - `Coach2Lead/Models/MailTemplate.cs`
  - `Coach2Lead/Models/MailTemplateAttachment.cs`
  - `Coach2Lead/Models/MailTemplateRecipient.cs`
  - `Coach2Lead/Models/TextMessage.cs`
  - `Coach2Lead/Models/TextInboundMessage.cs`
  - `Coach2Lead/Models/TextRecipient.cs`
  - `Coach2Lead/Models/SmtpSettings.cs`
  - `Coach2Lead/Models/CompanySettings.cs`
- Concrete template example
  - `Coach2Lead/Models/Kaizen/LessonsLearnedMailTemplate.cs`
- Services and routines
  - `Coach2Lead.Web/App/ApplicationMailService.cs`
  - `Coach2Lead.Web/App/ApplicationTextService.cs`
  - `Coach2Lead.Web/App/Services/Coach2LeadMailService.cs`
  - `Coach2Lead.Web/App/Jobs/SendMessagesRoutine.cs`
  - `Coach2Lead.Web/App/Jobs/ReceiveMessagesRoutine.cs`
  - `Coach2Lead.Web/App/Jobs/Meet and Act Management/MeetAndActManagementRoutine.cs`
  - `Coach2Lead.Web/App/Jobs/SendFeedbackRoutine.cs`
- Controllers and views
  - `Coach2Lead.Web/Controllers/Twilio/SmsController.cs`
  - `Coach2Lead.Web/Controllers/MailController.cs`
  - `Coach2Lead.Web/Areas/Meetings/Controllers/ProposalController.cs`
  - `Coach2Lead.Web/Views/Mail/Proposal.cshtml`
  - `Coach2Lead.Web/Areas/Meetings/Views/Proposal/Vote.cshtml`
- Scheduling and config
  - `Coach2Lead.Web.Jobs/Functions.cs`
  - `Coach2Lead.Web.Jobs/CronExpression.cs`
  - `Coach2Lead/AppConfig.cs`
  - `Coach2Lead.Web/Web.config`
- Ancillary ecosystem
  - `Coach2Lead/Models/Workflows/MailMessageWorkflow.cs`
  - `Coach2Lead/Models/Workflows/TextMessageWorkflow.cs`
  - `Coach2Lead/Entity/EntityMailMessage.cs`
  - `Coach2Lead/Entity/EntityTextMessage.cs`
  - `Coach2Lead.Web/App/Domain/Entity/EntityMailMessageQuery.cs`
  - `Coach2Lead.Web/App/Domain/Entity/EntityTextMessageQuery.cs`
  - `Coach2Lead.Web/App/Domain/Entity/ResourceControllerBase.cs`
  - `Coach2Lead.Web/Areas/Manage/Controllers/API/ManageResourceController.cs`

## Standard Workflow
1. Start from symptom type: outbound delivery, inbound processing, or proposal response handling.
2. Trace producer path first:
   - Mail: `Coach2LeadMailService` or workflow builders create `MailMessage`.
   - Text: routines/workflows create `TextMessage`.
3. Confirm queue persistence and flags (`QueuedOn`, `Sent`, `Error`, response flags).
4. Confirm consumer cadence in `Functions.cs`:
   - `SendMessages` drains outbound queues.
   - `ReceiveMessages` links inbound texts to outbound messages expecting responses.
5. Verify transport/config branch decisions:
   - Mail: default SMTP vs company SMTP vs Graph API special branch.
   - Text: Twilio send behavior and environment redirect/disable logic.
6. For proposal voting, follow full chain:
   - proposal text enqueue -> send -> Twilio callback -> inbound correlation -> vote handler.
7. Validate by smoke-testing from queue insertion to terminal state and checking exception fields if failed.

## Guardrails
- Preserve queue-consumer pattern: produce with `QueueMail` / `QueueText`; let `SendMessagesRoutine` send.
- Respect company-level gates in `CompanySettings` (`DisableMailMessages`, `DisableTextMessages`, `SmtpServiceEnabled`).
- Keep response idempotency flags coherent:
  - `TextInboundMessage.Handled`
  - `TextMessage.ResponseReceived`
  - `TextMessage.ResponseHandled`
- Do not place secrets in docs or code comments; document only config key names.
- Do not bypass proposal response correlation logic in `ReceiveMessagesRoutine` and handler logic in `MeetAndActManagementRoutine`.

## Validation
- Core anchor coverage: all listed files are referenced in docs or workflow notes.
- Section order matches canonical `c2l-*` structure.
- Troubleshooting matrix includes queue, SMTP, Twilio callback, and proposal vote scenarios.
- Transport documentation includes environment behavior (`IsMailEnabled`, `RedirectEmails`, `IsTextEnabled`, `RedirectText`).
- Reference docs clearly separate:
  - domain model map
  - runtime flow map
  - troubleshooting and smoke tests

## Golden Example
Full inbound proposal vote path:
1. `MeetAndActManagementRoutine.SendCreateProposalText(...)` builds a `TextMessage`:
   - recipient(s)
   - `ResponseExpected = true`
   - response window
   - `ResponseHandlerName = "ProposalVoteHandler"`
   - `ResponseHandlerTag = proposalId`
2. `ApplicationTextService.QueueText(...)` persists queue record.
3. `SendMessagesRoutine` calls `ApplicationTextService.SendQueuedTextMessages()` and Twilio sends the SMS.
4. Twilio posts to `SmsController.Callback(...)`, which stores `TextInboundMessage` with `Handled = false`.
5. `ReceiveMessagesRoutine` finds unhandled inbound messages and links each to the latest eligible outbound `TextMessage` by sender number and response window, then sets:
   - `TextMessage.ResponseReceived = true`
   - `TextMessage.Response = inbound`
   - `TextInboundMessage.Handled = true`
6. `MeetAndActManagementRoutine.RunProposalVoteHandler()` processes linked responses:
   - parse selected option digits from inbound body
   - resolve proposal via `ResponseHandlerTag`
   - clear prior participant votes
   - apply new votes and recalculate
   - notify proposal hub
   - set `TextMessage.ResponseHandled = true`

## Output Contract
- Source-aligned explanation of:
  - message aggregates and flags
  - queue producers and consumers
  - Twilio callback ingestion and correlation
  - proposal vote response handling
  - mail rendering through MVC views
- Concrete file anchors for each claim.
- Troubleshooting guidance that maps symptoms to specific code paths and flags.

## Companion Skills
- `c2l-webjobs-routines`
- `c2l-multi-tenancy-guards`
- `c2l-solution-orientation`
- `c2l-repository-pattern`

## Skill-Specific Topics
- Mail body generation is MVC view rendering via `/Mail/*` routes consumed by `Coach2LeadMailService`.
- Proposal voting supports two channels:
  - email link to `Meetings/Proposal/Vote`
  - SMS reply pipeline handled asynchronously by routines.
- SMTP settings resolve by company settings when enabled; otherwise defaults are taken from `AppConfig` keys.
- Twilio inbound data is persisted first and handled asynchronously; callback does not perform proposal voting directly.
- Ancillary model surfaces (`EntityMailMessage`, `EntityTextMessage`, workflow message definitions) are part of the ecosystem and should be included in root-cause analysis for message lineage.
