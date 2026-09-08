# UX consistency review —2026-09-07

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Review covers all40module documents and shared profiles, read against current feature authority before adaptation.

| Severity | Finding | Normalization | Status |
| --- | --- | --- | --- |
| Critical | F01: Generic UX matrices implied support/share availability across modules | Replaced with context intersection and explicit source lifecycle; Vault/operational/Calendar exclusions. | Resolved in docs; authorization runtime tests pending |
| Critical | F02: Financial/Vault/open egress details could be mistaken for approved implementation | Conditional table/workflow/ADR gates Q-04/Q-05/Q-06/Q-07; no approval relabeling. | Product decisions remain open |
| High | F06: Notification UX said may create channels | MUST all3attempts independently for all categories; delivery not read receipt. | Resolved docs; provider delivery tests later |
| High | F07: Planner UX said planning changes scheduling | Pin only planning metadata; source Task schedules unchanged, no unscheduled Task assumption. | Resolved docs |
| High | F08: Verification provisioning timing inconsistent | Pending User/token; activate+PersonalSpace+default grants atomically at verification. | Resolved technical design preserving confirmed email gate |
| High | F09: Module screen inventories lacked route/action/content/state detail | Unique197Screen IDs and concrete per-screen contracts, forms, controls, lifecycle and dialogs. | Resolved docs; prototypes/testing later |
| Medium | F10: Projects card added Status/Priority/Tag beyond approved fields | Title/Start/End only; status filter/detail kept. Documents Title/Type/Tag only. | Resolved docs |
| Medium | F11: Roadmap still called routine fields/tree rules Open | Canonical design notice + reconciliation: Title limits, Tag history, Folder no-move, Task/Project backward rules, all-day reminder already delegated. | Historical text preserved, no longer normative where superseded |
| Medium | F12: Calendar resize/default, share error disclosure and fixed-week semantics inconsistent | Preserve Calendar defaultDay/selected view; anonymous share generic unavailable; Planner Monday delegated, GitHubUTCweek metric explicit. | Resolved docs |
| Medium | F13: Generic mobile/keyboard/empty states left implementation guesses | Normative profiles, column priority/detail fallback, no drag-only, partial provider vs empty, focus/back. | Resolved docs; accessibility verification pending |
| Medium | F14: Reference register relied on product names/root help and implied behavior | Official dated evidence per reference; API/product-page/excerpt limits; failed extraction not used as proof. | Resolved evidence labeling; no authenticated testing claimed |
| Low | F15: Delete/Cancel/Archive wording and generic success toasts inconsistent | Canonical verbs, Canceled spelling alias, domain-specific irreversible warnings and accepted-vs-done. | Resolved docs |


## Recommended normalization applied

One AppShell/profile library; one destructive vocabulary/dialog system; exact source defaults/columns; contextual readonly instead of generic all-module access; source-specific lifecycle and provider errors; keyboard Move/reorder and mobile Back; shared search scopes; no reference autosave/retention/team/Vault/financial semantics adopted.

## Review checklist

- Same action wording and confirmation model across modules, with documented domain exceptions.
- No module-local replacement design systems/components.
- Filters/sorts follow source spec; time overlap versus ICS containment distinguished.
- Every inventory entry has primary/secondary/data/layout/state/exit/keyboard/mobile contract.
- Source links/context don't hide a permission escalation or reset unsafe cache.
- Destructive actions either recover via approved Trash or explicitly warn irreversible.
- Provider failure/loading not falsely shown as empty or saved success.

Remaining major decisions: [Q-01…Q-12](../decisions/ux-open-questions.md). This is a document-level audit; no live UI, keyboard, contrast, screen-reader, responsive or usability tests were run. Final wireframes/mockups and implementation must validate these contracts after approval.
