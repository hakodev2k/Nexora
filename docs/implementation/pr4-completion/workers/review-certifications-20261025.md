# Independent Certification review — 2026-10-25

Reviewer `/root/templates_completion`, separate from Certification author `learning_completion`. Root retains integration/release accountability. Source base75b6d9f plus coordinated working-tree delta. Reviewed current Certification contract, FX40 feature BR004/006 and AC003, application/API/service/codec, migration0095, native client/screen and unit/component/SQL tests. Existing baseline/routed architecture/backend/database/security/frontend/verification/owner isolation rules apply. No product source edited by this reviewer.

## C40-R01 — Medium DB state CHECK permits UNKNOWN

Migration0095 CK_Certification_State uses (Status='Archived' AND PreArchiveState='Active') without explicitly rejecting NULL. SQL CHECK accepts UNKNOWN, so an Archived row with NULL prior state passes the intended state invariant. Application writes currently set valid prior state; no network exploit or corrupted production data is claimed.

Requested fix sent immediately to root: add PreArchiveState IS NOT NULL to the Archived branch and an owner-scoped real SQL negative UPDATE test expecting constraint failure. This is analogous to the separately discovered/fixed Template T24-R01. At this initial review snapshot the finding is OPEN pending author/root correction and re-review; below hashes identify exactly that snapshot.

## Other bounded observations

- SQL SELF capability checks session/security stamp, owner PersonalSpace/User mapping, role, module readiness/user grant, current action status and Admin allow/deny; reads filter owner and registry contract/availability before paging or detail/history projection.
- Mutation owner lock + serializable transaction, rowversion, digest receipt, private version snapshot, registry revision and audit remain one transaction. Replay checks actor/owner/id/action/status and returns opaque ACK under current authority; no stale field replay.
- List/history cursor and preview scope are authenticated and encrypted with versioned AES-GCM envelope; cursor binds owner/filter/today, lifecycle proof binds actor/owner/id/revision/operation/expiry. No secret or credential ID field is introduced.
- Date-only effective expiry uses server clock + trusted current profile IANA timezone; same-day expiry remains active. FX40 BR006 explicitly makes Archive read-only; service/UI enforce update/renew denial and retain dates/history through unarchive.
- Client unknown-write closures preserve original command/key;401/403/404 clears private state; optional URL rendered as escaped inert text, no fetch/navigation. Reminder controls are absent and manual-renewal zero-intent limitation is explicit. FX40-AC003 (pending expiry reminder) is NOT proven or complete.
- No other blocking source finding in the bounded reviewed delta. This is not comprehensive FX40/security/migration/production approval.

## Independently executed check

`dotnet test tests/Nexora.UnitTests/Nexora.UnitTests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~CertificationContractTests --logger "console;verbosity=minimal"`: **11PASS/0fail/0skip**,39ms,exit0; Debug projects rebuilt successfully. Cases are a subset of root/author suites, not11additional unique cases. No SQL/API/browser/timing/provider execution by reviewer. Root reports296wholeSQL cases separately; this review does not invent a personal execution of those cases.

## Initial reviewed source fingerprints

| File | SHA256 |
|---|---|
| `src/Nexora.Application/Learning/CertificationContracts.cs` | `698C550285D947CAC5C38D11537C2E6AD181E7EB7FF28AD0691BEC887CBE8DEF` |
| `src/Nexora.Infrastructure/Learning/SqlCertificationService.cs` | `72FB4B0FA2C08EF9ED5C171DB4EABF82001D68914AD42FC67A0E7AF7C773E732` |
| `src/Nexora.Infrastructure/Learning/CertificationTokenCodec.cs` | `C2EDE257A1CAB2631BB7584A5419C10FE578B5EA54AF5E12B9C133684FDF8811` |
| `src/Nexora.Api/Features/Learning/CertificationEndpoints.cs` | `35117A5C1CEABF7B2160C612264BCFEC2C680EB6B186AD31F334E450AC966092` |
| `database/migrations/20261025_0095_certifications_manual.sql` | `83BBD549EEA6177C2DF1F9E4A0C850005D9F36EFCE5359D0AA346E68DB81842F` |
| `web/Nexora.Web/src/certificationApi.ts` | `3D40708B210ED86F45BBB7663547FED28F1A99907ED0AF91FBEE0A9D01605E59` |
| `web/Nexora.Web/src/CertificationScreen.tsx` | `26CF38A5CAB0857422410ED3FA04BA94B39F1A3EDBE353D22586F2233CDB7E45` |
| `tests/Nexora.UnitTests/CertificationContractTests.cs` | `1F28397FEF1A7ACF9F95D0B64BCFB91DEB342DD3CFC14B4CD6B0092DFA1965AA` |
| `tests/Nexora.IntegrationTests/SqlCertificationTests.cs` | `C7E6D51990C6FBB84E788A4A33B529939CC4C975CA91C2F7CD4C42F50E5EF4BE` |
| `web/Nexora.Web/src/certificationApi.test.ts` | `E37CCF8F277D8964833CDFC7F6FFD136A92192558C0762E64828E6A497F3196F` |
| `web/Nexora.Web/src/CertificationScreen.test.tsx` | `F5B801F64FCE411D8F412834C6C31594F33268D52C5F8858F4DFE723C53C307C` |

Future source changes require re-review/hash refresh. C40-R01 must be closed with actual correction and SQL evidence before claiming the DB state invariant passed.

## C40-R01 correction re-review

The author/root corrected the Archived branch to `PreArchiveState IS NOT NULL AND PreArchiveState='Active'`. The new `SQL_rejects_archived_certification_with_missing_prior_state` case creates its own synthetic owner/record, attempts a parameterized owner+ID-scoped invalid update, requires SQL547 and independently checks the retained record is Active. This targets the actual SQL UNKNOWN failure, without removing assertions or weakening lifecycle semantics. No other application source changed for this correction.

Source disposition: **FIXED / re-reviewed**, no remaining blocking source finding in this bounded Certification delta. Real SQL execution of the new case remains coordinator evidence; this reviewer did not execute it and does not claim that it passed.

| Corrected file | SHA256 |
|---|---|
| `database/migrations/20261025_0095_certifications_manual.sql` | `83BBD549EEA6177C2DF1F9E4A0C850005D9F36EFCE5359D0AA346E68DB81842F` |
| `tests/Nexora.IntegrationTests/SqlCertificationTests.cs` | `C7E6D51990C6FBB84E788A4A33B529939CC4C975CA91C2F7CD4C42F50E5EF4BE` |

All other reviewed source hashes above remain applicable. Whole FX40/R1 acceptance, Certification reminder/evidence/credential/provider/Trash capabilities and final-source runtime sign-off remain separate gates.
