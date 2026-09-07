# Cross-layer acceptance walk-throughs

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

These are executable test designs for a later approved implementation, not automated tests run here. Each suite uses source BR/AC in feature spec, proposed schema, application command and screen outcome.

| Scenario | Data / transaction | UX / expected proof |
| --- | --- | --- |
| Project close races Task Save | Project root lock; ProjectVersion/TaskVersion/Reminder/Outbox transaction | FX11-S04 vs FX12-S03: winner valid, loser parent locked; no partial child edit/old reminder |
| Backward Task drag + retry | Status/rank/version/required reason + idempotency | FX12-S01/S05: cancel unchanged; retry one version; failure card returns original rank |
| Document manual unchanged Save / retry | PageVersion unique command key, new distinct Save sequence | FX20-S04/S05: unchanged distinct Save new version, retry same version; no autosave indicator |
| Archive parent with independent Archived and Trash children | ArchiveBatch membership excludes earlier archive/Trash | FX20-S08/S09: correct cohort restore; sidebar archived child still readonly; no resurrection |
| Folder Trash / child restore / file pins | TrashBatch and original immutable topology, FileReference versions | FX20-S10/FX08-S02: parent-first exact cohort, independent Trash remains; file purge blocked by pins |
| ICS mixed rows and timezone | ImportedUid, ManualEvent typed date/instant, ignored VALARM | FX13-S07: valid Scheduled only, recurring/invalid/duplicate reports; all-day no date shift; export containment |
| Notification browser denied | Notification +3Delivery rows; independent attempts | FX06-S02/S03: In-app and Email continue, Browser unavailable not received; delete inbox not audit |
| Emergency audit write fails | No AccessSession/data capability without committed audit/intent | FX05-S04: remains reason form, no user data; successful entry banner/owner intent all3 |
| Permission revoked while form/share/provider result loads | Context/current grant check before response/cache display | Global state contract clears protected content and exits, no stale private preview |
| Time/Focus concurrent sessions and source terminal | Unique active slot/root closure contract, immutable entry history | FX18-S01/FX19-S02: no duplicate active timer, SourceClosed stops linked work, conversion once |
| Finance posting and Vault recovery | Q-05/Q-04 conditional tables/invariants | Affected FX27/28 actions blocked until policy; no runtime financial/security assurance |
| Price provider outage / workflow unknown effect | Nullable observation price; stable effect key/Unknown reconciliation | FX30-S03 stale gap not0; FX34-S05 no blind duplicate external retry |
| Resume updated after application | Exact ResumeVersion FK/pin and immutable file | FX39-S02/S06/S07 always submitted version, no latest substitution |
| New hypothetical module | New schema + manifest/contracts only, no core column additions | Shell/Search/Trash/Register gracefully discover compatible capability, disabled contribution safe |
| Keyboard/mobile flow across every module | No persistence change; source ETag/action contracts same | Every197Screen ID primary/error/destructive/Back reachable without drag/hover; actual accessibility test later |
