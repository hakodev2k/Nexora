# Test continuation — 2026-10-24 (Asia/Ho_Chi_Minh)

Scope: continue implemented-feature logic, SQL/API, browser and native filesystem verification on Draft PR4. No merge, production, real provider or live account action. Test-created Windows accounts exist only on disposable GitHub runners. Current user instruction authorizes synthetic QA; no product authorization or policy assertions were relaxed.

## Confirmed intermediate execution

| Source / execution | Actual outcome |
| --- | --- |
| f4cdabf; main CI39907458765, merge7c4228e954bbea06d265d3276ad89b394b26ff33 | Browser12/12 PASS; downloaded sanitized JUnit artifact12481975246. Frontend187PASS/1existingSKIP. SQL compilation blocked by CS8619 in new test. Windows retained old required-gate failure because harness had not yet shipped. |
| f4cdabf; dedicated CI39907453019 | Nine groups × desktop/mobile =18/18PASS/0SKIP. Downloaded all9 sanitized artifacts and parsed their JUnit. These are accessibility/navigation audits, not every business workflow. |
| 3489f594; SQL job120604035254 in CI40008957176 |283total:274PASS,9FAIL,0SKIP. All12 new release SQL cases PASS. All9 WorkLog cases failed before domain assertions because FX40 remained disabled in fixture. |
| Current local frontend after asynchronous oracle correction |21files;187PASS,1existingSKIP,0FAIL (9.00s). Focus recovery focused10/10PASS (3.11s). |
| 7832ce34; Windows job120607343377 in CI40009919807 | Required gate FAIL at reject-insecure/UnsafeFileRestore. Native A/B/C and earlier stages ran before this failure; the full W01–W10 gate did not pass. |

## Corrections and retained oracles

- Corrected nullable anonymous target payload type blocking SQL compilation; HTTP payload and expectations unchanged.
- Added9 manual WorkLog HTTP/SQL cases: opaque create/update replay and exact history; minute bounds; archive/unarchive; Active/Archived trash/restore; purge/history disposal; owner/stale ETag; current Admin grants; literal category/title/date report filters. Added matching outside-day records to make date-bound assertions meaningful.
- WorkLog tests now opt in to installed FX40 through authenticated Admin preview/commit before creating test sessions, with async restoration of previous system/registration flags. Existing default policy remains unchanged. Earlier9failures remain recorded.
- Full frontend local run caught a timing failure after dialog removal but before background accessibility restoration. The test now awaits the same accessible Active phase heading. No hidden-role fallback or assertion removal.
- Windows harness uses three real nonadmin process identities, trusted staged binaries/private config, positive runtime/operator reads, foreign native error5 for enumerate/read/create/overwrite/delete/DACL/owner, restart, unsafe ACL rejection, actual timeout/cancellation/crash and ownership-scoped cleanup. No password/secret is printed or passed on the command line.
- Windows failures now retain only static scenario/phase and strict allowlisted child-stage diagnostics; raw stderr/payload stays private.
- Runtime isolated W08 failure confirmed that an unmodified FileSecurity snapshot did not restore the original DACL. The fixture explicitly marks captured Access SDDL modified before SetAccessControl; final CheckPrivatePolicy remains mandatory. Independent reviewer approved this bounded fix.

## Evidence boundary and workers

Five actual child AI workers participated: browser, SQL, Windows, independent review, coverage inventory. Root ran central checks, handled compilation/runtime failures and published commits. The separate9UIUX workflow jobs are CI shards, not9new AI agents. Browser and coverage workers completed; Windows worker was interrupted by service usage limit before final runtime acceptance, and root continued its fixes. Independent review resumed and covered the final W08 fix. SQL worker's WorkLog source was completed before interruption; root added date negatives and scoped policy setup with independent review.

Local integration restore was blocked by proxy403; later local rebuild also encountered missing cache packages after workspace inactivity. These are infrastructure failures, not passing tests. Actual SQL compilation/execution uses GitHub SQL Server CI. A vanished Git alternate caused local diff/object errors; root rebuilt and verified remote3489 blobs/tree/commit metadata without overwriting working files. git diff --check subsequently passed.

## Latest verified execution

| Boundary | Source / CI / job | Observed result |
| --- | --- | --- |
| Real SQL/API |73951b /40010021330 /120607685364 |283PASS,0FAIL,0SKIP. Both new files compile and all21new cases pass; earlier same-source SQL e87/40009760251 also283PASS. |
| Windows native gate |73951b /40010021330 /120607685680 |31PASS,0FAIL,3Linux-onlySKIP;26child reports; requiredWindows gate SUCCESS. |
| Linux filesystem gate |73951b /40010021330 /120607685691 |32PASS,0FAIL,2Windows-onlySKIP; requiredLinux gate SUCCESS. |
| Backend unit |73951b /40010021330 /120607685690 |54xUnitPASS,0FAIL,0SKIP. Its custom harness repeats the Linux filesystem suite and is not additional unique coverage. |
| Frontend unit |73951b /40010021330 /120607685352 |187PASS,0FAIL,1existingSKIP. Local corrected full run agrees. |
| Browser business E2E |f4cdabf /39907458765 /120262576054 |12PASS,0FAIL,0SKIP; sanitized JUnit parsed. |
| Nine UIUX shards |f4cdabf /39907453019 |18PASS,0FAIL,0SKIP; all9artifact JUnits parsed. |

Latest73951b main CI40010021330 tests PRmerge79579dbb07f9f513f6a6d6c159255b5b947acba0. Its5nonbrowser jobs are SUCCESS. The browser rerun is still installing Playwright/system dependencies and has not reached product tests at this evidence checkpoint. The3489 UIUX rerun has7successful shards, auth/gated still installing dependencies. These later incomplete runs do not replace the earlier confirmed browser results and are not claimed passed. The only product-source edit in this workstream is the reviewed main landmark wrapper already present in f4cdabf; later changes are test-source, diagnostics and evidence.

Final reviewed test SHA256:

| File | SHA256 |
| --- | --- |
| SqlReleaseCoverageTests.cs |af66816d2c3ba30cbc296df17426dea18f88b7e6fbf3ae44b93e09a183ddea3c |
| SqlWorkLogCoverageTests.cs |98850cc4a5721a0af27d665d34c950fb7d00f969ae3710b90b5476260aa707d5 |
| WindowsCaptureProcessTests.cs |24c318e37b4770296924edacb44257c174463967f7ac2181193eb4d36de267b0 |
| ProductivityRecovery.test.tsx |98c4d15c52ae693e6a0761fd09b0b950a5a4bbd4781f371a044db20b2436b0c2 |


## Remaining acceptance limits

This bounded continuation does not establish100percent R1 or exhaustive feature coverage. The40FX inventory explicitly retains missing actions, business browser journeys, concurrency/fault schedules, source linking and approved-provider boundaries. A route scanner pass does not certify each popup, visual quality, business outcome or security path. Prior failures, independent source review and actual runtime results are separate evidence.
