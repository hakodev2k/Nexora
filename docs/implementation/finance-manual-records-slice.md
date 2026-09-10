# Finance manual-records slice

Status: `SLICE_IMPLEMENTED` on PR #4 under `DEC-20260909-014` (code-only; runtime/functional verification remains owner work).

## Boundary

This slice implements only the current FX27 manual scope: owner-created categories and records containing category, nonnegative decimal amount (up to `decimal(28,8)`), explicit three-letter currency, date-only occurrence and optional private note. It deliberately does not implement accounts, income/expense signs, transfers, ledger posting, budgets, bills, CSV import/export, deletion or provider/payment behavior.

## Traceability

| Contract | Implementation |
| --- | --- |
| FX27-MAN-AC01..05 | `IFinanceService`; `SqlFinanceService`; `/api/v1/finance/categories`; `/api/v1/finance/records` |
| `finance.manual_category.*` | Category SQL table, normalized owner-unique title, usage-protected remove |
| `finance.manual_record.*` | Owner/category composite FK, decimal/currency/date validation, create/update with `ETag`/`If-Match` |
| `finance.manual_summary.read` | Same-filter, per-currency `SUM`; no FX or balance label |
| Owner/isolation | Every query resolves `OwnerId` from trusted `IdentityPrincipal`; request body cannot select owner |
| Concurrency/idempotency | SQL rowversion compare-and-swap and durable `RequestReceipt` per mutation |
| Audit | Category/record mutations write redacted `security.AuditEvent` rows; amount/note are never copied into the audit payload |

Migration `20260910_0008_finance_manual_records.sql` adds the two tables, owner/category integrity constraints, indexes and canonical permission metadata. Admin SELF remains default-deny and requires an explicit `Allow`; support/share projections are not exposed.

## Verification boundary

Static verifier and frontend build may be run for implementation error detection. SQL migration execution, .NET compilation, API/functional tests, browser E2E, manual QA, fixtures and synthetic test data are intentionally not run in this code-only turn.
