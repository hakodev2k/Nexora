# UX decisions and authority

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

| ID | Decision | Status/source |
| --- | --- | --- |
| UX-D01 | One shell + shared primitive/profile library; module-local extensions only | Resolved delegated; UX-00…15A |
| UX-D02 | Projects Grid, Project Tasks Kanban, Documents Grid, Calendar Day | Approved PO; FX-COM-004; never override by persisted last view |
| UX-D03 | Projects card/table Title/Start/End; Documents Title/Type/Tag | Approved PO/features; removes previous extra card fields |
| UX-D04 | Explicit manual Document Save, immutable Type/Mode/location, complete version history | Approved PO + delegated limits in FX-20 |
| UX-D05 | All notification categories create three channel attempts; no preferences/mute | Approved PO + FX-COM-014; replaces may create wording |
| UX-D06 | Planner pin does not reschedule Task; Monday week start delegated | FX-15-BR/data; corrects earlier UX scheduling sentence/Q-09 ambiguity |
| UX-D07 | Unique Screen IDs/routes proposed and common state inheritance | Resolved delegated; not implemented endpoints |
| UX-D08 | Keyboard Move/Reorder, dialog focus, mobile stack/detail variants | Resolved delegated; W3C/Linear references adapted |
| UX-D09 | Safe explicit Support/Emergency projections, no ambient Admin access | Approved PO restrictions; Q-04 unresolved Vault metadata |
| UX-D10 | Sensitive share/recovery/financial/egress gates stay Proposed | Major Q authority retained, no UI-derived approval |
| UX-D11 | Canceled spelling normalized, source Cancelled alias documented | Resolved delegated terminology; no lifecycle change |
| UX-D12 | Dashboard attention default widgets retained from FX-26 delegated baseline | No new PO question for routine widget layout |
| UX-D13 | Technical DB/schema/query choices linked to architecture ADRs | Technical decision; separate from product and code approval |


No mass Approved label for newly chosen behavior. Reference rationale must state Apply/Adapt/Reject and evidence limitation. Revisit only when new PO decision or verified conflict materially changes scope/security/lifecycle, not individual icon/button preferences.
