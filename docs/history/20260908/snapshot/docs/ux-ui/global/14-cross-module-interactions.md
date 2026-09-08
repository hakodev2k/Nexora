# UX-14 — Cross-module interaction patterns

> **Current decision amendment — 2026-09-07:** Internal-only user journeys; paused module navigation removed; Account/Vault deletion exceptions and SuperAdmin RECOVERY context override generic purge/restore patterns. Language default vi, selectable en. External research URLs in docs are evidence, not product navigation. [Normative PO decisions](../../requirements/10-owner-decisions-20260907.md). Conflicting older proposal paragraphs below are historical; current field/action overrides are in the linked delta. Docs-only.

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

| Origin → destination | Behavior | Source authority / return |
| --- | --- | --- |
| Task → Calendar | Readonly occurrence, Open Task to edit; status retained terminal | Task source; Back restores Calendar date/view |
| Planner → Task | Pin existing Task by plan date, no copy or reschedule | Task controls schedule/status; Back retains selected plan day |
| Goal → Task | Read progress/status; remove reference explicit | No Task completion from Goal; unavailable denominator remains |
| Project terminal → Time/Focus/Reminder | Stop linked timer/focus, invalidate pending alerts at source transition | No continuing locked Task through another module |
| Documents → Files | Cover/inline file draft then manual Page Save pins clean file/version | Files scan/purge guard; cancel leaves old saved version intact |
| History → Source | Readonly preview; restore only source-approved command creates new version | Immutable identity/lifecycle not bypassed; Back original detail |
| News/Bookmark → Read Later | Upsert one queue source reference, preserve reading state | Original source remains separate, no duplicate body |
| Shopping → Personal Asset | Explicit selected purchase-line draft with actual price/evidence | Save target idempotently links; cancel no Asset/Finance side effect |
| Assets/Integrations → Vault | Select reference, no secret preview outside guarded execution | Owner Vault resolves credentials; support no reveal |
| Job → Resume | Pin exact immutable file/Document version | New Resume version does not alter submitted or shared artifact |
| Interview → Calendar | Major source/sync question Q-12 | No automatic third Calendar source implemented by UX |
| Search/Dashboard → Any provider | Safe summary, current module/access recheck, context-preserving source route | Failure/stale badge; source command owns mutations |
| Automation → Module action | Registered typed command with own validation/permission/current revision | No raw table write; cancel not rollback of accepted external effect |
| Share Project → Task | Readonly Task details under current link context | No owner API/history/reason path; entire Project sharing, no Task exclusions |
| Template → Create form | Seed allowed editable values, explicitly choose immutable required type/mode | Normal form Save and validations, no IDs/grants/history/secret copies |


Cross-module picker always shows target module/type and safe title, current owner only; missing/disabled provider is unavailable, not empty list. Removing a link never deletes source unless an explicit approved aggregate lifecycle says so. Sensitive context stays attached across navigation; no action after switching grant/owner without fresh authorization. Partial cross-provider summary load fails locally; atomic domain command failure reports no partial saved state. [Transaction catalog](../../design-database/11-relations-and-transactions.md) and [architecture contracts](../../architecture/02-module-boundaries.md) define implementation boundaries.
