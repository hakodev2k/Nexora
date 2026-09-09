# Versioned payload and encrypted field contracts

> **Current decision amendment — 2026-09-07:** Physical delta17 is current for User/ShareLink/Vault flags, recovery wraps, four new tables and Career CalendarLink replacement. Baseline field counts/encryption/purge/Interview proposals are superseded only where specified; no migration executed. [Normative PO decisions](../requirements/10-owner-decisions-20260907.md). Conflicting older proposal paragraphs below are historical; current field/action overrides are in the linked delta. Docs-only.

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

JSON fields are not permission to defer every field decision. This document binds each payload family to typed keys, validation and version migration. SQL ISJSON validates syntax only; provider DTO validator enforces keys/types/limits. Decimal in transport JSON is a decimal string; UTC instant ISO8601 Z; date YYYY-MM-DD; resource reference carries opaque Id/type and optional exact version, with Owner derived not caller asserted.

## Canonical payload families

| Family / table fields | Required shape | Validation / exposure |
| --- | --- | --- |
| ModuleRelease.ManifestJson | schemaVersion integer; moduleCode string; version/compatibility strings; dependencies array {moduleCode,kind,range}; resources/actions/contributions arrays of stable keys; migrationIds array | Trusted developer artifact only; unknown executable fields rejected. Navigation route proposals are not API endpoints. |
| ResourceType.CapabilitiesJson | schemaVersion integer; capability identifiers array; projection contract versions map; supportedContexts array | Deny missing context; never auto-add Support/share because a table exists. |
| Preference.ValueJson | Registered preferenceKey selects exact DTO: nav {collapsed:boolean,pinnedModuleCodes:string[]}; list {view,sort,density}; locale {language,timezone,weekStart} | No secret or arbitrary business fields; Q-09 controls language/default currency; delegated Planner week Monday preserved. |
| ProjectVersion.SnapshotJson | All Project business fields plus tags array {id,label}; schemaVersion; source revision; immutable owner/project assertions | Reason stored separately owner-only. Terminal status cannot be restored/reopened. |
| TaskVersion.SnapshotJson | All Task editable fields; checklist array {id,text,checked,position}; tags {id,label}; reminder {kind,exactAt?,timezone}; rank; immutable Project assertion | Full snapshot restoration creates new version. Config past due Expired; no old alert dispatch. |
| PageVersion.MetadataJson | title string; tag {id?,label}?; visual null or {kind:Emoji/Builtin,value} or {kind:Cover,fileId,crop}; immutable type/editor/parent/folder assertions | No duplicate authority for body; transient crop preview not saved until manual Save. Publish/Archive/Trash transitions update Activity and concurrency, not content Save versions; restoring content never changes current lifecycle. |
| PageVersion.Body Block | schemaVersion integer; blocks array with stable id/type and typed payload | Allowed paragraph,H1..H3,list/checklist,quote,divider,link,code,image,simple table. Text spans bounded; link HTTP(S)/safe internal contract; image file reference must Clean/same owner. No arbitrary embed/script/HTML. Max1MiB canonical UTF8. |
| PageVersion.Body Markdown | CommonMark text plus GFM tables/task lists/strikethrough; no executable directives | Literal raw HTML escaped. Max1MiB UTF8; preview sanitized. Formatting does not autosave. |
| Page.CoverCropJson | schemaVersion integer; x,y,width,height decimal0..1; focalX,focalY0..1; orientation integer | x+width<=1,y+height<=1,positive dimensions; non-destructive. JPG/PNG/WebP<=5MiB and25MP. |
| SearchProjection.FacetJson / SavedQuery.QueryJson | schemaVersion; allowed module/type/tag IDs/date boundaries/sort; query string<=500 | Reject raw SQL or secret query values; remove inaccessible facets before returning counts. |
| Template.SeedJson | schemaVersion; registered target type; editable seed fields matching create DTO | Strip owner/IDs/history/permissions/shares/credentials before validation; immutable choices still explicit at create. |
| Bookmark.MetadataJson | schemaVersion; fetched {title,description,iconUrl}; fetchedAt; perFieldSource map Manual/Provider; canonicalUrl | Conservative URL normalization; external fetch SSRF guard; owner edits win. |
| Outbox.PayloadJson / Job.ArgumentsJson | schemaVersion; resource reference; sourceRevision; safe changed-field keys; registered options only | No entire document, ledger row, token, secret or signed capability. Job handler allowlist required. |
| ImportRow.CandidateJson / ImportBatch.OptionsJson | Importer-specific DTO matching target create contract; rowNumber; mapping rules; validated fields | Projects/Tasks absent. ICS candidate Title/Description/time kind/UTC or date range/UID only; VALARM excluded. |
| ExportJob.FilterJson | schemaVersion; allowed sourceKinds/statuses; range null or {start,end}; containmentMode | No arbitrary fields or all-data bypass; Calendar containment fixed FullyContained. |
| Finance snapshot and rule JSON | Typed Account/TransactionLeg/Split/Bill field snapshots or schedule {frequency,interval,timezone,start,end?} | Q-05 conditional. No free-form executable formula or arbitrary currency conversion. |
| Automation.DefinitionJson | schemaVersion; trigger {key,version,config}; ordered steps {stepKey,actionKey,version,inputBindings,condition?}; limits | Q-07; typed mapping only, no user JS/Python/SQL. Max20steps proposal; exact run pins version. |
| Automation.SafeResultJson | schemaVersion; status; providerReceiptReference?; safe result field allowlist | Never interpolate Vault secret into persisted run output or UI. |
| Webhook EventTypesJson / Connection.ScopesJson | schemaVersion; explicit event keys and allowed projections/actions | Q-07 egress approval; no wildcard all-user payload. |
| DashboardWidget.OptionsJson | schemaVersion; registered widget type; allowed filter/range/display options | No raw SQL/custom scripts; provider controls safe fields. |
| TopicWatch keywords/exclusions/source IDs | Normalized nonempty literal phrase arrays; source public IDs array; matching mode Any/All | No regex/AI hidden execution; queryVersion invalidates only future matches. |
| Asset/DigitalAsset safe snapshots | Exact nonsecret aggregate/subtype fields with source Manual/Observed and timestamp; resource refs | Serial/contact/credential values never fall into safe JSON; encrypted snapshots separate. |


## Additional JSON families — exact bounded contracts

Every shape below has schemaVersion:integer=1; required keys are listed without a question mark. Arrays preserve declared ordering, reject duplicate stable IDs, and are validated before persistence. Scalar business field limits inherit the corresponding data dictionary. Unknown keys are rejected rather than silently retained.

| Fields | Shape | Rules |
| --- | --- | --- |
| AuditEvent.DetailsJson / ActivityEvent.SummaryJson | {changedFields:string[], beforeCodes?:map<string,string>, afterCodes?:map<string,string>, reasonReference?:opaqueId, sourceRevision?:integer, counts?:map<string,integer>} | No raw before/after personal values, token or document body. Audit action registry owns exact allowed field/code names. Owner reason display resolves separately through authorized provider. |
| RestoreRun.VerificationJson | {checks:[{key:string,status:Pass/Fail/NotRun,evidenceReference?:string,errorCode?:string}], migrationSetDigest:string, fileManifestDigest?:string, keyInventoryReference?:string} | Redacted references only; all required checks Pass before Completed; no key material or backup contents in JSON. |
| TimeEntryVersion.SnapshotJson | {description?:string,category?:string,tagLabels:string[],startAt:instant,endAt?:instant,taskRef?:resourceRef,projectRef?:resourceRef,status:string,reason?:string} | Full owner-only semantic version; duration derived; no changes to immutable owner/source that violate original-source gate. |
| RecurringRule.TemplateJson | {kind:Bill/Transaction, accountRef:resourceRef, amount:decimalString,currency:string,categoryRef?:resourceRef,memo?:string} | Q-05; same validation as explicit create, no authorization or identifiers copied into generated resource. |
| TopicWatch arrays / Repository.TopicsJson / CertificateDetail.SubjectAlternativeNamesJson | Keywords/Excludes/Topics/SAN: string[]; SourceIds: opaquePublicFeedId[] | Literal values, unique normalized entries; field length limits and total bounded payload; no regex, expression evaluation or private credentials. |
| TrackedProduct.VariantOptionsJson | {options:[{key:string,label:string,value:string}]} | Exact selected variant identity; key unique; changed provider variant never silently retargets history. |
| Comparison.CriteriaJson / ComparisonItem.ValuesJson | Criteria {criteria:[{id:string,label:string,kind:Text/Number/Boolean,unit?:string,position:integer}]}; Values {values:[{criterionId:string,value:string/decimalString/boolean}]} | Values must reference current comparison criteria of matching kind; no executable scoring formula. Missing value is Unknown, not zero. Criteria deletion previews removed recorded values. |
| ToolHistory.InputJson / OutputJson | {toolKey:string,toolContractVersion:string,fields:map<string,typedScalarOrArray>} | Each allowlisted tool owns explicit input/output DTO matching workbench; never a generic arbitrary dump. History requires opt-in; secret/credential/JWT payload excluded by policy. |
| RankingSnapshot.QueryJson | {createdFrom:date,createdToExclusive:date,languages?:string[],minStars?:integer,sort:TotalStarsDescending,scope:PublicRepositories} | Rank rule version fixed; no claim delta-stars or all-provider completeness. Dates and pagination/rate limits retained with snapshot metadata. |
| DefinitionVersion.ValidationJson | {valid:boolean,checkedAt:instant,contractVersions:map<string,string>,issues:[{stepKey?:string,fieldPath?:string,code:string,severity:Error/Warning}]} | Revalidate against current capabilities before enable/run; stored validation is not continuing permission. |
| Connection.ScopesJson / Webhook.EventTypesJson | Scopes {allowedActions:string[],allowedProjections:string[],direction:Inbound/Outbound/Both}; EventTypes {eventKeys:string[],projectionVersions:map<string,string>} | Q-07 allowlist; no wildcard user-content exposure, arbitrary code or unapproved provider. |
| SystemJob.ArgumentsJson / SystemConnection.SettingsJson | {handlerOrProviderKey:string,contractVersion:string,options:registeredTypedObject} | No OwnerId=null wildcard; system job only explicit public-cache/maintenance scope. Provider credential stored via external SecretReference, not options. |
| AssetVersion / DigitalAssetVersion / CertificationVersion safe snapshots | {resourceType:string,fields:exactNonsecretAggregateDto,relations:resourceRef[],evidenceFileIds:opaqueId[],provenance?:map<string,{source:Manual/Observed,observedAt?:instant}>} | The named table defines every field/type; sensitive serial/contact/credential values excluded into encrypted snapshot or Vault reference. Evidence pins immutable files. |


## Block document payload details

The root is {schemaVersion:1,blocks:Block[]}. Block has id:string (stable within page), type, and the type-specific fields below; no arbitrary extra properties. Empty blocks array is valid because content body is optional. Canonical serialized body remains <=1MiB UTF8.

| Block type | Typed fields | Interaction/data guard |
| --- | --- | --- |
| Paragraph / Heading / Quote | spans:[{text:string,marks?:Bold/Italic/Strike/InlineCode[],link?:safeUrl}]; Heading additionally level:1/2/3 | Literal text; unique mark names; no hidden HTML or inline event handler. |
| List / Checklist | ordered:boolean for List; items:[{id:string,spans:Span[],checked?:boolean}] | Checked only Checklist; bounded flat item order. No Task entity/subtask creation implied by checkbox. |
| Divider | No payload fields | No empty arbitrary config bag. |
| Link | url:safeUrl; label?:string | HTTP(S) or approved typed internal resource route; link handling rechecks current access. |
| Code | language?:registeredLanguageKey; text:string | Escaped literal content, never evaluated. |
| Image | fileId:opaqueId; alt?:string; caption?:string | Clean same-owner FileObject plus version FileReference pin, no external executable embed. |
| Table | columns:[{id:string,label?:string}]; rows:[{id:string,cells:Span[][]}] | Each row cell count equals columns; delegated bounds50rows/20columns; no formulas/database view. |


## Vault typed plaintext contract inside encryption boundary — Q-04

Outer SQL never contains the following plaintext. Name1..200 required and ItemType immutable; fields inside payload carry schemaVersion and type. Optional strings omitted, not fake empty secrets. Key architecture/recovery/export remain Proposed.

| Item type | Encrypted typed fields | Validation |
| --- | --- | --- |
| Password | name:string; username?:string; password:string; urls?:string[]; notes?:string; tags?:string[] | Nonempty password; URL scheme allowlist; no autofill/browser extension scope implied. |
| SecureNote | name:string; text:string; tags?:string[] | Nonempty note; never index content. |
| ApiKey / Token | name:string; secret:string; service?:string; expiresAt?:instant; notes?:string | Mask by default; expiry metadata outside encryption only after Q-04 disclosure decision. |
| SshKey | name:string; privateKey:string; publicKey?:string; passphrase?:string; notes?:string | Bounded parser, no network authentication or shell execution. |
| DatabaseCredential | name:string; engine?:string; host?:string; port?:integer; database?:string; username?:string; password:string; connectionString?:string | Port1..65535 if present; no Test Connection until approved network scope. |
| RecoveryCodes | name:string; service?:string; codes:array {id:string,value:string,used:boolean}; notes?:string | Stable code IDs; mark-used is versioned owner action, not automatic auth to third party. |
| LicenseSecret | name:string; key:string; product?:string; notes?:string | No activation/provisioning. |
| Generic | name:string; fields:array {id,label,value,kind:Text/Secret}; notes?:string | Bounded typed key/value collection inside this one Vault item type, not global database EAV; secrets protected. |


Every sensitive non-Vault Encrypted binary field uses a common envelope: algorithm/version, nonce, ciphertext, authTag, keyReference/keyVersion and authenticated owner/resource/field context. The envelope is one binary-encoded versioned structure; do not store unauthenticated raw ciphertext. Encryption keys and signing keys are supplied by configured key service, never by browser persistent storage. Algorithm choice/rotation and recent-auth policy are security ADR work, not claimed cryptographic implementation.
