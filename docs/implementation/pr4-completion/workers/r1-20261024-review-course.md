# Bounded independent Course recovery review — 2026-10-24

Reviewer: Einstein (independent of the root's manual recovery implementation). Latest session instruction supersedes the earlier implementation assignment: review only; write this report only. No product edits, commit, push, tests, fixtures, browser, application runtime or build/typecheck were performed by this reviewer.

Source: branch `impl/m01-s00-scaffold`, HEAD `14c76bcb6960e4ce53369dce388a8480b1c0299a`, with existing shared-workspace changes. PR #4 remains unmerged. Authorization: explicit session review request; DEC-20260909-014 and its current code-only amendment. No implementation or functional acceptance is asserted by this report.

Current product-file SHA256 at focused CR-01 re-review (working tree, not just HEAD):

| File | SHA256 |
| --- | --- |
| `web/Nexora.Web/src/CourseScreen.tsx` | `092B1C53EB42A5E8B9A4464660F4C929B50946A5A8011A93318C8C2F270B3D46` |
| `web/Nexora.Web/src/courseApi.ts` | `44B417C5480769FA266BA498ED7CA146FCDAB6DAF60B5E7506259FE87421410D` |
| `web/Nexora.Web/src/App.tsx` | `04FEE05592A2D8435E2A6D6F3385481ADA7FBD8E2655FF58CA49009E1232E716` |

CourseScreen is byte-identical to the initial review. App has changed for the root's other workbench work: its current hash is recorded for checkpoint context, not a renewed whole-App signoff. The original native Course wiring review was against App hash `61F1CCE88BD930263A65EC944C168047C074FF75F89DAC4822943AD50470E5A4`; focused inspection still finds the native Course mount/callback wiring. Initial courseApi hash was `3296EAEC7D72C04528C7619696960ED0CD7EF04679EC14F237404E9DA46CD36E`.

## Focused independent re-review disposition

**CR-01 resolved by source inspection; bounded source signoff for the correction. No confirmed blocker remains from this focused re-review.** Authorization: the owner's follow-up explicitly requested independent review of corrected bytes and report/hash updates only. HEAD remains `14c76bcb6960e4ce53369dce388a8480b1c0299a` plus working-tree changes.

- `writeCourse` now passes the exact contract status to `apiFetch`; the existing status check throws status-0 `UnexpectedAcknowledgement` before parsing/accepting a wrong successful status, including 202.
- Course create requires 201; metadata update, progress, milestone add/change and lifecycle commands require 200. SQL receipt replay returns the stored original status, so create replay still requires 201; other replay requires 200.
- IDs must be nonzero UUIDs; an existing parent must match the submitted parent, case-insensitively. Revisions must be quoted canonical Base64 for exactly eight decoded bytes: the shape check, decode-length check and encode round trip match SQL's quoted rowversion representation.
- Milestone add requires a generated valid child ID/revision. Update/completion/move require the submitted exact child ID and a valid child revision. Non-child operations and milestone delete allow only null/omitted child acknowledgement fields. Every non-purge write requires a valid parent revision; purge requires an explicitly null parent revision. These match the current `CourseAcknowledgement` DTO and SQL `Ack`/purge branches.
- Invalid status/shape throws into the unchanged CourseScreen unresolved-request path. It does not mark `committed` or clear the original closure. No request body, key, revision or wrapper signature was changed by this reviewer.

This closes the source defect, not functional acceptance. No test, browser, application runtime, fixture, compiler/typecheck or lint command ran during re-review. The unrelated App workbench changes are outside this focused signoff. Human-owned QA and release gates remain pending.

## Original confirmed finding (historical; now resolved)

**CR-01 — P2: the ACK validator accepts non-final HTTP success and insufficient milestone identity.** Location: `web/Nexora.Web/src/courseApi.ts:14–21`, `milestoneAdd` and `milestoneChange`; consumer: `CourseScreen.tsx:79–100`.

`writeCourse` calls `apiFetch(path, init)` without its supported `expectedStatus` argument. `apiFetch` accepts every `response.ok` status in that case. Thus a JSON 202 carrying a syntactically valid parent `itemId` and `etag: null` passes this validator. `executeWrite` then marks the operation committed, clears the retained original request and displays the saved acknowledgement. A pending response must not finalize recovery. The current service contract returns 201 for Course create and 200 for the other writes, including milestone add and committed receipt replay; this is visible in `SqlCourseService.Create`, `Ack`, and `Mutate`. UX-09 explicitly distinguishes 202 from completed success.

Additionally, the validator accepts any string (including empty) or null parent ETag for every operation, and never examines `milestoneId`/`milestoneETag`. A milestone update/completion/move acknowledgement naming a different child, or an add acknowledgement omitting the generated child, can therefore retire the original request as successful. The actual server DTO already supplies those fields; no new schema fields are needed. `SqlCourseService.Ack` supplies the affected child for add/update/completion/move. Delete intentionally omits the removed child, and purge legitimately has null parent ETag, so validation must preserve those operation-specific exceptions.

Required correction: give each wrapper its documented final status and expected acknowledgement shape/identities; require meaningful revisions where returned by the current contract; require the expected child identity for existing-child operations and a valid generated child for add. Reject unexpected status/shape as `UnexpectedAcknowledgement` so the existing uncertain-original-request path retains the same key/body/revisions. Preserve valid purge/delete and receipt-replay responses. This is a source-confirmed validation defect; no malformed response was injected or observed at runtime.

Initial disposition: changes requested for CR-01; blanket source signoff withheld. Superseded by the focused re-review disposition above for the corrected courseApi fingerprint.

## Source observations without an additional confirmed defect

- All existing writes converge on `executeWrite`: metadata create/update, progress, milestone add/update/completion/move/delete and lifecycle commit. `persist`, `childAction` and `commit` capture copied records, payload values, request key and revisions before sending. Retry uses `originalWrite.current.send`, rather than rebuilding from mutable form state. The synchronous `inFlight` ref blocks overlapping writes; a retained original prevents replacement commands.
- Network/status-0, 5xx, 408 and the listed unresolved receipt codes retain the original. Once unresolved, even a subsequent definitive denial does not silently replace that original. Editing/confirmation dialogs are hidden during uncertainty. Explicit stop explains that the server may have committed and that stopping does not undo it; it requires a separate confirmation. No local/session storage was added for private commands or records.
- Valid ACK completion is separated from subsequent read availability: the retained command is cleared before refresh, and a failed post-ACK read does not resubmit the write. `load` obtains capabilities before protected GETs, and `refreshPane` checks the current read ref before and after awaits. Refresh is current-read proof, rather than a source of mutation authority.
- Current 401/403/404 handling clears capabilities, list/cursor, editor/draft, pane, lifecycle/remove dialogs and conflict projections, invalidates the read generation and aborts the active load. The capabilities-read-denied path also clears protected UI. An unresolved original can remain hidden in its closure for same-context reconciliation; it is not rendered as a stale private projection. 401 delegates to App session clearing, which unmounts the authenticated screen.
- Native Course navigation uses `courseId`, `onOpenCourse` and `onBackToCourses`; App supplies an owner/profile/module/resource-keyed Course mount. `/learning/courses` and UUID detail routes are internal. `safeReturnPath` rejects foreign origins, credentials, query/hash and unrecognized paths; login return rebuilds the native route. Normal navigation and browser Back pass through the shared dirty-leave guard. Course blocks these transitions during an unresolved request or in-flight work; ordinary metadata/progress/milestone editor drafts use the dirty-leave flow. No external Course URL is opened by this wiring.

These observations describe the inspected source paths only. They do not certify browser timing, accessibility, server authorization, SQL replay or functional acceptance. No additional confirmed privacy/navigation defect was established in this bounded pass.

## Contract and evidence binding

Sources read: root `AGENTS.md`; `.agents/skills/nexora-engineering/SKILL.md`; `.ai/profiles/nexora-implementation-agent.md`; Technical Lead README/core rules and `skills/code-change-review.md`; `.ai/routing.json`; `.ai/verification.md`; frontend and security routed rules/skills; owner-isolation README/rules/review skill; verification routed skills/completion guard. The code-only amendment overrides their generic requests to add/run tests.

Product sources: `docs/README.md`, `docs/delivery/README.md`, `docs/delivery/01-current-scope.md`; current owner decisions `12-owner-decisions-local-e2e-implementation.md` and `13-owner-decisions-20261019-pr4-blockers.md`; goals README/system goals, P07 goal and task/evidence template; FX40 module goals; `docs/features/00-shared-behavior.md`, `40-learning-and-work-log.md`; action catalog `modules/40-learning.md`; UX globals 03/05/09/15 and module `40-learning.md`; `learning-course-contract.md` under `pr4-missing-features-20261002`; common frozen-retry/safe-leave guidance in the manual Work Log contract.

Supporting source inspected: `api.ts` (`apiFetch`), `navigationGuard.ts`, `ResourceFormDialog.tsx`, `CourseEndpoints.cs`, `CourseContracts.cs` and relevant `SqlCourseService.cs` ACK/receipt paths. Backend behavior was inspected only to establish the existing wire contract, not independently accepted in full.

| Goal / requirements | Exact bounded actions | Source evidence | Execution / remaining gate |
| --- | --- | --- | --- |
| NXG-SYS-05/13; FX-COM-AC-002/003; P07-CRS-001, FX40-S03 | `learning.course.create/update/progress/milestone` | Frozen closures and synchronous guard; CR-01 corrected ACK status/shape/identity checks | Independent focused source re-review complete; functional verification NOT RUN |
| NXG-SYS-02/03/08; FX-COM-AC-001/004/005 | `learning.course.read`, composed write/read denial | Capability-before-GET, generation/abort/current-read guards, clearing paths and App mount key | Source reviewed only; owner/security runtime acceptance NOT RUN |
| NXG-P07-01; NXG-FX40-G01/G02; FX-40-BR-001/006, AC-001/002 | `learning.course.complete/abandon/archive/unarchive/trash/restore/purge` | Frozen lifecycle preview/date/key; native Course wiring; no Task/Goal mutation introduced in these files | Recovery/navigation source review only; broader domain AC acceptance NOT RUN |

Executed checks: read/search commands, repository status/branch/HEAD inspection using a per-command `safe.directory` override, and SHA256 fingerprint reads. No global Git configuration changed. Initial plain Git inspection was rejected for workspace ownership; the per-command override allowed read-only inspection. No compiler, typechecker, linter, application, test runner, SQL, provider or browser command executed. Existing root/worker build results are not claimed as reviewer evidence.

Rollback/impact: report-only change; no API/DB/migration/runtime behavior changed by this reviewer. CR-01 remediation and focused independent source re-review are complete at the recorded fingerprint; human-owned functional QA and release gates remain pending.
