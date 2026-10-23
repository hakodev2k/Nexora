# Independent QA/security review — 2026-10-23

Reviewer: `/root/qa_review_oct23`, separate from the Browser, SQL and Windows
test authors. Initial source: PR4 branch `impl/m01-s00-scaffold`,
`fbafbaa482b094f26f067f1697d1f9d610e5367c`; root verified1748file checkout.
This is a bounded evolving review, not complete feature or R1 acceptance.

## Authorization and sources actually loaded

Current user explicitly requested continuing logic/E2E testing of implemented
features. That current task and the earlier explicit isolated synthetic QA
authorization supersede the historical code-only amendment for this test scope;
AGENTS.md remains unchanged. D01–D09=A, personal ownership, real-provider/secret/
production restrictions and DraftPR4 are retained. No reviewer source fixes,
grants, deployments or credentials were introduced.

Read AGENTS.md; nexora-engineering skill; implementation profile/routing;
TechnicalLead core rules; .ai/verification.md; documentation/delivery entry
points; system goals; current D01–D09 decision record; QA contract/test-strategy
skills; completion-claim guard; data-protection, identity/access and secrets rules.
Read WP-WIN implementation-plan5.5 and W01–W10 verification matrix, actual
LocalAccountMessageSink test registration, current browser specs/config and
runner JUnit sanitization, FX25-S02 modal UX contract.

Applicable goals: NXG-SYS-01/02/03/05/07/08/13/14. Evidence is required at the
boundary under test: actual SQL transactions are not proven by MSW; OS deny is
not proven by application identity rejection; visual snapshots are not complete
interaction, accessibility or business acceptance.

## Baseline evidence independently inspected

Parsed the downloaded sanitized browser JUnit from CI39792570922,
artifact12433005954, actual tested PRmerge2497194:
**28cases:21passed,2failure,5error,0skipped**. The two failures are the registration/
login/keyboard journeys; errors are mobile password reset and auth/navigation
audit groups on both viewports. Uploaded exception details are deliberately
masked. These counts are baseline evidence, not results for later changes.

Source diagnosis sent to browser author:

- `PERSONAL SPACE` is inside the new closed native account details panel;
  `m01-identity.spec.ts` expects it visible immediately after login. Home instead
  renders HOME. Test should assert current authenticated semantic content and
  explicitly open account disclosure when that panel is being exercised.
- Logout is also inside that disclosure, so reset must open it before clicking
  Logout. Do not remove reset/login or private-capture assertions.
- CommandPalette has an explicit FX25-S02 DIALOG profile. A generic scanner that
  requires visible main for every modal state can report false blockers. Exact
  dialog name/combobox/Escape/focus assertions are appropriate; blanket missing-
  main acceptance is not.
- Sanitized stage diagnostics should contain only static allowlisted codes,
  booleans and bounded numeric outcomes; never raw Playwright/network exceptions,
  account email, token, capture key, password or payload. Existing JUnit scrubber
  clears all failure detail/properties/output, so a separate safe diagnostic
  artifact needs an explicit upload path.

These are source-grounded triage findings; author runtime reproduction and final
CI are needed before claiming the original browser failures closed.

## Review conditions communicated before implementation

SQL D08: same original key/body after deletion must produce opaque committedACK
without DTO/name, second Tag or additional audit; a changed body must conflict,
and current original-write authority revocation must still deny. Test both native
Organization and Documents boundaries. Goal eligibility must be exercised with
real Admin current SQL Allow/Deny/Unset/revoke, not only manifest booleans.

Windows W01–W10: actual three distinct nonadmin child SID/PID/start-time/nonce
contexts; A/B positive against actual protected capture; C reads staging control
and reaches native filesystem boundary; error5 denial cannot be replaced by
missing path/executable, timeout or app refusal. Preserve runtime/operator
FullControl policy. C cannot receive DELETE_CHILD on capture parent. Trusted
staged executable/config/report provenance and exact SID/creation-owned account/
process/path cleanup are required. Concrete timeout/crash/cancel cases, temporary/
pending/final stages, restart and insecure-ACE rejection must run before the full
required Windows gate can pass. Partial source implementation is not that proof.

## Current review status

### IR-QA-01 — deterministic stale-response test synchronization (resolved)

Root's new SharedResourceAccessibility stale-token regression initially rerendered
before confirming the old MSW handler had started, then asserted absence with
waitFor immediately after releasing its deferred response. Absence already held,
so that assertion could pass before late delivery. Requested an old-request-
started latch before token change and deterministic response completion/React
flush before final absence assertion. This is a test-quality finding, not a new
claim of a reproduced production leak. Root added the old-request-started latch before rerender, then releases the old response inside awaited React act with an old-response-returned latch. Reviewer reread and independently ran the focused test file: all3 tests passed. This closes the synchronization finding.

Source review of root's SharedResourceScreen div→main semantic change found no
authority/projection/request behavior change. It gives public loading/unavailable
content a main landmark. Two basic loading/unavailable/missing-token regressions
are coherent; the synchronized stale-response case now passes independently. Expanded
UIUX workflow path triggers do not increase workflow permissions; they make
application changes eligible for fresh audits rather than reuse historical shots.

| Workstream | Author | State |
|---|---|---|
| Public share semantic main + regressions | root | Source reviewed; reviewer independently3/3 Vitest PASS |
| Browser failures + real SQL journeys | qa_browser_oct23 | Current source reviewed; final real browser evidence pending |
| SQL owner/receipt/Goals regression | qa_sql_oct23 | 12 cases reviewed; realSQL execution pending |
| Windows real principals | qa_windows_oct23 | Corrected source reviewed; findings source-resolved; actual Windows runtime pending |

### SQL assertion review

All business mutations/read assertions use actual HTTP endpoints against SqlApiFixture;
only disposable test session/policy setup and independent row/audit counts use SQL.
Admin tests exercise explicit current Unset/Allow/Deny/revocation, dependent reader
permissions, and completed-receipt authority checks. D08 covers Organization,
Documents and Organization wrapper→Documents. Opaque deleted receipts assert exact
three-field ACK, no ETag, no resurrection and no additional succeeded audit.
Reviewer requested whole owner/namespace/name counts to detect a second resource
under a different id; author added these (deleted=0, concurrent=1). Concurrent
Task.WhenAll submits both requests; it does not prove a forced SQL lock schedule.
Root nullable anonymous title branch compile correction was reread and preserves
request payload/expected results. Current source is suitable for compilation/realSQL execution without weakened
expected outcomes; no runtime result is inferred from source review.

### Browser assertion review

Identity assertions now use actual authenticated Home/me state; native account
menu is opened before logout. Reset commit/login and private-capture assertions
remain. Exact CommandPalette route checks its specified dialog, named focused
combobox/listbox, Escape, restored Home and focus outside closed dialog, retaining
axe analysis. The new Goals journey writes through UI then reads through actual
API: numeric precision/progress/history, Active at100percent, archive/unarchive
and reload persistence. Its extra page shares the authenticated context, so it
proves refreshed UI persistence, not a second login/session.

Static safeStep enum and JUnit allowlist retain only source-defined failure stage;
raw assertion/payload details remain masked. The nine area groups moved out of
base45second config into independent240second UX config. Reviewer independently
ran Playwright discovery: all18 audit cases (9groups×2viewports) remain discoverable.
This is inventory evidence, not browser pass. Expanded workflow triggers and
unchanged contents:read permissions are appropriate. Full9-shard outcomes remain
required separately from base browser suite.

### Windows findings sent to author (resolved in source; runtime pending)

- IR-QA-02: cleanup-capture runs operatorB, but intentionally insecure inherited
  fixture givesB only ReadAndExecute and no parent DELETE_CHILD. Cleanup should
  use the owningA or validated parent rather than fail on that fixture.
- IR-QA-03: AssertFault catches every ProbeFailureException, so startup/config/
  identity failure can be mislabeled as expected timeout/cancel/crash. Require
  expected failure reason and started-child handshake. For crash, A must prove
  actual temp content/policy before C denial/sweep; error5 behind an inaccessible
  directory alone cannot establish that a crashed temp existed.
- IR-QA-04: Account.Create can throw after successful NetUserAdd but before the
  object is returned/added to tracked list (SID lookup or secure-string setup),
  leaving an account untracked. Roll back the exact freshly owned account on
  this intermediate failure. Never delete by an unverified generic account name.

Additional trace requirements sent to author: W03 explicit ReconcilePending; W08
actual operator read rejection/skip without ACL repair (unsafe directory throws,
unsafe owned file is skipped per current ReadCaptured contract); W09 injected
ownership-manifest mismatch refuses destructive cleanup. Guard source alone is
not an executed negative case.

Author corrected all three source findings: runtimeA deletes its inherited fixture;
expected fault enum is accepted only after an atomic child identity-ready report,
with A proving actual crashed-temp content and ACL; Account.Create wraps post-add
initialization with native USER_INFO_23 SID/comment verification and guarded rollback.
W03 now invokes actual ReconcilePending then promotion replay. W08 checks both
unsafe directory init/read rejection and an unsafe owned final file skipped without
repair, restoring only the owned fixture afterward. W09 explicitly injects and
restores a manifest mismatch. Reviewer reread these corrections, including root
RawToken compile fix: no remaining source review blocker. Native Windows runtime
is still required; compile/source review does not prove any W01–W10 runtime pass.

Reviewed native C probes require error5 for enumerate/read/create/overwrite/delete/
DACL/owner and snapshot preservation; distinct actual child SID/PID/creation-time/
nonce and absence of bypass privileges are checked. No password is placed on CLI
or reported. These strengths do not waive the findings or actual Windows gate.

### Additional WorkLog9 source review

Read the manual, filter/history and trash contracts dated2026-10-18 and current
SqlWorkLogService. New SqlWorkLogCoverageTests uses real HTTP for all business
writes; direct SQL only configures disposable Admin policy or reads independent
counts/tombstone. Expected opaque ACK,403current authority,404owner,412revision,
428missing revision, readonly Archived and exact Active/Archived restore agree
with those contracts. Receipt replay asserts history/audit once; purge asserts
source/private history absent and minimal Purged registry retained. No test-only
business state insertion or weakened production guard was introduced.

IR-QA-05 (source-resolved): date-filter scenario initially created every source on the same day;
its result could pass with ignored date predicates. Requested matching entries
on the day before/after to prove exclusion, plus post-Trash list/count agreement.
Root added matching400/500minute sources on2026-10-17/19 and post-Trash
report/list entries1 checks. Reviewer reread: ignored bounds now fail the
expected2entries/150minutes. This was a bounded test gap,
not a production defect claim. Preview expiry/tamper/foreign source, retained
references, cursor scope/pagination, transaction rollback and browser uncertainty
remain unproven by these9 cases and must stay residual.

## Independently executed current checks

- Vitest5: `vitest run src/SharedResourceAccessibility.test.tsx --maxWorkers=1`,
  exit0,3passed,1file,3.13seconds,2026-10-23T23:39:53UTC. Initial invocation with
  unsupported minWorkers option failed CLI parsing, then corrected; it ran no tests.
- Playwright UX `--list` with synthetic dummy loopback configuration: exit0,
  exactly18cases; no browser/API/SQL operation performed.
- `python scripts/dev/test-e2e-sanitizer.py`: exit0,1test passed (0.002seconds);
  canary/unknown stage/attributes/streams/properties removed and repeated
  sanitization idempotent.
- `git diff --check`: exit0 at reviewer checkpoint.
- Baseline sanitized JUnit parsing and source/contract review as described above.

Reviewed checkpoint SHA256:

| File | SHA256 |
|---|---|
| web/Nexora.Web/src/App.tsx | 8df8ac7d552a83e32ceb21d3617ca2ee33a302500fc060f0906e0f87e1b10db1 |
| web/Nexora.Web/src/SharedResourceAccessibility.test.tsx | d6b9eea5130ccb9fee85421aa240231e422001509353964cd5a48fddf921a66a |
| tests/Nexora.IntegrationTests/SqlReleaseCoverageTests.cs | af66816d2c3ba30cbc296df17426dea18f88b7e6fbf3ae44b93e09a183ddea3c |
| tests/Nexora.UnitTests/WindowsCaptureProcessTests.cs | 06455e2e1319ded76b870765b6b45351a32dacd2f00aec5b5351e5598e2a5e95 |
| tests/Nexora.IntegrationTests/SqlWorkLogCoverageTests.cs | 98850cc4a5721a0af27d665d34c950fb7d00f969ae3710b90b5476260aa707d5 |

Hashes bind source review only and must be refreshed if author changes it. No SQL,
full browser, actual Windows or complete R1 pass is claimed at this checkpoint.

Root reports pushed3489f594 and runningCI40008957176; current CI is pending,
not reviewer-verified pass. Local reviewer git object lookup subsequently failed
because an alternate object path was unavailable; source fingerprints above
were calculated directly from readable files. Parent notified; no git repair or
implementation mutation was made by reviewer.

Parent later reports actual Windows CI failed generic Protocol/InvalidOperationException;
this remains a runtime failure, regardless of source findings closed above. Reviewer
read new diagnostic delta: static child5stage allowlist and exact parent parser
publish no raw stderr/payload. No guard was relaxed. Reviewer noted that failures
before ready handshake need the same strict stage parser in handshake exit branch
to be useful; current phase-only output is safe but incomplete. CI SQL outcome
remains pending as reported by parent. Root subsequently added the same exact allowlist parser on child exit before
handshake; reviewer reread it and updated Windows hash above. Diagnostic changes
are source-approved only; they do not fix or reinterpret the actual Windows failure.

Parent reports SQL run at3489:274passed/9failed; the12 new SqlReleaseCoverage
cases passed, while all9 WorkLog cases failed at initial create403 because
disposable fixture preserves FX40 disabled policy. This is a real failed run,
not feature acceptance. Proposed correction reuses existing Course test pattern:
scoped real SuperAdmin module preview/commit before creating test sessions, then
awaited restoration of original system/registration booleans. It preserves the
production/module default and normative403 tests. Reviewer requested retaining
bounded status assertions rather than copying raw preview-response failure logs.
Corrected scoped fixture source reread: await-using Enable precedes every
case session, real Admin preview/commit asserts blockers empty and exact success,
awaited disposal restores original booleans, and no raw preview-body assertion
logs were introduced. Source-approved; corrected runtime rerun remains pending.

Parent later reports Windows df7 fails at reject-insecure; earlier A/B/C publish,
seven native deny probes and restart ran before W08. This is partial failed-run
evidence, never full W01–W10 pass. Reviewer read extended W08 static substage
literals and exact individual-line stderr allowlist: unrelated warning lines
are discarded, no raw exception/payload printed, and all assertions retained.
Reviewer suggested watching UnsafeFileRestore: unmodified FileSecurity snapshot
may have no dirty sections to persist; force owned original ACL restoration if
confirmed. This is a source hypothesis awaiting exact runtime failure stage.
Current Windows source hash above includes diagnostics, not an acceptance pass.
