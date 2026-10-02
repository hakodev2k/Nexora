# FX40 personal Skill evidence

Authority, source IDs, API/DB/security/UX scope, migration and rollback: `learning-skill-contract.md`. Requirements remain main `8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3`; this batch follows PR4 Time/Wishlist commits `5764509c0e3c7d60b4fdaa0a98409d3f31d91b94` and `b1e69224a067c2253b5a57575e8502572c5a7a54`. Tested source hashes: `skill-source-hashes.json`.

Nine exact keys `learning.skill.read/create/update/proficiency/archive/unarchive/trash/restore/purge` map to FX40-S02, `/api/v1/learning/skills`, `ISkillService` / `SqlSkillService`, typed `learning.Skill`, same-owner Resource registration and actual Trash cohorts. Only these actions become locally grantable. The installed package is Skills only, with prior system/default policy and user grants preserved. Whole FX40 remains Partial; evidence/merge/Course/Certification/Plan/WorkLog and sensitive sharing/support are not accepted by this batch.

Executed results:

| Check | Actual result |
| --- | --- |
| Standalone API Release build | 0 warnings, 0 errors |
| FunctionalTestOperator Release build | 0 warnings, 0 errors |
| Full real SQL/API integration regression | 31 passed, 0 failed, 0 skipped; `out/evidence/full-sql-skills.trx` |
| Final strengthened Skill cases | 5 passed, 0 failed, 0 skipped; `out/evidence/skills-reviewed-final.trx` |
| Migration fixture | Fresh DB application, replay, previous-schema upgrade, readiness, bootstrap and checksum guard |
| Browser DB upgrade | Existing disposable DB upgraded normally through 37 migrations, without reseeding or deleting prior data |
| Backend units | 12 passed, 0 failed, 0 skipped; `out/evidence/unit-skills.trx` |
| Native boundary checks | 30 passed, 0 failed, 4 pre-existing platform skips |
| Final frontend units | 69 passed, 0 failed, 1 pre-existing DST-policy skip |
| Final frontend TypeScript/Vite build | Passed; bundle over 500 kB warning remains |
| Real browser Skill workflow | 5 passed, 0 failed, 0 skipped; all five required viewports; sanitized report `skill-browser-evidence.json` |

SQL cases verify NFKC/invariant normalization, BIN2 retained-name uniqueness including Archived/Trash, expanded-name validation, literal queries and tied 25-row keyset pagination, owner/foreign cursors, correct category/date persistence, explicit proficiency/action separation, opaque acknowledgements and current grants on replay, Admin Allow/Deny/Unset, read prerequisites, disabled module denial, invalid protected fields, stale/missing revisions, archive/Trash restoration, NULL/state and same-owner FK constraints, unavailable merge field, corrupt frozen cohorts, retained-reference purge guard, duplicate/concurrent idempotency and injected audit rollback. Duplicate name attempts leave no new receipt, owner registry or actor create-audit write.

The full 31-case run preceded one assertion strengthening: duplicate-audit comparison now counts actor+action across all target IDs. The final five Skill cases reran that strengthened test against unchanged product source. No earlier failed result is counted as success. An initial disabled-module test used the invalid disabled+registration-on combination; it was corrected to use the existing valid policy and preserve product validation.

Browser normal form login and actual API/SQL assertions cover empty explicit assessment selection, create, metadata preserving level, separate proficiency write, dirty Cancel/Back, current-version conflict compare/reapply, 26 real API prerequisites and visible pagination, archive/cohort restore/unarchive/purge, revoked-grant data clearing and horizontal overflow at 1920/1366/768/390/320 widths. No authentication state injection, page-limit changes, record deletion to avoid pagination, permission/rate-limit weakening or fake success.

Independent reviewer `/root/independent_review` reviewed the bounded contract, authorization, schema/cohorts, name normalization, NULL merge guard, Active-only assessment, UI and final test sources. All identified corrections are implemented. Reviewer ran no tests; execution evidence above belongs to this implementation run. No whole-module, provider or production acceptance is claimed. Raw credentials, sessions and error contexts remain private ignored sandbox artifacts.
