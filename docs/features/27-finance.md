# Finance — current basic manual records

FX27 · Current specification · 2026-09-08 · Contracted local slice implemented on PR #4 under DEC-20260909-014 (runtime verification remains owner work). [Historical ledger proposal](../history/20260908/snapshot/docs/features/27-finance.md). Approved: người dùng nhập danh mục và giá tiền. Typed data/API conventions là Resolved delegated; advanced financial policy không được tự suy từ phần này.

## Scope and behavior

Current basic flow: create category → enter amount/currency/date → Save → list/filter/group same-currency records → edit with concurrency. Không yêu cầu tạo ledger account, chọn Income/Expense, transfer, interest hoặc budget trước khi lưu một khoản giá tiền.

| Field | Required / validation | Behavior |
| --- | --- | --- |
| Category.Title | Required, trim1–100, unique normalized owner/title | User creates own names; no forced income/expense classification |
| Record.CategoryId | Required same-owner category | Inline category create then choose; no cross-owner reference |
| Amount | Required decimal(28,8), finite nonnegative | Transport decimal string; no float or currency conversion |
| CurrencyCode | Required explicit selection | No automatic VND from Vietnamese UI; each summary currency separate |
| OccurredOn | Required; form default current user-local date, editable | Date-only, unchanged on timezone switch |
| Note | Optional≤2000 | Owner-private; no secret dereference |

## Lists, edits and errors

Default records table sorted OccurredOn desc then ID, page25/max100; columns date/category/amount+currency/note indicator. Filter category/currency/date range; search category title/note within owner records. No-result distinct no-data; decimal zero distinct unavailable. Save explicit with If-Match for edits and idempotency; stale update412 offers compare/reload. Summary sums only same currency/category within filter, never labels result balance/net worth/profit. Unknown fields rejected.

Category delete permitted only when no ManualRecord references it; blocked otherwise. Record deletion/void/ledger correction remains P-H05, so no generic Trash/purge command or generic CRUD wrapper can invent it. Existing records are editable through history/audit scope, not immutable posted ledger semantics. History keeps safe owned old/new field values; global audit redacts amounts/private note.

## Authorization and cross-module use

Owner isolation + system/user module gates + exact manual action keys. Sharing/support field projections remain P-H03; no public financial values by default. No bank sync, payment execution or live FX. Common Dashboard/Search may consume only a defined safe owner projection; paused Automation cannot trigger records. Finance basic is not in M01; gets its own API/story package before code.

## Source and references

[PO Q05](../requirements/10-owner-decisions-20260907.md), [manual schema](../design-database/07-finance-vault.md#finance-manualrecord), [actions](../action-catalog/modules/27-finance.md), [UX](../ux-ui/modules/27-finance.md). Actual Budget reference in [history](../history/20260908/snapshot/docs/features/27-finance.md) illustrates ledger workflows; those workflows are not the current simple price record requirement. Current basic follows PO's manual-entry decision.

## Acceptance

- FX27-MAN-AC01: Create category+record needs no Account/TransactionLeg; duplicate retry one record.
- FX27-MAN-AC02: UserA cannot bind UserB category; 0 is valid; negative/overflow/float ambiguity rejected.
- FX27-MAN-AC03: Two currencies produce separate totals; vi/en change does not reinterpret price/currency/date.
- FX27-MAN-AC04: Stale edit does not overwrite; used category delete rejected; record delete is unavailable pending policy.
- FX27-MAN-AC05: No amount/note/secret in generic admin/search/share preview outside approved projection.

## Conditional advanced scope

Earlier FX-27-BR/AC ledger IDs retained in history as **Proposed/Blocked**, not redefined as manual-record acceptance. Accounts, transfers, bills, subscriptions, budgets, debt, FX, ledger correction/deletion/import require P-H05 and concrete story contracts. They are not canceled or silently removed from Release1. [Current scope](../delivery/01-current-scope.md).
