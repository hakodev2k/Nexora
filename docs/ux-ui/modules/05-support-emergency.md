# FX-05 — Support / Emergency / Security Center — UX/UI Specification

**Status:** UX blueprint; implementation not approved by this document.  
**Baseline:** `89198351a3d6cf937179d234a0f16e8cf8c259d7`

## 1. Scope
Translate the current FX-05 feature behavior into a coherent Nexora UI. Reference products do not add scope automatically.

## 2. Requirement sources
- `docs/features/05-support-emergency-and-security-center.md`
- `docs/features/00-shared-behavior.md`
- `docs/features/90-open-decisions.md`
- related `docs/requirements/**` and phase requirements referenced by the feature source
- `docs/ux-ui/global/**`

## 3. Reference products
- Microsoft Customer Lockbox
- Break-glass systems

## 4. Reference behavior analysis
- Persistent mode banners.
- Always show user/module/expiry/read-only.
- Emergency shows reason/audit status.
- Never provide cross-user Vault Reveal/Copy.

Classification:
- **Apply** familiar mechanics that do not change Nexora semantics.
- **Adapt** when ownership/lifecycle/privacy differs.
- **Reject** unapproved collaboration, retention, automation or editing behavior.
- **Future** useful behavior requiring explicit scope.

## 5. UX principles
1. Primary job is within one transition from module entry.
2. Current state and next valid action are visible.
3. Disabled/read-only/stale/provider-failed/empty are distinct.
4. UI reflects server lifecycle proactively.
5. Reuse global primitives before defining exceptions.

## 6. Information architecture
One global module entry. Stable subareas use module-local navigation. Browse uses list/grid/table/board only where supported. Detail uses page or context panel. Global Search uses safe provider projections.

## 7. Screen inventory
- 01 — Grant Support
- 02 — Support Session
- 03 — Emergency Access
- 04 — Security Center

Routes are UX proposals, not claims about deployed frontend/API routes.

## 8. Navigation
Preserve filter/sort/scroll when returning from detail. Deep links resolve after auth/module/permission checks. Mobile converts nested panes to stacked navigation. Ctrl/Cmd+K never bypasses lifecycle gates.

## 9. User journeys
- Primary: entry → browse/search → open/create → validate → explicit Save/action → feedback → reconcile.
- Alternative: browse → filter/sort/view → inspect → return with context.
- Error: action → safe error → preserve state → retry/recover.
- Read-only: open → visible reason/state → browse allowed data.
- Destructive: action → impact preview when needed → explicit confirmation → authoritative update.

## 10. Screen specifications
Browse: title, primary action, scoped search, supported filters/sort, distinct loading/empty/error/unavailable.
Detail: identity, status, high-priority metadata, primary valid action, secondary actions, history where owned.
Create/Edit: explicit Save, inline validation, dirty guard, immutable fields visible, stale revision never overwrites.

## 11. Forms & validation
Use `global/05-forms-and-validation.md`. Required fields/domain validation come from source.

## 12. Collection behavior
Use `global/06-lists-grids-tables-kanban.md`. Drag always has accessible alternative.

## 13. Search / Filter / Sort
Scope is visible. Local search stays local. Filter state survives detail navigation. Sensitive payload never appears in preview.

## 14. Lifecycle UX
Terminal/Archived/Trash are not generic synonyms. Read-only lifecycle has a persistent visible explanation.

## 15. Action matrix
| Context | Browse | Inspect | Edit | Lifecycle | Purge |
|---|---:|---:|---:|---:|---:|
| Active owner | Yes | Yes | If allowed | If valid | If supported |
| Read-only lifecycle | Yes | Yes | No unless source allows | Limited | Per feature |
| Trash | Trash view | Safe detail | No | Restore if valid | If allowed |
| Support | Scoped | Yes | No | No | No |
| Emergency | Scoped | Yes | No | No | No |
| Share viewer | Shared projection | Yes | No | No | No |

## 16. Dialogs
Confirmation for meaningful lifecycle changes; reason dialog when source requires it; dependency/aggregate preview before destructive batch/tree operations.

## 17. Loading / Empty / Error / Degraded
Distinct: initial no data, no search result, no filtered result, loading, request failed, unavailable, denied, stale/provider-degraded when applicable.

## 18. Permissions / Sensitive context
Client visibility is not authorization. Support/Emergency are scoped read-only only. Follow `global/12-security-sensitive-ux.md`.

## 19. Responsive
Desktop can use module nav + workspace + optional detail panel. Tablet collapses navigation. Mobile stacks browse→detail. Tables prioritize fields.

## 20. Accessibility
Keyboard create/open/edit/save/cancel; no drag-only actions; visible focus; text/icon status; accessible dialogs/tables/charts; focus returns after overlays.

## 21. Cross-module integration
Cross-module objects are references. Owning module retains state authority.

## 22. Delegated UX decisions
Layout, pane choice, toolbar order, responsive transform, confirmation presentation and keyboard affordances are Resolved delegated unless they change major product/security/financial/privacy behavior.

## 23. Major open questions
Q-02, Q-04

Behavior depending on these remains **Proposed/Blocked**, not Approved.

## 24. Acceptance checklist
- [ ] Primary journey has no hidden interaction dependency.
- [ ] Lifecycle state and valid next actions are visible.
- [ ] Loading/empty/error/unavailable differ.
- [ ] Keyboard path exists.
- [ ] Mobile has no dead end.
- [ ] Destructive vocabulary is canonical.
- [ ] Support/Emergency cannot mutate.
- [ ] Reference products add no unapproved scope.
- [ ] Major open decisions are marked Proposed/Blocked.
