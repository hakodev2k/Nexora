# Independent Bookmark recovery review — 2026-10-26

Scope: root-authored App Bookmark HTTP408/closeBlocked/informed-abandon/create-only changes and functional/bookmark-recovery.spec.ts assertions; read-only review, no App edits or functional runs. Base a0215f7 plus coordinated source. Reviewer authored shared visual primitives, but did not author the reviewed root recovery behavior or functional cases; this report does not independently approve the reviewer's shared primitives/modal code.

Current AGENTS/nexora-engineering/profile/Technical Lead core and earlier loaded frontend/security/verification/UX sources apply. Authorization: DEC014 automated QA, unchanged D01–D09. Trace UX-05 pending/exact retry/dirty recovery, UX-11 modal guards and NXG-SYS-02/03/13/14. No product/provider/permission gate relaxed.

Outcome: **no blocking source defect identified** in this bounded reviewed delta.

- HTTP408 joins network/5xx/in-progress unknown outcomes. Pending save disables editable fields/reset, guards load/navigation and blocks normal Cancel/Escape with closeBlocked. Retry keeps original key and unchanged body/ETag; conflict refresh resets key for the new revision. Informed abandonment explicitly explains a possible commit and performs a reconciled read without replay; canceling abandonment retains frozen intent.
- Create-only Template handoff waits for current create capability, not read. Read-denied load retains returned action decisions, clears private rows/editor and skips the protected library GET. Server remains authoritative; default deny and module checks unchanged. Opaque committed acknowledgment clears protected state without synthesizing read results.
- Functional timeout fault calls the genuine API via route.fetch first and substitutes only the successful response with HTTP408. Positive writes/replays use real server results. Assertions verify committed SQL row, frozen draft, disabled Cancel/Escape, exact two request body/key/result-id equality, one list result and one persisted record after reload. Abandonment checks explicit warning, retained intent when canceled, one sent command and persisted state. Request interception is removed in finally.
- Component source additionally covers update ETag and mocked create-only capability/no-read paths. New functional cases are POST create under UserA; they do not independently prove create-only Admin or PUT runtime authorization. Root requested a separate genuine-Admin permission browser case; authored independently as templates-permissions.spec.ts and pending exclusive execution. Do not claim that case PASS before execution.

No tests executed during this review; root's actual runtime/CI results must be recorded separately. Selected source review is not full-feature/release acceptance.

## Reviewed SHA256
- `web/Nexora.Web/src/App.tsx`: `2F51F975BC550B29FD597AD6C47A0BA711F2F303136126CF8953709242B6466C`
- `web/Nexora.Web/src/ResourceFormDialog.tsx`: `006A6220944A2D0C0C3E000C0195E29943BCF29880D2EEE91DBCCE5CA5ECF58A`
- `web/Nexora.Web/src/BookmarkRecovery.test.tsx`: `F1FC8DD937180B4611809448F659FF919849F0BB51C48F9DAAF14E325FDF123C`
- `web/Nexora.Web/functional/bookmark-recovery.spec.ts`: `03D8514CC82E504DEF6BA7B0AFA710AFE6A3E14D53F2400156CD7CCCCBD34BFF`
