# Baseline restoration — bounded verifier and fixture slice

Accountable owner: root implementation agent. Source base: PR #4 branch `impl/m01-s00-scaffold`, `40f5345406ecc716281550909a7b6b78077fca1c`; requirements main `8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3`. Authorization: current PO full-product continuation and follow-up “Do it”; local/synthetic verification authorized, no provider activation or merge. This slice adds no module behavior or migration.

Goals: NXG-SYS-08/14/15; M01 S00 verification tooling and S08 current registered Admin routes. Exact role/action/preview/ETag/idempotency/CSRF behavior is preserved; this patch does not change any endpoint or permission. Rules read: mandatory startup/core/routing/verification, verification route, backend testing-rules/testing-strategy, Technical Lead code-change-review. Architecture/security routes were additionally read for ongoing contract audit; they are not proof of executed security acceptance.

## Changes and acceptance

- `scripts/dev/verify-s00.py` now requires `commitAdminUserRole`, `commitAdminUserPermissions`, `commitAdminUserModules`, matching existing `.WithName` registrations. The same checks remain mandatory. Missing each new marker was independently exercised and rejected; no old route is introduced.
- `.ai/scripts/verify-baseline.py` reads documents as UTF-8 and compares relative paths using `.as_posix()`, preserving containment, completeness and gate predicates on Windows. The prior native invocation failed decoding; UTF-8 invocation then exposed Windows separator mismatch. Both failures are recorded, not discarded.
- `tests/Nexora.IntegrationTests/SqlApiFixture.cs` pins file storage to its generated `_runRoot/files` through both the fallback environment variable and canonical `Nexora__LocalFileStoragePath`. Both prior environment values are restored by existing tracking; the host is disposed before cleanup. This prevents ambient configuration from directing synthetic uploads into the checkout or unrelated storage. No new test suite or product fixture records are committed.

Canonical UTF-8 LF SHA256 of final source:

| Path | SHA256 |
| --- | --- |
| `.ai/scripts/verify-baseline.py` | `cb8475e141b2741f53eedcb64c9761e0fb7152cb72762140446f7d6e529bd539` |
| `scripts/dev/verify-s00.py` | `a2add8084487c669c17326953a31158b4756af86d1b703d93ae8c9a905e0beb8` |
| `tests/Nexora.IntegrationTests/SqlApiFixture.cs` | `261104f43e4319c955b281a06be4ca3739b6fcb27bbab0e9bbbeb9aa9f74d08e` |

## Actual local verification

Default sandbox execution still fails before process creation. Approved escalated execution succeeds; the earlier host blocker is narrowed accordingly. Node REPL remains unavailable. No auto-review policy rejection was received.

| Command / check | Actual result | Scope / limitation |
| --- | --- | --- |
| `python scripts/dev/verify-s00.py` | PASS, exit0 | Static checks, not full workflow acceptance |
| `python .ai/scripts/verify-baseline.py` after fix | PASS, exit0; 10 routes / 15 tests | Agent packages and gate regressions |
| `dotnet build src/Nexora.Api/Nexora.Api.csproj --configuration Release --no-restore` | PASS; 0 warnings/errors | Current product binary |
| `dotnet build src/Nexora.Bootstrap/Nexora.Bootstrap.csproj --configuration Release --no-restore` | PASS; 0 warnings/errors | Bootstrap binary |
| `dotnet build tests/Nexora.FunctionalTestOperator/Nexora.FunctionalTestOperator.csproj --configuration Release --no-restore` | PASS; 0 warnings/errors | Root operator binary |
| `dotnet test tests/Nexora.UnitTests/Nexora.UnitTests.csproj --configuration Release --no-restore` | PASS; 47/47, 0 skips | Backend xUnit |
| `npm run build --prefix web/Nexora.Web` | PASS | TypeScript + Vite; existing 684.94kB bundle warning |
| `npm run test:unit --prefix web/Nexora.Web` | PASS with skip; 83 passed / 1 skipped | Existing DST skip remains |
| Final `dotnet test tests/Nexora.IntegrationTests/Nexora.IntegrationTests.csproj --configuration Release --no-restore --logger "trx;LogFileName=baseline-sql-api-final.trx" --results-directory out/verification-baseline` | PASS; 122/122, 0 skips | Actual loopback SQL, generated fixture-owned DB; final fixture binary |
| Native custom runner `--require-os windows` | FAIL, exit1; 30 passed / 0 failed / 4 skipped | One required Windows A/B/C process-identity case is unconditional skip; three other skips are Linux-only |
| `git diff --check` | PASS | Reviewed source diffs |

An initial SQL run was interrupted by host restart without a complete report and is not counted as passing evidence. The next pre-fixture-change run completed122/122; the final rerun above independently completed122/122 on the new fixture. Raw TRX, credentials, account manifests, captures, generated file objects, browser output and tracked generated JUnit changes are excluded from source publication.

## Independent review

Reviewer: `/root/baseline_review`, separate from author. Reviewed two verifier patches; ran both actual Python verifiers, diff-check, and in-memory removal of each current Admin marker. No blocking finding. Fixture review found canonical configuration could override fallback storage; author added the canonical tracked override. Reviewer re-reviewed final two-line patch: finding resolved, no blocking finding; host disposal/environment restoration/temp-root cleanup ordering retained. Reviewer did not independently run SQL or browser tests. This is a narrow implementation review, not a whole-product security audit.

## Actual retained-state reconciliation

Read-only SQL journal has47 rows, including both Monitoring0045 and News0046. Monitoring0045 matches the root and isolated source hashes. News0046 matches both isolated migration and committed staged draft. **0046 is applied in retained local SQL and is immutable there**, even though it is not installed in the current PR product manifest/source. Historical “0046 unapplied” checkpoint wording is superseded for that retained environment only. No migration file/checksum was edited; no new number assigned.

The root reviewed46-file manifest replay completed normally and preserved core counts exactly:8 Users,8 PersonalSpaces,320 Projects,281 Tasks,2029 Calendar Events. The extra applied0046 journal row was retained. Source state in `out/blocked-module-apis` remains detached at `d46ca6e52143677cbd22f778f1c3503b27d0deba` with unpublished modifications; no reset or broad copying occurred. Later local data/source cannot be treated as published acceptance without comparison.

## Browser and remaining gates

Root API on loopback16443 and Vite on loopback16173 reuse retained synthetic state, without reseed. Root API and validated TLS proxy both report Ready. Node trusts the public localhost development certificate through private runtime configuration; TLS verification remains enabled. Core provider activation remains disabled.

Discovery:265 tests in21 files, five approved viewports. Setup attempts failed before journeys: first connection-refused after bad private plugin entry; second blank-page setup timeout after bundled out-of-tree config could not resolve react-refresh. No product source/dependency was changed to solve these; native private config-loader now renders login correctly. Both failed attempts remain evidence. Full265 regression is currently running from the beginning, normal6500ms login pacing, one worker, no page-limit increase/data pruning. A complete pass is not yet claimed.

Windows A/B/C gate requires an executable real-principal harness; the current test source unconditionally skips it. Merely setting configuration or removing the required gate cannot resolve it. This remains a precise verification gap, not a reason to report Windows PASS. CI SQL/browser still need their own isolated runner configuration; successful local runs do not fix CI variables/secrets.

Rollback: revert only this small tooling/fixture patch; no application state or migration rollback is needed. Keep generated local/private files excluded. Release1 and all resumed modules remain incomplete; functional/security/provider/production states stay separate.
