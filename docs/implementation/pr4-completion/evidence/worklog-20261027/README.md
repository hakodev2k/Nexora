# E40d manual WorkLog — real browser/SQL evidence

PR4 base a0215f7 plus exact worktree fingerprints below. Owned f47 isolated runtime; real synthetic-user login and approved Ready-module policy/grant preview+commit, restored/asserted in finally. No provider, production, raw secret or positive mock response.

| Scenario definition | Verified assertions | Current-source execution |
|---|---|---|
| Manual form/history/report/lifecycle | Dirty Cancel→Continue keeps fields; explicit discard resets. Native0 blocks POST. Real create/reload/edit persists exact SQL owner/date/minutes. Created+Updated snapshots retain old/new notes; filtered day/category report1entry90gross minutes. Archive readonly→Trash→restore actual Archived→Unarchive→Trash→explicit purge. | Desktop PASS within initial67case broad report; mobile PASS in final3case Goal/WorkLog run |
| Validation/foreign/current denial | Backend0/-1/1441 rejects422, fractional1.5 rejects400 strict integer binding; no invalid entry. Current-enabled UserB cannot read another owner's detail/history or obtain private list. Revocation during edit clears private DOM/draft and leaves SQL unchanged; restore/reload returns real owner record. | Desktop PASS within initial67case broad report; mobile PASS in final3case Goal/WorkLog run |

Two unique WorkLog scenario definitions, two required viewports:4current-source executions. Earlier focused desktop2PASS15.1s/mobile2PASS11.7s preceded the discovered Cancel fix and are retained separately, not relabeled as proof of that fix. Final mobile run included Goal1+WorkLog2, **3PASS23.9s**; only2of those executions are WorkLog. Initial broad suite overall was56PASS/11FAIL, not a full regression PASS, although its2WorkLog cases passed. Coordinator owns subsequent full-suite/current-head reconciliation.

Actual defect fixed: inner form Cancel bypassed the shared dirty guard and duplicated its Cancel. One-line removal reuses the existing ResourceFormDialog guard; no data/service/permission/schema behavior changed. Focused component verification2new+6shared cases **8PASS2.50s**; app and targeted browser typechecks passed. Other modules' same defect was not silently generalized as fixed.

## Populated synthetic visual evidence

![Desktop owner day/category gross report](desktop-gross-report.png)

![Mobile populated manual form with one guarded Cancel](mobile-manual-form.png)

These are real screenshots with synthetic fixture text; full-page mobile capture includes other synthetic owner rows behind the modal. Inspected for readable controls/viewport overflow; final human UI acceptance remains pending.

## Executed source / retained evidence

| Source | SHA256 |
|---|---|
| WorkLogScreen.tsx | 70AFE60628B8D4C7BAE3CFF1A3086033F7CA89D5D458E278EE2A3E9A1E28A932 |
| workLogApi.ts | 0BA7E52B485EBD99F6256BAA435544EE996A06F4817646798F1490742A0F1626 |
| SqlWorkLogService.cs | A015CED9F010D3B540FF4E90F5639274B5A3131A4FC0A44CF30D4ED489944E01 |
| functional/worklog.spec.ts | 92FCDE8F3520CB2B565480AA72CA3B6EE855880ED8CFDF9309F756934A9ADC3E |

Local reports: `artifacts/active-increment-desktop-broad-initial-{results.json,junit.xml}` and `artifacts/goal-worklog-mobile-final-{results.json,junit.xml}`; [worker handoff](../../workers/worklog-runtime-20261026.md). No credentials, account manifest or private browser storage copied into docs.

This proves the bounded manual workflows above, not linked TimeEntry AC004, global Trash integration, every concurrency/permission path, whole FX40 or RELEASE READY. Those absent/gated siblings and independent/CI/human acceptance remain explicit. Later relevant source changes invalidate affected evidence.
