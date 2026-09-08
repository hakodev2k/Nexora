# Disable, revoke, cached capabilities và workers

> **Current decision amendment — 2026-09-07:** User.IsDeleted is a current authority gate; Account/Vault never purge; permanent sharing invalidation; explicit SuperAdmin RECOVERY mode added; Paused product scope outranks grant. Read ../requirements/10-owner-decisions-20260907.md and current catalog before old Q references. [Normative PO decisions](../requirements/10-owner-decisions-20260907.md). Conflicting older proposal paragraphs below are historical; current field/action overrides are in the linked delta. Docs-only.

| Change | Reads / UI | Queued/running work | Preserved data |
| --- | --- | --- | --- |
| System module disabled | Hide live contributions/navigation; deep link shows unavailable without payload | Prevent new work; recheck at dequeue and before effects; pause/cancel per declared safe contract | Disable never purge |
| Per-user module disabled | Clear that owner's visible cache, stop active refresh; no scope switch | Work for that owner denied at next authorization checkpoint | Resource/history remain under retention rules |
| Admin action Deny/removed | Exact action unavailable, source reads cleared if denied; other allowed actions remain | Re-evaluate original principal/action before commit; no retry privileged fallback | Audit records exact change |
| User revoke support | End all derived sessions; persistent banner replaced by access-ended view | No later read, export, mutation or continuation allowed | Consent/access audit remains |
| Emergency expires/ends | Remove target content and terminate session | Deny future target reads; no impersonation continuation | Minimal audit/security notification kept |
| Resource Trash/terminal/Archive | Re-resolve source state; remove unavailable previews | Invalidate reminders/projections by source revision and source rules | Cohort/history retained, no automatic purge |
| Sharing policy or restore | Existing-link effects Q-03 remain blocked decision | Do not invent automatic link resurrection | Source-specific approved Document behavior still applies |

## Enforcement boundaries

Capabilities are advisory snapshots keyed by principal/context/module-policy/resource revision. They are not credentials. Server handlers use current authority on every request and before commit. Grant transaction emits invalidation intent; service instances must reject stale policy epochs, not wait for a client refresh. Long-lived JWT permission lists alone are insufficient.

Read streams and download URLs must not grant unbounded future access. Prefer short-lived source-bound delivery with current access check at issuance and start; signed bearer URLs have a disclosed residual validity window that needs security design, not a false “instantly recalls files” claim. Revoke clears client caches best-effort; cannot recall plaintext already read, copied, downloaded or captured.

External side effect that provider already accepted cannot be unsent by revoke. Track accepted/unknown/failed distinctly; unknown outcome not automatically retried. Outbox intent revocation rechecked before send; logs must describe boundary/time, not claim instantaneous global cancellation. Admin job retry requires declared idempotent/retryable step and current original authority.

Internal cleanup such as stopping a timer when source Project closes may run under trusted consistency contract after source authority changes. It can only terminate/update derived state required by the source transition, never create new user work or reveal data through a disabled module.

## Notifications and support

All notification classes attempt In-app, Email and Browser Push. Browser permission denied/no subscription is a delivery limitation, not channel opt-out. Record In-app creation, Email queue/provider accepted/failure, Push subscription/accepted/failure; never say “read/delivered to device” from provider acceptance alone. Revoke support and emergency intent audit are durable; emergency data view only after reason/audit/three-channel notification intents committed, without waiting for external email provider success.

## Responses

Owner-safe capability reasons: ActionDenied, ModuleUnavailable, LifecycleLocked, DependencyUnavailable, DecisionBlocked, StepUpRequired, Conflict. Wrong-owner/unknown resource uses generic unavailable, not permission explanation revealing existence. On revoked form: stop submit, clear protected payload, explain access change; do not persist dirty sensitive data to browser storage. On revision conflict while still authorized: keep safe draft in memory, show diff and explicit retry, no auto overwrite.
