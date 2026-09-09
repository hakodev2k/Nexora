# Field classification — identity / platform

> Current specification · reconciled 2026-09-08 · Docs-only. [Previous version](../../history/20260908/snapshot/docs/design-database/classification/02-core-identity-platform.md) is historical evidence, not implementation input.

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

[Classification policy and index](../15-field-classification.md). SQL types, nullable CLR mappings and default sensitivity are design specifications, not DTO exposure permissions.

<a id="identity-user"></a>
## identity.User

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Restricted system |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| CreatedByUserId | uniqueidentifier | Guid? | Restricted system |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| UpdatedByUserId | uniqueidentifier | Guid? | Restricted system |
| RowVersion | rowversion | byte[] | Restricted system |
| Email | nvarchar(320) | string | Restricted system |
| NormalizedEmail | nvarchar(320) | string | Restricted system |
| PasswordHash | nvarchar(1024) | string | Secret/credential envelope |
| SecurityStamp | nvarchar(64) | string | Secret/credential envelope |
| State | varchar(64) | string | Restricted system |
| VerifiedAt | datetime2(7) | DateTime (UTC only)? | Restricted system |
| FailedAccessCount | int | int | Restricted system |
| LockoutUntil | datetime2(7) | DateTime (UTC only)? | Restricted system |
| DisplayName | nvarchar(100) | string | Restricted system |
| TimeZoneId | nvarchar(100) | string | Restricted system |
| Locale | nvarchar(35) | string | Private preference; vi/en, NOT NULL |
| EmailConfirmed | bit | bool | Restricted system |
| AvatarFileId | uniqueidentifier | Guid? | Restricted system |
| IsDeleted | bit | bool | Restricted identity metadata |
| DeletedAt | datetime2(7) | DateTime (UTC only)? | Restricted identity metadata |
| DeletedByUserId | uniqueidentifier | Guid? | Restricted actor metadata |

<a id="identity-session"></a>
## identity.Session

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Restricted system |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| CreatedByUserId | uniqueidentifier | Guid? | Restricted system |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| UpdatedByUserId | uniqueidentifier | Guid? | Restricted system |
| RowVersion | rowversion | byte[] | Restricted system |
| UserId | uniqueidentifier | Guid | Restricted system |
| HandleHash | binary(32) | byte[] | Secret/credential envelope |
| IssuedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| ExpiresAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| IdleExpiresAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| RevokedAt | datetime2(7) | DateTime (UTC only)? | Restricted system |
| RecentAuthenticatedAt | datetime2(7) | DateTime (UTC only)? | Restricted system |
| DeviceLabel | nvarchar(200) | string? | Restricted system |
| LastSeenAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| SecurityStamp | nvarchar(64) | string | Secret/credential envelope |

<a id="identity-onetimetoken"></a>
## identity.OneTimeToken

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Restricted system |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| CreatedByUserId | uniqueidentifier | Guid? | Restricted system |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| UpdatedByUserId | uniqueidentifier | Guid? | Restricted system |
| RowVersion | rowversion | byte[] | Restricted system |
| UserId | uniqueidentifier | Guid | Restricted system |
| Purpose | varchar(64) | string | Restricted system |
| TokenHash | binary(32) | byte[] | Secret/credential envelope |
| ExpiresAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| ConsumedAt | datetime2(7) | DateTime (UTC only)? | Restricted system |
| EmailSnapshot | nvarchar(320) | string | Restricted system |

<a id="identity-role"></a>
## identity.Role

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Restricted system |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| CreatedByUserId | uniqueidentifier | Guid? | Restricted system |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| UpdatedByUserId | uniqueidentifier | Guid? | Restricted system |
| RowVersion | rowversion | byte[] | Restricted system |
| Code | varchar(64) | string | Restricted system |
| Description | nvarchar(500) | string | Restricted system |

<a id="identity-userrole"></a>
## identity.UserRole

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Restricted system |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| CreatedByUserId | uniqueidentifier | Guid? | Restricted system |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| UpdatedByUserId | uniqueidentifier | Guid? | Restricted system |
| RowVersion | rowversion | byte[] | Restricted system |
| UserId | uniqueidentifier | Guid | Restricted system |
| RoleId | uniqueidentifier | Guid | Restricted system |

<a id="platform-personalspace"></a>
## platform.PersonalSpace

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Restricted system |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| CreatedByUserId | uniqueidentifier | Guid? | Restricted system |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| UpdatedByUserId | uniqueidentifier | Guid? | Restricted system |
| RowVersion | rowversion | byte[] | Restricted system |
| UserId | uniqueidentifier | Guid | Restricted system |
| State | varchar(64) | string | Restricted system |

<a id="platform-securityinvariant"></a>
## platform.SecurityInvariant

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Restricted system |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| CreatedByUserId | uniqueidentifier | Guid? | Restricted system |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| UpdatedByUserId | uniqueidentifier | Guid? | Restricted system |
| RowVersion | rowversion | byte[] | Restricted system |
| Code | varchar(64) | string | Restricted system |

<a id="platform-module"></a>
## platform.Module

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Restricted system |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| CreatedByUserId | uniqueidentifier | Guid? | Restricted system |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| UpdatedByUserId | uniqueidentifier | Guid? | Restricted system |
| RowVersion | rowversion | byte[] | Restricted system |
| Code | nvarchar(100) | string | Restricted system |
| DisplayName | nvarchar(200) | string | Restricted system |
| InstalledVersion | nvarchar(50) | string | Restricted system |
| ContractVersion | nvarchar(50) | string | Restricted system |
| State | varchar(64) | string | Restricted system |
| SystemEnabled | bit | bool | Restricted system |
| SharingEnabled | bit | bool | Restricted system |
| RegistrationEnabled | bit | bool | Restricted system |
| PolicyRevision | bigint | long | Restricted system |
| SharingEpoch | bigint | long | Administrative metadata |

<a id="platform-modulerelease"></a>
## platform.ModuleRelease

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Restricted system |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| ModuleId | uniqueidentifier | Guid | Restricted system |
| Version | nvarchar(50) | string | Restricted system |
| ManifestJson | nvarchar(max) | string | Restricted system |
| ArtifactDigest | binary(32) | byte[] | Restricted system |
| Compatibility | nvarchar(200) | string | Restricted system |
| RecordedAt | datetime2(7) | DateTime (UTC only) | Restricted system |

<a id="platform-moduledependency"></a>
## platform.ModuleDependency

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Restricted system |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| CreatedByUserId | uniqueidentifier | Guid? | Restricted system |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| UpdatedByUserId | uniqueidentifier | Guid? | Restricted system |
| RowVersion | rowversion | byte[] | Restricted system |
| ModuleId | uniqueidentifier | Guid | Restricted system |
| DependencyModuleId | uniqueidentifier | Guid | Restricted system |
| VersionRange | nvarchar(100) | string | Restricted system |
| Kind | varchar(64) | string | Restricted system |

<a id="platform-modulemigration"></a>
## platform.ModuleMigration

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Restricted system |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| CreatedByUserId | uniqueidentifier | Guid? | Restricted system |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| UpdatedByUserId | uniqueidentifier | Guid? | Restricted system |
| RowVersion | rowversion | byte[] | Restricted system |
| ModuleId | uniqueidentifier | Guid | Restricted system |
| MigrationKey | nvarchar(200) | string | Secret/credential envelope |
| Checksum | binary(32) | byte[] | Restricted system |
| State | varchar(64) | string | Restricted system |
| StartedAt | datetime2(7) | DateTime (UTC only)? | Restricted system |
| CompletedAt | datetime2(7) | DateTime (UTC only)? | Restricted system |
| ErrorCode | nvarchar(100) | string? | Restricted system |

<a id="platform-usermodulegrant"></a>
## platform.UserModuleGrant

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
| Enabled | bit | bool | Private owner |
| RegistrationPolicyRevision | bigint | long? | Private owner |
| GrantRevision | bigint | long | Private owner |

<a id="platform-permission"></a>
## platform.Permission

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Restricted system |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| CreatedByUserId | uniqueidentifier | Guid? | Restricted system |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| UpdatedByUserId | uniqueidentifier | Guid? | Restricted system |
| RowVersion | rowversion | byte[] | Restricted system |
| ModuleId | uniqueidentifier | Guid | Restricted system |
| Code | nvarchar(150) | string | Restricted system |
| Description | nvarchar(500) | string | Restricted system |
| Risk | varchar(64) | string | Restricted system |

<a id="platform-adminpermission"></a>
## platform.AdminPermission

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Restricted system |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| CreatedByUserId | uniqueidentifier | Guid? | Restricted system |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| UpdatedByUserId | uniqueidentifier | Guid? | Restricted system |
| RowVersion | rowversion | byte[] | Restricted system |
| UserId | uniqueidentifier | Guid | Restricted system |
| PermissionId | uniqueidentifier | Guid | Restricted system |
| Effect | varchar(64) | string | Restricted system |

<a id="platform-resourcetype"></a>
## platform.ResourceType

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Restricted system |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| CreatedByUserId | uniqueidentifier | Guid? | Restricted system |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| UpdatedByUserId | uniqueidentifier | Guid? | Restricted system |
| RowVersion | rowversion | byte[] | Restricted system |
| ModuleId | uniqueidentifier | Guid | Restricted system |
| Code | nvarchar(100) | string | Restricted system |
| ContractVersion | nvarchar(50) | string | Restricted system |
| CapabilitiesJson | nvarchar(max) | string | Restricted system |

<a id="platform-resource"></a>
## platform.Resource

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| ResourceTypeId | uniqueidentifier | Guid | Private owner |
| Availability | varchar(64) | string | Private owner |
| Revision | bigint | long | Private owner |
| PurgedAt | datetime2(7) | DateTime (UTC only)? | Private owner |

<a id="platform-resourcelink"></a>
## platform.ResourceLink

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| SourceResourceId | uniqueidentifier | Guid | Private owner |
| TargetResourceId | uniqueidentifier | Guid | Private owner |
| RelationType | nvarchar(100) | string | Private owner |
| TargetVersion | bigint | long? | Private owner |
| State | varchar(64) | string | Private owner |

<a id="platform-preference"></a>
## platform.Preference

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| ModuleId | uniqueidentifier | Guid? | Private owner |
| PreferenceKey | nvarchar(100) | string | Secret/credential envelope |
| SchemaVersion | int | int | Private owner |
| ValueJson | nvarchar(max) | string | Private owner |

<a id="identity-accountmessageintent"></a>
## identity.AccountMessageIntent

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Restricted system |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| CreatedByUserId | uniqueidentifier | Guid? | Restricted system |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| UpdatedByUserId | uniqueidentifier | Guid? | Restricted system |
| RowVersion | rowversion | byte[] | Restricted system |
| UserId | uniqueidentifier | Guid | Restricted system |
| TokenId | uniqueidentifier | Guid? | Restricted system |
| Kind | varchar(64) | string | Restricted system |
| IntentKey | nvarchar(200) | string | Secret/credential envelope |
| State | varchar(64) | string | Restricted system |
| DueAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| ErrorCode | nvarchar(100) | string? | Restricted system |
| DeliveryEnvelope | varbinary(max) | byte[]? | Secret/credential envelope |

<a id="identity-mfacredential"></a>
## identity.MfaCredential

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Restricted system |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| CreatedByUserId | uniqueidentifier | Guid? | Restricted system |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| UpdatedByUserId | uniqueidentifier | Guid? | Restricted system |
| RowVersion | rowversion | byte[] | Restricted system |
| UserId | uniqueidentifier | Guid | Restricted system |
| Method | varchar(64) | string | Restricted system |
| SecretEnvelope | varbinary(max) | byte[] | Secret/credential envelope |
| ConfirmedAt | datetime2(7) | DateTime (UTC only)? | Restricted system |
| DisabledAt | datetime2(7) | DateTime (UTC only)? | Restricted system |
| LastAcceptedStep | bigint | long? | Restricted system |

<a id="identity-recoverycode"></a>
## identity.RecoveryCode

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Restricted system |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| CreatedByUserId | uniqueidentifier | Guid? | Restricted system |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| UpdatedByUserId | uniqueidentifier | Guid? | Restricted system |
| RowVersion | rowversion | byte[] | Restricted system |
| UserId | uniqueidentifier | Guid | Restricted system |
| BatchId | uniqueidentifier | Guid | Restricted system |
| CodeHash | binary(32) | byte[] | Secret/credential envelope |
| UsedAt | datetime2(7) | DateTime (UTC only)? | Restricted system |
| RevokedAt | datetime2(7) | DateTime (UTC only)? | Restricted system |

<a id="platform-systemconnection"></a>
## platform.SystemConnection

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Restricted system |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| CreatedByUserId | uniqueidentifier | Guid? | Restricted system |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| UpdatedByUserId | uniqueidentifier | Guid? | Restricted system |
| RowVersion | rowversion | byte[] | Restricted system |
| ProviderKey | nvarchar(150) | string | Secret/credential envelope |
| Title | nvarchar(200) | string | Restricted system |
| EndpointEnvelope | varbinary(max) | byte[]? | Secret/credential envelope |
| SecretReference | nvarchar(300) | string | Restricted system |
| ConfigSchemaVersion | int | int | Restricted system |
| SettingsJson | nvarchar(max) | string | Restricted system |
| State | varchar(64) | string | Restricted system |
