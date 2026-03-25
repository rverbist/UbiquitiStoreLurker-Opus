# Coach2Lead Messaging Runtime Flows

<!-- toc:start -->
<details>
  <summary>
   <strong>Table of Contents</strong> Click to open
  </summary>

- [Source-of-Truth Anchors](#source-of-truth-anchors)
- [Outbound Mail Flow](#outbound-mail-flow)
- [Outbound Text Flow](#outbound-text-flow)
- [Inbound SMS Flow](#inbound-sms-flow)
- [Proposal Voting by SMS Flow](#proposal-voting-by-sms-flow)
- [Timer Cadence](#timer-cadence)
- [WebJobs Host vs Self-Host Background Path](#webjobs-host-vs-self-host-background-path)

</details>
<!-- toc:end -->

## Source-of-Truth Anchors

- `Coach2Lead.Web/App/Services/Coach2LeadMailService.cs`
- `Coach2Lead.Web/App/ApplicationMailService.cs`
- `Coach2Lead.Web/App/ApplicationTextService.cs`
- `Coach2Lead.Web/App/Jobs/SendMessagesRoutine.cs`
- `Coach2Lead.Web/App/Jobs/ReceiveMessagesRoutine.cs`
- `Coach2Lead.Web/App/Jobs/Meet and Act Management/MeetAndActManagementRoutine.cs`
- `Coach2Lead.Web/App/Jobs/SendFeedbackRoutine.cs`
- `Coach2Lead.Web/Controllers/Twilio/SmsController.cs`
- `Coach2Lead.Web/Controllers/MailController.cs`
- `Coach2Lead.Web/Views/Mail/Proposal.cshtml`
- `Coach2Lead.Web/Areas/Meetings/Controllers/ProposalController.cs`
- `Coach2Lead.Web.Jobs/Functions.cs`
- `Coach2Lead.Web.Jobs/CronExpression.cs`
- `Coach2Lead.Web/Global.asax.cs`
- `Coach2Lead/AppConfig.cs`

## Outbound Mail Flow

1. Domain producer creates mail payload:
   - Most domain notifications call `Coach2LeadMailService` methods.
   - Content is fetched from MVC mail endpoints (`/Mail/*`) rendered by `MailController`.
2. Producer populates `MailMessage`:
   - recipients (`To`, `Cc`, `Bcc`)
   - subject and body
   - `CompanyId`
   - optional attachments.
3. Producer queues using `ApplicationMailService.QueueMail(...)`:
   - sets `QueuedOn = DateTime.UtcNow`
   - persists to DB.
4. `SendMessagesRoutine` executes and calls `ApplicationMailService.SendQueuedMailMessages()`.
5. Mail service branches queue draining:
   - default SMTP path for companies without client SMTP enabled
   - client SMTP path grouped per company with per-company settings/rate limits.
6. `SendMail(id)` loads company settings, recipients, and attachments.
7. Transport decision:
   - special company branch can use Graph API
   - otherwise `CreateSmtpMessage(...)` + `SendMailUsingSmtp(...)`.
8. Status persistence:
   - success: `Sent = true`, `SentOn` set
   - failure: `Error = true` and exception fields populated.

## Outbound Text Flow

1. Producer creates `TextMessage` in routine/service/workflow code.
2. Producer queues via `ApplicationTextService.QueueText(...)`:
   - sets `QueuedOn = DateTime.UtcNow`
   - persists record.
3. `SendMessagesRoutine` calls `ApplicationTextService.SendQueuedTextMessages()`.
4. Text service loads unsent, non-error, text-enabled records and calls `SendText(id)`.
5. `SendText(id)`:
   - validates company text settings
   - validates recipient numbers
   - sends each recipient via Twilio (`MessageResource.CreateAsync`).
6. Status persistence:
   - success: `Sent = true`, `SentOn` set
   - failure: `Error = true` and exception fields populated.

## Inbound SMS Flow

1. Twilio posts inbound request to MVC action `SmsController.Callback`.
2. Controller stores raw payload as `TextInboundMessage` with `Handled = false`.
3. `ReceiveMessagesRoutine` (timer-driven) loads unhandled inbound rows.
4. For each inbound row, routine finds latest matching outbound `TextMessage`:
   - `Sent == true`
   - `ResponseExpected == true`
   - `ResponseReceived == false`
   - inside response window (if configured)
   - recipient number matches inbound `From`.
5. If match exists:
   - sets `message.ResponseReceived = true`
   - sets `message.Response = inbound`.
6. Marks inbound row as handled:
   - `TextInboundMessage.Handled = true`.
7. Persists all changes.

## Proposal Voting by SMS Flow

1. `MeetAndActManagementRoutine.RunProposalNotifications()` selects not-yet-notified proposal participants.
2. For participants with phone numbers, `SendCreateProposalText(...)` builds an outbound `TextMessage`:
   - options list in message body
   - `ResponseExpected = true`
   - response window from now to now + 2 hours
   - `ResponseHandlerName = "ProposalVoteHandler"`
   - `ResponseHandlerTag = proposalId`.
3. Message is queued (`QueueText`) and later sent by `SendMessagesRoutine`.
4. Participant replies by SMS; Twilio callback persists inbound row.
5. `ReceiveMessagesRoutine` correlates inbound response to outbound proposal text and sets `ResponseReceived`.
6. `MeetAndActManagementRoutine.RunProposalVoteHandler()` processes received responses:
   - selects messages where `ResponseReceived && !ResponseHandled`
   - filters `ResponseHandlerName == "ProposalVoteHandler"`
   - parses digits from inbound body into selected options
   - resolves proposal by `ResponseHandlerTag`
   - finds participant by phone number
   - clears prior votes, applies new votes, recalculates proposal totals, notifies hub
   - sets `ResponseHandled = true`.

## Timer Cadence

From `CronExpression` and `Functions`:
- `EveryThirtySeconds` (`0,30 * * * * *`):
  - `ReceiveMessages`
  - `SendMessages`
  - `CollectInactiveSessions`
  - `FeedbackManagementRoutine`
- `EveryMinute` (`0 * * * * *`):
  - domain routines including `MeetAndActManagementRoutine` and others.
Messaging impact:
- Outbound queue drain cadence is every 30 seconds.
- Inbound correlation cadence is every 30 seconds.
- Proposal response handling runs in `MeetAndActManagementRoutine` every minute.

## WebJobs Host vs Self-Host Background Path

Primary path:
- `Coach2Lead.Web.Jobs` host executes `Functions` timer triggers using cron expressions.
Optional self-host path (web app process):
- `Global.asax` starts an in-process timer when `AppConfig.SelfHostBackgroundJobs` is true.
- That timer runs a routine list including `ReceiveMessagesRoutine`, `SendMessagesRoutine`, and `MeetAndActManagementRoutine`.
- Self-host interval is controlled by `AppConfig.RunBackgroundJobsTimerInterval` (one minute).
Operational note:
- WebJobs schedule is authoritative for timer expressions.
- Self-host path is environment-controlled fallback/alternative execution surface.
