# Shared UI foundation — 2026-10-25

## Task brief and bounded plan

Requested outcome: professional, consistent, readable UI, shared forms/toolbars/cards/dialog presentation and responsive390/768/1440 behavior; real visual review after implementation. Source: PR4 branch `impl/m01-s00-scaffold`, base `a0215f7`, coordinated local changes. Authority: current explicit user UI/QA request and DEC-20260909-014 automated-QA amendment. D01–D09, contracts-first, normal owner authorization and production/provider restrictions remain. Root owns App integration; this worker owns shared styles, primitives, presentation-only ResourceFormDialog buttons and pinned UI dependencies.

Rules loaded: AGENTS, nexora-engineering skill, implementation profile, Technical Lead README/core, baseline/frontend routes, verification. Requirements: UX-04/05/10/11, UX-001..006, accessibility requirements and NXG-SYS-13/14. Scope is presentation/shared components; no SQL/API/business/security behavior changes. Plan: inspect existing controls/cascade, compare maintained libraries from primary docs, add one small foundation, integrate real consumers, verify native labels/buttons and existing modal protection, build, inspect actual desktop/tablet/mobile UI and reconcile evidence. Rollback: revert this bounded UI/dependency delta, no persistence migration. Human UI/product acceptance remains pending.

## Library evaluation

| Candidate | Primary source/current assessment | Decision |
|---|---|---|
| Radix Primitives | [Introduction](https://www.radix-ui.com/primitives/docs/overview/introduction), [accessibility](https://www.radix-ui.com/primitives/docs/overview/accessibility), [releases](https://www.radix-ui.com/primitives/docs/overview/releases): unstyled, incremental accessible primitives; individual packages supported. | Adopt only Label and Slot behind Nexora shared API. No second UI framework/provider/reset. |
| React Aria | [Official overview](https://react-aria.adobe.com/), [getting started](https://react-spectrum.adobe.com/react-aria/getting-started.html): broad adaptive accessible unstyled components and internationalization. | Suitable alternative, but introducing a second event/interaction layer alongside existing native forms/modal safeguards is unnecessary for this slice. |
| Mantine | [Official Vite setup](https://mantine.dev/guides/vite/): full styled system/provider/setup. | Good full-suite candidate; broader theme/provider/CSS migration would exceed this bounded compatibility change. |

Choice is an engineering inference from current project constraints, not a claim competitors lack accessibility. Official docs consulted2026-10-25. Installed exact public npm versions `@radix-ui/react-slot1.5.0`, `@radix-ui/react-label2.1.17`, both MIT, React19 compatible; local package metadata confirms `sideEffects:false`. [MIT source](https://github.com/radix-ui/primitives/blob/main/LICENSE). Four packages added with lifecycle scripts disabled. Radix supplies label/composition primitives, not visual styling, auth or a claim of WCAG compliance. Local CSS supplies calm neutral surfaces/blue accents per UX-04. Native controls and current modal implementation retained; no needless replacement of interaction security.

## Implementation

- `src/ui/primitives.tsx`: native Button with explicit safe default `type=button`; FormField association for label/hint/error through Radix Label; wrapping Toolbar (no false ARIA-toolbar keyboard contract); semantic Card composition through Radix Slot.
- `styles.css`: shared semantic light/dark tokens, 4px spacing roles, readable typography, consistent44px buttons and native16px inputs, cards/toolbars/forms, portaled dialog styling, focus/reduced-motion/forced-color states, narrow/tablet layout. Existing class names serve as compatibility adapters instead of adding40 competing design systems.
- ResourceFormDialog consumes shared native Buttons without changing handlers, isolation, dirty-leave continuation, beforeunload, busy guards, focus trapping/restoration or authorization.
- Root integrates shared fields/buttons/toolbar into actual Bookmark workflows separately. This is incremental migration; not every legacy screen has been rewritten as primitive JSX.

## Verification and remaining gates

Initial focused run before dependency security updates: `npm run test:unit -- src/ui/primitives.test.tsx src/ResourceFormDialog.test.tsx`:10PASS (4new primitive cases+6existing dialog cases),0failure,4.69s. Existing React async-act warning observed. Build/typecheckPASS,392modules. Initial output main JS1177.37kB/303.42kBgzip; CSS47.72kB/10.14kBgzip, Markdown split159.33kB/48.72kBgzip. These are total build sizes, not measured Radix-only overhead; Vite reports main chunk>500kB. No bundle-performance target acceptance claimed.

Security audit discovered existing frontend dependencies with3High/3Moderate advisories. Root authorized compatible patch remediation; final patch/audit/build/test and visual evidence follow in addenda. Do not treat initial checks as final-source runtime verification. Final human UI/product acceptance pending. Screenshot scans cannot certify full accessibility; assistive-technology/manual acceptance remains required.

## Compatible security remediation and repeated verification

Full initial audit:3High/3Moderate. Confirmed advisories and compatible patches:

- Vite6.4.3→6.4.5; Windows editor command injection/traversal fixes, including [GHSA-587h-7wmj-h5pm](https://github.com/advisories/GHSA-587h-7wmj-h5pm). Stays within Vite6, no framework migration.
- source-map-js1.2.1→1.2.2; [indexed source-map offset DoS GHSA-68fv-2mgg-jv7q](https://github.com/advisories/GHSA-68fv-2mgg-jv7q). Dependency path includes Vite→PostCSS and test toolchains.
- Vitest/coverage-v85.0.0→5.0.5, matching peer versions; mocker code injection and Windows redirect traversal Moderate advisories from npm audit metadata.
- jsdom→undici8.12.3, compatible transitive patch addressing event-source/SRI/content-encoding vulnerabilities including High content-encoding DoS.

Used pinned direct versions and targeted transitive updates with `--ignore-scripts`; no `audit fix --force` or major upgrade. Final `npm audit --json`:exit0, **0vulnerabilities** at execution time, evidence `design-system-dependency-audit-20261025.json`. This is a dependency-advisory check, not penetration testing/security certification. Initial sandbox metadata fetch failed EACCES; public metadata/install/audit succeeded through approved network tool escalation.

Repeated focused run on Vitest5.0.5:10PASS,0failure,3.35s; same async-act warning. Build/typecheck on Vite6.4.5:PASS,392modules,5.60s Vite build; main1177.40kB/303.43kBgzip, CSS47.72kB/10.14kBgzip, Markdown159.33kB/48.72kBgzip. Main chunk size warning remains; bundle figures include the current coordinated App source and are not isolated per-library measurements. No additional runtime/visual acceptance is implied.

## Shared-source fingerprints before visual verification
- `web/Nexora.Web/src/styles.css`: `3F556EAF00F619EA975B88DB3BC24DC112FFA1A0FD70CCB0CBCE38D90E5CAA01`
- `web/Nexora.Web/src/ui/primitives.tsx`: `CCC63FBD9F920E8C061437006331D8441E90395D04C55E9F91E745E7BC8CE0BE`
- `web/Nexora.Web/src/ui/primitives.test.tsx`: `DB74410241EB71A8E01D5A274D5032EC42BF1C1813A2F18E03847E2FF36FDF66`
- `web/Nexora.Web/src/ResourceFormDialog.tsx`: `006A6220944A2D0C0C3E000C0195E29943BCF29880D2EEE91DBCCE5CA5ECF58A`
- `web/Nexora.Web/package.json`: `CCE9A75CA6F561465272A751450983D0D53A02A5E3A1803D56EA018E8524821F`
- `web/Nexora.Web/package-lock.json`: `88D2C95D5D2649CF486DABFD432081CE4B9B68A01E63B8BEEE1586CA938CE8B0`

Review note: mobile dialog actions retain DOM order (no CSS reversal), so visual and keyboard order agree. Final CSS fingerprint supersedes the pre-visual fingerprint above: `49088419FDF64C969E7D49F4E20CF42D1B44FE55B62F7C14590298D25E51C658`.

Token contrast calculation (sRGB relative luminance, not a whole-page accessibility test): control border #8190a5 against white3.25:1; dark control border #6b809f against #1822343.96:1; white primary button text against #315fca5.80:1, dark accent solid #436fce4.77:1. Corrected initial light control border2.01:1 before final visual capture. Legacy high-specificity screen styles still need actual rendered Axe/manual evaluation.

## Actual browser execution and visual iteration

Owned isolated runtime supplied by root; helper verifies OS ownership and private synthetic-account configuration. No credential values copied. Frontend restarted on patched Vite6.4.5. Added actual tablet768x1024 project in playwright.ux-audit.config.ts; scanner derives screenshot names and observations from actual viewport/innerWidth, not project-name assumptions. Source marker: `a0215f7b3e85d33503cff9077b41cc065240cb81-existing-feature-worktree`; this is a worktree label, not an immutable final commit. Root must reconcile hashes/delivery commit and invalidate relevant later source edits.

Executed twice, keeping evidence separate:

1. worker `ui-foundation`, session4161, reserved auth/navigation/knowledge:9PASS,0failure/skip/errors,2.4minutes. Actual screenshots exposed low-specificity shared control defaults losing to legacy selectors. Corrected selector specificity and normal input font weight, preserving states/handlers. Initial evidence retained.
2. worker `ui-foundation-final`, session75202, same9cases:9PASS,0failure/skip/errors,2.3minutes (JUnit140.185908s). **Nine unique scenarios, two runs, not18unique acceptance cases.** Evidence `web/Nexora.Web/artifacts/uiux-audit/ui-foundation-final/playwright-junit.xml`, nine sanitized JSON reports and117PNGs.

| Final project | Auth observations/dialogs | Navigation observations/dialogs | Knowledge observations/dialogs |
|---|---|---|---|
| desktop1440x900 | 6/0 | 10/3 | 14/7 |
| tablet768x1024 | 6/0 | 10/2 | 14/7 |
| mobile390x844 | 6/0 | 10/3 | 14/5 |

Total90route observations and27opened-dialog captures. Normal UI seeding produced real isolated SQL Bookmark records; repeated runs produce allowed duplicate display titles, not hardcoded runtime mock results. These scanner cases are not comprehensive business-flow acceptance. Nine SMALL_INTERACTIVE_TARGETS heuristic findings remain across Home/Notifications/Collections and three viewports; checkbox raw bounds may differ from full label hit areas, so manual target-spacing review remains. Captured reports show no JS or page/dialog horizontal overflow findings and no Axe failures in sampled initial main-content scans. **Axe does not comprehensively cover all routes/states or portaled dialogs in this scanner.** No screen-reader or full WCAG compliance claim.

### Images personally opened

Initial run: Anonymous-1440-login-initial.png; UserA-1440-bookmarks-initial.png; UserA-1440-organize-templates-initial.png; UserA-768-organize-templates-dialog-0.png.

Final run (paths relative to final screenshots directory):
- Anonymous-1440-login-initial.png
- Anonymous-390-login-initial.png
- UserA-1440-bookmarks-dialog-0.png and UserA-1440-bookmarks-dialog-1.png
- UserA-768-bookmarks-initial.png
- UserA-768-bookmarks-dialog-0.png and UserA-768-bookmarks-dialog-1.png
- UserA-390-initial.png
- UserA-390-bookmarks-initial.png
- UserA-390-bookmarks-dialog-0.png

Observed: coherent readable labels, separated controls/actions, reachable modal footer, visible focus and responsive reflow in these captures. Original unstyled Template portal collision is absent in the opened tablet capture. Password magenta regions are intentional screenshot masks; blue main outlines are keyboard focus. Mobile screenshots use device pixel ratio, actual CSS viewport remains390.

Residual polish/verification: Bookmark modal repeats the same title as an inner heading with nested outlined fieldset; informed root to remove redundant inner heading in its App ownership if adopted. Modal input borders appear lighter than library filters; actual computed/rendered contrast still requires inspection before claiming the numeric token ratio applies universally. No whole-product smoothness/beauty/acceptance claim follows from selected screenshots. Human Product Owner UI acceptance, independent shared-modal/dependency review, full affected regression and current delivered-revision CI remain release gates.

## Final worker-owned source fingerprints at handoff
- `web/Nexora.Web/src/styles.css`: `C98238C472809DA46D6A7039F227467450E4AD81C23059A70B21E8D4546480DC`
- `web/Nexora.Web/src/ui/primitives.tsx`: `CCC63FBD9F920E8C061437006331D8441E90395D04C55E9F91E745E7BC8CE0BE`
- `web/Nexora.Web/src/ui/primitives.test.tsx`: `DB74410241EB71A8E01D5A274D5032EC42BF1C1813A2F18E03847E2FF36FDF66`
- `web/Nexora.Web/src/ResourceFormDialog.tsx`: `006A6220944A2D0C0C3E000C0195E29943BCF29880D2EEE91DBCCE5CA5ECF58A`
- `web/Nexora.Web/package.json`: `CCE9A75CA6F561465272A751450983D0D53A02A5E3A1803D56EA018E8524821F`
- `web/Nexora.Web/package-lock.json`: `88D2C95D5D2649CF486DABFD432081CE4B9B68A01E63B8BEEE1586CA938CE8B0`
- `web/Nexora.Web/playwright.ux-audit.config.ts`: `0B59E1A3D79BFDBBAEF7BC3AE25E3F3BEE987266CFED05880BC8882E681F7AF8`
- `web/Nexora.Web/artifacts/uiux-audit/ui-foundation-final/playwright-junit.xml`: `356C35033A7AC422691BF5A7ED321D27990A04C50EAF9DF99759112796397087`

## Final portal correction and actual rendered verification — supersedes residual Bookmark polish above

Root independently confirmed the remaining pale input boundary: the original portal selector had higher specificity than the compatibility layer. Corrected that original rule to use `--nx-border-control` and shared radius (no !important). Root removed redundant inner Bookmark heading and applied neutral resource-dialog-fields class to the existing disabled fieldset; shared CSS removes that nested decorative box without removing disabled semantics. Root added real Card-asChild consumers for Bookmark articles. Mobile quick-search keyboard hint/visual label are clipped, preserving the button's unchanged accessible name/title and44px action.

Added bounded real-browser test `shared foundation: Bookmark create/edit visual and rendered control contrast`. It independently creates a unique synthetic Bookmark through ordinary UI Save, verifies the populated result, opens create and its own record edit, asserts one heading, resolves actual computed input border/background/token and calculates relative-luminance contrast, captures actual UI, and closes with Escape. No authentication bypass, CSS injection or browser-storage injection. Three actual projects1440/768/390. First anchored grep selected zero tests (exit1, no scenario executed); corrected full-title substring executed **3PASS in6.8s**,0failure/skip/errors.

Evidence: `web/Nexora.Web/artifacts/uiux-audit/ui-foundation-controls/` contains JUnit, three sanitized computed-control JSONs and nine populated/create/edit screenshots. All18field observations (3fields ×2modes ×3viewports) used actual `rgb(129,144,165)` border against white, **3.2477316577:1**. This verifies these rendered light-mode control boundaries only, not every control/theme or full WCAG compliance. Nine earlier scanner scenarios remain separate from these three targeted scenarios; do not sum overlapping route/screenshot observations as feature acceptance.

Personally opened **all nine** final foundation-populated/create/edit screenshots at1440,768,390. The repeated title and nested box are removed; labels/inputs/actions remain separated, darker boundaries visible, footer reachable, mobile header compact. No newly observed clipping/overlap in these selected captures. This closes the specific Bookmark border/polish concern recorded above. Human final UI/product acceptance remains pending.

### Final source and targeted-evidence fingerprints
- `web/Nexora.Web/src/styles.css`: `A6AA797BC800C16D93EF9C2E12B0AE2E4E3D07C6AAAFF0EB21E0647ACD680325`
- `web/Nexora.Web/src/App.tsx`: `2F51F975BC550B29FD597AD6C47A0BA711F2F303136126CF8953709242B6466C`
- `web/Nexora.Web/src/ui/primitives.tsx`: `CCC63FBD9F920E8C061437006331D8441E90395D04C55E9F91E745E7BC8CE0BE`
- `web/Nexora.Web/src/ResourceFormDialog.tsx`: `006A6220944A2D0C0C3E000C0195E29943BCF29880D2EEE91DBCCE5CA5ECF58A`
- `web/Nexora.Web/e2e/pr4-uiux-audit.spec.ts`: `3802E689A26E93B5511691DBD92BB59433836021402235621599ACFA2CE65028`
- `web/Nexora.Web/playwright.ux-audit.config.ts`: `0B59E1A3D79BFDBBAEF7BC3AE25E3F3BEE987266CFED05880BC8882E681F7AF8`
- `web/Nexora.Web/artifacts/uiux-audit/ui-foundation-controls/playwright-junit.xml`: `FC59966F9DF8C35729AFBB83D9E59A827BA104F340D37405E9E769464658B26B`

Final repeated implementation checks after portal/title/Card/header fixes: build/typecheck PASS392modules, Vite6.4.5 built6.15s; CSS48.05kB/10.20kBgzip; main1184.99kB/306.10kBgzip; Markdown159.33kB/48.72kBgzip. The main>500kB warning remains. Focused primitive4+dialog6 again10PASS onVitest5.0.5,2.67s,0failure; repeated async-act warning retained. These repetitions are not additional unique tests. Worker-owned diff --check passed (line-ending notices only). Final targeted JUnit3tests0failure/skip/errors,6.784237s.

Curated2026-10-26: exactly9 final safe synthetic PNGs copied byte-for-byte to [repository visual evidence](../evidence/ui-foundation/README.md),946023bytes total, source/destination SHA256 matched. README includes per-image hashes/source-test fingerprints and explicitly pending final acceptance. Independent root Bookmark recovery review: [review](review-bookmark-recovery-20261026.md); no blocking bounded source defect, no reviewer functional execution.

## Final2026-10-26 continuation

Shared modal scroll isolation/CSSOM defect and independent resolution are detailed in [modal report](modal-isolation-20261026.md); current genuine-Admin Template→create-only Bookmark browser proof in [permission report](templates-permissions-20261026.md); direct-content card/native-control root cause and final3viewport real Time/Course regression in [layout report](shared-card-layout-20261026.md). These are bounded verified paths, not full-release acceptance.

After final shared changes, refreshed selected Bookmark populated/create/edit cases **3PASS**,5.9s in worker ui-foundation-frozen. All nine fresh actual screenshots personally opened again; copied byte-for-byte over the earlier curated set, exactly9PNGs plus source/test/hash manifest at [curated evidence](../evidence/ui-foundation/README.md). Token boundary assertions still verify all18light-mode field observations. This set supersedes pre-scroll image freshness limits; earlier run/failure evidence remains retained. No further production source edits planned after final static-filter fix; root owns broad regression, latest-head CI and delivery.
