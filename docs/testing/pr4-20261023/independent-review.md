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
| Windows real principals | qa_windows_oct23 | Current source reviewed; findings below pending author remediation and Windows runtime |

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
Current source is suitable for compilation/realSQL execution without weakened
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

### Windows findings sent to author (open)

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

Reviewed native C probes require error5 for enumerate/read/create/overwrite/delete/
DACL/owner and snapshot preservation; distinct actual child SID/PID/creation-time/
nonce and absence of bypass privileges are checked. No password is placed on CLI
or reported. These strengths do not waive the findings or actual Windows gate.

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
| tests/Nexora.IntegrationTests/SqlReleaseCoverageTests.cs | 29c19f03bbcffba7c2aba73ba58fbe9104b8a09a142c53fcb8d4142a806f3dcd |

Hashes bind source review only and must be refreshed if author changes it. No SQL,
full browser, actual Windows or complete R1 pass is claimed at this checkpoint.
