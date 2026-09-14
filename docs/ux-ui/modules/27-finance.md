# Finance — UX/UI Specification

Current basic scope · 2026-09-08 · Local UI slice implemented on PR #4 under DEC-20260909-014 (runtime verification remains owner work). [Previous advanced UX](../../history/20260908/snapshot/docs/ux-ui/modules/27-finance.md) is proposal history.

## 1. Scope

Manual categories/prices are implemented as the current local slice; advanced ledger and correction workflows remain gated P-H05, sensitive sharing P-H03. Not part of M01 delivery labels.

## 2. Requirement sources

[Feature](../../features/27-finance.md), [PO Q05](../../requirements/10-owner-decisions-20260907.md), [actions](../../action-catalog/modules/27-finance.md), [data](../../design-database/07-finance-vault.md#finance-manualrecord).

## 3. Reference products

Actual Budget reference retained from [product register](../references/product-reference-register.md). PO manual entry controls scope; no external account sync.

## 4. Reference behavior analysis

| Product | Behavior | Decision | Rationale |
| --- | --- | --- | --- |
| Actual Budget | Dense transaction table | Adapt common Nexora table | Date/category/price rows are comparable, no balance semantics imported |
| Actual Budget | Linked transfers | Future / blocked | Manual prices do not have ledger legs; P-H05 required |

## 5. UX principles for this module

Always display currency beside amount; zero differs missing. No inferred financial accounting. Explicit Save and owner-private data.

## 6. Information architecture

Finance → Records, Categories. Advanced sidebar entries absent until approved and implemented. Full catalog definitions are not fake working navigation.

## 7. Screen inventory

| ID | Screen | Proposed route | Purpose | Primary action |
| --- | --- | --- | --- | --- |
| FX27-S13 | Manual records | /finance/records | Owned price records and filtered same-currency totals | New record |
| FX27-S14 | Manual record form | /finance/records/new; /finance/records/:id/edit | Category/amount/currency/date/note | Save |
| FX27-S15 | Categories | /finance/categories | Own category names | New category |

FX27-S01..S12 remain historical advanced screen IDs, not current implementation entry points; exact former layouts are in the snapshot and require P-H05 before resumption.

## 8. Navigation

Finance opens Records; toolbar Categories and New record. Row opens form; Back/Cancel restores filters/scroll; direct form link Back→Records. Dirty guard Save/Discard/Keep editing.

## 9. Primary user journeys

Create category → New record → explicit currency + amount/date → Save → record visible. Alternative: inline category create, then continue form. Empty category state offers Create. Invalid category/amount shows field errors. Used-category deletion shows dependency count without deleting; foreign record404 and safe Back.

## 10. Screen specifications

| Screen | Header / regions | Search/filter/sort | States and behavior |
| --- | --- | --- | --- |
| S13 | Finance; New record; category/date/currency toolbar; table; grouped totals | Search category/note; filter category/currency/date; OccurredOn desc + ID; page25 | First-use CTA; filtered-none Clear filters; skeleton on first load;503 retry, no fake0;403 clear data; row opens S14 |
| S14 | New/Edit record; fields; Save/Cancel footer | N/A form; category picker owner-scoped | Required category/amount/currency/date, optional note; duplicate submit disabled;422 focus field;412 compare/reload; session expiry clears protected draft; no delete/status/post controls |
| S15 | Categories; New category; name table; usage count | Name search; normalizedTitle asc+ID; page25 | Inline rename explicit Save; delete unused only after confirmation; referenced category409 explain; mobile list detail retains actions |

No Archive/Trash state for current basic records until lifecycle approved; generic component must not expose those actions. Disabled module replaces content with safe unavailable screen, not empty data. Read-only permission removes Save and explains reason; API remains authoritative.

## 11. Forms and validation

Category1–100, record amount decimal string nonnegative within28,8; explicit currency, local date; note≤2000. No account/type/transfer/FX field. Inline category creation has its own retry identity; cancel record does not silently delete an already saved category.

## 12. Lists / Grid / Table / Kanban behavior

Table columns date/category/amount+currency/note indicator/actions. No Grid/Kanban in current basic slice. Touch row opens detail; action buttons keyboard labeled; no bulk mutation until semantic contract exists.

## 13. Search / Filter / Sort

Server owner filter before text/filter/page. Query change resets cursor; preserve filter in internal URL only, never note or amount value in route. Totals reflect same current filter and remain per currency. Failed fetch is not no results.

## 14. Lifecycle UX

Saved records read/edit; no posted/draft ledger distinction. Financial deletion unresolved; no Trash/void/post/reverse. Category removable only unused.

## 15. Action matrix

| State | Action | Available | UX |
| --- | --- | --- | --- |
| Own saved record | Read/Edit | Yes if authorized | Explicit Save with revision |
| Own saved record | Delete/Archive/Share | No current contract | No executable control; gated scope |
| Unused category | Rename/Remove | Yes | Save / destructive confirmation |
| Used category | Remove | No | Explain references, preserve records |
| Module disabled | Any source mutation | No | Unavailable page, no stale edit |

## 16. Dialogs

Unused category removal: title/name/reference preview, Remove category/Cancel, irreversible unused metadata warning;409 stays dialog. Stale edit: compare server fields and draft, Reload/Keep editing; no overwrite. Record deletion dialog N/A until business rule approved.

## 17. Loading / Empty / Error / Degraded

Skeleton first load; first-use New record; filtered-none Clear; inline422;412 conflict;401 reauth;403 unavailable;503 retry. No provider dependency for manual prices; no fake currency conversion.

## 18. Permissions / Read-only / Sensitive contexts

Owner-only baseline. Admin/SuperAdmin normal route does not read another user's records. Sharing/support projection pending; masking not authorization. No amount/note toast/log or persistent browser storage.

## 19. Responsive behavior

Desktop table; tablet keep date/category/amount, note in detail; mobile stacked rows and full-screen form. Currency never hidden, Save visible above keyboard; do not force horizontal page scroll.

## 20. Accessibility

Amount label includes currency; localized visual formatting with canonical submit string; error text linked; totals table/text equivalent; focus return from dialog, keyboard row actions, status not color-only.

## 21. Cross-module integration

Typed ManualCategory/ManualRecord and Resource owner identity; no TransactionLeg/Account dependency. Own search/dashboard only approved projections; disabled modules invalidate contributions. No paused automation handlers.

## 22. UX decisions made by delegated authority

Table/default sort/page25, explicit currency/display unit, inline category create and conflict handling. None approves advanced finance semantics.

## 23. Major open questions

[P-H03 sensitive projections and P-H05 advanced Finance](../../delivery/02-decision-proposals.md). Basic manual record contract does not depend on resolving every ledger question.

## 24. Acceptance checklist

- Required fields and no Account prerequisite match FX27-MAN-AC01..05.
- Two-currency totals remain separate; locale/timezone do not reinterpret values.
- Conflict, cross-owner, read-only, module-disabled and failed-fetch tests specified.
- Old S01..S12 workflows cannot execute without explicit scope/contract upgrade.
- Runtime/UI tests not executed in this documentation phase.
