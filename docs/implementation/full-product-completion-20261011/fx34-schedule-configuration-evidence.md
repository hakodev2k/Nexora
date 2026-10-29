# FX34 schedule configuration and calendar preview

Task owner root; independent source review baseline_review. Main8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3; PR4 parent07f650e6b8180b50d61185297f1cf8112b8423ce. Current session authorizes continuing local FX34 implementation/D03 design delegation; no G3/G4, deployment, paid provisioning or PR merge. AGENTS code-only reserves functional tests/fixtures for human QA.

Action automation.schedule.update SELF; P06-AUT-002/003/006/009/013/014; FX-34-BR-002/004; goals NXG-FX34-G02/G03. Sources feature34/action-catalog/modules34/UXmodules34, DB09 proposedSchedule and phase06 requirements; precedence preserves original pause/history, explicit current PO resumes local engineering only. Rules Nexora skill/profile/technicallead and routed backend/database/frontend/security apply.

## Actual vertical behavior

GET /api/v1/automation/definitions/{id}/schedule reads owner-private current immutable schedule version bound to a DefinitionVersion, definition ETag, next-run preview and BindingStale. No config returns ScheduleVersion0 with no artificial records. ExecutionAvailable=false explicitly: scheduled dispatch is not implemented in this slice.

POST same path /preview calculates five future occurrences without writes/effects. Bounded Daily/Weekly subset uses IANA timezone orUTC, hour0..23, minute0..59, Sundaybit0 weekday mask1..127, Daily127, explicit-offset start inclusive/end exclusive and MissedPolicySkip. Unknown frequency/zone/policy invalid. DST nonexistent wallclock skipped; repeated wallclock emits earlierUTC occurrence only. No external cron library or unrestricted expressions; cron/interval advanced forms remain missing. Clock supplied into pure planner, serverpreview uses current UTC; bounded366day scan. No runtime DST correctness claim.

PUT schedule requires current owner/PersonalSpace/session/module/read and schedule.update, UUID Idempotency-Key and canonical If-Match. Disabled Draft/Disabled Active definition only; current target authority and stored definition parser checked. Serializable owner/definition lock serializes save; appends immutable ScheduleVersion, updates current pointer/NextDueAt and definition revision, writes audit and receipt atomically. Same-key replay returns historical identity/revision; UI reloads current projection. Editing definition makes binding stale; schedule is not silently rebound. No changes to historical migrations0047..0050 or prior owner data.

0051 adds owner-composite Schedule/ScheduleVersion keys, definition/version/current-version FKs, privateJSON bounds and due index; registers exact permission without enabling module/grants/runtime. Current schedule has no Enabled field because no execution path is activated here. NextDueAt is persisted planning state, not proof of a queued occurrence. No migration apply/replay runtime performed.

Real React ScheduleDialog is reachable from workflow cards. Typed fields, serverpreview, currentSQLread/save, immutable ACK200 and request body/ETag/key retry after unknown outcomes. Dirty reload asks discard; 412 requires current reload. Busy/uncertain input/navigation constrained. Generation and flight reset handle StrictMode; keyed definition dialog, owner denial and404 clear dialog; competing parent controls disabled. Responsive base dialog/layout reused, browser verification NOT_RUN.

## Evidence

PASS final API Release build0warnings/errors; PASS TypeScript/Vite build with existing bundle-size warning; PASS scoped diffcheck. Independent review confirmed owner/action/ETag/receipt/version/FK guards and final UI fixes. Initial source blockers StrictMode flight, denial clearing and dirtyReload were fixed and re-reviewed. End boundary exclusive and zone100 bound reviewed.

NOT_RUN unit/SQL/API/browser, migration apply/replay/upgrade, DST runtime, save/replay/concurrency/revocation runtime per active code-only directive. Human QA: empty/read/save/update; foreignowner/actiondisabled; staleETag; samekey/bodyconflict; lostACKrefresh; StrictMode; dirtyReload; revokedmodule/session; DST gap/fold in representative IANAzones; leap/yearboundary; exactstart/end; nofutureoccurrence; stale definitionbinding; migration replay and retainedowner counts. No sample/mock records or tests added. Disposition PARTIALLY_IMPLEMENTED; configuration code acceptance remains unverified.

Production files: Application/Automation/AutomationScheduleContract.cs and IAutomationDefinitionService.cs; Infrastructure/Automation/SqlAutomationSchedule.cs and SqlAutomationDefinitionService.cs; Domain/Access/ActionGrantPolicy.cs; Infrastructure/Authorization/SqlSelfCapability.cs; Infrastructure/Local/M01MigrationManifest.cs; Api/Features/Automation/AutomationDefinitionEndpoints.cs; web/src/AutomationScheduleDialog.tsx, AutomationScreen.tsx, automationApi.ts; migration0051.

## Precise remaining work

Durable occurrence dedupe/enqueue, scheduler restart/missed/coalescing/per-definition capacity, authority for scheduled dispatch and worker integration remain missing. No in-memory timer or placeholder success is shipped. Cron/interval dialect extensions, dry_run, support.read and purge remain unfinished. R1/full733action/202screen semantic reconciliation remains incomplete; this artifact updates affected action only, no completion percentage. Provider effects default inactive.

Next executable source slice: SQL-backed occurrence identity and scheduler processing integrated with existing Task worker; pin definition/schedule versions, check current authority, persist Skip/busy-coalescing outcomes and bounded leases. Preserve configuration versions and use approved Task domain, never direct Task inserts. Any unresolved durable-background authority policy must be recorded before enabling it. No rollback/delete/reseed performed; disabling execution remains existing safe operational gate.
