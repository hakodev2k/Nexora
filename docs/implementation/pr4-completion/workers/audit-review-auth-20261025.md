# Independent bounded authentication review — 2026-10-25

Reviewer `/root/frontend_audit` did not author these backend changes. Author:
`/root/backend_audit`; coordinator `/root`. Starting HEAD
`7a75bb6f9b84053bb646649ef9df7fec2836d23b`, reviewed working source identified by
the hashes below. This is a separate-author source review plus a focused rerun,
not whole-FX01/security/release acceptance.

Read applicable `backend` and `security` route files from `.ai/routing.json`
(all14 backend and8 security paths), in addition to mandatory startup/baseline
and verification routes already recorded in this worker's frontend report.
Read current M01 API defaults, FX01 feature/goals, Identity endpoint mapping,
ApiResult conversion and EmailNormalizer to inspect the actual boundary.

Contract binding: M01-S02/S03/S04, `M01-AC03`,
`docs/delivery/milestone-01/02-api-contracts.md` transport/throttling paragraphs;
FX01 `identity.account.login` and reauthentication/registration/reset flows;
`FX-01-BR-005`, NXG-FX01-G02, NXG-SYS-03/08/14. Session/account SQL authority and
provider gates are unchanged. Current user task authorizes tests/review; no
production/provider permission is inferred.

## Findings and disposition

**No new blocking source finding in this bounded final delta.**

| Area | Source observation | Verification limit |
|---|---|---|
| CSRF/origin | Content/origin/CSRF rejection occurs before Subject resolves the reauthentication session through SQL. Existing route-origin policy is not weakened. | Focused missing-CSRF test proves ordering for that path, not every origin/configuration combination. |
| Equivalent routes | Trailing slash removed; auth suffix and idempotency path matching are ordinal case insensitive. Tests exercise ASP.NET routing aliases with shared login failure quota. | Canonical public routes reviewed; no broad route-fuzz claim. |
| Failure quota |10 credential failures per subject+source,100 admitted attempts/source; 5 submissions per subject and20/source. Outcome completion counts401 credential failures; successful checks release pending subject capacity. | API ToHttp returns status-bearing IResult implementations; actual SQL/session state graph not exercised here. |
| Concurrency | Dictionary and count/pending transitions share one lock. In-flight checks reserve capacity. Complete/Dispose use the same guarded completed flag and release at most once. | Focused parallel reservation test passed; no load/SLA proof. |
| Capacity/expiry |4096 HMAC-keyed counters maximum; live entries not evicted to bypass limits; saturation429. Expired entries without pending work removed. | Pending long-running work retains fail-closed capacity until completion. No distributed/restart guarantee. |
| Privacy | Random per-instance HMAC secret; no raw subject/IP/password/session retained in counter keys or emitted in rate-limit errors. | No timing/penetration/log-canary proof inferred. |
| Login hash work | Exactly one VerifyDetailed on either current user hash or dummy hash. Known invalid proof no longer adds a second dummy verification. | Legacy hash costs, SQL lookup and total response timing can still differ; no constant-time end-to-end claim. |
| Null-user correction | Explicit `user is null || passwordVerification == Failed` rejects unknown identity even if submitted text matches dummy proof. It cannot reach `ToSnapshot` or issue a session. | Confirmed by final source inspection; no SQL fake-success injection was executed. |
| Dependency registration | Dedicated throttle is singleton, TimeProvider.System registered before app build; no mutable scoped dependency captured. | Multi-process/restart/proxy topology remains a future runtime/release boundary. |

Coordinator reported an earlier null-user dummy-success edge and author correction;
this reviewer verified the corrected final condition. This review does not claim
to have discovered that earlier issue independently. Existing sharing-only
RequestRateLimiter and unrelated SQL session/concurrency concerns are outside this
bounded delta and are not certified here.

## Independently executed check

Command, repository root:

```powershell
dotnet test tests/Nexora.UnitTests/Nexora.UnitTests.csproj --no-restore --filter "FullyQualifiedName~AuthenticationRateLimitTests|FullyQualifiedName~AuthenticationThrottlePolicyTests" --logger "console;verbosity=minimal"
```

Actual exit0, **14 passed /0failed /0skipped**, test duration406ms, .NET10 Windows.
This includes loopback HTTP security-filter tests and in-memory throttle policy
tests. It is a subset of the author's145-case suite, **not14 additional unique
tests**. No SQL, Redis, real Identity login runtime, E2E, multi-instance or provider
execution performed by this reviewer.

## Reviewed source hashes (SHA256)

| Path | Hash |
|---|---|
| `src/Nexora.Api/Security/EndpointSecurityFilters.cs` | `62B717A77A38965C4E3C3D00B184758E82E2F400B78C26C088CB9B4B1D9CE0E7` |
| `src/Nexora.Api/Security/AuthenticationRequestThrottle.cs` | `1F76FBDD9D0719764E0081EB086AF527DECF1B483B62CF1BB615DE0273C4B177` |
| `src/Nexora.Api/Program.cs` | `AFDFB7A8DBE087D572EED625EB6F50F60DC8C8661A6961A3AB74590838FBE6AA` |
| `src/Nexora.Infrastructure/Identity/SqlIdentityService.cs` | `A1FDFABABC7E1E7413CD2FDF261C187B9A5A7DF24E9F2DD201EFE5B0AFAA0B4E` |
| `tests/Nexora.UnitTests/AuthenticationRateLimitTests.cs` | `54F88E2377CC199E5F09C594C33D9C402A4DD2B1021360B00C84D31CEDCE2CCB` |
| `tests/Nexora.UnitTests/AuthenticationThrottlePolicyTests.cs` | `2C8E1A515150AC1A218D2D2447B54B12A470D751B6CC129428B2C52E0D5DBA19` |

No product code changed by reviewer in this assignment. Future relevant edits
invalidate this hash-bound review. Release remains subject to the full outstanding
acceptance/evidence gates; no general authentication security approval is granted.
