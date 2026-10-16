# Verification continuation at 028d94 + FX35 source

> Historical checkpoint; see `continuation-20261013-code-only.md` for current branch, checks and scope. Subsequent selected SQL/API190/190, backend unit49/49 and frontend125pass/1existing skip were executed before the current code-only amendment and before the new FX22 delta. They are not fresh whole-tree functional acceptance for the current source. No browser E2E/Docker-hosted CI result is claimed. Raw private artifacts remain excluded.

Main8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3; PR4 parent028d94a064f000a48c8bd793c4996e5fc0475119. Exact changed-source hash set to be published in this directory. New user authorizes focused local synthetic testing; earlier code-only gates are superseded for this continuation. No retained database/data/provider or credential used; local Windows integrated authentication with new fixture-owned loopback SQL Express database, safe cleanup. Existing raw artifacts remain excluded from commit.

| Command/scope | Actual outcome | Limits |
|---|---|---|
| dotnet build API Release --no-restore | PASS0warnings/errors | Compilation only |
| npm run build | PASS78modules; existing bundle-size warning | No browser acceptance |
| dotnet test UnitTests Release | PASS49/49;0skipped | Exact migration manifest assertion includes new0054 |
| focused SQL Composite/Tag initial | FAIL9/9 at fixture setup SQL207line9 | Acceptance assertions not executed; not product9bugs |
| after production0052 compilation boundary repair | PASS9/9 | All migrations apply/replay/checksum guard and generatedTestServer fixture bootstrapped |
| FX35+Composite+Tag initial | FAIL2/PASS10/total12 | NewtestCSRF request omittedbody and syntheticSQL used unquotedreservedidentity schema |
| intermediate correction | FAIL1/PASS11/total12 | Old CSRF request remained until exactsource edit; no production authorization weakened |
| final focused FX35+Composite+Tag | PASS12/12;0skipped | FX35CRUD/paging/replay/owner/current revocation/audit rollback/API mappedfield/origin/revision/confirmation/CSRF denial; no provider operations |
| npm run test:unit -- --reporter=default | PASS122; SKIPPED1; total123 across11files | Existing skip retained, jsdom/MSW, not browser E2E or FX35UI functional acceptance |
| independent source review baseline_review | PASSafter confirmed fixes | Source review; reviewer did not independently execute tests |
| diffcheck | PASS | Raw artifacts not staged |

Migration0052 originalfile/checksum unchanged. SQL Server failed to bind CHECK authority fields added by ALTER in same compilationbatch; runner now executes that exact immutable script in two compilationbatches, samephysicalconnection/BEGIN/COMMIT. Other migrations unchanged. Canceled requests no longer cancel explicit bounded rollback/appLock release. Freshmigration/replay proved; cancellation-specific timing remains NOT_RUN. This is a production migration-execution defect repair, not a test weakening.

Remote G01–G05 source and Tag/API changes preserved by FF+stashapply. G01 composite3realSQLcases passed, Tag6realSQLcases passed including literalwildcards/paging/byID/replay, plus existing3TagUI tests in full frontend run passed. Broader audit, concurrency/session/globalcontexts and browserE2E still pending. These results do not certify all733actions/202screens or wholeTagFX24 scope.

FX35 configuration4actions source implemented with exactPOapproval; SQLAPIacceptance3tests passed. Disabled-only, credentialNULL, emptyscopes, providerinactive remains enforced. Browser/responsive/keyboard, Admincontrolplane/grant-negative/concurrency and all dormant17actions runtime matrix still pending. Webhook/transport/n8nadapter not implemented; D04–D09/G3/G4 activation unresolved. Not FX35 operational or R1complete.
