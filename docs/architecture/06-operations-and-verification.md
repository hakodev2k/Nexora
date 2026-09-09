# Operations and architecture verification plan

> **Current decision amendment — 2026-09-07:** User.IsDeleted is a current authority gate; Account/Vault never purge; permanent sharing invalidation; explicit SuperAdmin RECOVERY mode added; Paused product scope outranks grant. Read ../requirements/10-owner-decisions-20260907.md and current catalog before old Q references. [Normative PO decisions](../requirements/10-owner-decisions-20260907.md). Conflicting older proposal paragraphs below are historical; current field/action overrides are in the linked delta. Docs-only.

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

## Runtime boundaries proposed

API and Worker may start as separate hosts sharing module binaries and SQL database; local deployment can co-locate processes. Redis used only for rebuildable cache/rate-limit assist, never authoritative session/grant/job/balance state. Object storage abstraction stores uploaded binaries/private export artifacts; SQL owns metadata/reference/scan state. Email/Push adapters publish independent attempt outcomes. No cloud/provider purchase or installation in this phase.

## Jobs and events

Durable SQL queue/lease with bounded retry/jitter, worker identity, lease expiry and stable intent/effect keys. Cancel/disable checks before each effect. Module uninstall cannot erase queued records as a substitute for recovery plan. Admin jobs surface counts/handler/state/redacted errors only, separate from User Monitoring. Dead-letter inspection does not reveal raw source data or credentials.

## Observability

Correlation across request → command revision → outbox → job/run → channel attempt; log allowlisted IDs/codes/duration, no user body/secret/token. Metrics include source conflict rate, stale projection age, notification channel failures, overdue lease count, invalid import rows, module dependency failures and restore verification. Numeric SLO/retention/cost thresholds remain Q-08, so dashboards must not show fictitious committed SLA.

## Verification layers after code approval

| Layer | Evidence required |
| --- | --- |
| Architecture tests | Forbidden assembly/DbContext dependencies; only trusted registrations; missing contribution fails safely. |
| Domain unit tests | State matrices, backward reasons, immutability, money goldens after Q-05, archive cohorts and typed schema validators. |
| SQL integration tests | Real SQL Server constraints/filtered indexes/transactions/rowversion/locks/rollback; not in-memory provider substitute. |
| Contract tests | New module compatibility, safe projections, disabled/unavailable handlers, schema-version migrations. |
| Security tests | Cross-owner traversal, share/grant revoke, CSRF/session recovery, XSS/SSRF, logs/secret leak and scoped file URLs. |
| E2E UX tests | All screen/action matrices, error/loading/empty difference, keyboard/no-drag alternative, mobile back paths. |
| Fault injection | SQL rollback, duplicate deliveries, lost provider response, worker crash/restart, cache outage, partial import and key mismatch recovery. |
| Load/recovery | Owner skew and long history/Trash; file scan; email budget; backup SQL/files/key compatibility; Q-08 targets. |


## Release gates

Docs baseline review → close relevant Q decisions and technical ADRs → approve wireframes/acceptance scenarios → create traceable Ready stories → explicit implementation authorization → scaffold/implement → tests/security/performance/recovery evidence → release approval. This docs update does not skip any gate. Production topology/provider budgets and purchase decisions are not delegated small UX choices.
