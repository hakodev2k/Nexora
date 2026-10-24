# D09=A — PR #4 remains Draft: exact-source release and acceptance controls

Decision source: DEC-20261019-PR4 D09=A, DEC-20260909-010/014; scope R1 40 feature groups, active catalog actions + source-derived BR/AC/UX/DB and cross-cutting security requirements. **No merge, production launch or live provider execution.** PR review is documentation evidence, not full technical approval.

## Current baseline and evidence provenance

At initial review `7a1f157621d7dc5b734f79ec8caf5e0dec71f039`, PR is open/draft/unmerged. Last inspected **application** CI for `4d9876ac00457f8888b864e0dc63721b5a316eeb`: [run 39151552763](https://github.com/hakodev2k/Nexora/actions/runs/39151552763), *failure*:
- SQL/API xUnit **250 pass / 2 fail**: Markdown raw HTML expected422/got201 at `SqlApiIntegrationTests.cs:549` (approved escaped literal rule needs confirmation), revoked owner ShareLink list at `SqlSharingRepairTests.cs:408` (D07 owner confirms owner-safe metadata).
- Backend xUnit **47 pass / 2 fail**: grantable action manifest for `documents.page.restore_version` and migration-list expectation only through0058 while installed migration list through0085. Review authoritative action metadata/independent expected migration inventory before asserting new expected values.
- Frontend Vitest **149 pass / 2 fail / 1 skip**: Home dashboard fixture unresolved load; Notifications unread ETag action control not reached. Both require source/fixture isolation; not established as production bug solely from a test failure.
- Browser E2E sanitized artifact `12144743847`: **8 pass / 2 fail**; same `m01-identity.spec.ts` register/verify/login/keyboard journey fails on desktop and mobile. The sanitized JUnit masks assertion detail; rerun isolated synthetic environment and collect safe per-step evidence; never publish raw secret-bearing call logs.
- Windows ACL **30 pass / 4 skip**, required actual A/B/C distinct process/principal boundary test skipped => fail. Linux filesystem pass; Agent Baseline different workflow pass. Overall **NOT GREEN**.
- Earlier QA snapshot `QA-20261018T140148Z-acc869` on a different dirty local source reported 20 findings: 2 reproduced UI problems (Dashboard 3 Quick Creates and overflow 320/768), 2 spec/oracle issues (now D07/D08 decided), 16 test/verification issues, 87 absent actions in its limited inventory, 1825 not-run per-operation templates. Rebase and retest, never treat those historical counts as exact-head certified product coverage.

New documentation commits change HEAD after `4d9876a`; previous CI results are historical and **must not** be reported as tests on this docs revision.

## Definition of Ready vs Done

| Stage | Required evidence | Forbidden inference |
|---|---|---|
| SPECIFIED | Current normative requirement, BR/AC, action, UX, DB and owner decision IDs | Not a handler |
| CONTRACT_READY | Reviewed typed API, status/error matrix, owner/transaction, SQL migration/rollback, UI states, negative/performance/edge tests and no unapproved PO dependency | Not deployed/verified |
| CODE_PRESENT | Registered route/service+SQL+UI source and exact component test, preserving action registry/denial | Not runtime-safe |
| EXECUTED_VERIFIED | Full backend+frontend+SQL/API+real browser/Windows/fault tests run **and pass on same final SHA/source tree**; E2E uses synthetic real SQL/API, not only MSW | Not all R1 AC necessarily covered |
| ACCEPTED | Full active scope RTM traced per AC with current source/test/review, no blocking High/Critical risk, Product Owner/module acceptance | Not production approved |
| PRODUCTION_APPROVED | Separate security/operations backup/restore/capacity/RPO/RTO/provider/cost/launch PO Go/No-Go | Explicitly not authorized now |

All 40 FX must have explicit requirement-by-requirement disposition; R1 is not complete because 40 menu cards render or action catalog has 733 keys. Use evidence ledger with columns `requirementId, source:line:sha, actor, exact action/gates, API+UI+SQL pointers, positive/denied/state/race cases, implementationStatus, runId, testedSha, testResult, independentReviewer, residualBlocker, owner, decisionId`. No row marked completed until all required parts exist; simulation is an independent class, not equivalent to real-provider acceptance. Redact payloads.

## PR4 release blocking ledger (initial ownership)

| Priority | Workstream / owner | Exact exit criteria | Impact |
|---|---|---|---|
| P0 | Grant metadata and migration validation — Backend/DB | Explicit source-authorized `documents.page.restore_version` grantability; ordered migrations0059–0085 expected/checksums and upgrade/rollback; xUnit pass without weakening gate | Security, schema integrity |
| P0 | Windows actual A/B/C — Windows QA + Security | Three real independently verified OS identities/processes, allowed authorized operator actions, unauthorized foreign denial, evidence no SID spoof; required test not skipped | OS protection |
| P0 | Source current share and receipt semantics — Sharing/Organization | D07+D08 exact owner+viewer/replay tests; unescaped/unsafe Markdown cannot execute; full SQL pass + independent review | Data exposure/idempotency |
| P1 | Frontend/Browser — React + QA | Repair source vs fixture dashboard loading, Notification controls, actual M01 identity desktop/mobile failure; rerun all on final SHA | Usability/auth |
| P1 | Dashboard UX — React/UX | Exactly one Quick Create, width fit at320/390/768/desktop, keyboard focus and dirty navigation under state changes | Actual user defect |
| P1 | Missing workflows — FX owners | Implement local contract-complete vertical slices from updated 40 FX audit; preserve restricted Vault, provider and sensitive share limits; no silent R1 rescope | Product incomplete |
| P0 | Independent security/DB review — Reviewer separate from author | Review changed owner isolation, grants, V2 receipt/replay, migrations, notification worker, sharing, Finance, protected projections; name/revision/outcome recorded | High-risk signoff |
| P0 | Final semantic requirement trace — Analyst/QA/Product Owner | Read remaining selected/unread sources, forward and reverse trace all normative AC/action/dependencies, unverified vs blocked status and exact evidence | Acceptance denominator unknown |

### Existing tickets / reconciliation

Historical `NQA-012` and `NQA-013` are **DECISION_RESOLVED / TEST_AND_IMPLEMENTATION_VERIFICATION_PENDING**: owner picked D07=A and D08=A, so their old oracles are not automatically new business standards. NQA-003/004 remain product UX issues requiring exact-head rerun. NQA-019 is a required failing Windows gate, not allowable CI “flakiness”. Nothing in this plan authorizes lowering tests simply to make green.

## Suggested acceptance matrix for each active exact action

- Actors A/B, owner/source cross-reference, anonymous/deleted/role Admin+SuperAdmin, explicit Allow/Deny/Unset, FX module disabled/enabled/paused.
- Valid/invalid state graph Active/Archived/Trash/Purged; terminal state; source missing/revoked, field-specific action composition; direct API vs UI vs worker.
- Quoted ETag current/stale/malformed, same UUID/body retry, UUID conflicting body, concurrent same-key writers, crash after commit, legacy receipt, TTL retention and safe opaque projection when read revoked.
- DB composite owner FKs, migration startup/rollback/retained data, SQL transaction lock/race/deadlock and pagination25+ owner/filter-bound cursor with tamper.
- UI 320/390/768/desktop, keyboard/screen reader smoke, form validation, focus trap/restore, dirty navigation, API 401/403/409/412/422/503, no stale private content, no false empty-success.
- External effects strictly synthetic/disabled; actual network/real provider forbidden for D04 A. Threat model/owner data protection, logs/secrets and file ACL negative proof.

## Evidence collection and safe testing runbook

1. Freeze final implementation commit/tree after documentation changes; review PR diff and current Source/PO gates.
2. Build/typecheck, run backend unit + Windows actual-principal gate, independent schema migration manifest+clean SQL up/rollback and real SQL/API tests. No test skip reclassification to green.
3. Run React components, real browser backend/SQL journeys on desktop/mobile incl M01 identity, Dashboard, Finance, Sharing/Tags. Sanitize artifacts, preserve raw private traces in approved owned CI storage only.
4. Collect security/revocation/race/unstable network/backup/restore evidence and independent reviewer decision per changed risky slice. Do not include production data/provider secrets.
5. Generate verified RTM and approve each committed capability separately. New code or affected schema/policy invalidates earlier evidence. If run not performed: `NOT_RUN`.
6. Keep PR **Draft** until all blocker closure verified; a separate owner action can request Ready for Review. **Do not merge, release, start real provider workers or provision paid services**.

## Planned outcomes, not verified outputs

This file is a **runbook/acceptance contract only**. It creates no QA run, migration, automatic task, source handler or pass claim. Current disposition is `DRAFT / PRODUCT_INCOMPLETE / VERIFICATION_PENDING` on the documentation head.
