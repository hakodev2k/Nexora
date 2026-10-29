# E06b existing trusted writer reconciliation — 2026-10-27

Scope owner shopping_completion; coordinator root. HEAD `a0215f7b3e85d33503cff9077b41cc065240cb81` plus worktree. Read current FX06 feature/BR001–006/AC001–003, action catalog `notifications.dispatch.publish/deliver` SYSTEM-only boundaries, NXG-FX06-G01/G02/G03, current DEC014/D04. No production change or new transport is authorized by this source reconciliation.

Report21 originally used “delivery worker effects.” Source inspection corrects the bounded workflow: **existing transactional local inbox projection writers**, not a general three-channel delivery worker. Generic channel-delivery worker is unstarted and excluded from this existing-feature task. This leaves its R1 requirement open; it is not deferred/cancelled by PO or claimed complete. Real SMTP/push providers remain D04-gated. Dedicated preactivation `AccountMessageDeliveryWorker` belongs to FX01 and is not interchangeable with general FX06 delivery.

| Existing producer | Source / effect boundary | Relevant requirement / evidence |
|---|---|---|
|Focus phase completion|`SqlFocusService.FinishDueAsync` + API `FocusPhaseWorker`; serializable owner lock, current running/due state, current owner/action before effect and before commit; calls `LocalModuleNotificationWriter.FocusCompleted` in same transaction|FX06 BR001/002, NXG-FX06-G01/G02; actual FN-042 evidence below.|
|Task/Calendar reminder|`SqlReminderService.DispatchDueAsync` + API `ReminderDispatchWorker`; durable reminder lease/state/retry and current source proof; `EnsureNotificationProjection` same transaction|Existing FX14 integration/evidence must be traced separately; source presence alone is not PASS.|
|Automation failure projection|`SqlAutomationManualRuns` → `LocalModuleNotificationWriter.AutomationFailed`; transactional logical dedupe and completed local projection outbox|Existing local simulation only; live providers remain disabled. Not executed by this reconciliation.|

`LocalModuleNotificationWriter` uses `NOT EXISTS ... WITH(UPDLOCK,HOLDLOCK)` and stable logical keys; schema migration0001 enforces `UX_Notification_LogicalKey`. Each existing writer records three channel outcomes: InApp Delivered, Email NotApplicable/ProviderUnavailable and BrowserPush PermissionUnavailable/ProviderUnavailable. This truthfully records absence of delivery; it does **not** satisfy “Email actually sent” or live browser-push acceptance. No fake success, arbitrary SYSTEM publish endpoint or UI grant is introduced.

## Existing executed evidence inspected (not rerun by this worker)

Root-owned `web/Nexora.Web/artifacts/active-increment-desktop-broad-initial-junit.xml`, suite `focus.spec.ts`, timestamp `2026-10-26T15:33:04.395Z`: FN-042 **1 PASS,0 failures,0 skipped**,66.753s. Parent independently reported the broad run execution. Test definition alone is not the pass evidence; this actual preserved JUnit is.

The case uses legitimate normal login and module preview/commit, real UI1-minute Focus, pause/resume/reload/cancel, then real60s phase completion while the browser is offline. Read-only `sqlFocusCompletion` (FunctionalTestOperator `read-focus-completion`) queries the actual stable `focus.completed:<sessionId>` key, asserts exactly one logical notification, exact ownerUserId and exact three channel states. It returns to UI, checks no active phase and converts the completed phase into an actual persisted TimeEntry. This proves a genuine existing trusted InApp effect; no rewritten SQL timer is used by the browser test.

Limit: initial proof queries once immediately after completion. Repeated later worker passes/concurrent finishers, revoked current authority and atomic rollback need explicit bounded regression evidence if not already present elsewhere. A final re-query after the subsequent conversion could strengthen repeated-cycle dedupe without duplicating a second timer journey. No additional assertion is reported executed at this checkpoint. Source inspected current authority/dedupe, but source inspection is not an executed race/rollback test.

No claim of entire FX06 acceptance, general Email/Push delivery, full workflow race coverage or RELEASE READY. Human UI/product acceptance, independent review and current-head CI remain separate gates.

## Added exact existing-writer contract and executed regression

Root authorized additional current-authority/concurrency/rollback QA for the existing finisher; no new worker or provider. Exact binding: `focus.session.finish_phase` WORKER/SYSTEM, FX19 BR001/002/004 and AC003, NXG-FX19-G02/G03; existing FX06 SYSTEM publication effect BR001/002, NXG-FX06-G01/G02 plus owner/current-authority common security gates. `SqlFocusService.FinishDueAsync` owns current running/due transition, owner serialization, notification writer and audit in one transaction. Test assertions cover those existing contracts, not Task-linked Focus or general live channel dispatch.

New `tests/Nexora.IntegrationTests/SqlNotificationTrustedWriterTests.cs` starts each phase through actual CSRF/authenticated API using legitimate synthetic active sessions and real migrated Ready FX18/19 defaults. No catalog/grant status is promoted. Only the precisely owned phase's StartedAt/LastRunAt are moved consistently to its valid due boundary in the disposable DB; state remains Running/elapsed0 until the real finisher acts. This **SQL clock fixture** is distinct from FN-042's actual60s browser timer.

| Scenario | Actual assertions | Result |
|---|---|---|
|4 concurrent finishers, then4 repeated finishers|One Completed inactive phase, exact planned elapsed, stable final rowversion; one logical notification, one SYSTEM audit, exact owner and3 truthful channel rows/attempts/error codes|PASS|
|Current FX19 grant revoked then restored through real SuperAdmin preview/commit APIs|Revoked current capability leaves phase Running with no notification/audit; normal restore allows exactly one completion|PASS|
|Notification insert persistence fault|Owned trigger throws only for this event; phase/event/audit rollback; durable failures1+future retry; drop owned fault and move owned retry clock due; exactly one successful recovery|PASS|
|Authority changes inside completion transaction|Owned phase-only trigger disables existing owner grant after completion update; before-commit authority denies and phase/event/audit/grant all rollback; drop fault, genuine completion then succeeds once|PASS|

Executed `dotnet test ... --configuration Debug --filter FullyQualifiedName~SqlNotificationTrustedWriterTests`: **4PASS0FAIL0SKIP**,3s test duration, fresh uniquely generated `Nexora_Test_<GUID>` database; real SQL migrations/API/finisher. No shared API rebuild/restart. TRX curated `evidence/focus-writer-20261027/focus-writer.trx`. Fault triggers removed in `finally`; fixture disposes only its own DB. These are bounded fault/clock fixtures, not production behavior or live-provider simulation.

FN-042 was also rerun after adding a final exact `sqlFocusCompletion` equality against the first committed notification snapshot, after normal UI conversion/replay plus6s beyond the actual worker's5s poll interval. **1PASS77.773s**, timestamp `2026-10-27T15:36:55.867Z`, desktop1366×768. Curated `evidence/focus-writer-20261027/focus-and-support-desktop-junit.xml` also includes Support gate1PASS; do not double-count that as Focus coverage. Actual offline phase completion remains real60s, no SQL clock rewrite in this browser test.

Source fingerprints: new SQL tests `76DD60C82C048CEA60D116378E1F24442C13BEFAAB7C3B5BC136FD6F4893D35D`; final FN-042 `4BD59DEDFFC4D552D7F724113F7EBD4B2B7217708E48C2BF625DA2FD99CF2BAD`. Focus production source unchanged. Independent root review pending for new regression scope/source/evidence; no full module or release acceptance inferred.
