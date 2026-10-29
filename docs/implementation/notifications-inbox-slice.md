# Notification inbox local slice

Status: `SLICE_IMPLEMENTED` in PR #4; runtime acceptance remains owner verification.

## Contract trace

- Goal: `NXG-FX06-G01…G03` and cross-cutting owner/lifecycle/audit goals.
- Source: [FX-06 Notification Center](../features/06-notification-center.md), `NTF-001…NTF-010`, `P02-NTF-001…P02-NTF-006`, and `FX-06-AC-001…003`.
- Actions/operations: `notifications.view`, `notifications.update`,
  `notifications.delete`, `notifications.dispatch.publish`; operationIds are
  `listNotifications`, `markNotificationRead`, `markAllNotificationsRead`,
  `deleteNotifications`, and `publishNotification`.

## Implemented behavior

- Notification data is projected by `OwnerUserId` from the authenticated
  SQL-backed session. No request can supply an alternate owner.
- Inbox supports unread filtering, newest-first bounded pagination, mark
  read/unread with `ETag`/`If-Match`, watermark-based mark-all-read, and bulk
  soft-delete. Deleted inbox rows remain available to audit/delivery state.
- Logical publication is deduplicated by the database logical key and durable
  idempotency receipt. Publication creates InApp, Email and BrowserPush delivery
  rows in one transaction and queues one `operations.Outbox` dispatch event.
- Browser Push starts as `PermissionUnavailable`; Email/Push delivery workers
  and subscriptions are local-safe boundaries only. No external provider call,
  secret or token is used.
- Notification bodies reject obvious credential/token material and expose only
  safe title/body/source metadata. Delivery attempts and last error are shown
  per channel without provider credentials.

## Evidence status

The agent ran `npm run build --prefix web/Nexora.Web` and
`python3 scripts/dev/verify-s00.py`. .NET build, SQL migration execution,
functional/API/integration/E2E/browser tests and manual QA were not run because
the current instruction is code-only and the required runtimes are unavailable.
Those checks remain human-owner work; this document does not mark the slice
`SLICE_VERIFIED_LOCALLY`.
