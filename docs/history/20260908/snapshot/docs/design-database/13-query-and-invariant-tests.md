# Query contracts and database verification specification

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

## Query/index workload map

| Read surface | Predicate and ordering | Candidate index / caveat |
| --- | --- | --- |
| Projects Grid/Table | Owner + nonTrash; Title contains; Tag/Status/date overlap; Title ASC, Id ASC | Project owner/lifecycle/title; tag join owner/tag/project. Contains search not guaranteed B-tree seek. |
| Task Kanban | Owner + Project + nonTrash; Status/time; Title or Tag; Rank ASC, Id ASC within column | Task owner/project/status/rank. Filtered counts computed by same predicate, never complete loaded-page count. |
| Task Table | Same scope; requested sort with stable Id; default Start ASC then Id | Owner/project/start or end candidate. Cursor binds filter/sort signature. |
| Calendar | Authorized Task projection + ManualEvent; overlap with visible interval; all-day date overlap separately | Task owner/start/end; ManualEvent owner/timed or date fields. ICS export uses containment, NOT viewport overlap. |
| Documents location | Folder children + directly located root pages; no descendant query; page Type/Tag/CreatedAt; UpdatedAt DESC,Id | Owner/folder/parent/trash/updated index; folder rows have no fabricated DocumentType or Tag. |
| Document Archived | Owner + Archived nonTrash; flat roots and children; UpdatedAt DESC | Owner/status/updated; retain parent label only if authorized. |
| Notification Center | Recipient owner + notDeleted; read/category filter; OccurredAt DESC,Id | Owner/deleted/read/occurred covering safe title. Mark all cutoff avoids marking a concurrently arriving notification. |
| Job claim | Pending due <=now or expired lease; bounded batch | State/due/lease. Conditional atomic update and lease token; skip locked alternative evaluated against isolation settings. |
| Price history | Owner/product/currency/variant; ObservedAt in range; ASC,Id | Owner/product/observed index; separate successful sample and missing/error annotations. |
| Ledger proposal | Owner/account; financial date; category/payee/type; date DESC,Id | Account legs join transaction index; exact currency partitions; reconciliation numeric goldens Q-05. |
| Global search | Owner + registered allowed modules/types; source current visibility; ranked query | Projection index rebuildable, security recheck before snippet return. Never return total counts for unauthorized sources. |
| Audit | Administrative permission, allowed minimized subject/action/time window | OccurredAt + action/subject candidates; payload not indexed by default. |


## Parameterized invariant tests required before implementation readiness

Each scenario maps to DB columns, command and UX response; these are test specifications, not executed integration tests.

1. OwnerA creates child referencing OwnerB parent/Tag/File/Resume: physical FK/contract denies, response nonenumerating; no partial row/history/outbox.
2. Two requests consume same verification/reset token: exactly one consumes; provisioning creates one PersonalSpace/grant snapshot.
3. Concurrent last-SuperAdmin demotions/disables: serialize invariant lock; at least one rejects before account change.
4. Close Project races Task create/edit/restore/reorder: consistent root lock; winner determines second request response. No child mutate after terminalAt.
5. Task backward drag/restore without reason: reject transition; UI restores card position and retains entered data. Forward move preserves stable rank.
6. Duplicate explicit Save with same command key: one history version; different key with unchanged Document body: new version.
7. Parent Archive races child Archive/Trash: exact membership and previous state; Unarchive only own cohort, never revive Trash.
8. Folder/page create depth3, child reparent, root Folder reassignment: reject through all paths, including imported snapshot and direct API payload.
9. Page version restores historical tag label whose Tag was deleted: preview rebind/create; current Trash/Archived references still block tag deletion.
10. Parent Project terminal while child in Trash: individual Task restore denied. Parent itself in Trash: restore parent batch first, no isolated Task restore.
11. Calendar DST gap/fold/floating timezone/all-day span: explicit preview; timed instant invariant under account-zone change; all-day calendar dates invariant.
12. ICS mixed valid/recurring/missing fields/duplicate UID/VALARM/status: per-row report; only valid unique nonrecurring become Manual Scheduled; no alarm import. Reimport race unique UID yields one Event.
13. ICS export containment: partly overlapping Event excluded; Task terminal/status choices retained; no Reminder/internal IDs/reasons.
14. Three channel attempts generated exactly once per intent; Browser unavailable or Email transient failure cannot cancel other channels or delete inbox.
15. Source revision changes at reminder due instant: old intent cannot create alert; restored historical reminder is Expired without missed-run catchup.
16. Two timers or Focus sessions start across tabs: filtered unique active slot plus conflict response. Stop/retry/conversion idempotent.
17. Source purge races ResourceLink/FileReference creation: shared registry/root guard prevents dangling successful link or deletion of pinned file.
18. Webhook retry with same MessageId, lost external response or late worker lease: dedupe and Unknown/reconcile handling; no false exactly-once claim.
19. Feed/provider outage: preserve last successful data with timestamp and error, never zero price/empty-success; private read state unaffected.
20. Finance multi-leg post, split rounding, reversal allocation: Q-05 goldens required before approval; Vault decrypt/recovery/key rotation Q-04 independent security suite.

## Performance and capacity

Measure SQL execution plans with skewed owners and large personal datasets, simultaneous edits, long Trash/history and disabled modules. Pagination must be server bounded; do not fetch every owner's row to filter client-side. Keyset cursor includes sort values, owner/module scope hash and stable Id; for editable rank lists retain anchor and reload after conflict. No production latency, user-count, quota or RPO target is declared achieved: Q-08 and later load tests remain gates.
