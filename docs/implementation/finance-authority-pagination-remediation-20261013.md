# Finance manual authority, replay and pagination remediation

Current local FX27 manual slice on PR #4, baseline c072a97. This run's explicit user instruction authorizes implementation and real local/test/UI smoke verification, superseding the older code-only test restriction. No advanced ledger, record deletion, sharing/support projection, provider or production activation.

Source contracts: `docs/features/27-finance.md`, `docs/ux-ui/modules/27-finance.md` FX27-S13/S14/S15, `docs/action-catalog/modules/27-finance.md` eight active manual actions, FX27-MAN-AC01..05 and NXG-FX27-G01..03.

| Surface | Contract |
| --- | --- |
| Lists | Default25/max100 keyset paging. Category normalizedtitle/ID ascending; records occurrenceDate/ID descending. Optionalcursor authenticated to owner/kind/canonicalfilters/limit with existing AES-GCM envelope pattern. Literalboundedsearch (category100,records200); invalid/mismatched/tamperedcursor422. |
| Exact detail | GET categories/{id},records/{id}, current own read authority and ETag; foreignresource404. |
| Counts | Nullable usageCount suppressed without current record+summary read. Category read/picker is still allowed; internal category removal always checks references. |
| Queries | Current authority, data/counts and full-filter independent-currency totals share Serializable transaction; no dirty reads. |
| Mutations | Explicit trustedaction inside Serializable, including nullkey, rechecked beforecommit; same-owner category guard; no permission derived from canonicaltext. |
| Replay | Durable status/code plus ID/ETag and keyed full authorized projection digest only; no plaintext amount/note/title. Same currentread/owner/ETag/digest required for unchanged detail. Derived title/count change, newer/deleted/unreadable resource yields existing committedACK without second effect. Initiallyopaque results remainopaque. Remove replay204. Legacy payloadlessreceipt safe409 for unchangedTTL. |
| Canonical input | StructuredJSON retains displaycase/field boundaries; financial values only feed keyed digest. |
| Money | Decimal28,8 input remains string. SQL38,8 SUM read as SqlDecimal exactstring to avoid CLRdecimaloverflow/rounding. |
| Projection | Resource read gate or common {id,committed:true,detailAvailable:false}; no detail query aftercommit. |

No migration/schema changes. Code rollback does not rewrite retained data/receipts. Client nullablecount/ACK handling must accompany API changes.

`SqlFinanceAuthorityTests` has eight realSQL cases for paging/bindings/exactID owner guards/literalsearch, durable replay incl derivedprojectionchanges, Adminprojection/grants, finalauthorization rollback with/withoutkey, decimal/locale/ETag/legacyreceipt behavior. Fixture-only trigger removed in finally; not a productionmigration. Root records actual final build/runtime/CI evidence and independent review; this file claims no runtimepass or fullFX27 completion. UI owner integrates UI smoke. Safe owner old/new edit history remains a separate missing source requirement: current Finance schema has no versionstore, and redacted globalaudit does not supply it. AdvancedFinance/deletion stay gated.


## 2026-10-16 continuation

Ported the independently reviewed Finance repair to PR #4 baseline `1cd27a46a2ba1f24ab2b6f32f84771230f4b1761`. Finance production sources and contracts were unchanged from c072a97 to that remote HEAD. Current manual feature/UX/action/goal contracts were re-read; no scope or requirement was expanded. User again explicitly authorized testing and UI smoke verification. Newer Documents/history/tag/migration work was preserved. Final build/real-SQL/UI results and a fresh independent review are recorded by the root owner; prior source review is not runtime verification.
