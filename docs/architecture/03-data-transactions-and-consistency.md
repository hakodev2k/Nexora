# Data access, consistency and concurrency

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

## Transaction ownership

One domain command coordinates required local participants through a platform UnitOfWork contract. The coordinator opens one SQL connection/transaction and supplies it to participating module DbContexts through public transactional handlers. It never reaches into another module's entity set. Each participant maps only its owned tables and kernel references needed by its persistence model. No remote call is inside this transaction.

Order locks: account/security gate when needed → affected root registry identities sorted by GUID → domain aggregate roots → child/rank rows → registry/history/outbox/idempotency writes. Project close and Task write contend on the same Project gate. Page parent/cascade and child operations contend on parent page gate. Link/purge contend on target registry reference guard. Deadlock retry is bounded and uses original idempotency key; final failure returns retryable conflict, not success toast.

EF Core allows relational contexts to share a connection/transaction. Its automatic retry strategy must wrap the entire unit of work consistently; MARS/savepoint caveats mean the design defaults MARS disabled. [Official transaction documentation](https://learn.microsoft.com/en-us/ef/core/saving/transactions), checked2026-09-07. This is a design constraint, not library code tested here.

## ETag and full versions

Every mutable command requires current ETag except explicit create/idempotent append endpoints. Missing precondition yields PreconditionRequired; stale token yields Conflict with safe latest revision and changed field names. Never send another user's actual record in conflict response. UI retains authorized draft in memory and offers compare/reload/reapply; no blind overwrite or silent last-write-wins. Full content version number is independent of SQL rowversion.

Task history restore validates source owner/immutable Project and current parent state, checks backward reason, rebinds allowable tag identities and carries old reminder as Expired if past. Document restore creates new PageVersion and current media references; no rewriting old rows or lifecycle restoration. Publish/Archive/Unarchive/Trash alone create Activity only, not a content Save version. History raw snapshot is never a public API update DTO.

## Projection strategy

TaskCalendar is a live query contract over current Tasks; ManualEvent separate. Calendar visible-range query uses overlap, ICS export uses complete containment. Search can maintain a durable projection with sourceRevision but must recheck current visibility before returning previews. Dashboard panels request independent provider results with freshness, not a monolithic cross-schema SQL join. Finance balances are derived from approved ledger semantics; cache is never user-editable truth.

## Asynchronous events

Outbox writes commit with source command. Dispatcher leases and publishes minimal event; consumer receipt commits with local derived mutation. Transport is at-least-once. Unique owner/event/consumer/effect keys prevent duplicate logical action; an external side effect cannot be guaranteed exactly-once without provider idempotency/reconciliation.

Reminder intent carries source revision/due/config and checks module/lifecycle again immediately before creating one Notification plus three Delivery rows. Old source version is canceled/expired, not sent. Browser push permission absence, Email provider failure and In-app availability have independent outcomes. Historical source restore does not backfill old alerts.

## Partial failure contract

Domain command requiring Task+Calendar reminder+history fails wholly if a required transactional participant fails. A later search indexing failure does not roll back successful Save; UI displays saved revision and search freshness status. Provider errors keep last successful observed data, never fake zero or empty-success. External integration timeout after sending yields Unknown where result cannot be proven; user Retry must reconcile before repeating irreversible effect.

## Purge and referential integrity

Registry identity survives as minimized tombstone only under approved retention. Domain payload deletion checks all pinned current/version/file/Resume references. NO ACTION FKs prevent accidental physical cascade, but domain guards determine whether references may be detached or deletion is blocked. SQL Server has no assumed deferred FKs: insert/delete order is explicit and validated within transaction. Account-wide purge remains Q-01/Q-08, Vault key destruction Q-04.
