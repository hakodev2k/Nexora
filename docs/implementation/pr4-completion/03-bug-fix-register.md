# Current bug fixes and contract reconciliation

Source checkpoints: Toolbox94fac58, backendccbc64d, Knowledge053b015,
Productivity4eed0af, frontend738f528. Final CSS/evidence are separate continuation.
Whole product remains INCOMPLETE; source review is bounded, not release acceptance.

| Finding / priority | Root cause and reproduction | Actual change | Evidence and limits |
|---|---|---|---|
| ACCESS-01 P0 | Explicit Admin Allow rejected for five installed Goals actions | Exact eligibility keys in ActionGrantPolicy; no grants/defaults changed | Unit54pass; independent source review; SQL gate separate |
| TEST-01 P0 | Unit oracle denied installed Documents restore_version and Notifications source.open | Reconciled exact positives; SYSTEM/unshipped assignment negative retained | Current unit PASS |
| TEST-02 P0 | Expected migration inventory stopped0058 while production manifest includes0085 |27literal reviewed filenames added to independent unit expected list |86oracle==86manifest==86disk; no production manifest change |
| TEST-03 P0 | D07 owner revoked list incorrectly expected empty | Inactive metadata with null token; active exclusion/viewer and detail404 retained | Integration build PASS, SQL rerun pending |
| TEST-04 P0 | Markdown literal source treated as forbidden HTML by old SQL test | Assert201 and exact literal stored source; renderer remains inert | SQL pending; no raw HTML execution approved |
| HARNESS-01 P1 | Linux dotnet child launched without assembly; premature Dispose masks error | Dotnet host prepends current assembly; cleanup retains process until catch | Required Linux suite32pass/0fail/2Windows-only skips |
| DASH-01 P1 | QuickCreate/LayoutEditor/Drilldown siblings shared identical keys | Purpose-prefixed keys preserve React identity | Component and controlled browser one-QuickCreate assertion |
| DASH-02 P1 | Equivalent profile/callback rerenders reset layout without editor refetch | Semantic context dependencies/latest callback ref | Equivalent-profile regression |
| DASH-03 P1 | Missing layout field propagated undefined and never settled spinner | Typed response validation gives actionable error; null-layout fixture correct | Invalid-response regression |
| UI-01 P1 | Finance duplicated inner/modal headings and fragmented actions | Single heading; form-associated shared footer Save/Cancel | Component80pass/1skip; controlled mobile/dialog journeys |
| UI-02 P1 | Mobile account/header density, nonshrinking form/grid and contrast | Native compact disclosure+Escape focus; semantic colors/reflow; selector wrapping |3width probes pass; professional UX still partial |
| TIME-01 P0/P1 | Late history/purge/read response restores private state after deny/selection change | Request/selection epochs and exact-capability cleanup before unrelated reads | Productivity10pass; independent review |
| TIME-02 P1 | Native form ignored returned validation object | Error set and rendered inside modal; invalid form sends no write | Reviewer-found regression repaired |
| FOCUS-01 P1 | Polls overlapped and dependent private forms survived capability loss | Next poll after completion; exact prefs/cancel/record/source cleanup | Deterministic deferred/poll tests |
| FORM-01 P1 | Cancel/Escape lost dirty drafts; fields could change during write | Native dirty ResourceFormDialog and submission locking | User interaction regressions |
| DOC-01 P1 | Compare showed only two raw snapshots; selector change kept stale comparison | Bounded exact-source line diff/metadata with expandable originals; selection clears old diff |18Knowledge tests; independent15diff tests overlap subset |
| DOC-02 P1 | Mode switch allowed late Markdown import preview to apply into Block draft | Epoch/file/preview invalidation, source provenance detach while preserving body | Deferred-preview and selector regressions |
| TOOL-01 P1 | Regex Count rescanned full match collection after output cap | At most101probed matches,100returned; cooperative elapsed check |10SQL regression cases compiled; checkpoint CI260pass2unrelatedfail |
| TOOL-02 P0/P1 | Regex parser exception echoed private pattern; invalid UTF8 escaped validation | Safe RegexInvalid title and DecoderFallback→InputInvalid422 | Marker-negative/malformed input SQL cases |
| TOOL-03 P2 | SHA1 alias omitted checksum warning emitted for SHA-1 | Alias warning aligned | SHA1/SHA-1/MD5 SQL cases |

Independent review also caught and resolved a SQL-test tuple helper compile error
and undefined popup surface CSS token fallback. Those are implementation-review
findings, not claims of additional reproduced production defects.

D08 native Tag replay implementation was not changed in this continuation;
expanded deletion/replay/current-authority acceptance remains tracked work.
Windows actual A/B/C gate remains failing; no assertion or security protection
was weakened to make it green. All unexecuted cases remain unverified.

## Oct23 source repairs — runtime verification pending

| ID | Reproduction / root cause | Change | Evidence / closure gate |
|---|---|---|---|
| IR-01/IR-03 | Denied source rows affected pagination; scan-until-full repair had unbounded batches/retained state | Source-owned eligibility before TOP26; bounded definitive source/link proof and cursor revalidation on one connection | sharing-pagination-review.md; independent-review.md; SQL NOT_RUN |
| IR-02 | Compatible request digests could select divergent successful receipt identities | Require exact active success status/code/JSON agreement; ambiguity conflicts | Independent source review; final SQL NOT_RUN |
| BUILD-23-01 P1 | npm build on Windows resolved DocumentVersionDiff to similarly named algorithm .ts; TS2305/1261/1149 | Rename algorithm and existing test stem to documentTextDiff; preserve component/algorithm behavior | Reproduced compile failure, subsequent tsc/Vite PASS; no functional test execution |
| BUILD-23-02 P2 | Stash/remote integration duplicated listCalls declaration | Remove duplicate declaration without changing assertion behavior | TypeScript build PASS |
| R23-LT-01 P2 | Post-ACK navigation occurred while flight/modal busy guard still held | Queue committed destination; normal guarded navigation after settled render | review-oct23-learning-toolbox.md; source re-review required; runtime NOT_RUN |
| R23-LT-02 P2 | Toolbox catalog denial retained prior input/output and stale catalog | Shared authority cleanup, response generations and private-state clearing | review-oct23-learning-toolbox.md; source re-review required; runtime NOT_RUN |

No current functional PASS count is inferred from compilation. Existing prepared
regressions belong to the prior authorized QA work and were not executed here.

## CI follow-up SQL-SHARE-01

Controlled source deletion/List returned503 rather than200 in CI40066173203
(289PASS/1FAIL). Source-first eligible relation plus explicit LOOP/FORCE ORDER
now covers the advisory query as well as definitive proof. Tests/assertions are
unchanged. Independent source review has no blocker; final-source SQL rerun is
required. See [reproduction, evidence and closure](11-sharing-concurrency-20261024.md).
