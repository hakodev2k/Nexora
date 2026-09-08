# Field classification — security

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

[Classification policy and index](../15-field-classification.md). SQL types, nullable CLR mappings and default sensitivity are design specifications, not DTO exposure permissions.

<a id="security-sharelink"></a>
## security.ShareLink

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
| TokenHash | binary(32) | byte[] | Secret/credential envelope |
| Mode | varchar(64) | string | Private owner |
| ExpiresAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| RevokedAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| SuspendedByTrash | bit | bool | Private owner |
| ProjectionVersion | nvarchar(50) | string | Private owner |

<a id="security-sharealloweduser"></a>
## security.ShareAllowedUser

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| ShareLinkId | uniqueidentifier | Guid | Private owner |
| UserId | uniqueidentifier | Guid | Private owner |

<a id="security-supportgrant"></a>
## security.SupportGrant

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
| DurationMode | varchar(64) | string | Private owner |
| ExpiresAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| RevokedAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| ConsentVersion | nvarchar(50) | string | Private owner |

<a id="security-accesssession"></a>
## security.AccessSession

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| ActorUserId | uniqueidentifier | Guid | Private owner |
| ModuleId | uniqueidentifier | Guid | Private owner |
| Mode | varchar(64) | string | Private owner |
| SupportGrantId | uniqueidentifier | Guid? | Private owner |
| Reason | nvarchar(1000) | string? | Sensitive personal |
| ExpiresAt | datetime2(7) | DateTime (UTC only) | Private owner |
| EndedAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| OpeningAuditId | uniqueidentifier | Guid | Private owner |

<a id="security-auditevent"></a>
## security.AuditEvent

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Restricted system |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| EventId | uniqueidentifier | Guid | Restricted system |
| ActorReference | nvarchar(100) | string | Restricted system |
| SubjectReference | nvarchar(100) | string? | Restricted system |
| ModuleCode | nvarchar(100) | string? | Restricted system |
| Action | nvarchar(150) | string | Restricted system |
| Outcome | varchar(64) | string | Restricted system |
| OccurredAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| CorrelationId | uniqueidentifier | Guid | Restricted system |
| AccessSessionReference | uniqueidentifier | Guid? | Restricted system |
| DetailsJson | nvarchar(max) | string | Restricted system |
| PreviousDigest | binary(32) | byte[]? | Restricted system |
| EventDigest | binary(32) | byte[] | Restricted system |

<a id="security-activityevent"></a>
## security.ActivityEvent

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
| Revision | bigint | long | Private owner |
| Action | nvarchar(100) | string | Private owner |
| OccurredAt | datetime2(7) | DateTime (UTC only) | Private owner |
| SummaryJson | nvarchar(max) | string | Private owner |
| AccessMode | varchar(64) | string | Private owner |
