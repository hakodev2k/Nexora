# Work Log category filter and private edit history

## Task brief / authority

Accountable owner: implementation agent, Technical Lead. User requests continuing
all remaining coding slices; DEC-20260909-014 authorizes local implementation.
Branch `impl/m01-s00-scaffold`, HEAD `1cd27a46a2ba1f24ab2b6f32f84771230f4b1761`.
Starting source manifest220 files /0 drift; protected owner12 files /0 drift.
Code-only: no new tests, fixtures, SQL execution, runtime QA or provider execution.
Independent owner/security/DB review Pending, not substituted by self-review.

Sources: P07-WRK-001; FX40-BR005/006; FX40-S06 category/date list and
“Edit with history”; UX sections12–18. Exact SELF actions:
`learning.worklog.read/create/update/archive/unarchive`, `learning.report.read`.
Goals NXG-P07-01, NXG-FX40-G03, NXG-SYS02/03/05/08/09/11/13/14.
AC004 TimeEntry dedup remains outside this manual slice and unimplemented.
Rules: baseline and architecture/backend/frontend/security/database/verification/
owner-isolation routes in `.ai/routing.json`, loaded in preceding continuation;
instruction paths unchanged, critical auth/migration/browser/concurrency rules
reread. No absent on-demand package is reported as executable verification.

## Bounded plan and contracts (before code)

1. Optional `category` on existing list/report queries: trimmed nullable text,
   maximum100 UTF-16 units; literal equality under the existing SQL column
   collation, never wildcard/substring matching. Empty means all categories.
   Same predicate for list and gross report, same current owner/registry/action
   guards. Signed list cursor binds category too; old categoryless cursors fail
   closed after the scoped cursor version changes. Native filter retained only
   in the existing owner-keyed memory; changes clear records/report/cursor.
2. Additive0081 creates owner-scoped WorkLogVersion snapshots. Each successful
   create/update/archive/unarchive appends the resulting manual fields/status,
   monotonically increasing per-log sequence, current actor, server capture time
   and original source rowversion in the SAME transaction as domain, registry,
   audit and receipt. No snapshot on replay, denied/conflicting/failed writes.
   Existing logs have no fabricated pre-migration history: first later command
   captures its actual pre-command row as `HistoryStarted`, then its outcome.
   This is capture time, not a claim about historical creation/edit time.
   Immutable here means no user-facing update/delete history command; future
   source purge must explicitly dispose of snapshots, not leave an orphan.
3. GET `/{id}/history?cursor=` requires current `learning.worklog.read` and own
   existing Active/Archived source with current WorkLog registry proof in a
   Serializable transaction, rechecked before release.25 rows, sequence DESC,
   TOP26 seek. Encrypted purpose-specific cursor bound to actor/owner/source,
   bounded/canonical decoding; foreign/malformed cursor422. Missing source404,
   revoked action403. Never expose snapshots through generic Audit/Search,
   Sharing/Support/Emergency, URLs or persistence in the browser.
   Response `{items,nextCursor}`; items contain sequence, event, capturedAt,
   original source updatedAt and manual snapshot fields/status; no write ETag.
4. Native detail explicit-load history, per-version expandable escaped text,
   date-only values unchanged; capture instant in profile timezone. Honest
   empty/loading/error states, next page; clear history on source/filter/context
   changes, reload, mutation completion or access denial. Existing epoch/abort
   fences apply. No restored history version command or inferred time records.

## Data, recovery and evidence

0081 has composite owner/source FK, actor FK, per-source unique sequence and
source rowversion, bounded fields/status/event checks and owner/source seek key.
No data migration/backfill/grant/provider side effects. SQL authoritative;
existing owner UPDLOCK serializes writer sequence allocation. Additive table
creation may acquire schema locks; migration is code only, NOT EXECUTED.
Rollback app code before schema rollback; retain/export history through a separately
authorized owner recovery path rather than dropping captured personal data.
No destructive down migration. Existing source-only old writers could omit
history during mixed-version deployment: deploy new writer exclusively before
claiming capture coverage. No claim of coverage before deployment or migration.

Acceptance intent, NOT RUN: category list/report agree; category-bound cursor
cannot be reused with another filter; foreign owner/source/cursor and revoked
permission fail closed; create/edit/archive/unarchive append exact snapshots;
same receipt replay appends none; rollback leaves none; competing edits retain
strict ETag conflict; legacy baseline honestly labelled; private notes escape,
stale source reads cannot land in new selection. Compile/build only will be
recorded after implementation. Required independent review remains Pending.
