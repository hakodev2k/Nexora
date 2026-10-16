# Notification inbox authority and SYSTEM publication boundary

PR #4 baseline 1cd27a46, 2026-10-16. User explicitly authorizes implementation and real local/test/UI smoke verification; that supersedes the older code-only execution restriction for this run. No provider or production execution is approved.

## Source and task contract

- `docs/action-catalog/modules/06-notifications.md`: exact SELF inbox actions, WORKER/SYSTEM dispatch actions, trusted intents only and no arbitrary-send permission.
- `docs/action-catalog/00-authorization-contract.md`: current account/session/module/action/owner, before-commit recheck, current read projection independent of mutation rights; SuperAdmin is not SYSTEM or another owner's SELF context.
- `docs/features/06-notification-center.md`: FX-06-BR-001..006, FX-06-AC-001..003, NTF-001..010; current selection/watermark, owner isolation, no cached private source body and retained delivery/audit.
- `NXG-FX06-G01..03`, NXG-SYS-02/03/05/08/13/14; FX06-S01/S02 safe unavailable/error states.

## Implemented contract

| Surface | Behavior |
| --- | --- |
| Inbox query | Current inbox-read authority, notification rows, per-owner channel projections and unread count share one Serializable transaction. No dirty read. |
| Read/unread command | Explicit trusted mark_read/mark_unread action checked inside the write transaction even without a key, and before commit. ETag is exact quoted rowversion. |
| Single outcome | Full DTO requires current inbox.read. Otherwise common `{id,committed:true,detailAvailable:false}` acknowledgement. Same-key original opaque result stays opaque after read permission is granted. |
| Replay | Receipt retains status/code, opaque ID/revision/action and keyed authorized full-projection digest; no notification title/body/source or delivery DTO stored. Current own lifecycle/read and identical full projection required for DTO; deleted/newer/unreadable/changed delivery state yields committed ACK without reapplying the mutation. Legacy unbound full DTO receipts remain safe409 for their TTL. Existing read-state receipt namespace is retained independently from actual command authorization. |
| Mark all | Original watermark/count retained only for an originally readable outcome; current read is required to replay it. Otherwise ACK uses the authorized PersonalSpace collection reference. Replay does not apply the operation to later arrivals. |
| Delete | Explicit1..100 distinct IDs, no silent truncation. Foreign/missing selection fails atomically. Soft-delete preserves delivery/security audit; replay204 does not expand selection. |
| Source projection | No registered current source resolver exists for cached Task/Event/other source references. Such rows return `sourceUnavailable:true`, generic safe title/body and null sourceRef. This fails closed instead of exposing cached private source snapshots or adding cross-module private table reads. |
| SYSTEM boundary | Removed unsupported POST `/api/v1/admin/notifications`, its public SELF `Publish` method and publish request DTO. No approved arbitrary content/recipient exception exists. Existing Focus/Reminder/Identity/Automation transactional writers are unchanged; no core worker/channel attempt is disabled and no fake provider success is introduced. |

## Verification and remaining scope

`SqlNotificationAuthorityTests` adds nine real-SQL cases (seven Facts plus a two-case Theory) for ACK privacy/original shape, current read/session/module revocation, durable single outcomes and delivery/lifecycle changes, source snapshot suppression, watermark and explicit selection, transaction-final rollback and rejection of arbitrary publication even for SuperAdmin. Fixtures are explicit synthetic SQL setup in the existing isolated disposable fixture; they are not an HTTP publisher bypass. Root coordinates existing integration/browser fixture migration away from the removed route.

Root records actual final builds/runtime checks and independent review. This document does not claim SQL/browser acceptance or complete FX06. Registered source open/current-source proof, inbox cursor/category/date filter completion and real delivery/provider acceptance remain separate requirements. Current safe source-unavailable rendering is a security disposition, not a completed source resolver. No schema, migration, delivery writer or Program registration changed.
