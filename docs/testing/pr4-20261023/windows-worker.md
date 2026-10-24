# Windows actual-principal test workstream

Author: /root/qa_windows_oct23. Runtime continuation and this report: root, with independent source review by /root/qa_review_oct23. Current user authorized isolated synthetic QA; guard restricts the harness to identified disposable GitHub Windows runners. Production/live accounts are outside scope.

## Actual implementation

New WindowsCaptureProcessTests.cs; probe dispatch in Program.cs; replaces the prior hardcoded Windows A/B/C skip in LocalAccountMessageSinkTests.cs. Three nonce-marked NetUserAdd nonadmin accounts launch credentialed processes. Private configs and child reports bind exact SID/PID/start-time/nonce; native control read distinguishes filesystem deny from startup/config failure. No password CLI/logging. Setup and cleanup verify exact account SID/marker and owned run path; destructive cleanup refuses manifest/reparse mismatch.

Runtime/operator positive canaries are checked in temporary, pending and final stages. Foreign child executes native enumerate/read/create/overwrite/delete/DACL/owner attempts requiring error5, and owner snapshots prove no mutation. ReconcilePending, restart replay, inherited/explicit unsafe directory rejection, unsafe file skip, injected timeout/cancel/crash, old temporary sweep, partial setup rollback and final cleanup remain mandatory.

## Review and failures

Independent review caught operator cleanup lacking deletion rights on an intentionally inherited fixture; catch-all fault acceptance; post-NetUserAdd account orphan risk; missing reconcile/read/manifest negatives. Author corrected these before execution. Root fixed RawToken compile access. Initial native execution reached W08 but failed with generic diagnostics. Root added strict static stage codes only; an actual rerun isolated UnsafeFileRestore. Independent reviewer predicted the unmodified descriptor issue; root explicitly marked original Access SDDL modified before restoring and retained CheckPrivatePolicy. Previous failed runs remain evidence, not passes.

## Actual final Windows result

Source73951b869a0bf1eaa32a7ddd52b63d401b56f9b3; CI40010021330; job120607685680:SUCCESS.34discovered,31passed,0failed,3skipped. All3skips are Linux-only tests on Windows; no required Windows case skipped. W01–W10 harness completed with26native child reports. Gate remained failclosed until this real run passed. See evidence/windows-final-summary.json and CI job for the bounded result.

The author worker hit the service usage limit before final runtime acceptance. Root continued compilation and runtime fixes; independent reviewer resumed and approved final source. This was1child AI workstream, not26AI agents. No production ACL behavior changed. This gate certifies the implemented local capture contract, not all Windows configurations or all R1 features.
