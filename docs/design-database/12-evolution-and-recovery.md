# Module evolution, migration and recovery design

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

## Extension rule

Core is extensible by stable module/resource/action/contribution identifiers and contracts, not by adding a column for every new module. Domain fields remain typed relational tables in the provider's schema. User/Admin may configure approved settings but cannot create schemas, executable handlers or external plugins. Do not use a universal Entity/Field/Value table or serialize all domain records into one JSON column merely to claim extensibility.

## New-module design exercise — not new R1 scope

Use hypothetical VehicleMaintenance only to test extension boundaries:

1. Developer creates a trusted manifest with module key vehicle-maintenance, platform compatibility, dependency on Personal Assets/Files, resource ServiceRecord, permissions and versioned contributions.
2. New module owns its own schema and DbContext; ServiceRecord fields are typed (asset ResourceId reference, service date, odometer decimal with unit, notes, files). It validates PersonalAsset type and same owner through provider contract; no Assets table mapping or direct read.
3. Platform registers Module, ModuleRelease, ResourceType and Permission rows through deployment. Existing User table, Task table, Page table, Finance ledger and other schemas gain **no new columns**.
4. Search, navigation, dashboard, expiry reminders, Trash, import/export and backup discover only declared contracts. A module with no sharing contribution has no Share action. No mandatory 40-case switch in kernel.
5. New deployment includes module migrations and contract tests. A hard dependency outage blocks relevant operations; optional dashboard contribution can degrade without crashing shell.
6. Disable stops new access/work, preserves schema/data/version history. Upgrade validates compatibility/checksum and runs ordered migration under lease. Uninstall/purge is a separate irreversible-data decision, never the Disable button.

Pass criteria: no core/domain table rewrite, no direct cross-module assembly/DbContext reference, all owner/permission/lifecycle tests still apply, registry contains only identity/capability metadata. This is a documentation exercise; no new module is committed to Release1.

## Schema version and rollout

One SQL Server database with per-module schemas initially. Each migration is module-owned, monotonically ordered and checksum pinned. Deployment-only identity can change schema; API/worker principals cannot. Migration dependency DAG uses hard platform/schema contracts, not arbitrary runtime calls. Startup verifies expected minimum/maximum schema versions; failed/unapplied required migrations prevent module Ready.

Use expand → backfill bounded batches → verify → switch reads/writes → contract old fields in later approved release. Mixed versions require explicit compatibility window, never assuming an old binary understands a new required enum/JSON field. Record backfill cursor and counts outside user-facing business state. Sensitive fields require encryption and log review before any backfill.

Rollback app binary only if current schema remains backward compatible. Irreversible transform requires tested forward-fix/recovery plan and approved backup; do not promise universal down-migration. Database backups, object files, key inventory and module versions form one recovery manifest.

## Disable, delete and recovery distinctions

Disable does not purge data or revoke the owner's ownership; it gates reads/actions/jobs/contributions. A retained favorite shows module unavailable without stale sensitive preview. Pending jobs recheck module/source revision before effects. Already accepted Email/Push cannot be recalled. Share behavior on sharing-policy disable and Trash restoration remains Q-03, while source Trash always blocks exposure.

Owner Trash is indefinite until owner purge where explicitly approved. System job logs, staging files, backups and audit retention are separate Q-08 policies. File purge checks current and historical references; deleting one FileReference never deletes another owner's object. Same-owner binary deduplication may be added later but must not create cross-owner existence channels.

Account deletion Q-01 is not generic recursive cascade: disable account/session and new jobs, inventory per-module owned rows/files/key envelopes and exact references, expose approved export choices, execute approved purge order, retain only approved minimized audit/tombstones and disclose backup residuals. Do not implement grace duration until approval.

## Recovery acceptance scenarios

- Restore SQL snapshot without matching file/key manifest: fail validation before serving traffic.
- Module absent/older than backed-up schema: module unavailable until compatible version deployed, never silently drop its data.
- Restore parent Project with Tasks: exact deletion cohort only; Tasks independently in Trash beforehand remain there.
- Restore archived Document tree: original states/cohorts survive; archived children do not become editable by accident.
- Rotation interrupted: resume deterministic cursor; never destroy key required by live/history/backup version.
- Regenerate search/cache from authoritative rows and compare owner/gate filtering; no restored stale share token gets access without current policy.

No backup/restore command was run. Production RPO/RTO, capacity and key-recovery decisions remain open.
