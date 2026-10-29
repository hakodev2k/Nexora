# FX18 uncertain-save — independent source review

Reviewer: `/root/shopping_completion`; author: `learning_completion`; coordinator: `/root`.
Requested outcome: independently inspect the bounded TimeTrackingScreen uncertain-save change, without editing the author file or executing tests/runtime. Current authorization is the latest user AGENTS code-only amendment. Branch `impl/m01-s00-scaffold`, base `75b6d9f4f0ac247680e22cc07bf26a4890071ef2` plus shared working changes. No capability, provider, migration or release gate is changed by this review.

Reviewed source: `web/Nexora.Web/src/TimeTrackingScreen.tsx`, `timeApi.ts`, relevant `apiFetch` error/CSRF behavior, `ResourceFormDialog.tsx` and author's `workers/time-recovery-20261025-code-only.md`. Bound sources: FX18-S01/S03, `FX-18-BR-001/002/003/005`, `FX-18-AC-001`, action catalog module18 and UX module18 section16 same-key retries; goals NXG-FX18-G01/G03 and NXG-SYS-05/13/14. Startup/engineering skill, Technical Lead core, frontend routing rules/skills and module goals apply. Product server authorization remains authoritative.

Snapshot SHA256 of TimeTrackingScreen: `61BBC679ACFD8FCDC28AAEDDC956A40E54E5C1404967EF643B7B8C63E21D404D`. This is working-tree source identity, not final Git commit/runtime evidence.

## Source conclusions

| Concern | Reviewed reasoning | Disposition |
|---|---|---|
| Exact command/key/ETag freeze | Lines61–69 capture submitted strings, instant conversions, overlap Boolean, key and a cloned entry (including original ETag) once. Retries invoke that retained closure. Inputs disabled on unknown result; `changed()` cannot replace the captured key. timeSave serializes that body and uses captured entry ETag. | No blocking source defect established. |
| Duplicate submit | Lines60/72 use a synchronous ref before the first await, so a second immediate submit returns while the first is pending. The same retained command is used after uncertainty. | No blocking source defect established for editor save. Other pre-existing mutation controls are outside this patch. |
| Unknown versus definitive result | Network/0/408/5xx and409 RequestInProgress retain command and freeze fields; definite errors clear retained command while preserving editable form values. A confirmed result clears/closes editor before separate reload. A refresh failure therefore does not reopen a supposedly uncertain write. | No blocking source defect established. |
| Auth denial/privacy | Current401/403 pass through fail/clear, which invalidates load/history/purge epochs, clears retained command, private field state, selections and visible records.401 calls parent auth loss. | No blocking source defect established. This is source inspection, not cross-user execution proof. |
| Async epoch | Save captures current loadSequence and refuses stale success/error effects. Load cleanup invalidates sequence on unmount. Finally clears latch/busy; no new editor can open while an active/retained command exists. | No blocking source defect established in current guarded editor path. |
| Navigation/discard | ResourceFormDialog blocks close/Escape/internal leave while busy or closeBlocked; beforeunload warns. Unknown state offers a separate two-step abandonment with explicit prior-commit warning, then clears command/fields and reloads. | No blocking source defect established. Browser unload cannot be guaranteed prevented by source alone. |

## Finding

**TR18-R01 — Low, visual consistency.** At the reviewed snapshot line104 the editor `<form onSubmit=...>` lacks `className="resource-form"`. Root's shared `.resource-form-dialog .resource-form` grid/label styling consequently does not target this form. Source reproduction: open Manual entry or Start timer; form is portaled within ResourceFormDialog, but the required descendant selector has no matching form class. Add the existing resource-form class to the form; no business/permission change is needed. Coordinator already owns this correction. No screenshot or runtime reproduction was performed under this code-only assignment.

No Critical/High or other blocking defect was established in the bounded patch. This is an independent source review, **not** functional acceptance or proof of FX18 completion.

Source-only closure: root added `className="resource-form"`; reviewer re-read the actual form at line105 and confirmed TR18-R01 FIXED in source. New screen SHA256: `5FBB332A66482A263C7573DDF6C15D7BDE2295B4FA9D1D5D75561AA0BE70C095`. No browser recapture or visual acceptance follows from that correction.

## Evidence limits / pending owner QA

No tests, fixtures, synthetic records, browser recaptures, runtime commands or new compile commands were executed by this review. The author's prior typecheck is reported by its author, not independently rerun here. Owner QA remains required for actual lost response and replay, rapid duplicate submit, current authorization revocation, stale ETag correction, refresh failure after commit, keyboard focus and navigation/unload behavior. All pre-existing FX18 linked-source/report/history/lifecycle completion gaps remain separately tracked; feature release status stays NOT VERIFIED. Later author/root edits require source re-review or updated identity before applying these conclusions to final bytes.
