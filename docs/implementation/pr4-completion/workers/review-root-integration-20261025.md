# Independent root integration source review — 2026-10-25

Reviewer: Templates/UIUX worker; author of Templates delta, independent of root integration/App/shared styles and Certification author. This review does not independently approve my Templates service. Branch impl/m01-s00-scaffold, base75b6d9f4f0ac247680e22cc07bf26a4890071ef2 plus coordinated uncommitted changes. Scope: root integration source and source-documented oracle corrections. No tests, browser, runtime, or synthetic data executed during this continuation, following latest code-only amendment. Mandatory Nexora startup/routed rules and current DEC014/D01–D09 remain binding.

## Findings and disposition

| ID | Severity | Source finding | Outcome |
|---|---|---|---|
| ROOT-R01 | Medium | App Bookmark template effect required read+create although draft preparation and ordinary Add require create. Valid create-only context silently lost the intended form handoff. | Reported to root; root changed effect to require create only. Re-inspected corrected source. Read-denied branch retains returned action capabilities but clears private rows/editor. No read authority widened. FIXED by source review; alternate-permission runtime NOT VERIFIED. |
| ROOT-R02 | Medium | Bookmark uncertain-outcome predicate omitted HTTP408; sent command could be treated as definitely failed despite possible commit. | Root added408 beside network0/5xx/RequestInProgress. Re-inspected; same intent/key retry path remains. FIXED by source review; runtime NOT VERIFIED. |
| ROOT-R03 | Medium test-oracle gap | functional/content.spec.ts Documents scenario expects old selected body disabled immediately after archive, then Unarchive still on same selection. Current tree commit callback clears selected/body, invalidates list, suspends protected reads and shows committed notice. The disabled condition only applies to an actual selected Archived record. | Existing expectation is stale for opaque-commit flow; recorded run remains FAILED, not retroactively passed. Human QA should explicitly reload Archived scope, reopen current protected record, assert body read-only, unarchive with current revision, reload Library and verify Published/body. No test edits/reruns in this continuation. |

## Reviewed integration

- Program.cs registers ITemplateService and ICertificationService with stateless SQL service singletons, short-lived connections and current TimeProvider; Shopping service scoped. All three endpoint mappers installed. No live-provider worker activation observed in this wiring.
- M01MigrationManifest includes0093 Templates,0094 Shopping,0095 Certification in order after0092. This is source wiring review, not migration execution proof.
- ActionGrantPolicy adds finite canonical local actions to ApprovedShippedSelfActions: Templates9, Shopping9, Certification6. Eligibility is not a grant, module enablement or status promotion; existing SELF/current authority enforcement is retained.
- App route parsing and safe-return path allow finite local Templates/Certification/Shopping routes; detail identifiers constrained. Template handoff remains ephemeral, scope-bound to profile/personalsSpace/role/modules, nonce-keyed and one-time consumed. Bookmark initial draft uses empty form baseline then seeds values, preserving unsaved-change detection; actual persistence still requires ordinary explicit Bookmark Save under server authorization.
- Certification/Shopping component keys include owner/capability scope and resource context; protected module checks retained. UI visibility remains separate from backend authorization.
- Portal CSS explicitly targets .resource-form-dialog fields, grid/fieldset and44px controls with focus styling; it now reaches portals outside module ancestors, addressing UIUX-K01 selector root cause. Certification filter field-groups/grid collapse at768px. Productivity section class and card/action wrapping support narrow screens. Mobile Templates density/copy changes are bounded presentation changes. Post-fix appearance/interaction NOT VERIFIED; no screenshot recapture permitted in this continuation.
- Documents Markdown oracle uses literal script input and asserts no DOM script execution. This is compatible with escaped CommonMark input; preserving raw stored text is not script execution. The independent archive oracle mismatch above remains open for human QA.

## Review gate
No unaddressed Critical/High source finding identified in this bounded integration review. ROOT-R01/02 source corrections re-reviewed. ROOT-R03 remains test-oracle remediation and runtime gate, not evidence that the test passed. This is a source-review checkpoint, not whole-product or release acceptance. UIUX6-case evidence predates latest visual source fixes. Root-reported compile/typecheck results are separate evidence, not reviewer-executed checks.

## SHA256 reviewed source fingerprints
- `web/Nexora.Web/src/App.tsx`: `494EE820F4C2C6188F62D39F421838E7B5CF1E2DBFF24DF1C95FA501E5F816D9`
- `src/Nexora.Api/Program.cs`: `F4E4732D54FFD18A8A19624C1207591DF849BE1A4202AE129384918BD62E7E19`
- `src/Nexora.Domain/Access/ActionGrantPolicy.cs`: `B98E02F29BB5DC5982EEA64B27EAAF2D62B56E57BD4066349EEE6F14705C5995`
- `src/Nexora.Infrastructure/Local/M01MigrationManifest.cs`: `75EA900737B37D3F9E470C30270BBA645E21044AFCB0D1BEC6798A95E6C44DB4`
- `web/Nexora.Web/src/styles.css`: `93121F2AD30D0F40B882387BB7A96450897546E8D7EE620785F6CFB2B0B219E3`
- `web/Nexora.Web/src/CertificationScreen.tsx`: `CDCAAD831A9D199540D5C5C3E2F0D155AA7758E0C3885B3D09EA4F3ED730F495`
- `web/Nexora.Web/src/templates.css`: `350464458F8821F1FADAF59CBEC6F3058C822EE4BD8227A6D58BB0E634505673`
- `web/Nexora.Web/functional/content.spec.ts`: `BA15AAE83904D82EC7499DC548931D3BA7E223007A09D83BF8F9C352D621CC68`
