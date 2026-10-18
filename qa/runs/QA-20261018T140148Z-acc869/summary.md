# QA verdict: PARTIAL/BLOCKED

QA-20261018T140148Z-acc869; HEAD 1cd27a46a2ba1f24ab2b6f32f84771230f4b1761 plus dirty worktree,612-source fingerprint CSV SHA256 309471C6BC07B5FBCAFE934FD02A776C0B1A616EA6E3FB70E9BB204E01BF22E7. Không đề nghị release: official unit gate không compile, các test hiện có còn fail/stale-oracle, required Windows SID gate và full semantic coverage/review chưa xong. Không sửa product code.

| Layer | PASS | FAIL | Not run/blocked |
|---|---:|---:|---|
| Official backend unit |0|compile|Whole suite blocked by NQA-001|
| Independent unchanged xUnit continuation |46|1|2 Tag cursor cases excluded; does not replace official gate|
| Original custom checks |30|0|4 skipped;1 required Windows BLOCKED,3 Linux-only N/A|
| Frontend jsdom/MSW |125|5|1 skipped|
| Real SQL/API integration |193|5|No skips|
| Existing real browser desktop/dev |45|11|56 attempted, no skipped cases|
| Targeted browser/API/SQL/keyboard |17|0|Earlier harness/default-gate attempts retained separately|

Không cộng các dòng này thành business coverage: assertions có overlap và component fixtures không phải E2E. 733 catalogue actions +365 endpoint declarations/34 API folder groups là hai inventory kỹ thuật khác nhau, không phải1098 business requirements. 30 static routes discovered;36 runtime route entries exercised across5 viewports,180 initial observations +15 focused populated observations. 78 axe scans,0 violations reported: không chứng nhận WCAG.

Journeys thực: UserA/B native login; WorkLog UI create/reload/edit/manual paging53 records, exact SQL owner/count, stale ETag/replay/foreign-owner, archive/Trash/actual-state restore/purge+versions; lost response AFTER real commit then same intent retry; separate-admin revoke while dirty→403/private clear/no SQL mutation; anonymous401; native mobile390x844 lifecycle with Cancel focus/history; Documents literal HTML nonexecution/version2/publish/current archive cohort UI+SQL; Finance explicit VND decimal123456.12345678→7.25000000/date/usage guard. Existing45 browser passes cover additional Calendar ICS, Career, Course, assets, tags, bookmarks, Planner/Focus/Time/toolbox/sharing subsets; inspect functional-results.json per-case assertions, not complete module claims.

Confirmed product issues: NQA-003 duplicate Quick Create and NQA-004 horizontal overflow. Two specification ambiguities and16 test/verification infrastructure issues are separate in findings.md/csv. No confirmed cross-owner leak in executed cases; this is not security certification. Old tests with obsolete markup/publish/currency/selectors are not automatically product bugs; original failures remain preserved.

Performance production warm local,53 WorkLogs, native CPU/no throttle: route readiness samples 100/104/111/112/120ms median111/max120; modal samples 24/25/25/26/32ms median25/max32. Metric is explicit UI readiness, not LCP/INP; no reliable p95, cold-device, CLS or field INP measured. Main bundle1,041.46kB/gzip265.08kB, existing warning; no latency failure inferred from size alone.

Major limitations: per-operation AC/actor/lifecycle/concurrency variants not fully expanded/executed (1825 obligation templates remain NOT_RUN); UNKNOWN handlers need semantic review;87 clearly missing local operations remain implementation gaps; full mobile journeys outside WorkLog, registration/MFA/browser all failure states, physical devices/screen reader/200% zoom, three real Windows principals, live provider delivery and large load/restore/restart races remain unverified. No sampling promoted to full coverage.

Artifact deviation: Vitest's configured JUnit reporter overwrote pre-existing web/Nexora.Web/artifacts/vitest-junit.xml despite CLI output override. Current result was preserved in frontend-unit.xml; prior bytes were not backed up and cannot safely be reconstructed. No checkout/reset attempted. Product/test source fingerprints remain unchanged; acknowledge this generated-artifact preservation failure.

Cleanup status: see seed-manifest.json and cleanup.log. Credentials/keys stay only in private temporary runtime until cleanup; public artifacts are redacted. Internal screenshots were visually inspected; not included as report attachments.

Report correction: planned-case-obligations.csv holds unexpanded NOT_RUN templates; test-cases.csv now uses the same consistent schema as executed-cases.csv. These templates are not designed/executed acceptance cases. NQA-020 tracks the News heading failure. Goal numeric definition update was exercised; definition removal was not tested.


Final verification: final-source-verification.json confirms same HEAD/branch and 0 changes across612 source/config/test paths. Exact owned SQL database and private runtime were removed and verified absent (cleanup.log); synthetic IDs/redacted reports retained. Two internally inspected screenshots were deleted, not attached. 449 normalized executed-case records:426 PASS/22 FAIL/1 NOT_RUN across overlapping layers;30 additional passing custom checks and4 skips remain separately reported, never business requirement coverage. Detailed obligations remain unexecuted. Independent review Pending.
