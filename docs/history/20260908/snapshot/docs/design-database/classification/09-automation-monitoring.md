# Field classification — automation / monitoring

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

[Classification policy and index](../15-field-classification.md). SQL types, nullable CLR mappings and default sensitivity are design specifications, not DTO exposure permissions.

<a id="automation-definition"></a>
## automation.Definition

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| Title | nvarchar(200) | string | Private owner |
| CurrentVersion | bigint | long | Private owner |
| Enabled | bit | bool | Private owner |
| State | varchar(64) | string | Private owner |
| TrashBatchId | uniqueidentifier | Guid? | Private owner |

<a id="automation-definitionversion"></a>
## automation.DefinitionVersion

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| DefinitionId | uniqueidentifier | Guid | Private owner |
| VersionNumber | bigint | long | Private owner |
| SchemaVersion | int | int | Private owner |
| DefinitionJson | nvarchar(max) | string | Private owner |
| ValidationJson | nvarchar(max) | string | Private owner |
| CommandKeyHash | binary(32) | byte[] | Secret/credential envelope |

<a id="automation-schedule"></a>
## automation.Schedule

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| DefinitionId | uniqueidentifier | Guid | Private owner |
| CronExpression | nvarchar(100) | string? | Private owner |
| TimeZoneId | nvarchar(100) | string | Private owner |
| NextDueAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| LastOccurrenceKey | nvarchar(200) | string? | Secret/credential envelope |
| Enabled | bit | bool | Private owner |
| StartsAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| EndsAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| Frequency | varchar(64) | string | Private owner |
| IntervalSeconds | int | int? | Private owner |
| MissedPolicy | varchar(64) | string | Private owner |

<a id="automation-run"></a>
## automation.Run

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| DefinitionId | uniqueidentifier | Guid | Private owner |
| DefinitionVersion | bigint | long | Private owner |
| TriggerKey | nvarchar(200) | string | Secret/credential envelope |
| State | varchar(64) | string | Private owner |
| StartedAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| EndedAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| CancelRequestedAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| CorrelationId | uniqueidentifier | Guid | Private owner |

<a id="automation-steprun"></a>
## automation.StepRun

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| RunId | uniqueidentifier | Guid | Private owner |
| StepKey | nvarchar(100) | string | Secret/credential envelope |
| AttemptNumber | int | int | Private owner |
| State | varchar(64) | string | Private owner |
| EffectKey | nvarchar(200) | string | Secret/credential envelope |
| StartedAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| EndedAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| SafeResultJson | nvarchar(max) | string? | Private owner |
| ErrorCode | nvarchar(100) | string? | Private owner |

<a id="automation-connection"></a>
## automation.Connection

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| ProviderKey | nvarchar(150) | string | Secret/credential envelope |
| Title | nvarchar(200) | string | Private owner |
| VaultResourceId | uniqueidentifier | Guid? | Private owner |
| ScopesJson | nvarchar(max) | string | Private owner |
| State | varchar(64) | string | Private owner |
| LastTestAt | datetime2(7) | DateTime (UTC only)? | Private owner |

<a id="automation-webhook"></a>
## automation.Webhook

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| ConnectionId | uniqueidentifier | Guid? | Private owner |
| Direction | varchar(64) | string | Private owner |
| EndpointEncrypted | varbinary(max) | byte[] | Secret/credential envelope |
| EndpointDigest | binary(32) | byte[] | Private owner |
| SigningVaultResourceId | uniqueidentifier | Guid | Private owner |
| EventTypesJson | nvarchar(max) | string | Private owner |
| Enabled | bit | bool | Private owner |
| Revision | bigint | long | Private owner |

<a id="automation-webhookdelivery"></a>
## automation.WebhookDelivery

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| WebhookId | uniqueidentifier | Guid | Private owner |
| MessageId | nvarchar(200) | string | Private owner |
| Direction | varchar(64) | string | Private owner |
| State | varchar(64) | string | Private owner |
| ReceivedAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| LastAttemptAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| AttemptCount | int | int | Private owner |
| PayloadDigest | binary(32) | byte[] | Private owner |
| ErrorCode | nvarchar(100) | string? | Private owner |
| HttpStatus | int | int? | Private owner |
| SchemaVersion | int | int | Private owner |
| OccurredAt | datetime2(7) | DateTime (UTC only)? | Private owner |

<a id="monitoring-monitor"></a>
## monitoring.Monitor

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| Title | nvarchar(200) | string | Private owner |
| Kind | varchar(64) | string | Private owner |
| Target | nvarchar(2048) | string | Private owner |
| IntervalSeconds | int | int | Private owner |
| ExpectedStatus | int | int? | Private owner |
| Enabled | bit | bool | Private owner |
| State | varchar(64) | string | Private owner |
| LastObservedAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| HeartbeatTokenHash | binary(32) | byte[]? | Secret/credential envelope |

<a id="monitoring-observation"></a>
## monitoring.Observation

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| MonitorId | uniqueidentifier | Guid | Private owner |
| ObservedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| Outcome | varchar(64) | string | Private owner |
| LatencyMs | int | int? | Private owner |
| HttpStatus | int | int? | Private owner |
| ErrorCode | nvarchar(100) | string? | Private owner |

<a id="monitoring-incident"></a>
## monitoring.Incident

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| MonitorId | uniqueidentifier | Guid | Private owner |
| OpenedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| ResolvedAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| State | varchar(64) | string | Private owner |
| OpeningObservationId | uniqueidentifier | Guid | Private owner |
| ClosingObservationId | uniqueidentifier | Guid? | Private owner |
| NotificationIntentKey | nvarchar(200) | string | Secret/credential envelope |
