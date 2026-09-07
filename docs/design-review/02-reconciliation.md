# Source reconciliation and consistency findings

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

| Severity | ID | Baseline issue | Normalization | Status |
| --- | --- | --- | --- | --- |
| Critical | F01 | Generic UX matrices implied support/share availability across modules | Replaced with context intersection and explicit source lifecycle; Vault/operational/Calendar exclusions. | Resolved in docs; authorization runtime tests pending |
| Critical | F02 | Financial/Vault/open egress details could be mistaken for approved implementation | Conditional table/workflow/ADR gates Q-04/Q-05/Q-06/Q-07; no approval relabeling. | Product decisions remain open |
| High | F03 | Database roadmap lacked complete field types/keys/relations | Expanded dictionary, per-field types/nullability/FKs/indexes/classification and JSON contracts. | Resolved document gap; SQL verification pending |
| High | F04 | Layer namespaces insufficient future-module boundaries | Module implementation+Contracts assembly boundaries, per-schema contexts and contribution test kit. | Resolved technical design; enforcement tests later |
| High | F05 | Cross-context transaction, reference/purge race and version/Trash cohorts vague | Unit of work transaction catalog, root locks, registry reference guard, Archive/Trash batches, file/version pins. | Resolved technical design; concurrency tests later |
| High | F06 | Notification UX said may create channels | MUST all3attempts independently for all categories; delivery not read receipt. | Resolved docs; provider delivery tests later |
| High | F07 | Planner UX said planning changes scheduling | Pin only planning metadata; source Task schedules unchanged, no unscheduled Task assumption. | Resolved docs |
| High | F08 | Verification provisioning timing inconsistent | Pending User/token; activate+PersonalSpace+default grants atomically at verification. | Resolved technical design preserving confirmed email gate |
| High | F09 | Module screen inventories lacked route/action/content/state detail | Unique197Screen IDs and concrete per-screen contracts, forms, controls, lifecycle and dialogs. | Resolved docs; prototypes/testing later |
| Medium | F10 | Projects card added Status/Priority/Tag beyond approved fields | Title/Start/End only; status filter/detail kept. Documents Title/Type/Tag only. | Resolved docs |
| Medium | F11 | Roadmap still called routine fields/tree rules Open | Canonical design notice + reconciliation: Title limits, Tag history, Folder no-move, Task/Project backward rules, all-day reminder already delegated. | Historical text preserved, no longer normative where superseded |
| Medium | F12 | Calendar resize/default, share error disclosure and fixed-week semantics inconsistent | Preserve Calendar defaultDay/selected view; anonymous share generic unavailable; Planner Monday delegated, GitHubUTCweek metric explicit. | Resolved docs |
| Medium | F13 | Generic mobile/keyboard/empty states left implementation guesses | Normative profiles, column priority/detail fallback, no drag-only, partial provider vs empty, focus/back. | Resolved docs; accessibility verification pending |
| Medium | F14 | Reference register relied on product names/root help and implied behavior | Official dated evidence per reference; API/product-page/excerpt limits; failed extraction not used as proof. | Resolved evidence labeling; no authenticated testing claimed |
| Low | F15 | Delete/Cancel/Archive wording and generic success toasts inconsistent | Canonical verbs, Canceled spelling alias, domain-specific irreversible warnings and accepted-vs-done. | Resolved docs |


## Historical roadmap corrections

Roadmap05 retains prior planning text for traceability but no longer defines current field contracts. Specifically: Document Title1..200, no Folder move, one Tag with active/Archived/Trash references blocking delete but history-label snapshot not FK, immutable root/child/Folder, Project InProgress→NotStarted reason, all-day end-exclusive dates/23:45 prior-day reminder, Timer/Focus limits, Goal formulas and Dashboard default widgets are already resolved in feature specs. Do not ask PO these again as Open.

UX historical Q-09 statement about week-start is not a new blocker: feature15 delegates Monday and feature33 defines UTCMonday ranking; language/default currency Q-09 remains open. New visible choices can be proposed separately if business intent changes.

Current product vocabulary Canceled normalizes historical Cancelled spelling only. Automation PartiallySucceeded/TimedOut/Skipped remain distinct result meanings, not collapsed into generic Failed. No lifecycle rewritten merely for color consistency.

## Remaining material uncertainties

See [Q impact register](03-decision-impact.md); they are not defects silently fixed by UI research. High-impact security/recovery/financial/provider/workflow rules require PO approval and technical verification. Column storage precision is not final currency rounding policy. Masking is not a cryptographic architecture. Snapshot/diagram existence is not integration-test evidence.
