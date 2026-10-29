# FX37 personal Asset evidence

Main authority `8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3`; implementation base PR4 `9097ff73f29b0d3c4030f71fea1b69c6e56859d9`. Contract: personal-assets-contract.md. Bind P07-AST-001/002/004/005, FX-37-BR-001/002/003/005, FX37-S01/S02/S03/S06, NXG-P07-01, NXG-FX37-G01/G02/G03 and system owner/authorization/concurrency/lifecycle/atomicity goals. These are partial field/behavior contributions, not full story/AC acceptance.

Exactly ten SELF keys assets.asset.read/create/update/transition/history/archive/unarchive/trash/restore/purge use IPersonalAssetService, SqlPersonalAssetService, /api/v1/assets/personal, typed assets.PersonalAsset and immutable V-profile AssetVersion, migration0038 and the Asset screen. Snapshot fields are explicitly allowlisted and private. No serial or loan implementation is represented by this subset. SQL is authoritative; no external effect.

| Executed verification | Actual result |
| --- | --- |
| Final standalone API Release build | Passed, 0 warnings, 0 errors |
| FunctionalTestOperator Release build | Passed, 0 warnings, 0 errors |
| Full real SQL/API regression | 41 passed, 0 failed, 0 skipped; out/evidence/full-sql-personal-assets.trx |
| Final strengthened Asset cases | 5 passed, 0 failed, 0 skipped; out/evidence/personal-assets-strengthened-final.trx |
| Backend units | 12 passed, 0 failed, 0 skipped; out/evidence/unit-personal-assets.trx |
| Native boundary runner | 30 passed, 0 failed, 4 pre-existing platform/identity skips |
| Frontend units | 71 passed, 0 failed, 1 pre-existing DST-policy skip |
| Final TypeScript/Vite production build | Passed; existing >500 kB bundle warning, JS594.65 kB |
| Final real browser Asset workflow | 5 passed, 0 failed, 0 skipped, 0 flaky; normal form login at 1920/1366/768/390/320 widths |
| Existing browser sandbox migration | 39 approved migrations applied normally; prior synthetic records retained |
| Independent source review | Backend/migration/authorization/UI and strengthened evidence assertions reviewed; all actionable findings closed |

The full 41-case run included fresh SQL application, replay, preceding-schema upgrade, readiness/boot/checksum checks. It preceded test-only strengthening of audit/receipt payload privacy and independent Title/SQL-GUID ordering assertions; all five final strengthened cases ran against unchanged built product binaries. No duplicated product build was inferred from BuildProjectReferences=false.

SQL proves explicit non-loan states, forbidden protected/foreign fields, owner read/history/write isolation, independent transition/history permissions, update-read prerequisite, current replay revocation, missing/stale versions, Name paging with ties, immutable history/version/date/action filters, database NULL-safe constraints, exact frozen cohort, linked-source retention, pins, simultaneous idempotency and audit-failure rollback. Snapshot Notes/reasons stay out of actual RedactedDiffJson and receipt ResultJson. V history remains through Sold/Archive/Trash/restore; authorized purge removes own history/payload only.

Browser proves explicit initial choices, dirty Cancel/Back, concurrent metadata conflict and deliberate reapply, normal accumulated-record pagination, literal model/type filtering, state change/private reason, version/action/UTC history filters, actual history paging, Sold→Archive→Trash→restore→unarchive, purge and revoked history access clearing data while Reload remains usable. Browser tab-close, all action grant combinations, state-conflict UI and full remaining FX37 behavior are not claimed passed by this workflow.

Failures retained in local artifacts: initial SQL test expected 500 instead of existing safe SQL-error503; its pin fixture used nonexistent columns. Strengthened receipt oracle initially assumed UserId rather than the actual hashed subject schema. These fixture defects were corrected without relaxing product guards. First browser run found an implicit select label; the second needed visible pagination to a new Asset among retained records. Product label and fixture pagination were corrected; no records were deleted or page limits changed. Independent review also found the wrong transition capability, copied HTTP-status cast and busy/close recovery defects; all were fixed before the final run.

Automatic approval review initially rejected SQL execution because later AGENTS instructions retained the code-only amendment. The user then explicitly authorized: “Yes—override the code-only amendment for isolated local testing.” The successful checks followed that override. No production/provider approval is implied.

Source SHA256: personal-assets-source-hashes.json. Sanitized browser evidence: personal-assets-browser-evidence.json. Raw private runtime/config/reports and credentials are excluded from commits. Serial crypto/recent-auth, loan semantics, purchase/warranty/repair/component/accessory/files/Finance/Vault/source refs and sensitive share/support projections remain open. Whole FX37 and the parent eighteen-module task remain Partial; full existing browser regression is still pending for the final task state.
