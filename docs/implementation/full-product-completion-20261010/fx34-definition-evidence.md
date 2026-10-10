# FX34 definition foundation — implementation evidence

Source base `89123040efe5e3168c56db5ac2864193705c7188`, main `8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3`; accountable owner root. Authority and exact wire/acceptance boundaries: [fx34-definition-contract.md](fx34-definition-contract.md). Current execution code-only; no new tests, mock/demo records or artificial fixtures.

Source: `src/Nexora.Application/Automation/AutomationContractRegistry.cs` and `AutomationDefinitionContract.cs`. Implemented closed versioned registry, bounded strict JSON shape parsing, immutable typed sequence, literal Task inputs, fixed redacted error codes and default-unavailable execution/simulation contributions. No endpoint/DI worker/SQL schema/UI or action dispatch added; no module/user enablement, migration, credentials or provider call. Action coverage: **PARTIALLY_IMPLEMENTED** `automation.definition.validate` only; no full catalog-action acceptance. Functional/security runtime evidence **NotRun**; real provider **PROVIDER_INACTIVE**, production not approved.

Rules read before source: startup/Technical Lead/profile, architecture/security routes, backend/database/frontend/owner-isolation routes and verification guidance. Sources: FX34 feature/action/UX/P06/DB09/goals; existing `TaskCommand` and `SqlProductivityService.ValidateTask` for Task projection parity. Pure shape parsing uses no SQL or trusted actor, and cannot replace the required runtime authority checks.

Executed implementation checks:

| Command | Result | Limit |
| --- | --- | --- |
| `dotnet build src/Nexora.Application/Nexora.Application.csproj --configuration Release --no-restore` | PASS0warnings/0errors | Initial foundation compilation |
| First API Release build | FAIL:20warnings/4errors, MSB3026/3027/3021 | Existing owned root API locked DLLs; not hidden as pass |
| API Release build after verifying/stopping only owned root API PID | PASS0warnings/0errors | Final corrected source compiled; retained SQL/files untouched; runtime not restarted |
| `git diff --check` | PASS | Whitespace only |

Independent source reviewer `/root/baseline_review` identified malformed escaped Unicode decoding escaping the fixed-error path and raw-title length differing from trimmed-title contract. Fixed by bounded JsonException/InvalidOperationException rejection, raw UTF-16 surrogate check and trim-based title limit. Reviewer re-read actual final source: both resolved, no blocking finding for foundation-only scope. Reviewer ran no functional tests/runtime/build; source review does not prove runtime acceptance.

Remaining: current SQL authority/resources/contributions validation, definition/version persistence, API/UX, actual Task dispatch adapter, schedule/timezone/cron, version-bound runs, dedupe/leasing/recovery/cancel/retries, simulation implementation, safe history/support, import/export and human QA acceptance. `SupportsVerifiedDryRun` returns unavailable for current closed registry; never invokes any action. No evidence from the older baseline test suite is reused as FX34 acceptance.

Rollback: revert only these new Application contract files and corresponding docs; no database or user state rollback. Release1 remains incomplete.
