# PR4 backend authentication audit — 2026-10-25

Accountable worker: `/root/backend_audit`; coordinator and independent reviewer:
`/root`. This is a bounded authentication/security remediation, **not** an audit
completion claim for every backend, database or business module.

## Task brief and authority

| Field | Binding |
| --- | --- |
| Outcome | Inspect actual backend/security source, repair demonstrated current-scope defects and retain execution evidence |
| Source | `impl/m01-s00-scaffold`, starting HEAD `7a75bb6f9b84053bb646649ef9df7fec2836d23b`, plus the six changed backend/test files listed below; coordinator records final commit |
| Authorization | Current user's comprehensive PR4 audit explicitly authorizes implementation, additional tests, isolated runtime checks and parallel workers; this supersedes the older code-only amendment for this task |
| Product boundaries | DEC-20260909-014; DEC-20261019-PR4 D01–D09 preserved; no provider, production, real account or secret use |
| Requirements | `AUTH-002`, `AUTH-003`, `AUTH-005`; M01-S02/S03/S04, M01-AC03; current M01 API transport/authentication/rate-limit contract |
| Actions | `identity.account.login`, `identity.account.register`, `identity.account.resend`, `identity.account.reset_request`; reauthentication control endpoint and existing proof-endpoint guards |
| Goals | NXG-FX01-G02, NXG-SYS-03/08/14; no complete-goal acceptance inferred |
| Impact | One API singleton, HTTP filter/security state, one existing SQL Login password-proof branch; no schema/migration/data/grant changes |
| Rollback | Revert the bounded product/test diff on this branch; no persisted state migration or provider rollback required |
| Open gates | Separate-author review; real SQL/API and full browser/session/recovery acceptance; final-commit CI; production topology/security acceptance remains outside approval |

Startup and routing sources consulted: root AGENTS, nexora-engineering skill,
implementation profile, Technical Lead README/core rules, routing.json,
verification.md, docs/delivery/goals entry points and task/evidence template.
Relevant selected engineering paths are backend security/authorization/testing
rules, input-validation-secure-coding and testing-strategy skills,
security-code-review, and owner-boundary/completion-verification guidance.
Product sources read include `docs/features/01-identity-and-profile.md`,
`docs/goals/modules/01-identity-and-profile.md`,
`docs/action-catalog/modules/01-identity.md`, M01 stories/API/readiness,
DEC-014 and current D01–D09 decisions. These sources specify scope; their old
docs-only/not-implemented paragraphs are not evidence about current source.

## Findings and actual remediation

| ID / severity | Root cause and user impact | Action and verification |
| --- | --- | --- |
| AUD-BE-001 / High | ASP.NET accepts case variants/trailing slash, but auth suffix detection and case-sensitive raw-path counters treated equivalent paths differently. Exhausted canonical login quota could be bypassed through equivalent routes; trailing slash could also skip identity idempotency-key requirements. | **FIXED, TESTED**: canonical trailing-slash path, canonical policy families independent of casing; four real HTTP/filter regression cases and two trailing-slash idempotency cases. Before fix: 3 failures/1 pass; initial corrected regression: 4/4 pass. |
| AUD-BE-002 / High | Existing IP/path counters used 10 requests/min for login and did not implement contracted normalized-subject/IP failure limits, 15-minute windows, or bounded storage. Unknown subjects could create indefinitely retained entries. | **FIXED AT LOCAL FILTER LAYER, TESTED**: contracted 10 credential failures/subject+IP/15min and 100 credential requests/IP/15min; register/resend/reset 5 submissions/normalized-email/15min plus20/IP/15min. In-flight credential attempts reserve capacity; completion counts401 only; success/exception disposal releases reservations. Counter keys are process-keyed HMAC digests; store bounded4096, expired unused entries removed; saturation fails closed with429. Deterministic boundary, concurrency, isolation and saturation cases pass. |
| AUD-BE-003 / Medium | Known-account wrong password performed real password verification and then dummy verification; unknown account performed dummy verification once. This created a source-derived account-enumeration timing risk, contrary to the intended comparable missing-user cost. | **SOURCE FIXED, UNIT REGRESSION PASSED; SQL/TIMING NOT VERIFIED**: choose real/dummy hash then verify exactly once; explicitly deny missing user even if dummy literal matches. Root independent review caught the missing-user guard before handoff. No timing exploit or constant-time end-to-end guarantee claimed. |

The dedicated authentication limiter is process-local for this approved local
executable. It is not a distributed/production brute-force solution; restart or
multiple instances do not share these counters. No deployment/topology policy is
approved by this implementation. Existing ShareLink operational
`RequestRateLimiter` is preserved; its separate retention/storage behavior was
not remediated or accepted by this authentication slice.

Reauthentication's subject is resolved server-side from the validated current
session/profile, not client email. CSRF is validated before that lookup; a real
HTTP regression with no Identity/SQL registration proves invalid CSRF returns403
without attempting subject resolution. Exact canonical path remains in use for
idempotency guards. No request password/token is used as a counter key.

## Files reviewed and changed

Changed:

- `src/Nexora.Api/Security/AuthenticationRequestThrottle.cs` — new bounded,
  result-aware local subject/source limiter.
- `src/Nexora.Api/Security/EndpointSecurityFilters.cs` — canonical path, CSRF
  ordering, subject resolution, admission/result accounting; existing Sharing
  guard retained.
- `src/Nexora.Api/Program.cs` — singleton composition with existing TimeProvider.
- `src/Nexora.Infrastructure/Identity/SqlIdentityService.cs` — one hash check and
  explicit missing-user denial.
- `tests/Nexora.UnitTests/AuthenticationRateLimitTests.cs` — seven real
  loopback/Kestrel HTTP/filter cases, no database or external providers.
- `tests/Nexora.UnitTests/AuthenticationThrottlePolicyTests.cs` — seven policy
  cases, including theory expansions, with deterministic clock boundaries.

Read-only code review also covered CSRF/session/security headers/password hashing,
IdentityEndpoints/IdentityContracts/ApiResult, Login transaction branch,
SqlApiFixture/session authority fixture structure, and representative identity
and Goals browser specifications. This does not certify those complete flows.
SQL fixture source creates isolated synthetic sessions directly; the browser
specifications use real login. A sufficiently large shared browser batch can
correctly encounter the contracted IP quota; run isolation/window budgeting must
respect it rather than disable the limiter.

## QA execution evidence

Environment: Windows workspace, installed .NET10 SDK/runtime, `--no-restore` using
existing restored packages. Only the test-owned loopback filter harness was
started; no Nexora business API host, SQL database, browser or provider was started
by this worker.

| Command / phase | Actual result | Evidence / limit |
| --- | --- | --- |
| `dotnet test tests/Nexora.UnitTests/Nexora.UnitTests.csproj --no-restore --filter FullyQualifiedName~AuthenticationRateLimitTests` before canonical correction | FAIL:3 failed/1 passed/0 skipped | Demonstrated equivalent routes returned204 instead of expected429 |
| Same regression, `--no-build --no-restore --logger trx;LogFileName=rate-limit-before.trx` | FAIL:3 failed/1 passed | Local `evidence/oct25-backend/rate-limit-before.trx`; preserved baseline, not counted as an extra distinct scenario |
| Initial canonical correction targeted suite | PASS:4/4 | Local `rate-limit-after.trx`; historical intermediate source only |
| Initial canonical correction complete xUnit suite | PASS:137/137 | Local `backend-unit-after.trx`; superseded by final source |
| Expanded authentication policy/filter focus | PASS:13/13 | Local `authentication-final.trx`; before the additional CSRF-ordering regression |
| `dotnet test tests/Nexora.UnitTests/Nexora.UnitTests.csproj --no-restore --logger "trx;LogFileName=backend-unit-final.trx" --results-directory docs/implementation/pr4-completion/evidence/oct25-backend` | **PASS:145/145,0failed,0skipped** | Final authored backend source plus14 new distinct cases. Raw TRX is local/ignored; curated execution metadata retained alongside this report. Includes all14 new cases; do not add earlier run counts to145. |
| `git -c safe.directory=D:/Projects/ASP_NET_Core_Developer_2026/Nexora diff --check` | PASS after EOF correction | LF/CRLF warnings are not whitespace errors |
| SQL/API / DB / browser / E2E / load/timing | **NOT RUN by this worker** | Real SQL/runtime and broader journey gates remain; a unit-project filter harness is not business E2E |

One intermediate build failed with three compiler errors (nested constructor
access and removed Sharing limiter reference); corrected by internal nested
constructor/type visibility and retaining the existing Sharing helper. Subsequent
focused/full test commands rebuilt successfully. No failing assertions, tests,
rate-limit requirements or fixtures were removed/weakened.

Final verdict for this worker's slice: **implemented with bounded local test
evidence; release acceptance pending**. This work proves neither complete FX01 nor
whole-backend/security/Release1 readiness. Root owns final commit linkage,
independent-review disposition, CI and remaining release inventory.
