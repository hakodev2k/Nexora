# UX-13 — Admin, Support and Emergency modes

> **Current decision amendment — 2026-09-07:** Internal-only user journeys; paused module navigation removed; Account/Vault deletion exceptions and SuperAdmin RECOVERY context override generic purge/restore patterns. Language default vi, selectable en. External research URLs in docs are evidence, not product navigation. [Normative PO decisions](../../requirements/10-owner-decisions-20260907.md). Conflicting older proposal paragraphs below are historical; current field/action overrides are in the linked delta. Docs-only.

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Admin area has its own navigation and operational purpose. User list grants/roles/profile metadata do not link directly into their private data. Admin's personal modules still target their own PersonalSpace. No avatar/account switch that impersonates another User.

Support entry requires User consent for exactly one module, duration24h default/custom/until revoke, plus Admin current support permission. Any qualified Admin can start explicit session. Persistent banner contains SUPPORT MODE, target User, Module, expiry/countdown, Read-only, End Session. End closes current session; owner Revoke ends grant and all derived access, distinguish these labels.

Emergency entry separate privileged form: target/module/reason required, expiry disclosed, audit + immediate three-channel notification explained. Persistent EMERGENCY ACCESS banner has stronger semantic warning and explicit reason panel/audit committed state. No content shown until reason/audit/notification intent durably recorded. Audit failure blocks entry; external Email/Push failure recorded and retried without pretending sent.

Within modes, only approved safe readonly provider screens; links to other modules inaccessible, global search scoped to granted module safe contract or unavailable if no safe provider. No export/mutation/reveal/copy secrets. Vault metadata blocked pending Q-04, no SuperAdmin exception for decryption. Mode context carried through detail, dialogs, mobile and query keys.

At expiry/revoke: end view, clear private data, return to Admin safe landing; owner SecurityCenter shows actor/module/when/outcome. Role change/last-SuperAdmin dialogs show before/after and invariant block. Operation errors redacted, never diagnostic raw SQL/content. Reference [Customer Lockbox](https://learn.microsoft.com/en-us/purview/customer-lockbox-requests) supports consent/time/audit pattern only; Nexora User approval and24h differ deliberately.
