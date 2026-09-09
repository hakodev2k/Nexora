# Field classification — files / notifications / operations

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

[Classification policy and index](../15-field-classification.md). SQL types, nullable CLR mappings and default sensitivity are design specifications, not DTO exposure permissions.

<a id="files-fileobject"></a>
## files.FileObject

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| StorageKey | nvarchar(500) | string | Secret/credential envelope |
| OriginalName | nvarchar(255) | string | Private owner |
| MediaType | nvarchar(100) | string | Private owner |
| ByteLength | bigint | long | Private owner |
| Digest | binary(32) | byte[] | Private owner |
| ScanState | varchar(64) | string | Private owner |
| Lifecycle | varchar(64) | string | Private owner |
| CurrentRevision | bigint | long | Private owner |

<a id="files-filereference"></a>
## files.FileReference

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| FileObjectId | uniqueidentifier | Guid | Private owner |
| ResourceId | uniqueidentifier | Guid | Private owner |
| VersionNumber | bigint | long? | Private owner |
| Purpose | varchar(64) | string | Private owner |
| ReferenceKey | nvarchar(200) | string | Secret/credential envelope |

<a id="files-uploadsession"></a>
## files.UploadSession

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| FileObjectId | uniqueidentifier | Guid? | Private owner |
| UploadHandleHash | binary(32) | byte[] | Secret/credential envelope |
| ExpectedBytes | bigint | long | Private owner |
| ReceivedBytes | bigint | long | Private owner |
| State | varchar(64) | string | Private owner |
| ExpiresAt | datetime2(7) | DateTime (UTC only) | Private owner |
| ErrorCode | nvarchar(100) | string? | Private owner |

<a id="notifications-notification"></a>
## notifications.Notification

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| IntentKey | nvarchar(200) | string | Secret/credential envelope |
| Category | varchar(64) | string | Private owner |
| ResourceId | uniqueidentifier | Guid? | Private owner |
| Title | nvarchar(200) | string | Private owner |
| Body | nvarchar(2000) | string | Private owner |
| RouteKey | nvarchar(200) | string? | Secret/credential envelope |
| ReadAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| DeletedAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| OccurredAt | datetime2(7) | DateTime (UTC only) | Private owner |

<a id="notifications-delivery"></a>
## notifications.Delivery

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| NotificationId | uniqueidentifier | Guid | Private owner |
| Channel | varchar(64) | string | Private owner |
| State | varchar(64) | string | Private owner |
| AttemptCount | int | int | Private owner |
| NextAttemptAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| AcceptedAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| ErrorCode | nvarchar(100) | string? | Private owner |

<a id="notifications-deliveryattempt"></a>
## notifications.DeliveryAttempt

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| DeliveryId | uniqueidentifier | Guid | Private owner |
| AttemptNumber | int | int | Private owner |
| StartedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| FinishedAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| Outcome | varchar(64) | string | Private owner |
| ProviderMessageReference | nvarchar(200) | string? | Private owner |
| ErrorCode | nvarchar(100) | string? | Private owner |

<a id="notifications-pushsubscription"></a>
## notifications.PushSubscription

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| EndpointHash | binary(32) | byte[] | Secret/credential envelope |
| EncryptedEndpoint | varbinary(max) | byte[] | Secret/credential envelope |
| KeyReference | nvarchar(200) | string | Secret/credential envelope |
| BrowserLabel | nvarchar(200) | string | Private owner |
| RevokedAt | datetime2(7) | DateTime (UTC only)? | Private owner |

<a id="operations-idempotency"></a>
## operations.Idempotency

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| Scope | nvarchar(150) | string | Private owner |
| KeyHash | binary(32) | byte[] | Secret/credential envelope |
| RequestDigest | binary(32) | byte[] | Private owner |
| State | varchar(64) | string | Private owner |
| ResultReference | nvarchar(500) | string? | Private owner |
| ExpiresAt | datetime2(7) | DateTime (UTC only) | Private owner |

<a id="operations-outbox"></a>
## operations.Outbox

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| ModuleId | uniqueidentifier | Guid | Private owner |
| EventKey | nvarchar(200) | string | Secret/credential envelope |
| EventType | nvarchar(150) | string | Private owner |
| SchemaVersion | int | int | Private owner |
| ResourceId | uniqueidentifier | Guid? | Private owner |
| SourceRevision | bigint | long? | Private owner |
| PayloadJson | nvarchar(max) | string | Private owner |
| State | varchar(64) | string | Private owner |
| AvailableAt | datetime2(7) | DateTime (UTC only) | Private owner |
| LeaseUntil | datetime2(7) | DateTime (UTC only)? | Private owner |
| Attempts | int | int | Private owner |

<a id="operations-inboxreceipt"></a>
## operations.InboxReceipt

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| Consumer | nvarchar(150) | string | Private owner |
| EventKey | nvarchar(200) | string | Secret/credential envelope |
| ProcessedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| ResultReference | nvarchar(300) | string? | Private owner |

<a id="operations-job"></a>
## operations.Job

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| ModuleId | uniqueidentifier | Guid | Private owner |
| HandlerKey | nvarchar(150) | string | Secret/credential envelope |
| SourceResourceId | uniqueidentifier | Guid? | Private owner |
| SourceRevision | bigint | long? | Private owner |
| IntentKey | nvarchar(200) | string | Secret/credential envelope |
| ArgumentsJson | nvarchar(max) | string | Private owner |
| DueAt | datetime2(7) | DateTime (UTC only) | Private owner |
| State | varchar(64) | string | Private owner |
| LeaseOwner | nvarchar(100) | string? | Private owner |
| LeaseUntil | datetime2(7) | DateTime (UTC only)? | Private owner |
| AttemptCount | int | int | Private owner |
| LastErrorCode | nvarchar(100) | string? | Private owner |

<a id="operations-jobattempt"></a>
## operations.JobAttempt

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| JobId | uniqueidentifier | Guid | Private owner |
| AttemptNumber | int | int | Private owner |
| StartedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| EndedAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| Outcome | varchar(64) | string | Private owner |
| Summary | nvarchar(1000) | string? | Private owner |
| CorrelationId | uniqueidentifier | Guid | Private owner |

<a id="operations-trashbatch"></a>
## operations.TrashBatch

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| RootResourceId | uniqueidentifier | Guid | Private owner |
| DeletedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| RestoredAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| State | varchar(64) | string | Private owner |

<a id="operations-trashmember"></a>
## operations.TrashMember

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| BatchId | uniqueidentifier | Guid | Private owner |
| ResourceId | uniqueidentifier | Guid | Private owner |
| ParentResourceId | uniqueidentifier | Guid? | Private owner |
| PreviousLifecycle | varchar(64) | string | Private owner |
| Depth | int | int | Private owner |
| PurgedAt | datetime2(7) | DateTime (UTC only)? | Private owner |

<a id="operations-importbatch"></a>
## operations.ImportBatch

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| ModuleId | uniqueidentifier | Guid | Private owner |
| Format | varchar(64) | string | Private owner |
| FileObjectId | uniqueidentifier | Guid | Private owner |
| State | varchar(64) | string | Private owner |
| OptionsJson | nvarchar(max) | string | Private owner |
| AcceptedCount | int | int | Private owner |
| SkippedCount | int | int | Private owner |
| AppliedCount | int | int | Private owner |

<a id="operations-importrow"></a>
## operations.ImportRow

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| BatchId | uniqueidentifier | Guid | Private owner |
| RowNumber | int | int | Private owner |
| ExternalKeyHash | binary(32) | byte[]? | Secret/credential envelope |
| Outcome | varchar(64) | string | Private owner |
| ReasonCode | nvarchar(100) | string? | Sensitive personal |
| ResultResourceId | uniqueidentifier | Guid? | Private owner |
| CandidateJson | nvarchar(max) | string? | Private owner |

<a id="operations-exportjob"></a>
## operations.ExportJob

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| ModuleId | uniqueidentifier | Guid | Private owner |
| Format | varchar(64) | string | Private owner |
| FilterJson | nvarchar(max) | string | Private owner |
| State | varchar(64) | string | Private owner |
| FileObjectId | uniqueidentifier | Guid? | Private owner |
| ExpiresAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| SourceWatermark | nvarchar(200) | string? | Private owner |

<a id="operations-backuprun"></a>
## operations.BackupRun

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Restricted system |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| CreatedByUserId | uniqueidentifier | Guid? | Restricted system |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| UpdatedByUserId | uniqueidentifier | Guid? | Restricted system |
| RowVersion | rowversion | byte[] | Restricted system |
| StartedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| CompletedAt | datetime2(7) | DateTime (UTC only)? | Restricted system |
| State | varchar(64) | string | Restricted system |
| ManifestLocation | nvarchar(500) | string | Restricted system |
| DatabaseCheckpoint | nvarchar(300) | string? | Restricted system |
| ObjectManifestDigest | binary(32) | byte[]? | Restricted system |
| KeyInventoryReference | nvarchar(300) | string? | Secret/credential envelope |
| ErrorCode | nvarchar(100) | string? | Restricted system |

<a id="operations-restorerun"></a>
## operations.RestoreRun

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Restricted system |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| CreatedByUserId | uniqueidentifier | Guid? | Restricted system |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| UpdatedByUserId | uniqueidentifier | Guid? | Restricted system |
| RowVersion | rowversion | byte[] | Restricted system |
| BackupRunId | uniqueidentifier | Guid | Restricted system |
| TargetEnvironment | nvarchar(100) | string | Restricted system |
| State | varchar(64) | string | Restricted system |
| ApprovedByReference | nvarchar(100) | string? | Restricted system |
| StartedAt | datetime2(7) | DateTime (UTC only)? | Restricted system |
| CompletedAt | datetime2(7) | DateTime (UTC only)? | Restricted system |
| VerificationJson | nvarchar(max) | string | Restricted system |

<a id="operations-systemjob"></a>
## operations.SystemJob

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Restricted system |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| CreatedByUserId | uniqueidentifier | Guid? | Restricted system |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| UpdatedByUserId | uniqueidentifier | Guid? | Restricted system |
| RowVersion | rowversion | byte[] | Restricted system |
| HandlerKey | nvarchar(150) | string | Secret/credential envelope |
| IntentKey | nvarchar(200) | string | Secret/credential envelope |
| ArgumentsJson | nvarchar(max) | string | Restricted system |
| DueAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| State | varchar(64) | string | Restricted system |
| LeaseUntil | datetime2(7) | DateTime (UTC only)? | Restricted system |
| AttemptCount | int | int | Restricted system |
| ErrorCode | nvarchar(100) | string? | Restricted system |

<a id="operations-resourcereminderrule"></a>
## operations.ResourceReminderRule

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| ResourceId | uniqueidentifier | Guid | Private owner |
| RuleKey | nvarchar(100) | string | Secret/credential envelope |
| Kind | varchar(64) | string | Private owner |
| LeadDays | int | int? | Private owner |
| LocalTime | time(0) | TimeOnly? | Private owner |
| TimeZoneId | nvarchar(100) | string | Private owner |
| Enabled | bit | bool | Private owner |
| SourceRevision | bigint | long | Private owner |
