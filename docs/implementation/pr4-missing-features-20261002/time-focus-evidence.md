# Time/Focus completion operations — evidence

This is a validated incremental batch, not completion of the attached 18-module task. Thirteen target feature handler groups remain absent. No module availability flag is changed by this batch.

## Trace

| FX / screen | Action | Endpoint | Service / persistence | Current permission | Real verification |
| --- | --- | --- | --- | --- | --- |
| FX19-S03 history | focus.session.record_time | POST /api/v1/focus/sessions/{id}/record-time | SqlFocusService.RecordTime; time.Entry + time.FocusConversion; migration 0034 | FX19 action and independently FX18 time.entry.create, before receipt replay and inside transaction | SQL real-clock completion, foreign owner, early phase rejection, failed-audit rollback, concurrent different keys, actual 60-second Entry; normal browser confirmation |
| FX18-S02 Trash | time.entry.purge | POST /api/v1/time/entries/{id}/preview-purge and /purge | SqlTimeTrackingService; removes own Entry/Corrections; keeps opaque audit/receipt IDs; conversion FK pin | Current FX18 purge; owner, Trash, current ETag and explicit confirmation | Wrong owner, 428/412/422, active-state rejection, audit rollback, real deletion, receipt replay, reference pin, normal browser cancel/confirm |

Requirements were read independently from main at 8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3. Feature BR/AC and engineering routes are recorded in task-brief.md. Starting PR4 implementation: b049b60702f1d8f7a501bdfc555a8dadf95a7810.

## Executed checks

- Locked restore from an offline feed of official NuGet packages; lock files unchanged.
- Release API/integration build: zero warnings and zero errors. Commands use `dotnet build ... -c Release --no-restore -m:1 -nr:false -p:UseSharedCompilation=false` with the isolated .NET 10.0.103 SDK.
- Focused `SqlTimeFocusCompletionTests`: 3 passed, zero failed/skipped, SQL Server 2025 Developer on loopback with a generated Nexora_Test_UUID database. Existing SqlApiFixture runs migration staging, replay, journal checksum guard, readiness and normal bootstrap before actual API/SQL assertions. A subsequent complete SQL/API run also passed all existing 15 tests and these 3 tests.
- Backend xUnit: 12 passed. Existing native console contract runner: 32 passed, zero failed, 2 existing Windows-specific checks unavailable on Linux. The runner was executed through its native apphost so the actual child-process lock test ran.
- Frontend build/typecheck passed. Frontend unit: 69 passed, 1 pre-existing unrelated skip. Existing bundle-size warning remains.
- Browser form login, existing functional fixtures and private test operator, actual SQL assertions: Time/Focus/access cases passed at 1920x1080, 1366x768, 768x1024, 390x844 and 320x568. The first desktop-only run passed 3/3. The later matrix passed 14/15; desktop Time was interrupted by a Vite reload while the separate Sharing batch was edited. Its desktop case had passed before that edit. This is aggregate viewport evidence, not a claim that that matrix run was clean; full regression is rerun after code stabilizes.

Runtime and evidence are isolated under `/workspace/scratch/95ab7dbc246d`: `runtime/`, `evidence/`, `nexora-sandbox/`. Generated passwords/keys and browser accounts are private and excluded from Git. Data uses GUIDs, unique names and example.invalid accounts from SqlApiFixture and functional fixtures. No production provider was contacted. Notification availability is reported honestly by the existing local implementation.

Self-review checked owner composite FKs, the durable once-per-session pin, target action checks before replay, transactional receipt/audit rollback, ID-only responses, CSRF, revision/confirmation guards and preservation of retired actions. Independent security/schema/cross-module review remains pending. This evidence does not close that gate or claim complete Time/Focus source-link or Support functionality.
