# Documents library fields correction — plan and source binding

Owner root, branch impl/m01-s00-scaffold parent678e30b630fdcfc97c74d3cc43f8d435064b7da8/treeb47d6d1a059c2cb81a00b948e8f3a0aea05aeb3a. Full local DEC014/user authorization, code-only amendment, existing draftPR4/no merge. Frontend routing rules previously read apply. No API/DB/schema/authority/provider/grant changes.

Confirmed finding: docs/requirements/phases/phase-03-knowledge-search-dashboard.md P03-DOC-007 and docs/features/20-documents.md flow1 plus docs/ux-ui/modules/20-documents.md section12 require Grid/Table display only Title,DocumentType,Tag. Current App DocumentsScreen additionally shows EditorMode,Version,Status,Updated. NXG-FX20-G03; NXG-SYS-13/14. This is a semantic UI mismatch, not an inferred extra feature.

Plan: remove extra data fields from both list renderers; retain explicit Open action and accessible table caption/actions label, existing loading/error/paging/filter behavior. Editor detail continues to show its declared lifecycle/version. No folder/tree fake data or scope reduction. Rollback UI-only revert; no persistent state impact. Verify npm run build and scoped diffcheck; owner functional/browser QA not run. Product remains INCOMPLETE.

Build evidence: `npm run build` (tsc -b && vite build) PASS78modules832.67kBJS/existing >500kBwarning3.24s. Scoped `git diff --check -- web/Nexora.Web/src/App.tsx` PASS (LF/CRLF warning only). App SHA256 c67cc2199a937b3737e727e5b5694d467dfdea981af38ab90a344e52b0ebd5f7. Functional/browser/runtime checks NOT RUN; source review pending.

Independent security_review: diff only removes undeclared list fields; filtering/paging/action guards unchanged, no confirmed scoped blocker at above fingerprint. Bounded presentation correction IMPLEMENTED_VERIFICATION_PENDING.
