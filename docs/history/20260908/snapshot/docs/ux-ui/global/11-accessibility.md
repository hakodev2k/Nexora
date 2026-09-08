# UX-11 — Accessibility and keyboard specification

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Design target WCAG2.2AA; compliance is not claimed until visual-token and implemented assistive-tech tests pass. Use native semantic HTML before ARIA. Visible focus, skip-to-content, landmarks, single h1 and meaningful section hierarchy. Labels include purpose/unit/timezone; no placeholder-only labels. Required/error state programmatically linked, error summary focus navigates fields.

Keyboard: Tab/Shift+Tab through logical controls, Enter activates links/buttons, Space toggles checkbox; Escape closes current overlay and returns focus. No positive tabindex. Drag/reorder always Move/Move before/after command alternative. Kanban status changes/rejections announced; no only-color priority/status. Editor code indentation must document a keyboard exit from editor; shortcuts disabled in text input/IME unless intentional editor Save.

Modal: initial focus title/first field/Cancel according risk, contained tab sequence, background inert, Escape cancels except while noncancelable commit is already dispatched (announce pending), focus returns trigger or next surviving row. Combobox manual selection, aria-expanded/listbox relationship and active descendant; do not auto-select account/email on blur. [W3C dialog](https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/) and [combobox](https://www.w3.org/WAI/ARIA/apg/patterns/combobox/) reviewed2026-09-07.

Tables have caption/headers/sort announcements and named row-selection boxes. Grid cards use links not clickable generic containers. Chart value table/text equivalent; color palette has contrast and shape/label redundancy. Calendar day/event keyboard navigation, more-events list and Agenda alternative. Timer live announcement throttled to meaningful phase/end events, not each second.

Touch target44px goal; WCAG minimum/spacing exceptions verified during visual review. Contrast targets4.5:1 normal text,3:1 large text/controls as applicable; no exact palette approved here. Official [WCAG text contrast](https://www.w3.org/WAI/WCAG22/Understanding/contrast-minimum.html) and [target size minimum](https://www.w3.org/WAI/WCAG22/Understanding/target-size-minimum.html) reviewed2026-09-07 distinguish AA24px/spacing exceptions from our larger44px touch design goal. Reduced motion avoids animation-dependent comprehension; respect zoom/text-size preferences. Images decorative alt empty, meaningful cover/source image alt available; user image not auto-described by unapproved AI.

Acceptance checklist per module includes keyboard-only primary/error/destructive flow, focus after item removal, screen reader names/states,320px reflow,200%zoom, high contrast and reduced-motion behavior. No screenshot accessibility certification from document text alone.
