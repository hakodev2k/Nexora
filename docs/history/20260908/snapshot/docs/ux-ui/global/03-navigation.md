# UX-03 — Navigation and return behavior

> **Current decision amendment — 2026-09-07:** Internal-only user journeys; paused module navigation removed; Account/Vault deletion exceptions and SuperAdmin RECOVERY context override generic purge/restore patterns. Language default vi, selectable en. External research URLs in docs are evidence, not product navigation. [Normative PO decisions](../../requirements/10-owner-decisions-20260907.md). Conflicting older proposal paragraphs below are historical; current field/action overrides are in the linked delta. Docs-only.

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Every inventory route is proposed. Top-level module click opens its approved default (Projects Grid, Documents Grid, Calendar Day; Project detail Tasks Kanban). In-session navigation Back preserves view/filter/date/scroll/selection; a remembered preference must not silently override those approved entry defaults.

Breadcrumb parent nodes are links; current node text only. Browser Back and explicit Back share safe return context; if opening deep link directly, fallback to module root/Home. Return target same-origin allowlist, no open redirect through login/share. Create Save navigates new detail; Edit Save stays detail with success/revision; Cancel returns origin without mutation, dirty guard where needed.

Global Quick Create shows only registered create contributions with current module/action availability. Task create asks/selects an active Project then full Task form with required Start/End; no orphan Task or fake quick-save. Document create always asks Type and Editor. Calendar create ManualEvent only. Template opens normal form, not mutation itself.

Source links keep typed context: Calendar Task → Task detail; Planner pin → original Task; Resume application → exact Resume version; Share Project Task → shared readonly child detail under same link, never owner route. File picker closes back to originating form with reference draft; source Save commits association.

Unavailable state has Back/Home and explanation appropriate to owner. Anonymous share failure does not expose whether a private resource exists. No disabled hover tooltip as sole explanation. Mobile Back exits detail before module; closing module tree returns focus to its trigger. Keyboard shortcut reference visible via Help; shortcuts don't fire while typing or IME composition except editor-approved Save.
