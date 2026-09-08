# Relations, typed contracts and transaction boundaries

> **Current decision amendment — 2026-09-07:** Physical delta17 is current for User/ShareLink/Vault flags, recovery wraps, four new tables and Career CalendarLink replacement. Baseline field counts/encryption/purge/Interview proposals are superseded only where specified; no migration executed. [Normative PO decisions](../requirements/10-owner-decisions-20260907.md). Conflicting older proposal paragraphs below are historical; current field/action overrides are in the linked delta. Docs-only.

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

## Transaction catalogue

| Operation | Atomic write set | Serialization and rollback |
| --- | --- | --- |
| Verify email | Token consume + User activation + default User role + PersonalSpace + UserModuleGrant snapshot | Unique email/UserId; conditional token consume. Pending account has no active module access. Pre-activation transactional email intent is identity-owned, not a fake PersonalSpace. |
| Task Save/move/restore | Task + checklist/tags/rank + TaskVersion + registry revision + Reminder config + Outbox + Idempotency | Acquire parent Project gate then Task/rank group. Endpoints, import, automation and history use same command. Backward reason required. |
| Close Project | Project + version + source terminal guards + stop linked timer/focus + invalidate pending source work + outbox | Parent gate excludes concurrent Task create/edit/restore. Timer/focus domain contracts share local SQL transaction. Failure of required participant rolls back whole close. No Task state rewrite. |
| Document Save | Page + immutable PageVersion + current/historical FileReferences + registry + outbox | Explicit manual Save command key. If file not Clean/correct owner, entire Save rejects; old body remains. |
| Archive/Unarchive parent | Page tree states + ArchiveBatch/Members + Activity + registry (no content Save version) | Root-before-child deterministic locks. Existing independent Archived children excluded. Unarchive matching cohort only; Trash children not revived. |
| Tree Trash/Restore | TrashBatch/Members + domain lifecycle + registry + pending-job invalidation | Preview then revalidate under same root gate; original topology immutable. Parent terminal Project blocks Task restore. No automatic share revival decision. |
| Open Emergency | AccessSession + immutable AuditEvent + notification/outbox intent | All commit before returning data capability. Failed durable audit stops access; channel delivery failures do not erase audit/intent. |
| Notification creation | Notification + three Delivery rows + outbox | Unique owner/IntentKey, independent dispatch/retries. Inbox read state is unrelated to provider acceptance. |
| Finance posting proposal | Transaction + legs + splits + allocations + version/outbox | Q-05 gate. Enforce balanced same-currency transfer and no partial legs. Never save balance as user-editable value. |
| Provider ingestion | Public cache source rows + deterministic dedupe; owner consumer receipts separately | No transaction spans network call. Record observation then private fanout through deduped consumer; failure not zero/empty. |
| Link exact Resume version | Application/Timeline + ResumeVersion pin + FileReference retention | Same-owner exact FK/version guard; source file cannot disappear after submit. New Resume version never retargets application. |
| Webhook/automation effect | Durable effect intent + lease + provider call + observed outcome | No distributed transaction. At-least-once transport; stable side-effect key. Unknown outcome requires reconcile, not unconditional retry. |


## Exhaustive FK index

Every row below is a proposed physical FK. In addition each R aggregate binds (OwnerId,Id) to registry (OwnerId,Id). Semantic ResourceLink endpoints/type/version remain provider checks; do not draw them as FKs to arbitrary business tables. Optional FK0..1 expresses nullability; unique constraints in dictionaries may further limit incoming cardinality.

| From table | Field | Target | Outgoing cardinality | Key scope | Deletion |
| --- | --- | --- | --- | --- | --- |
| identity.User | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| identity.User | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| identity.User | AvatarFileId | files.FileObject | 0..1 | key only | NO ACTION; permission still required |
| identity.Session | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| identity.Session | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| identity.Session | UserId | identity.User | 1 | key only | NO ACTION; permission still required |
| identity.OneTimeToken | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| identity.OneTimeToken | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| identity.OneTimeToken | UserId | identity.User | 1 | key only | NO ACTION; permission still required |
| identity.Role | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| identity.Role | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| identity.UserRole | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| identity.UserRole | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| identity.UserRole | UserId | identity.User | 1 | key only | NO ACTION; permission still required |
| identity.UserRole | RoleId | identity.Role | 1 | key only | NO ACTION; permission still required |
| platform.PersonalSpace | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| platform.PersonalSpace | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| platform.PersonalSpace | UserId | identity.User | 1 | key only | NO ACTION; permission still required |
| platform.SecurityInvariant | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| platform.SecurityInvariant | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| platform.Module | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| platform.Module | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| platform.ModuleRelease | ModuleId | platform.Module | 1 | key only | NO ACTION; permission still required |
| platform.ModuleDependency | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| platform.ModuleDependency | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| platform.ModuleDependency | ModuleId | platform.Module | 1 | key only | NO ACTION; permission still required |
| platform.ModuleDependency | DependencyModuleId | platform.Module | 1 | key only | NO ACTION; permission still required |
| platform.ModuleMigration | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| platform.ModuleMigration | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| platform.ModuleMigration | ModuleId | platform.Module | 1 | key only | NO ACTION; permission still required |
| platform.UserModuleGrant | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| platform.UserModuleGrant | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| platform.UserModuleGrant | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| platform.UserModuleGrant | ModuleId | platform.Module | 1 | key only | NO ACTION; permission still required |
| platform.Permission | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| platform.Permission | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| platform.Permission | ModuleId | platform.Module | 1 | key only | NO ACTION; permission still required |
| platform.AdminPermission | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| platform.AdminPermission | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| platform.AdminPermission | UserId | identity.User | 1 | key only | NO ACTION; permission still required |
| platform.AdminPermission | PermissionId | platform.Permission | 1 | key only | NO ACTION; permission still required |
| platform.ResourceType | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| platform.ResourceType | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| platform.ResourceType | ModuleId | platform.Module | 1 | key only | NO ACTION; permission still required |
| platform.Resource | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| platform.Resource | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| platform.Resource | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| platform.Resource | ResourceTypeId | platform.ResourceType | 1 | key only | NO ACTION; permission still required |
| platform.ResourceLink | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| platform.ResourceLink | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| platform.ResourceLink | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| platform.ResourceLink | SourceResourceId | platform.Resource | 1 | OwnerId + key | NO ACTION; permission still required |
| platform.ResourceLink | TargetResourceId | platform.Resource | 1 | OwnerId + key | NO ACTION; permission still required |
| platform.Preference | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| platform.Preference | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| platform.Preference | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| platform.Preference | ModuleId | platform.Module | 0..1 | key only | NO ACTION; permission still required |
| security.ShareLink | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| security.ShareLink | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| security.ShareLink | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| security.ShareLink | ResourceId | platform.Resource | 1 | OwnerId + key | NO ACTION; permission still required |
| security.ShareAllowedUser | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| security.ShareAllowedUser | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| security.ShareAllowedUser | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| security.ShareAllowedUser | ShareLinkId | security.ShareLink | 1 | OwnerId + key | NO ACTION; permission still required |
| security.ShareAllowedUser | UserId | identity.User | 1 | key only | NO ACTION; permission still required |
| security.SupportGrant | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| security.SupportGrant | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| security.SupportGrant | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| security.SupportGrant | ModuleId | platform.Module | 1 | key only | NO ACTION; permission still required |
| security.AccessSession | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| security.AccessSession | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| security.AccessSession | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| security.AccessSession | ActorUserId | identity.User | 1 | key only | NO ACTION; permission still required |
| security.AccessSession | ModuleId | platform.Module | 1 | key only | NO ACTION; permission still required |
| security.AccessSession | SupportGrantId | security.SupportGrant | 0..1 | OwnerId + key | NO ACTION; permission still required |
| security.ActivityEvent | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| security.ActivityEvent | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| security.ActivityEvent | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| security.ActivityEvent | ResourceId | platform.Resource | 1 | OwnerId + key | NO ACTION; permission still required |
| files.FileObject | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| files.FileObject | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| files.FileObject | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| files.FileReference | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| files.FileReference | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| files.FileReference | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| files.FileReference | FileObjectId | files.FileObject | 1 | OwnerId + key | NO ACTION; permission still required |
| files.FileReference | ResourceId | platform.Resource | 1 | OwnerId + key | NO ACTION; permission still required |
| files.UploadSession | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| files.UploadSession | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| files.UploadSession | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| files.UploadSession | FileObjectId | files.FileObject | 0..1 | OwnerId + key | NO ACTION; permission still required |
| notifications.Notification | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| notifications.Notification | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| notifications.Notification | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| notifications.Notification | ResourceId | platform.Resource | 0..1 | OwnerId + key | NO ACTION; permission still required |
| notifications.Delivery | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| notifications.Delivery | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| notifications.Delivery | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| notifications.Delivery | NotificationId | notifications.Notification | 1 | OwnerId + key | NO ACTION; permission still required |
| notifications.DeliveryAttempt | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| notifications.DeliveryAttempt | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| notifications.DeliveryAttempt | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| notifications.DeliveryAttempt | DeliveryId | notifications.Delivery | 1 | OwnerId + key | NO ACTION; permission still required |
| notifications.PushSubscription | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| notifications.PushSubscription | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| notifications.PushSubscription | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| operations.Idempotency | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| operations.Idempotency | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| operations.Idempotency | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| operations.Outbox | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| operations.Outbox | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| operations.Outbox | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| operations.Outbox | ModuleId | platform.Module | 1 | key only | NO ACTION; permission still required |
| operations.Outbox | ResourceId | platform.Resource | 0..1 | OwnerId + key | NO ACTION; permission still required |
| operations.InboxReceipt | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| operations.InboxReceipt | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| operations.InboxReceipt | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| operations.Job | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| operations.Job | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| operations.Job | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| operations.Job | ModuleId | platform.Module | 1 | key only | NO ACTION; permission still required |
| operations.Job | SourceResourceId | platform.Resource | 0..1 | OwnerId + key | NO ACTION; permission still required |
| operations.JobAttempt | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| operations.JobAttempt | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| operations.JobAttempt | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| operations.JobAttempt | JobId | operations.Job | 1 | OwnerId + key | NO ACTION; permission still required |
| operations.TrashBatch | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| operations.TrashBatch | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| operations.TrashBatch | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| operations.TrashBatch | RootResourceId | platform.Resource | 1 | OwnerId + key | NO ACTION; permission still required |
| operations.TrashMember | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| operations.TrashMember | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| operations.TrashMember | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| operations.TrashMember | BatchId | operations.TrashBatch | 1 | OwnerId + key | NO ACTION; permission still required |
| operations.TrashMember | ResourceId | platform.Resource | 1 | OwnerId + key | NO ACTION; permission still required |
| operations.TrashMember | ParentResourceId | platform.Resource | 0..1 | OwnerId + key | NO ACTION; permission still required |
| operations.ImportBatch | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| operations.ImportBatch | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| operations.ImportBatch | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| operations.ImportBatch | ModuleId | platform.Module | 1 | key only | NO ACTION; permission still required |
| operations.ImportBatch | FileObjectId | files.FileObject | 1 | OwnerId + key | NO ACTION; permission still required |
| operations.ImportRow | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| operations.ImportRow | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| operations.ImportRow | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| operations.ImportRow | BatchId | operations.ImportBatch | 1 | OwnerId + key | NO ACTION; permission still required |
| operations.ImportRow | ResultResourceId | platform.Resource | 0..1 | OwnerId + key | NO ACTION; permission still required |
| operations.ExportJob | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| operations.ExportJob | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| operations.ExportJob | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| operations.ExportJob | ModuleId | platform.Module | 1 | key only | NO ACTION; permission still required |
| operations.ExportJob | FileObjectId | files.FileObject | 0..1 | OwnerId + key | NO ACTION; permission still required |
| operations.BackupRun | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| operations.BackupRun | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| operations.RestoreRun | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| operations.RestoreRun | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| operations.RestoreRun | BackupRunId | operations.BackupRun | 1 | key only | NO ACTION; permission still required |
| productivity.Project | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| productivity.Project | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.Project | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.Project | TrashBatchId | operations.TrashBatch | 0..1 | OwnerId + key | NO ACTION; permission still required |
| productivity.Task | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| productivity.Task | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.Task | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.Task | ProjectId | productivity.Project | 1 | OwnerId + key | NO ACTION; permission still required |
| productivity.Task | TrashBatchId | operations.TrashBatch | 0..1 | OwnerId + key | NO ACTION; permission still required |
| productivity.TaskChecklistItem | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| productivity.TaskChecklistItem | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.TaskChecklistItem | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.TaskChecklistItem | TaskId | productivity.Task | 1 | OwnerId + key | NO ACTION; permission still required |
| productivity.Tag | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| productivity.Tag | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.Tag | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.ProjectTag | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| productivity.ProjectTag | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.ProjectTag | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.ProjectTag | ProjectId | productivity.Project | 1 | OwnerId + key | NO ACTION; permission still required |
| productivity.ProjectTag | TagId | productivity.Tag | 1 | OwnerId + key | NO ACTION; permission still required |
| productivity.TaskTag | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| productivity.TaskTag | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.TaskTag | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.TaskTag | TaskId | productivity.Task | 1 | OwnerId + key | NO ACTION; permission still required |
| productivity.TaskTag | TagId | productivity.Tag | 1 | OwnerId + key | NO ACTION; permission still required |
| productivity.ProjectVersion | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| productivity.ProjectVersion | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.ProjectVersion | ProjectId | productivity.Project | 1 | OwnerId + key | NO ACTION; permission still required |
| productivity.TaskVersion | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| productivity.TaskVersion | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.TaskVersion | TaskId | productivity.Task | 1 | OwnerId + key | NO ACTION; permission still required |
| calendar.ManualEvent | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| calendar.ManualEvent | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| calendar.ManualEvent | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| calendar.ImportedUid | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| calendar.ImportedUid | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| calendar.ImportedUid | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| calendar.ImportedUid | ManualEventId | calendar.ManualEvent | 1 | OwnerId + key | NO ACTION; permission still required |
| calendar.ImportedUid | ImportBatchId | operations.ImportBatch | 1 | OwnerId + key | NO ACTION; permission still required |
| calendar.Reminder | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| calendar.Reminder | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| calendar.Reminder | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| calendar.Reminder | SourceResourceId | platform.Resource | 1 | OwnerId + key | NO ACTION; permission still required |
| productivity.PlannerPin | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| productivity.PlannerPin | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.PlannerPin | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.PlannerPin | TaskResourceId | platform.Resource | 1 | OwnerId + key | NO ACTION; permission still required |
| productivity.Goal | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| productivity.Goal | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.Goal | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.Goal | TrashBatchId | operations.TrashBatch | 0..1 | OwnerId + key | NO ACTION; permission still required |
| productivity.GoalTarget | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| productivity.GoalTarget | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.GoalTarget | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.GoalTarget | GoalId | productivity.Goal | 1 | OwnerId + key | NO ACTION; permission still required |
| productivity.GoalProgress | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| productivity.GoalProgress | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.GoalProgress | TargetId | productivity.GoalTarget | 1 | OwnerId + key | NO ACTION; permission still required |
| productivity.Habit | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| productivity.Habit | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.Habit | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.Habit | TrashBatchId | operations.TrashBatch | 0..1 | OwnerId + key | NO ACTION; permission still required |
| productivity.HabitSchedule | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| productivity.HabitSchedule | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.HabitSchedule | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.HabitSchedule | HabitId | productivity.Habit | 1 | OwnerId + key | NO ACTION; permission still required |
| productivity.HabitCheckIn | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| productivity.HabitCheckIn | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.HabitCheckIn | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.HabitCheckIn | HabitId | productivity.Habit | 1 | OwnerId + key | NO ACTION; permission still required |
| productivity.HabitCheckIn | ScheduleId | productivity.HabitSchedule | 1 | OwnerId + key | NO ACTION; permission still required |
| productivity.TimeEntry | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| productivity.TimeEntry | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.TimeEntry | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.TimeEntry | TaskResourceId | platform.Resource | 0..1 | OwnerId + key | NO ACTION; permission still required |
| productivity.TimeEntry | TrashBatchId | operations.TrashBatch | 0..1 | OwnerId + key | NO ACTION; permission still required |
| productivity.TimeEntry | ProjectResourceId | platform.Resource | 0..1 | OwnerId + key | NO ACTION; permission still required |
| productivity.FocusSession | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| productivity.FocusSession | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.FocusSession | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.FocusSession | TaskResourceId | platform.Resource | 0..1 | OwnerId + key | NO ACTION; permission still required |
| productivity.FocusSession | ConvertedTimeEntryResourceId | platform.Resource | 0..1 | OwnerId + key | NO ACTION; permission still required |
| productivity.FocusSegment | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| productivity.FocusSegment | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.FocusSegment | SessionId | productivity.FocusSession | 1 | OwnerId + key | NO ACTION; permission still required |
| documents.Folder | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| documents.Folder | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| documents.Folder | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| documents.Folder | ParentFolderId | documents.Folder | 0..1 | OwnerId + key | NO ACTION; permission still required |
| documents.Folder | TrashBatchId | operations.TrashBatch | 0..1 | OwnerId + key | NO ACTION; permission still required |
| documents.Tag | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| documents.Tag | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| documents.Tag | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| documents.Page | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| documents.Page | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| documents.Page | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| documents.Page | FolderId | documents.Folder | 0..1 | OwnerId + key | NO ACTION; permission still required |
| documents.Page | ParentPageId | documents.Page | 0..1 | OwnerId + key | NO ACTION; permission still required |
| documents.Page | TagId | documents.Tag | 0..1 | OwnerId + key | NO ACTION; permission still required |
| documents.Page | ArchiveBatchId | documents.ArchiveBatch | 0..1 | OwnerId + key | NO ACTION; permission still required |
| documents.Page | CoverFileId | files.FileObject | 0..1 | OwnerId + key | NO ACTION; permission still required |
| documents.Page | TrashBatchId | operations.TrashBatch | 0..1 | OwnerId + key | NO ACTION; permission still required |
| documents.PageVersion | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| documents.PageVersion | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| documents.PageVersion | PageId | documents.Page | 1 | OwnerId + key | NO ACTION; permission still required |
| documents.ArchiveBatch | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| documents.ArchiveBatch | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| documents.ArchiveBatch | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| documents.ArchiveBatch | RootPageId | documents.Page | 1 | OwnerId + key | NO ACTION; permission still required |
| documents.ArchiveMember | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| documents.ArchiveMember | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| documents.ArchiveMember | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| documents.ArchiveMember | BatchId | documents.ArchiveBatch | 1 | OwnerId + key | NO ACTION; permission still required |
| documents.ArchiveMember | PageId | documents.Page | 1 | OwnerId + key | NO ACTION; permission still required |
| knowledge.Bookmark | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| knowledge.Bookmark | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| knowledge.Bookmark | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| knowledge.Bookmark | TrashBatchId | operations.TrashBatch | 0..1 | OwnerId + key | NO ACTION; permission still required |
| knowledge.Snippet | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| knowledge.Snippet | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| knowledge.Snippet | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| knowledge.Snippet | TrashBatchId | operations.TrashBatch | 0..1 | OwnerId + key | NO ACTION; permission still required |
| knowledge.SnippetVersion | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| knowledge.SnippetVersion | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| knowledge.SnippetVersion | SnippetId | knowledge.Snippet | 1 | OwnerId + key | NO ACTION; permission still required |
| knowledge.ReadingItem | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| knowledge.ReadingItem | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| knowledge.ReadingItem | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| knowledge.ReadingItem | SourceResourceId | platform.Resource | 0..1 | OwnerId + key | NO ACTION; permission still required |
| knowledge.ReadingItem | TrashBatchId | operations.TrashBatch | 0..1 | OwnerId + key | NO ACTION; permission still required |
| organization.Tag | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| organization.Tag | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| organization.Tag | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| organization.ResourceTag | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| organization.ResourceTag | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| organization.ResourceTag | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| organization.ResourceTag | TagId | organization.Tag | 1 | OwnerId + key | NO ACTION; permission still required |
| organization.ResourceTag | ResourceId | platform.Resource | 1 | OwnerId + key | NO ACTION; permission still required |
| organization.Collection | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| organization.Collection | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| organization.Collection | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| organization.Collection | ModuleId | platform.Module | 1 | key only | NO ACTION; permission still required |
| organization.Collection | TrashBatchId | operations.TrashBatch | 0..1 | OwnerId + key | NO ACTION; permission still required |
| organization.CollectionMember | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| organization.CollectionMember | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| organization.CollectionMember | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| organization.CollectionMember | CollectionId | organization.Collection | 1 | OwnerId + key | NO ACTION; permission still required |
| organization.CollectionMember | ResourceId | platform.Resource | 1 | OwnerId + key | NO ACTION; permission still required |
| organization.Template | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| organization.Template | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| organization.Template | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| organization.Template | ModuleId | platform.Module | 1 | key only | NO ACTION; permission still required |
| organization.Template | ResourceTypeId | platform.ResourceType | 1 | key only | NO ACTION; permission still required |
| organization.Template | TrashBatchId | operations.TrashBatch | 0..1 | OwnerId + key | NO ACTION; permission still required |
| discovery.SearchProjection | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| discovery.SearchProjection | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| discovery.SearchProjection | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| discovery.SearchProjection | ResourceId | platform.Resource | 1 | OwnerId + key | NO ACTION; permission still required |
| discovery.SavedQuery | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| discovery.SavedQuery | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| discovery.SavedQuery | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| discovery.SavedQuery | ModuleId | platform.Module | 0..1 | key only | NO ACTION; permission still required |
| discovery.Favorite | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| discovery.Favorite | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| discovery.Favorite | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| discovery.Favorite | ResourceId | platform.Resource | 1 | OwnerId + key | NO ACTION; permission still required |
| discovery.RecentItem | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| discovery.RecentItem | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| discovery.RecentItem | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| discovery.RecentItem | ResourceId | platform.Resource | 1 | OwnerId + key | NO ACTION; permission still required |
| discovery.DashboardWidget | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| discovery.DashboardWidget | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| discovery.DashboardWidget | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| discovery.DashboardWidget | DashboardId | discovery.Dashboard | 1 | OwnerId + key | NO ACTION; permission still required |
| finance.Account | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| finance.Account | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| finance.Account | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| finance.Category | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| finance.Category | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| finance.Category | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| finance.Transaction | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| finance.Transaction | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| finance.Transaction | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| finance.Transaction | ReversesTransactionId | finance.Transaction | 0..1 | OwnerId + key | NO ACTION; permission still required |
| finance.TransactionLeg | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| finance.TransactionLeg | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| finance.TransactionLeg | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| finance.TransactionLeg | TransactionId | finance.Transaction | 1 | OwnerId + key | NO ACTION; permission still required |
| finance.TransactionLeg | AccountId | finance.Account | 1 | OwnerId + key | NO ACTION; permission still required |
| finance.TransactionSplit | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| finance.TransactionSplit | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| finance.TransactionSplit | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| finance.TransactionSplit | TransactionId | finance.Transaction | 1 | OwnerId + key | NO ACTION; permission still required |
| finance.TransactionSplit | CategoryId | finance.Category | 1 | OwnerId + key | NO ACTION; permission still required |
| finance.TransactionVersion | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| finance.TransactionVersion | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| finance.TransactionVersion | TransactionId | finance.Transaction | 1 | OwnerId + key | NO ACTION; permission still required |
| finance.Bill | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| finance.Bill | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| finance.Bill | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| finance.Bill | RecurringRuleId | finance.RecurringRule | 0..1 | OwnerId + key | NO ACTION; permission still required |
| finance.PaymentAllocation | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| finance.PaymentAllocation | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| finance.PaymentAllocation | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| finance.PaymentAllocation | BillId | finance.Bill | 0..1 | OwnerId + key | NO ACTION; permission still required |
| finance.PaymentAllocation | DebtId | finance.Debt | 0..1 | OwnerId + key | NO ACTION; permission still required |
| finance.PaymentAllocation | TransactionId | finance.Transaction | 1 | OwnerId + key | NO ACTION; permission still required |
| finance.RecurringRule | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| finance.RecurringRule | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| finance.RecurringRule | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| finance.Subscription | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| finance.Subscription | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| finance.Subscription | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| finance.Subscription | VaultResourceId | platform.Resource | 0..1 | OwnerId + key | NO ACTION; permission still required |
| finance.SubscriptionPrice | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| finance.SubscriptionPrice | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| finance.SubscriptionPrice | SubscriptionId | finance.Subscription | 1 | OwnerId + key | NO ACTION; permission still required |
| finance.Budget | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| finance.Budget | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| finance.Budget | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| finance.BudgetLine | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| finance.BudgetLine | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| finance.BudgetLine | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| finance.BudgetLine | BudgetId | finance.Budget | 1 | OwnerId + key | NO ACTION; permission still required |
| finance.BudgetLine | CategoryId | finance.Category | 1 | OwnerId + key | NO ACTION; permission still required |
| finance.SavingsGoal | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| finance.SavingsGoal | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| finance.SavingsGoal | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| finance.SavingsAccount | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| finance.SavingsAccount | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| finance.SavingsAccount | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| finance.SavingsAccount | SavingsGoalId | finance.SavingsGoal | 1 | OwnerId + key | NO ACTION; permission still required |
| finance.SavingsAccount | AccountId | finance.Account | 1 | OwnerId + key | NO ACTION; permission still required |
| finance.Debt | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| finance.Debt | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| finance.Debt | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| vault.Item | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| vault.Item | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| vault.Item | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| vault.Item | TrashBatchId | operations.TrashBatch | 0..1 | OwnerId + key | NO ACTION; permission still required |
| vault.ItemVersion | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| vault.ItemVersion | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| vault.ItemVersion | ItemId | vault.Item | 1 | OwnerId + key | NO ACTION; permission still required |
| vault.KeyEnvelope | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| vault.KeyEnvelope | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| vault.KeyEnvelope | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| vault.RotationRun | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| vault.RotationRun | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| vault.RotationRun | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| news.Feed | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| news.Feed | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| news.FeedSubscription | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| news.FeedSubscription | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| news.FeedSubscription | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| news.FeedSubscription | FeedId | news.Feed | 1 | key only | NO ACTION; permission still required |
| news.Article | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| news.Article | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| news.Article | FeedId | news.Feed | 1 | key only | NO ACTION; permission still required |
| news.ArticleState | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| news.ArticleState | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| news.ArticleState | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| news.ArticleState | ArticleId | news.Article | 1 | key only | NO ACTION; permission still required |
| news.TopicWatch | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| news.TopicWatch | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| news.TopicWatch | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| news.TopicMatch | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| news.TopicMatch | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| news.TopicMatch | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| news.TopicMatch | WatchId | news.TopicWatch | 1 | OwnerId + key | NO ACTION; permission still required |
| news.TopicMatch | ArticleId | news.Article | 1 | key only | NO ACTION; permission still required |
| shopping.TrackedProduct | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| shopping.TrackedProduct | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| shopping.TrackedProduct | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| shopping.TrackedProduct | TrashBatchId | operations.TrashBatch | 0..1 | OwnerId + key | NO ACTION; permission still required |
| shopping.PriceObservation | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| shopping.PriceObservation | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| shopping.PriceObservation | ProductId | shopping.TrackedProduct | 1 | OwnerId + key | NO ACTION; permission still required |
| shopping.PriceAlertRule | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| shopping.PriceAlertRule | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| shopping.PriceAlertRule | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| shopping.PriceAlertRule | ProductId | shopping.TrackedProduct | 1 | OwnerId + key | NO ACTION; permission still required |
| shopping.PriceAlertEvent | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| shopping.PriceAlertEvent | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| shopping.PriceAlertEvent | RuleId | shopping.PriceAlertRule | 1 | OwnerId + key | NO ACTION; permission still required |
| shopping.PriceAlertEvent | ObservationId | shopping.PriceObservation | 1 | OwnerId + key | NO ACTION; permission still required |
| shopping.WishlistItem | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| shopping.WishlistItem | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| shopping.WishlistItem | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| shopping.WishlistItem | TrackedProductResourceId | platform.Resource | 0..1 | OwnerId + key | NO ACTION; permission still required |
| shopping.WishlistItem | TrashBatchId | operations.TrashBatch | 0..1 | OwnerId + key | NO ACTION; permission still required |
| shopping.Comparison | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| shopping.Comparison | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| shopping.Comparison | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| shopping.ComparisonItem | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| shopping.ComparisonItem | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| shopping.ComparisonItem | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| shopping.ComparisonItem | ComparisonId | shopping.Comparison | 1 | OwnerId + key | NO ACTION; permission still required |
| shopping.ComparisonItem | WishlistResourceId | platform.Resource | 1 | OwnerId + key | NO ACTION; permission still required |
| shopping.Seller | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| shopping.Seller | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| shopping.Seller | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| shopping.Seller | MergedIntoId | shopping.Seller | 0..1 | OwnerId + key | NO ACTION; permission still required |
| shopping.PurchaseOrder | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| shopping.PurchaseOrder | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| shopping.PurchaseOrder | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| shopping.PurchaseOrder | SellerId | shopping.Seller | 0..1 | OwnerId + key | NO ACTION; permission still required |
| shopping.PurchaseOrder | FinanceResourceId | platform.Resource | 0..1 | OwnerId + key | NO ACTION; permission still required |
| shopping.PurchaseOrder | TrashBatchId | operations.TrashBatch | 0..1 | OwnerId + key | NO ACTION; permission still required |
| shopping.OrderLine | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| shopping.OrderLine | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| shopping.OrderLine | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| shopping.OrderLine | OrderId | shopping.PurchaseOrder | 1 | OwnerId + key | NO ACTION; permission still required |
| shopping.OrderLine | WishlistResourceId | platform.Resource | 0..1 | OwnerId + key | NO ACTION; permission still required |
| shopping.OrderLine | AssetResourceId | platform.Resource | 0..1 | OwnerId + key | NO ACTION; permission still required |
| shopping.ReturnRecord | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| shopping.ReturnRecord | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| shopping.ReturnRecord | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| shopping.ReturnRecord | OrderLineId | shopping.OrderLine | 1 | OwnerId + key | NO ACTION; permission still required |
| shopping.Warranty | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| shopping.Warranty | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| shopping.Warranty | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| shopping.Warranty | OrderLineId | shopping.OrderLine | 0..1 | OwnerId + key | NO ACTION; permission still required |
| developer.ToolDefinition | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| developer.ToolDefinition | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| developer.ToolFavorite | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| developer.ToolFavorite | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| developer.ToolFavorite | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| developer.ToolFavorite | ToolId | developer.ToolDefinition | 1 | key only | NO ACTION; permission still required |
| developer.ToolHistory | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| developer.ToolHistory | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| developer.ToolHistory | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| developer.ToolHistory | ToolId | developer.ToolDefinition | 1 | key only | NO ACTION; permission still required |
| developer.Repository | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| developer.Repository | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| developer.RankingSnapshot | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| developer.RankingSnapshot | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| developer.RankingEntry | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| developer.RankingEntry | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| developer.RankingEntry | SnapshotId | developer.RankingSnapshot | 1 | key only | NO ACTION; permission still required |
| developer.RankingEntry | RepositoryId | developer.Repository | 1 | key only | NO ACTION; permission still required |
| developer.SavedRepository | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| developer.SavedRepository | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| developer.SavedRepository | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| developer.SavedRepository | RepositoryId | developer.Repository | 1 | key only | NO ACTION; permission still required |
| automation.Definition | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| automation.Definition | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| automation.Definition | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| automation.Definition | TrashBatchId | operations.TrashBatch | 0..1 | OwnerId + key | NO ACTION; permission still required |
| automation.DefinitionVersion | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| automation.DefinitionVersion | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| automation.DefinitionVersion | DefinitionId | automation.Definition | 1 | OwnerId + key | NO ACTION; permission still required |
| automation.Schedule | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| automation.Schedule | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| automation.Schedule | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| automation.Schedule | DefinitionId | automation.Definition | 1 | OwnerId + key | NO ACTION; permission still required |
| automation.Run | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| automation.Run | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| automation.Run | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| automation.Run | DefinitionId | automation.Definition | 1 | OwnerId + key | NO ACTION; permission still required |
| automation.StepRun | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| automation.StepRun | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| automation.StepRun | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| automation.StepRun | RunId | automation.Run | 1 | OwnerId + key | NO ACTION; permission still required |
| automation.Connection | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| automation.Connection | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| automation.Connection | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| automation.Connection | VaultResourceId | platform.Resource | 0..1 | OwnerId + key | NO ACTION; permission still required |
| automation.Webhook | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| automation.Webhook | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| automation.Webhook | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| automation.Webhook | ConnectionId | automation.Connection | 0..1 | OwnerId + key | NO ACTION; permission still required |
| automation.Webhook | SigningVaultResourceId | platform.Resource | 1 | OwnerId + key | NO ACTION; permission still required |
| automation.WebhookDelivery | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| automation.WebhookDelivery | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| automation.WebhookDelivery | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| automation.WebhookDelivery | WebhookId | automation.Webhook | 1 | OwnerId + key | NO ACTION; permission still required |
| monitoring.Monitor | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| monitoring.Monitor | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| monitoring.Monitor | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| monitoring.Observation | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| monitoring.Observation | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| monitoring.Observation | MonitorId | monitoring.Monitor | 1 | OwnerId + key | NO ACTION; permission still required |
| monitoring.Incident | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| monitoring.Incident | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| monitoring.Incident | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| monitoring.Incident | MonitorId | monitoring.Monitor | 1 | OwnerId + key | NO ACTION; permission still required |
| monitoring.Incident | OpeningObservationId | monitoring.Observation | 1 | OwnerId + key | NO ACTION; permission still required |
| monitoring.Incident | ClosingObservationId | monitoring.Observation | 0..1 | OwnerId + key | NO ACTION; permission still required |
| assets.PersonalAsset | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| assets.PersonalAsset | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| assets.PersonalAsset | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| assets.PersonalAsset | PurchaseResourceId | platform.Resource | 0..1 | OwnerId + key | NO ACTION; permission still required |
| assets.PersonalAsset | FinanceResourceId | platform.Resource | 0..1 | OwnerId + key | NO ACTION; permission still required |
| assets.PersonalAsset | VaultResourceId | platform.Resource | 0..1 | OwnerId + key | NO ACTION; permission still required |
| assets.PersonalAsset | TrashBatchId | operations.TrashBatch | 0..1 | OwnerId + key | NO ACTION; permission still required |
| assets.PersonalAsset | InvoiceFileId | files.FileObject | 0..1 | OwnerId + key | NO ACTION; permission still required |
| assets.AssetAccessory | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| assets.AssetAccessory | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| assets.AssetAccessory | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| assets.AssetAccessory | ParentAssetId | assets.PersonalAsset | 1 | OwnerId + key | NO ACTION; permission still required |
| assets.AssetAccessory | ChildAssetId | assets.PersonalAsset | 1 | OwnerId + key | NO ACTION; permission still required |
| assets.AssetWarranty | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| assets.AssetWarranty | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| assets.AssetWarranty | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| assets.AssetWarranty | AssetId | assets.PersonalAsset | 1 | OwnerId + key | NO ACTION; permission still required |
| assets.AssetWarranty | ShoppingWarrantyResourceId | platform.Resource | 0..1 | OwnerId + key | NO ACTION; permission still required |
| assets.Repair | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| assets.Repair | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| assets.Repair | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| assets.Repair | AssetId | assets.PersonalAsset | 1 | OwnerId + key | NO ACTION; permission still required |
| assets.AssetLoan | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| assets.AssetLoan | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| assets.AssetLoan | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| assets.AssetLoan | AssetId | assets.PersonalAsset | 1 | OwnerId + key | NO ACTION; permission still required |
| assets.AssetVersion | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| assets.AssetVersion | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| assets.AssetVersion | AssetId | assets.PersonalAsset | 1 | OwnerId + key | NO ACTION; permission still required |
| assets.DigitalAsset | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| assets.DigitalAsset | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| assets.DigitalAsset | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| assets.DigitalAsset | VaultResourceId | platform.Resource | 0..1 | OwnerId + key | NO ACTION; permission still required |
| assets.DigitalAsset | TrashBatchId | operations.TrashBatch | 0..1 | OwnerId + key | NO ACTION; permission still required |
| assets.DomainDetail | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| assets.DomainDetail | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| assets.DomainDetail | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| assets.DomainDetail | DigitalAssetId | assets.DigitalAsset | 1 | OwnerId + key | NO ACTION; permission still required |
| assets.HostingDetail | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| assets.HostingDetail | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| assets.HostingDetail | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| assets.HostingDetail | DigitalAssetId | assets.DigitalAsset | 1 | OwnerId + key | NO ACTION; permission still required |
| assets.VpsDetail | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| assets.VpsDetail | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| assets.VpsDetail | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| assets.VpsDetail | DigitalAssetId | assets.DigitalAsset | 1 | OwnerId + key | NO ACTION; permission still required |
| assets.CertificateDetail | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| assets.CertificateDetail | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| assets.CertificateDetail | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| assets.CertificateDetail | DigitalAssetId | assets.DigitalAsset | 1 | OwnerId + key | NO ACTION; permission still required |
| assets.LicenseDetail | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| assets.LicenseDetail | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| assets.LicenseDetail | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| assets.LicenseDetail | DigitalAssetId | assets.DigitalAsset | 1 | OwnerId + key | NO ACTION; permission still required |
| assets.LicenseDetail | LicenseSecretResourceId | platform.Resource | 0..1 | OwnerId + key | NO ACTION; permission still required |
| assets.LicenseDetail | DeviceResourceId | platform.Resource | 0..1 | OwnerId + key | NO ACTION; permission still required |
| assets.ServiceDetail | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| assets.ServiceDetail | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| assets.ServiceDetail | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| assets.ServiceDetail | DigitalAssetId | assets.DigitalAsset | 1 | OwnerId + key | NO ACTION; permission still required |
| assets.ServiceDetail | FinanceSubscriptionResourceId | platform.Resource | 0..1 | OwnerId + key | NO ACTION; permission still required |
| assets.RenewalRecord | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| assets.RenewalRecord | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| assets.RenewalRecord | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| assets.RenewalRecord | DigitalAssetId | assets.DigitalAsset | 1 | OwnerId + key | NO ACTION; permission still required |
| career.Company | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| career.Company | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| career.Company | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| career.Company | MergedIntoId | career.Company | 0..1 | OwnerId + key | NO ACTION; permission still required |
| career.JobApplication | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| career.JobApplication | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| career.JobApplication | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| career.JobApplication | CompanyId | career.Company | 0..1 | OwnerId + key | NO ACTION; permission still required |
| career.JobApplication | ResumeVersionReference | career.ResumeVersion | 0..1 | OwnerId + key | NO ACTION; permission still required |
| career.JobApplication | TrashBatchId | operations.TrashBatch | 0..1 | OwnerId + key | NO ACTION; permission still required |
| career.Interview | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| career.Interview | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| career.Interview | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| career.Interview | ApplicationId | career.JobApplication | 1 | OwnerId + key | NO ACTION; permission still required |
| career.Interview | CalendarResourceId | platform.Resource | 0..1 | OwnerId + key | NO ACTION; permission still required |
| career.ApplicationEvent | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| career.ApplicationEvent | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| career.ApplicationEvent | ApplicationId | career.JobApplication | 1 | OwnerId + key | NO ACTION; permission still required |
| career.ApplicationEvent | ResumeVersionReference | career.ResumeVersion | 0..1 | OwnerId + key | NO ACTION; permission still required |
| career.Resume | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| career.Resume | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| career.Resume | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| career.Resume | TrashBatchId | operations.TrashBatch | 0..1 | OwnerId + key | NO ACTION; permission still required |
| career.ResumeVersion | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| career.ResumeVersion | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| career.ResumeVersion | ResumeId | career.Resume | 1 | OwnerId + key | NO ACTION; permission still required |
| career.ResumeVersion | FileObjectId | files.FileObject | 0..1 | OwnerId + key | NO ACTION; permission still required |
| career.ResumeVersion | DocumentResourceId | platform.Resource | 0..1 | OwnerId + key | NO ACTION; permission still required |
| career.ResumeShareVersion | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| career.ResumeShareVersion | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| career.ResumeShareVersion | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| career.ResumeShareVersion | ResumeResourceId | platform.Resource | 1 | OwnerId + key | NO ACTION; permission still required |
| career.ResumeShareVersion | ShareLinkId | security.ShareLink | 1 | OwnerId + key | NO ACTION; permission still required |
| career.ResumeShareVersion | ResumeVersionId | career.ResumeVersion | 1 | OwnerId + key | NO ACTION; permission still required |
| learning.Skill | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| learning.Skill | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| learning.Skill | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| learning.Skill | TrashBatchId | operations.TrashBatch | 0..1 | OwnerId + key | NO ACTION; permission still required |
| learning.Skill | MergedIntoId | learning.Skill | 0..1 | OwnerId + key | NO ACTION; permission still required |
| learning.SkillEvidence | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| learning.SkillEvidence | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| learning.SkillEvidence | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| learning.SkillEvidence | SkillId | learning.Skill | 1 | OwnerId + key | NO ACTION; permission still required |
| learning.SkillEvidence | ResourceId | platform.Resource | 1 | OwnerId + key | NO ACTION; permission still required |
| learning.Course | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| learning.Course | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| learning.Course | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| learning.Course | TrashBatchId | operations.TrashBatch | 0..1 | OwnerId + key | NO ACTION; permission still required |
| learning.CourseMilestone | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| learning.CourseMilestone | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| learning.CourseMilestone | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| learning.CourseMilestone | CourseId | learning.Course | 1 | OwnerId + key | NO ACTION; permission still required |
| learning.Certification | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| learning.Certification | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| learning.Certification | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| learning.Certification | CourseResourceId | platform.Resource | 0..1 | OwnerId + key | NO ACTION; permission still required |
| learning.Certification | TrashBatchId | operations.TrashBatch | 0..1 | OwnerId + key | NO ACTION; permission still required |
| learning.Plan | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| learning.Plan | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| learning.Plan | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| learning.Plan | TrashBatchId | operations.TrashBatch | 0..1 | OwnerId + key | NO ACTION; permission still required |
| learning.PlanItem | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| learning.PlanItem | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| learning.PlanItem | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| learning.PlanItem | PlanId | learning.Plan | 1 | OwnerId + key | NO ACTION; permission still required |
| learning.PlanItem | ResourceId | platform.Resource | 1 | OwnerId + key | NO ACTION; permission still required |
| learning.WorkLog | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| learning.WorkLog | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| learning.WorkLog | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| learning.WorkLog | TimeEntryResourceId | platform.Resource | 0..1 | OwnerId + key | NO ACTION; permission still required |
| learning.WorkLog | CourseResourceId | platform.Resource | 0..1 | OwnerId + key | NO ACTION; permission still required |
| learning.WorkLog | TaskResourceId | platform.Resource | 0..1 | OwnerId + key | NO ACTION; permission still required |
| learning.WorkLog | TrashBatchId | operations.TrashBatch | 0..1 | OwnerId + key | NO ACTION; permission still required |
| learning.WorkLog | ProjectResourceId | platform.Resource | 0..1 | OwnerId + key | NO ACTION; permission still required |
| identity.AccountMessageIntent | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| identity.AccountMessageIntent | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| identity.AccountMessageIntent | UserId | identity.User | 1 | key only | NO ACTION; permission still required |
| identity.AccountMessageIntent | TokenId | identity.OneTimeToken | 0..1 | key only | NO ACTION; permission still required |
| operations.SystemJob | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| operations.SystemJob | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.GoalTargetTask | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| productivity.GoalTargetTask | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.GoalTargetTask | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.GoalTargetTask | TargetId | productivity.GoalTarget | 1 | OwnerId + key | NO ACTION; permission still required |
| productivity.GoalTargetTask | TaskResourceId | platform.Resource | 1 | OwnerId + key | NO ACTION; permission still required |
| productivity.TimeEntryVersion | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| productivity.TimeEntryVersion | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| productivity.TimeEntryVersion | TimeEntryId | productivity.TimeEntry | 1 | OwnerId + key | NO ACTION; permission still required |
| discovery.Dashboard | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| discovery.Dashboard | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| discovery.Dashboard | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| learning.CertificationVersion | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| learning.CertificationVersion | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| learning.CertificationVersion | CertificationId | learning.Certification | 1 | OwnerId + key | NO ACTION; permission still required |
| assets.DigitalAssetVersion | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| assets.DigitalAssetVersion | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| assets.DigitalAssetVersion | DigitalAssetId | assets.DigitalAsset | 1 | OwnerId + key | NO ACTION; permission still required |
| operations.ResourceReminderRule | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| operations.ResourceReminderRule | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| operations.ResourceReminderRule | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| operations.ResourceReminderRule | ResourceId | platform.Resource | 1 | OwnerId + key | NO ACTION; permission still required |
| identity.MfaCredential | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| identity.MfaCredential | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| identity.MfaCredential | UserId | identity.User | 1 | key only | NO ACTION; permission still required |
| identity.RecoveryCode | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| identity.RecoveryCode | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| identity.RecoveryCode | UserId | identity.User | 1 | key only | NO ACTION; permission still required |
| assets.Component | OwnerId | platform.PersonalSpace | 1 | key only | NO ACTION; permission still required |
| assets.Component | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| assets.Component | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| assets.Component | AssetId | assets.PersonalAsset | 1 | OwnerId + key | NO ACTION; permission still required |
| platform.SystemConnection | CreatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |
| platform.SystemConnection | UpdatedByUserId | identity.User | 0..1 | key only | Actor attribution; nullable/anonymization Q-01 |


## Additional composite/version constraints

Automation.Run(OwnerId,DefinitionId,DefinitionVersion)→DefinitionVersion(OwnerId,DefinitionId,VersionNumber). Resume DocumentResourceId+DocumentVersion is a typed cross-module version pin, not a direct Documents table FK. ResourceLink.TargetVersion validates through a registered immutable-version provider. CurrentVersion pointers are verified in the same insert transaction; SQL Server has no assumed deferred constraint support. Page↔ArchiveBatch initialization inserts Page with null batch first, then batch/members/update atomically.

## Cross-module read distinction

TaskCalendar provider returns Task fields/status live and opaque CalendarUid; ManualEvent provider returns independent Events. Calendar merges by source-kind+UID and current authorization, no Task mirror table. Search is rebuildable and must recheck source. Goal/Planner/Time/Asset links use typed references, never bypass the source module being disabled. Shared Project resolver may read child Task details through Project provider, but must not expose Task history/reasons or act as a global Task-list endpoint.
