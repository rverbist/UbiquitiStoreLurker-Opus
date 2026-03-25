# Coach2Lead Messaging Troubleshooting

<!-- toc:start -->
<details>
  <summary>
   <strong>Table of Contents</strong> Click to open
  </summary>

- [Source-of-Truth Anchors](#source-of-truth-anchors)
- [Symptom-to-Cause Matrix](#symptom-to-cause-matrix)
- [Quick Smoke Test Recipes](#quick-smoke-test-recipes)
  - [SMTP test mail endpoint](#smtp-test-mail-endpoint)
  - [Proposal voting via mail link](#proposal-voting-via-mail-link)
  - [Proposal voting via SMS reply](#proposal-voting-via-sms-reply)
  - [Twilio callback ingestion](#twilio-callback-ingestion)
- [Safety Checklist](#safety-checklist)
- [Validation Scenarios](#validation-scenarios)

</details>
<!-- toc:end -->

## Source-of-Truth Anchors

- `Coach2Lead.Web/App/ApplicationMailService.cs`
- `Coach2Lead.Web/App/ApplicationTextService.cs`
- `Coach2Lead.Web/App/Jobs/SendMessagesRoutine.cs`
- `Coach2Lead.Web/App/Jobs/ReceiveMessagesRoutine.cs`
- `Coach2Lead.Web/App/Jobs/Meet and Act Management/MeetAndActManagementRoutine.cs`
- `Coach2Lead.Web/Controllers/Twilio/SmsController.cs`
- `Coach2Lead.Web/Areas/Manage/Controllers/API/ManageResourceController.cs`
- `Coach2Lead/Models/CompanySettings.cs`
- `Coach2Lead/Models/SmtpSettings.cs`
- `Coach2Lead/AppConfig.cs`
- `Coach2Lead.Web.Jobs/Functions.cs`

## Symptom-to-Cause Matrix

| Symptom | Likely cause | Verify | Fix direction |
| - | - | - | - |
| Mail queue grows and messages stay unsent | `SendMessagesRoutine` not running or send path errors | Check timer trigger/scheduler path and `MailMessage.Error` fields | Restore routine execution path; inspect and resolve recorded exceptions |
| Text queue grows and messages stay unsent | `SendMessagesRoutine` not running, text disabled, invalid recipients | Check `TextMessage.Error`, `DisableTextMessages`, recipient numbers | Re-enable text for company, correct numbers, and resolve send exceptions |
| Mail send fails with SMTP configuration error | Invalid default/client SMTP settings | Validate `SmtpSettings.ValidateSmtpSettings()` inputs | Correct SMTP host/port/user/password/sender and subject format |
| Mail sent but not to expected recipients | Environment redirect behavior active | Check `AppConfig.RedirectEmails` branch in send pipeline | Validate target environment assumptions; account for redirect behavior |
| SMS replies not tied to outbound message | `ReceiveMessagesRoutine` not running, response flags/window mismatch, phone mismatch | Check `TextInboundMessage.Handled`, outbound `ResponseExpected`, window bounds, recipient number equality | Ensure receive routine executes and response metadata/phone format are coherent |
| Reply correlated but proposal not updated | Proposal response handler not run or filtered out | Check `TextMessage.ResponseReceived`, `ResponseHandled`, `ResponseHandlerName`, `ResponseHandlerTag` | Ensure `MeetAndActManagementRoutine` runs and handler metadata is correct |
| Proposal vote options parsed incorrectly from SMS | Reply body does not contain expected digits | Inspect `Regex.Matches(message.Response.Body, "\\d")` behavior | Adjust user guidance text or parsing strategy in handler code |
| Queue messages silently not sent in test environment | Environment disables send (`IsMailEnabled` / `IsTextEnabled`) | Verify `AppConfig.Environment` and send gate flags | Use staging/nightly/prod-like setup for transport validation |

## Quick Smoke Test Recipes

### SMTP test mail endpoint

1. Authenticate as a user with access to Manage API actions.
2. Trigger `SendSmtpTestMail` in `ManageResourceController`.
3. Confirm queue insertion:
   - new `MailMessage` row with your user as recipient and `CompanyId` set.
4. Wait for send routine cadence.
5. Confirm terminal state:
   - success: `Sent = true`, `SentOn` populated
   - failure: `Error = true` with exception metadata.

### Proposal voting via mail link

1. Generate proposal notification flow that sends proposal mail.
2. Open rendered mail content (`/Mail/Proposal/{participantId}`) and capture vote link.
3. Open vote page (`/Meetings/Proposal/Vote/{participantId}?key={resource}`).
4. Submit vote.
5. Verify:
   - vote persisted through `ProposalController`
   - proposal recalculated and hub notified.

### Proposal voting via SMS reply

1. Ensure participant has phone number and proposal notify path queues text.
2. Verify outbound `TextMessage` values:
   - `ResponseExpected = true`
   - response window set
   - handler name/tag set for proposal.
3. Wait for send and confirm `Sent = true`.
4. Submit Twilio callback with a numeric reply body.
5. Verify correlation:
   - inbound row `Handled = true`
   - outbound `ResponseReceived = true` and `ResponseId` set.
6. Wait for `MeetAndActManagementRoutine` cycle.
7. Verify vote processing:
   - proposal votes updated
   - outbound `ResponseHandled = true`.

### Twilio callback ingestion

1. POST a valid Twilio-style `SmsRequest` payload to `/Sms/Callback`.
2. Confirm immediate persistence of `TextInboundMessage` row:
   - fields mapped correctly
   - `Handled = false` initially.
3. Wait for receive routine.
4. Confirm row transitions to `Handled = true` and correlation is applied when eligible.

## Safety Checklist

- Do not document or commit secret values; use key names only.
- Preserve tenant/company boundaries in queries and message creation paths.
- Preserve company-level delivery gates:
  - `DisableMailMessages`
  - `DisableTextMessages`
  - `SmtpServiceEnabled`.
- Keep queue-first architecture:
  - producers enqueue
  - consumer routine sends.
- Preserve response idempotency:
  - inbound `Handled`
  - outbound `ResponseReceived` / `ResponseHandled`.

## Validation Scenarios

1. Queue send scenario:
   - trace `MailMessage` from queue insertion to send success/failure flags.
2. Twilio inbound scenario:
   - trace callback persistence and receive routine correlation.
3. Proposal vote scenario:
   - trace SMS reply parse and vote updates in proposal manager path.
4. Settings override scenario:
   - verify default SMTP vs company SMTP behavior and disable flags.
5. Workflow producer scenario:
   - verify `MailMessageWorkflow` and `TextMessageWorkflow` generate queue records.
6. Entity wrapper scenario:
   - verify `EntityMailMessage` / `EntityTextMessage` query surfaces include recipients/attachments as expected.
