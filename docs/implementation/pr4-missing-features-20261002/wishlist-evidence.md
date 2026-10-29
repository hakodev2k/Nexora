# FX31 manual Wishlist implementation evidence

Requirements authority: main `8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3`. Starting PR4 source: `d46ca6e52143677cbd22f778f1c3503b27d0deba`. Source identity for this batch: `wishlist-source-hashes.json`. User authorized isolated real SQL/API/browser verification and independent review. Engineering and backend/database/security/frontend/verification routes are recorded in `continuation.md`.

FX31-S01, P05/RM13, NXG-FX31-G01/G03, NXG-SYS-02/03/04/05/08/09/14. Sources: main features/31-shopping-records.md, action-catalog/modules/31-shopping.md, ux-ui/modules/31-shopping-records.md, design-database/08-news-shopping-developer.md and kernel/Trash contracts. Exact scope, transaction, data, security and rollback contract: `continuation.md`.

Nine installed actions: `shopping.wishlist.read`, `.create`, `.update`, `.mark_purchased`, `.archive`, `.unarchive`, `.trash`, `.restore`, `.purge`. Each maps to FX31-S01, `/api/v1/shopping/wishlist`, `IWishlistService` / `SqlWishlistService`, typed `shopping.WishlistItem`, same-owner Resource registry and actual Trash cohorts in migration 0035. Action-specific SQL capability guards apply; update also requires read. Mutation receipts contain opaque IDs/revisions only. Other Shopping actions remain unavailable. Whole FX31 remains Partial.

Executed checks on the isolated checkout:

| Check | Actual result |
| --- | --- |
| API and FunctionalTestOperator Release builds | Passed, zero warnings/errors |
| Focused Time/Wishlist real SQL/API tests | 8 passed, 0 failed, 0 skipped; `out/evidence/wishlist-time-reviewed.trx` |
| All SQL/API integration tests | 26 passed, 0 failed, 0 skipped; `out/evidence/full-sql-api.trx` |
| Migration fixture | Fresh database, normal migration application, replay, preceding-schema upgrade, bootstrap and readiness exercised by the SQL fixture |
| .NET unit runner | 12 passed, 0 failed, 0 skipped; `out/evidence/unit-final.trx` |
| Native boundary runner | 30 passed, 0 failed, 4 pre-existing platform-specific skips |
| Final `npm run build` | TypeScript/Vite passed; Vite warned that the JS bundle exceeds 500 kB |
| Final `npm run test:unit` | 69 passed, 0 failed, 1 pre-existing DST-policy skip |
| Wishlist real browser workflow | 5/5 passed at 1920×1080, 1366×768, 768×1024, 390×844 and 320×568; normal form login, actual APIs/SQL, actual visible pagination |

Browser coverage: dirty cancel and Back preserve drafts; exact decimal strings persist; stale update compares current values and explicitly reapplies; 26 generated prerequisites force real pagination; purchase/archive/Trash/restore/unarchive preserve the archive cohort; purge removes payload; module grant revocation clears protected rows and draft; no horizontal overflow. The initial failures exposed implicit Notes/Status label ambiguity; final explicit label/ID associations passed all five viewports. No authentication state was injected. Tests restore prior module/grant policy through normal APIs.

SQL negatives include foreign owner/cursor, invalid transitions and fields, stale/missing revision, Deny/Unset/read prerequisites, disabled modules, cross-owner FKs, NULL-sensitive state constraints, retained references, corrupted cohort rejection, durable replay/conflicting body, and injected audit failure with atomic rollback. Synthetic fault setup does not substitute for successful API writes.

Independent reviewer `/root/independent_review` reviewed authorization, schema, cohorts, grant boundaries and UI conflict/dirty-leave behavior; raised findings were corrected and rereviewed. Reviewer performed source review only; runtime evidence above belongs to the implementation run.

Sandbox: `out/blocked-module-apis`, loopback SQL Express, disposable `Nexora_Test_<GUID>`, generated accounts using existing FunctionalTestOperator and SqlApiFixture conventions. Local API/frontend ports 15443/15173. Generated passwords, session material, connection strings and raw browser error contexts remain ignored private runtime data and are excluded from this commit. No provider purchase, Finance mutation or real outbound provider execution occurred.

This is focused batch evidence, not full-scope acceptance. The pre-existing full browser suite and the other missing modules remain pending. Ready denotes installed `wishlist-v1` only; installation preserves system/default policy and user grants. Rollback uses the preceding binary plus normal module disable, retaining additive schema/data without destructive SQL.
