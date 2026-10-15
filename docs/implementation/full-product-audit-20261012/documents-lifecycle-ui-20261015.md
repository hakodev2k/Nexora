# Documents declared lifecycle controls continuation

Owner root; source-only independent review security_review required. Branch impl/m01-s00-scaffold parentd8c0073b91241b684fb7dcb683bef5341a355aaa; main8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3. Full localuser/DEC014 approval andcode-only amendment remain. No grants/eligibility/provider/migration.

Source main FX20 userflow5 (Draft↔Published; both mayArchive), BR003/006 and action catalog publish/unpublish/archive/unarchive; UX20-S04/S05 lifecycle controls. Goal NXG-FX20-G02/G03. Confirmed DOC-LIFECYCLE-UI: existing UI exposes Archive onlyPublished, no ReturnDraft; missing preArchive state uses invented Draftfallback. Backend exactactions/previousstate authority now repaired in priorcheckpoint.

Bounded plan before finalvalidation: expose Publish/ReturnDraft for live states, separate confirmedArchive for bothDraft/Published, Unarchive only storedDraft/Published previousstate (nofallback). Retain existing exactcommand/action serverproof, dirty/pending/busy guards. Reuses components; no API/schema fields. Rollback revertUI. Not fulltree/cohort implementation orcapabilityUX certification.

Implementation applied; npmfrontendbuild andindependentreview pending. No tests/runtime/fixtures. Owner runtime expectations: DraftArchive/restoreDraft, PublishReturnDraft, dirty/pendingblocked, Archivedpreviousstatemissingblocked; server permissionnegative anddirectAPI remain guarded frompriorcheckpoint. Fullproduct INCOMPLETE.

Final npm run build PASS (tsc -b plusVite78modules; existing >500kB bundlewarning); no runtime/tests/fixtures. Backend/API unchanged from accepted andcompiled d8c0073. Independent security_review no confirmed blocker; final App SHA2e59c2e929d8e3f74d4d3ad0a2d7f7db519e1839ef2204ebf2fdff6dc89e76cb. Scoped diffcheckPASS. Bounded UI controls IMPLEMENTED — VERIFICATION PENDING; fullFX20/product INCOMPLETE.
