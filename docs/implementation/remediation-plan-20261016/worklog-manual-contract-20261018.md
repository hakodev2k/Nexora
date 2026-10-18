# FX40 manual Work Log vertical slice

## Task brief and sources

Owner: implementation agent/Technical Lead. Same impl/m01-s00-scaffold branch,
HEAD1cd27a46a2ba1f24ab2b6f32f84771230f4b1761, draft PR4, no merge. User explicitly
requests continued remaining requirement-backed coding. DEC014/local authorization
and code-only amendment apply: no tests/fixtures/demo records/runtime SQL/provider
execution. Same exact rule paths/routing as the Goals target-management brief;
also read current feature40, module goal40, action40-learning, UX40-learning and
phase07 P07-WRK-001, database10 learning.WorkLog, current PO language/privacy rules.
No new business decision is approved by this technical contract.

Bound NXG-P07-01, NXG-FX40-G03 and SYS02/03/04/05/08/09/11/13/14 to
P07-WRK-001, FX40-BR005/006, FX40-S06 and exact worklog.read/create/update/archive/
unarchive plus learning.report.read. FX40-AC004 (linked Time Entry deduplication)
remains NOT IMPLEMENTED by this manual-only slice; no fake source link support.
Skill merge/evidence history, Certifications, Plans, linked sources, Trash/purge
and complete overview/provider/report scope remain committed follow-on work.

## Data/API/security plan before implementation

Add learning.WorkLog with owner-composite platform.Resource identity, title1..200,
RecordedOn required date-only (unchanged by timezone changes), Minutes required
1..1440 for manual input, Notes optional<=20000, Category<=100, Employer<=200,
Active/Archived, PreArchiveState, actor timestamps/rowversion. Existing resource
links (TimeEntry/Course/Task/Project) stay absent from this API; unknown body members
are rejected. No backfill/mock/demo rows.0080 creates table/index/constraints,
WorkLog resource-type metadata and absent-only permission metadata, no Allow grant
or migration execution. SQL is sole authority, no cache/worker/provider/payroll.

- GET /api/v1/learning/work-logs/capabilities: current action booleans; report
  requires both report.read and worklog.read. No personal payload.
- GET /work-logs?status=Active|Archived&query=&from=&to=&cursor=: fixed25 rows,
  RecordedOn DESC then SQL GUID DESC. Literal title search<=200, optional inclusive
  date-only bounds; unknown state/range422. Encrypted authenticated cursor binds
  actor/owner/status/query/bounds and exact date/GUID boundary; max1024 token.
- GET /work-logs/{id}: current own read; safe404 missing/wrong owner. Metadata
  includes title/day/minutes/category/employer/notes/status/timestamps/quoted ETag.
- POST /work-logs: explicit manual fields; create Active resource+domain row,
  original opaque ACK201. PUT /{id}: same metadata, strict If-Match, Active only,
  update requires worklog.update+read. No lifecycle/owner/ref fields accepted.
- POST /{id}/archive and /unarchive: strict If-Match + {confirm:true}; Active to
  Archived with prior Active, restore only actual prior Active. Explicit native
  confirmation. Archived metadata is readonly. No auto archive or prior-state guess.
- GET /work-logs/report?from=&to=&status=Active|Archived&query=: inclusive required
  date window<=366 days, current report.read+worklog.read, same SQL filters. Returns
  per-day and overall entry count/gross manual minutes. It sums entries, does not
  deduplicate overlap or infer hours/payroll/competence. UI states that manual
  date/duration does not supply intervals to detect overlapping time.

Every query same Serializable transaction current actor/session/module/owner/action
before and after source read. Every mutation requires nonempty UUID Idempotency-Key,
current exact action, trusted owner lock matching existing Skill writers, source
revision/lifecycle, atomic platform registry update, metadata-only audit, opaque
typed receipt and final action recheck. Resolve original receipt before checking
current rowversion/lifecycle. Receipt binds actor/owner/operation/resource/request;
no title/notes/ETag snapshot returned on replay. Native unsafe requests require
CSRF, protected responses no-store, no command payload/authentication tokens in
logs/URLs. List cursor is encrypted context-bound seek metadata, not access authority. Invalid422,
denied403, missing404, stale412, missing/malformed ETag428, locked/conflict409,
persistence503; same-key conflicting intent409, original ACK201/200.

## UX and evidence

Learning Work Log tab/manual form uses current profile local date default, explicit
manual minutes, escaped title/notes/category/employer, read/detail/edit, date/state/
title filters25/page and explicit report load. New rows/reports never fabricated
on failure. Common dirty/focus/navigation/busy dialogs; freeze command/body/route/
ETag/key under uncertainty, same-intent retry, no automatic read after opaque ACK.
Clear private page on context change/denial/unmount; async generations fence stale
responses. Owner-scoped memory retains filters only, not records/notes. Unknown
command navigation/unload is guarded; explicit safe leave warns the original may
have committed and never resubmits it. Show recorded day as date-only, not UTC-converted instant. No disabled
source-picker placeholders that imply linked TimeEntry is implemented.

| Goals | Source/action | Code binding | Required owner assertion / result |
| --- | --- | --- | --- |
| NXG-FX40-G03; SYS11 | P07-WRK-001; BR005; worklog CRUD/report | Manual date/minutes validation + SQL gross report | 1/1440 bounds, date-only invariance, literal filters, gross totals: NOT RUN |
| SYS02/03/05/08 | Shared owner/source/concurrency/retry AC | Current authority, owner predicates, platform registry, opaque receipt/cursor | Foreign owner/action, stale/revoke/replay/race/tamper: NOT RUN |
| NXG-FX40-G03; SYS09/13 | BR006; FX40-S06; archive/unarchive | Actual prior state, readonly metadata, native confirmation/dirty guards | Archived edits denied, exact restore, uncertainty/focus/keyboard: NOT RUN |

Compile/build/typecheck only may detect implementation errors. Independent DB/
security/authorization review Pending. All functional/SQL/browser/runtime acceptance
NOT RUN. Rollback disables new routes/tab; retain owned rows/registry/audit/receipts,
no drop/delete/down SQL. Full Work Log including source-link AC004 remains open.
