# FX16 immutable progress history, local continuation

Owner: implementation agent; impl/m01-s00-scaffold at
1cd27a46a2ba1f24ab2b6f32f84771230f4b1761, existing PR4, no merge.
Current user requests remaining local code autonomously; DEC-20260909-014 and
AGENTS code-only amendment apply. No tests/fixtures/runtime SQL/provider execution.
Selected rules: nexora-engineering, implementation profile, Technical Lead core,
backend/frontend/database/security/owner-isolation/verification routes.

## Sources and plan

UX16 FX16-S03 explicitly includes history; action catalog16 goals.goal.history
is owner SELF only, excluding Support/link. Database05 GoalProgress is append-only
private evidence with numeric Value or Boolean Checked, Note, RecordedAt. Existing
0014 implements those fields and owner-composite FK. NXG-FX16-G01/G02 and shared
owner/concurrency/privacy acceptance bind this bounded history projection.
This is progress evidence only, not complete Goal metadata/lifecycle history;
no historical before/after state is fabricated from current Goal/target titles.

GET /api/v1/goals/{goalId}/progress-history?cursor= returns items and nextCursor,
fixed25 rows, RecordedAt DESC then SQL GUID DESC. Each item: immutable evidence ID,
targetId, kind, current target label (explicitly marked current), exact numeric
string or Boolean, note and recorded instant. No actor identity/SQL audit payload,
no total or hidden count. Current goals.goal.history + goal.read + target.read,
current actor/session/module/owner, own nonDeleted/nonTrash Goal and own Target.
Numeric/Boolean shapes only; Tasks has no manual progress. Completed/Abandoned/
Archived allow own history. Every page rechecks source and authority in one
Serializable read transaction before response. Invalid cursor422,
missing/wrong owner/Trash404; existing Goals ModuleUnavailable uses409 (including
unavailable authority), not a new403 envelope. Common CSRF for unsafe routes remains unchanged,
no-store applies. The query performs no native detail-open/Recents side effect.

Cursor is encrypted/authenticated with purpose-derived server key, binds actor,
owner, Goal and fixed limit; payload contains exact timestamp and GUID. Bounded
strict canonical encoding, SQL tuple seek. Live append-only history; new records
appear on explicit reload, no snapshot promise or sensitive browser persistence.
Use existing idempotency secret only for key derivation, never expose/log it.

Native Goal detail offers explicit load/next/reload; clear old page on request,
revision/selection change, stale generation, unmount or denial. No automatic
history fetch after a command/opaque acknowledgement. Timestamp display uses
existing explicit profile display preferences; text escaped by React. No edit,
restore or delete controls. Keep failed fetch distinct from empty evidence.

0077 adds absent-only Resolved permission metadata for existing goal.history;
never overwrite existing Deny/status or create Allow grants. Manifest registers
migration only, not executed. No business rows/backfill. Rollback disables route
and UI; retain evidence and permission metadata, no destructive SQL.

| Goals | Exact source / action | Code binding | Acceptance evidence |
| --- | --- | --- | --- |
| NXG-FX16-G01/G02 | UX16-S03; goals.goal.history; DB05 GoalProgress; feature16 explicit progress and BR003 | SQL own current source + immutable progress projection | Numeric/Boolean chronology, notes, no fabricated lifecycle: NOT RUN |
| NXG-SYS-02/03/08 | Shared owner/action/source/privacy AC | Same-TX authority/source proof, encrypted bound cursor, UI generation fencing | Cross-owner/revocation/Trash/tamper/tie pagination: NOT RUN |

Only compile/build checks may be reported as implementation-error detection.
Independent security/DB/authorization review Pending; functional/SQL/browser
acceptance NOT RUN. Remaining history/lifecycle/module scope stays IN PROGRESS.
