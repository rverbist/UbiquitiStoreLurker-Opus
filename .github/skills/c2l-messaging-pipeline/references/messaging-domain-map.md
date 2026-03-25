# Coach2Lead Messaging Domain Map

<!-- toc:start -->
<details>
  <summary>
   <strong>Table of Contents</strong> Click to open
  </summary>

- [Source-of-Truth Anchors](#source-of-truth-anchors)
- [Aggregate Map](#aggregate-map)
  - [MailMessage Aggregate (company-scoped)](#mailmessage-aggregate-company-scoped)
  - [MailTemplate Aggregate (company-scoped)](#mailtemplate-aggregate-company-scoped)
  - [TextMessage Aggregate (company-scoped)](#textmessage-aggregate-company-scoped)
- [Field and Flag Semantics](#field-and-flag-semantics)
- [Company Scoping Notes](#company-scoping-notes)
- [Config Key Inventory (names only)](#config-key-inventory-names-only)
- [Twilio Callback Mapping](#twilio-callback-mapping)
- [Related Ecosystem Models](#related-ecosystem-models)

</details>
<!-- toc:end -->

## Source-of-Truth Anchors

- `Coach2Lead/Models/MailMessage.cs`
- `Coach2Lead/Models/MailAttachment.cs`
- `Coach2Lead/Models/MailRecipient.cs`
- `Coach2Lead/Models/RecipientType.cs`
- `Coach2Lead/Models/MailTemplate.cs`
- `Coach2Lead/Models/MailTemplateAttachment.cs`
- `Coach2Lead/Models/MailTemplateRecipient.cs`
- `Coach2Lead/Models/Kaizen/LessonsLearnedMailTemplate.cs`
- `Coach2Lead/Models/TextMessage.cs`
- `Coach2Lead/Models/TextInboundMessage.cs`
- `Coach2Lead/Models/TextRecipient.cs`
- `Coach2Lead/Models/SmtpSettings.cs`
- `Coach2Lead/Models/CompanySettings.cs`
- `Coach2Lead/Models/Company.cs`
- `Coach2Lead.Web/Controllers/Twilio/SmsController.cs`
- `Coach2Lead/AppConfig.cs`
- `Coach2Lead.Web/Web.config`

## Aggregate Map

### MailMessage Aggregate (company-scoped)

| Component | Type | Purpose |
| - | - | - |
| `MailMessage` | root entity | Queued/sent mail payload with status and exception fields. |
| `MailRecipient` | child | Recipient address/name and `RecipientType` (`To`, `Cc`, `Bcc`, `From`). |
| `MailAttachment` | child | Attachment bytes or server path pointer. |
| `Company.Mails` | aggregate root relation | Company-level collection for outbound emails. |
Core fields on `MailMessage`:
- Identity and ownership: `Id`, `CompanyId`, `Company`.
- Payload: `Subject`, `Body`.
- Queue lifecycle: `QueuedOn`, `SentOn`.
- Delivery status: `Sent`, `Error`.
- Failure diagnostics: `ExceptionType`, `ExceptionMessage`, `ExceptionStackTrace`.
- Relations: `Recipients`, `Attachments`.

### MailTemplate Aggregate (company-scoped)

| Component | Type | Purpose |
| - | - | - |
| `MailTemplate<TEntity,...>` | abstract root | Reusable template definition that can produce a `MailMessage`. |
| `MailTemplateRecipient` | child | Recipient definition by direct email, person, position, or team. |
| `MailTemplateAttachment` | child | Template-level attachment definition. |
| `LessonsLearnedMailTemplate` | concrete example | Concrete template implementation that creates `MailMessage` from issue context. |
Core fields on `MailTemplate`:
- Ownership: `CompanyId`, `Company`.
- Template metadata: `Name`, `Subject`, `Archived`, `AccessRestricted`.
- Visual/body blocks: `TitleTemplate`, `SubTitleTemplate`, `HeaderTemplate`, `BodyTemplate`, `FooterTemplate`.
- Relations: `Recipients`, `Attachments`, `ResourceRights`.
- Factory method: `CreateMailMessage(TEntity entity)`.

### TextMessage Aggregate (company-scoped)

| Component | Type | Purpose |
| - | - | - |
| `TextMessage` | root entity | Queued/sent SMS payload with response-processing flags. |
| `TextRecipient` | child | Recipient number and `RecipientType`. |
| `TextInboundMessage` | inbound entity | Raw inbound Twilio callback payload, asynchronously reconciled. |
| `Company.Texts` | aggregate root relation | Company-level collection for outbound text messages. |
Core fields on `TextMessage`:
- Identity and ownership: `Id`, `CompanyId`, `Company`.
- Payload: `Message`.
- Queue lifecycle: `QueuedOn`, `SentOn`.
- Delivery status: `Sent`, `Error`.
- Failure diagnostics: `ExceptionType`, `ExceptionMessage`, `ExceptionStackTrace`.
- Response tracking:
  - `ResponseExpected`
  - `ResponseWindowStart`
  - `ResponseWindowEnd`
  - `ResponseReceived`
  - `ResponseHandlerName`
  - `ResponseHandlerTag`
  - `ResponseHandled`
  - `ResponseId` / `Response`
- Relations: `Recipients`.

## Field and Flag Semantics

| Field | Entity | Meaning |
| - | - | - |
| `Sent` | `MailMessage`, `TextMessage` | Transport send succeeded and timestamp is set. |
| `Error` | `MailMessage`, `TextMessage` | Last send attempt failed and exception info was persisted. |
| `ResponseExpected` | `TextMessage` | Outbound text expects an inbound reply to be correlated. |
| `ResponseReceived` | `TextMessage` | Correlation completed (`Response` was linked from inbound queue). |
| `ResponseHandled` | `TextMessage` | Domain-specific handler processed the linked response. |
| `Handled` | `TextInboundMessage` | Inbound callback payload has been consumed by receive routine. |
Additional recipient semantics:
- `MailMessage.AddRecipients(...)` and `TextMessage.AddRecipients(...)` deduplicate recipients by type + value.
- `Person.DisableMailMessages` and `Person.DisableTextMessages` are respected by helper overloads that accept `Person`.

## Company Scoping Notes

- `MailTemplate` is strictly company-scoped (`CompanyId` required).
- `MailMessage` and `TextMessage` both have nullable `CompanyId` but are operationally used as company-scoped queues in normal feature flows.
- `TextInboundMessage` has no `CompanyId`; scope is inferred when it is correlated to an outbound `TextMessage`.
- Company settings gate delivery behavior:
  - `CompanySettings.DisableMailMessages`
  - `CompanySettings.DisableTextMessages`
  - `CompanySettings.SmtpServiceEnabled`

## Config Key Inventory (names only)

Global app config keys used by mail/text behavior:
- Twilio: `TwilioClientId`, `TwilioSecretKey`, `TwilioPhoneNumber`
- SMTP default transport: `SmptHostName`, `SmtpPort`, `SmtpEnableSsl`, `SmtpUserName`, `SmtpPasswordEncrypted`, `SmtpSenderEmail`, `SmtpSenderName`
- Runtime context used by rendering/routing: `BaseUrl`, `Environment`
Company override settings for SMTP:
- `SmtpServiceEnabled`
- `SmtpHostName`
- `SmtpPort`
- `SmtpEnableSsl`
- `SmtpUserName`
- `SmtpPasswordEncrypted`
- `SmtpSenderEmail`
- `SmtpSenderName`
- `SmtpSubjectFormat`
- `SmtpRateLimit`

## Twilio Callback Mapping

`SmsController.Callback(SmsRequest request)` maps Twilio request fields into `TextInboundMessage`:
| `SmsRequest` field | `TextInboundMessage` field |
| - | - |
| `AccountSid` | `AccountSid` |
| `From` | `From` |
| `To` | `To` |
| `FromCity` | `FromCity` |
| `FromState` | `FromState` |
| `FromZip` | `FromZip` |
| `FromCountry` | `FromCountry` |
| `ToCity` | `ToCity` |
| `ToState` | `ToState` |
| `ToZip` | `ToZip` |
| `ToCountry` | `ToCountry` |
| `MessageSid` | `SmsSid` |
| `Body` | `Body` |
| `MessageStatus` | `MessageStatus` |
| n/a | `Handled = false` (explicitly initialized) |

## Related Ecosystem Models

Include these when tracing message lineage outside direct queue tables:
- Workflow producers:
  - `Coach2Lead/Models/Workflows/MailMessageWorkflow.cs`
  - `Coach2Lead/Models/Workflows/TextMessageWorkflow.cs`
- Entity wrappers:
  - `Coach2Lead/Entity/EntityMailMessage.cs`
  - `Coach2Lead/Entity/EntityTextMessage.cs`
- Query surfaces:
  - `Coach2Lead.Web/App/Domain/Entity/EntityMailMessageQuery.cs`
  - `Coach2Lead.Web/App/Domain/Entity/EntityTextMessageQuery.cs`
  - `Coach2Lead.Web/App/Domain/Entity/ResourceControllerBase.cs`
