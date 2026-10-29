# Shared card and native control layout — 2026-10-26

Scope/authority: root assigned shared root-cause correction after actual Shopping title squeeze and source inventory identified the same direct-flex pattern in Automation, Career, Course, Digital Asset, Focus, Goals, News, Skills, Time and WorkLog. DEC014 isolated QA and existing UX-04/05/10/11, UX-001/002/006, NXG-SYS-13/14 apply. No feature family, domain state, provider or permissions broadened. Root owns other feature screens; this worker changes shared CSS and only the explicitly authorized Time root presentation class, preserving content-panel and adding content-section/time-tracking-screen.

One compatibility adapter stacks cards that have direct headings/paragraphs/definitions/lists/preformatted content. Cards composed as body-div plus actions retain row layout; table rows with td-only content are unaffected. Headings wrap within readable columns. Actual screenshots then exposed Time buttons outside content-section receiving browser-default gray/tiny controls and Course resource-form fields escaping shared styling. Shared unclassed native button/form adapters supply44px targets and consistent tokenized fields; semantic module navigation wraps. The legacy sticky resource-form rule also let a Course filter cover a card title during scrolling: non-panel resource forms now static, styled side-panel forms retain their existing behavior.

## Actual verification

Auth/fixture uses genuine UserA/SuperAdmin logins; only already Ready FX18/40 policies and user enablement through legitimate preview/commit. All original module/system/registration/role dispositions restored and asserted in finally, cleanup errors preserve the scenario error. Normal Time and Course UI creation followed by actual SQL owner checks. No positive business mocks, data purge or external provider.

Final regression `functional/shared-card-layout.spec.ts` measures real title/body widths >75% of card, representative title <=3lines, no card/page horizontal overflow, actual heading hit-test unobscured after scrollIntoViewIfNeeded, all card actions>=44px, Time timer action>=44px and Course filter controls>=44px. Final **3PASS** at1366x768/768x1024/390x844,0failure/skip/errors, overall5.8s/4.4s/4.3s. XML/JSON copied per viewport under `artifacts/shared-card-layout/{1366,768,390}/verified-junit.xml` and verified-results.json. Earlier successful width-only runs are subsets/iterations, not additional unique scenarios or full-module coverage.

Personally opened actual Time/Course populated viewport screenshots at all three widths; after static-filter correction reopened final Course screenshots at all widths. Titles/actions remain readable and Course titles no longer hidden under its filter. Screenshots/measurement JSONs are local artifacts in the same directories; no human final UI/product acceptance claimed. Broader modules sharing the selector still require their own affected regression and representative visual review; source inventory alone is not runtime acceptance.

Initial orchestration attempt leaked the helper's Set-Location into its caller: tablet scenario passed but relative artifact copies and later helper invocations failed to resolve. No later cases executed in that attempt and no product failure inferred. Corrected orchestration to absolute helper/artifact paths with stop-on-error, preserved actual runs and completed the explicit final cases.

## Final source/test hashes
- `web/Nexora.Web/src/styles.css`: `EB47CA838980876EC9FE70687D27B415543A6A019DD27A32F2E9A0AE9B14E384`
- `web/Nexora.Web/src/TimeTrackingScreen.tsx`: `DA1D5AA2AEA56D9C688D0D5AAFCE3E28653BA8F408B4326FACDBD6C44AA8D618`
- `web/Nexora.Web/functional/shared-card-layout.spec.ts`: `D1B53816F3DDBB02CFF0A642588B93BE14CFCB24969A649C6943EDCBAC6292EA`
- `web/Nexora.Web/src/modalIsolation.ts`: `ECE837490D35F7A74587516E85B3743750323C0EEAFBADAAAB13D4A136562099`
