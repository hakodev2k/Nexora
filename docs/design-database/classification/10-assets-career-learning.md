# Field classification — assets / career / learning

> Current specification · reconciled 2026-09-08 · Docs-only. [Previous version](../../history/20260908/snapshot/docs/design-database/classification/10-assets-career-learning.md) is historical evidence, not implementation input.

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

[Classification policy and index](../15-field-classification.md). SQL types, nullable CLR mappings and default sensitivity are design specifications, not DTO exposure permissions.

<a id="assets-personalasset"></a>
## assets.PersonalAsset

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
| Brand | nvarchar(100) | string? | Private owner |
| Model | nvarchar(200) | string? | Private owner |
| SerialEncrypted | varbinary(max) | byte[]? | Secret/credential envelope |
| PurchasedOn | date | DateOnly? | Private owner |
| PurchaseAmount | decimal(28,8) | decimal? | Private owner |
| Currency | char(3) | string? | Private owner |
| State | varchar(64) | string | Private owner |
| Notes | nvarchar(max) | string? | Sensitive personal |
| PurchaseResourceId | uniqueidentifier | Guid? | Private owner |
| FinanceResourceId | uniqueidentifier | Guid? | Private owner |
| VaultResourceId | uniqueidentifier | Guid? | Private owner |
| TrashBatchId | uniqueidentifier | Guid? | Private owner |
| Category | nvarchar(100) | string? | Private owner |
| Seller | nvarchar(200) | string? | Private owner |
| InvoiceFileId | uniqueidentifier | Guid? | Private owner |

<a id="assets-assetaccessory"></a>
## assets.AssetAccessory

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| ParentAssetId | uniqueidentifier | Guid | Private owner |
| ChildAssetId | uniqueidentifier | Guid | Private owner |
| Notes | nvarchar(1000) | string? | Sensitive personal |

<a id="assets-assetwarranty"></a>
## assets.AssetWarranty

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| AssetId | uniqueidentifier | Guid | Private owner |
| Provider | nvarchar(200) | string? | Private owner |
| StartsOn | date | DateOnly? | Private owner |
| EndsOn | date | DateOnly? | Private owner |
| Terms | nvarchar(max) | string? | Private owner |
| ShoppingWarrantyResourceId | uniqueidentifier | Guid? | Private owner |
| CoverageKind | varchar(64) | string | Private owner |

<a id="assets-repair"></a>
## assets.Repair

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| AssetId | uniqueidentifier | Guid | Private owner |
| Title | nvarchar(200) | string | Private owner |
| StartedOn | date | DateOnly | Private owner |
| CompletedOn | date | DateOnly? | Private owner |
| Cost | decimal(28,8) | decimal? | Private owner |
| Currency | char(3) | string? | Private owner |
| Provider | nvarchar(200) | string? | Private owner |
| Notes | nvarchar(max) | string? | Sensitive personal |
| Result | nvarchar(2000) | string? | Private owner |

<a id="assets-assetloan"></a>
## assets.AssetLoan

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| AssetId | uniqueidentifier | Guid | Private owner |
| Borrower | nvarchar(200) | string | Private owner |
| LentOn | date | DateOnly | Private owner |
| ExpectedReturnOn | date | DateOnly? | Private owner |
| ReturnedOn | date | DateOnly? | Private owner |
| Notes | nvarchar(2000) | string? | Sensitive personal |

<a id="assets-assetversion"></a>
## assets.AssetVersion

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| AssetId | uniqueidentifier | Guid | Private owner |
| VersionNumber | bigint | long | Private owner |
| SafeSnapshotJson | nvarchar(max) | string | Private owner |
| SensitiveSnapshotEncrypted | varbinary(max) | byte[]? | Secret/credential envelope |
| Reason | nvarchar(2000) | string? | Sensitive personal |

<a id="assets-digitalasset"></a>
## assets.DigitalAsset

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
| Provider | nvarchar(200) | string? | Private owner |
| State | varchar(64) | string | Private owner |
| ExpiresOn | date | DateOnly? | Private owner |
| ObservedExpiryAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| ObservedAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| VaultResourceId | uniqueidentifier | Guid? | Private owner |
| Notes | nvarchar(max) | string? | Sensitive personal |
| TrashBatchId | uniqueidentifier | Guid? | Private owner |
| PreArchiveState | varchar(64) | string? | Private owner |
| Cost | decimal(28,8) | decimal? | Private owner |
| Currency | char(3) | string? | Private owner |
| RenewalCycle | nvarchar(100) | string? | Private owner |

<a id="assets-domaindetail"></a>
## assets.DomainDetail

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| DigitalAssetId | uniqueidentifier | Guid | Private owner |
| AsciiName | nvarchar(253) | string | Private owner |
| UnicodeName | nvarchar(253) | string | Private owner |
| Registrar | nvarchar(200) | string? | Private owner |
| AutoRenewRecorded | bit | bool? | Private owner |
| RegisteredOn | date | DateOnly? | Private owner |
| NameserverNotes | nvarchar(max) | string? | Sensitive personal |

<a id="assets-hostingdetail"></a>
## assets.HostingDetail

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| DigitalAssetId | uniqueidentifier | Guid | Private owner |
| Plan | nvarchar(200) | string? | Private owner |
| ControlPanelUrl | nvarchar(2048) | string? | Private owner |
| StorageLimitBytes | bigint | long? | Private owner |
| DomainName | nvarchar(253) | string? | Private owner |
| Region | nvarchar(100) | string? | Private owner |

<a id="assets-vpsdetail"></a>
## assets.VpsDetail

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| DigitalAssetId | uniqueidentifier | Guid | Private owner |
| HostName | nvarchar(253) | string? | Private owner |
| IpAddress | nvarchar(45) | string? | Private owner |
| CpuCount | int | int? | Private owner |
| MemoryMiB | bigint | long? | Private owner |
| OperatingSystem | nvarchar(200) | string? | Private owner |
| Plan | nvarchar(200) | string? | Private owner |
| Region | nvarchar(100) | string? | Private owner |

<a id="assets-certificatedetail"></a>
## assets.CertificateDetail

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| DigitalAssetId | uniqueidentifier | Guid | Private owner |
| Subject | nvarchar(500) | string | Private owner |
| Issuer | nvarchar(500) | string? | Private owner |
| Fingerprint | nvarchar(200) | string? | Private owner |
| NotBefore | datetime2(7) | DateTime (UTC only)? | Private owner |
| NotAfter | datetime2(7) | DateTime (UTC only)? | Private owner |
| HostName | nvarchar(253) | string? | Private owner |
| SubjectAlternativeNamesJson | nvarchar(max) | string? | Private owner |
| Source | varchar(64) | string | Private owner |

<a id="assets-licensedetail"></a>
## assets.LicenseDetail

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| DigitalAssetId | uniqueidentifier | Guid | Private owner |
| Product | nvarchar(200) | string | Private owner |
| Seats | int | int? | Private owner |
| LicenseSecretResourceId | uniqueidentifier | Guid? | Private owner |
| PurchasedOn | date | DateOnly? | Private owner |
| Vendor | nvarchar(200) | string? | Private owner |
| Edition | nvarchar(200) | string? | Private owner |
| DeviceResourceId | uniqueidentifier | Guid? | Private owner |

<a id="assets-servicedetail"></a>
## assets.ServiceDetail

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| DigitalAssetId | uniqueidentifier | Guid | Private owner |
| ServiceUrl | nvarchar(2048) | string? | Private owner |
| Plan | nvarchar(200) | string? | Private owner |
| AccountLabelEncrypted | varbinary(max) | byte[]? | Secret/credential envelope |
| FinanceSubscriptionResourceId | uniqueidentifier | Guid? | Private owner |

<a id="assets-renewalrecord"></a>
## assets.RenewalRecord

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| DigitalAssetId | uniqueidentifier | Guid | Private owner |
| RenewedOn | date | DateOnly | Private owner |
| PreviousExpiry | date | DateOnly? | Private owner |
| NewExpiry | date | DateOnly | Private owner |
| Amount | decimal(28,8) | decimal? | Private owner |
| Currency | char(3) | string? | Private owner |
| Notes | nvarchar(2000) | string? | Sensitive personal |

<a id="career-company"></a>
## career.Company

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
| Url | nvarchar(2048) | string? | Private owner |
| Location | nvarchar(200) | string? | Private owner |
| ContactEncrypted | varbinary(max) | byte[]? | Secret/credential envelope |
| Notes | nvarchar(max) | string? | Sensitive personal |
| MergedIntoId | uniqueidentifier | Guid? | Private owner |
| Industry | nvarchar(200) | string? | Private owner |

<a id="career-jobapplication"></a>
## career.JobApplication

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
| CompanyId | uniqueidentifier | Guid? | Private owner |
| Url | nvarchar(2048) | string? | Private owner |
| Stage | varchar(64) | string | Private owner |
| AppliedOn | date | DateOnly? | Private owner |
| SalaryMin | decimal(28,8) | decimal? | Sensitive personal |
| SalaryMax | decimal(28,8) | decimal? | Sensitive personal |
| Currency | char(3) | string? | Private owner |
| Notes | nvarchar(max) | string? | Sensitive personal |
| ResumeVersionReference | uniqueidentifier | Guid? | Private owner |
| TrashBatchId | uniqueidentifier | Guid? | Private owner |
| Location | nvarchar(200) | string? | Private owner |
| WorkMode | varchar(64) | string? | Private owner |
| EmploymentType | nvarchar(100) | string? | Private owner |
| SalaryText | nvarchar(1000) | string? | Sensitive personal |
| Description | nvarchar(max) | string? | Private owner |
| Source | nvarchar(200) | string? | Private owner |

<a id="career-interview"></a>
## career.Interview

Retired proposal; no current Interview columns. Use career.CalendarLink current domain dictionary.

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| ApplicationId | uniqueidentifier | Guid | Private owner |
| Title | nvarchar(200) | string | Private owner |
| StartAt | datetime2(7) | DateTime (UTC only) | Private owner |
| EndAt | datetime2(7) | DateTime (UTC only) | Private owner |
| TimeZoneId | nvarchar(100) | string | Private owner |
| Status | varchar(64) | string | Private owner |
| Location | nvarchar(500) | string? | Private owner |
| NotesEncrypted | varbinary(max) | byte[]? | Secret/credential envelope |
| CalendarResourceId | uniqueidentifier | Guid? | Private owner |
| RoundLabel | nvarchar(100) | string? | Private owner |
| InterviewType | nvarchar(100) | string? | Private owner |
| MeetingUrl | nvarchar(2048) | string? | Private owner |
| ParticipantsEncrypted | varbinary(max) | byte[]? | Secret/credential envelope |

<a id="career-applicationevent"></a>
## career.ApplicationEvent

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| ApplicationId | uniqueidentifier | Guid | Private owner |
| FromStage | varchar(64) | string? | Private owner |
| ToStage | varchar(64) | string | Private owner |
| OccurredAt | datetime2(7) | DateTime (UTC only) | Private owner |
| Note | nvarchar(2000) | string? | Private owner |
| ResumeVersionReference | uniqueidentifier | Guid? | Private owner |
| CompanyLabelSnapshot | nvarchar(200) | string? | Private owner |

<a id="career-resume"></a>
## career.Resume

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
| Status | varchar(64) | string | Private owner |
| TrashBatchId | uniqueidentifier | Guid? | Private owner |
| Language | nvarchar(35) | string? | Private owner |

<a id="career-resumeversion"></a>
## career.ResumeVersion

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| ResumeId | uniqueidentifier | Guid | Private owner |
| VersionNumber | bigint | long | Private owner |
| FileObjectId | uniqueidentifier | Guid? | Private owner |
| Label | nvarchar(200) | string? | Private owner |
| ContentDigest | binary(32) | byte[] | Private owner |
| DocumentResourceId | uniqueidentifier | Guid? | Private owner |
| DocumentVersion | bigint | long? | Private owner |

<a id="career-resumeshareversion"></a>
## career.ResumeShareVersion

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| ResumeResourceId | uniqueidentifier | Guid | Private owner |
| ShareLinkId | uniqueidentifier | Guid | Private owner |
| ResumeVersionId | uniqueidentifier | Guid | Private owner |

<a id="learning-skill"></a>
## learning.Skill

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
| Level | varchar(64) | string | Private owner |
| Description | nvarchar(max) | string? | Private owner |
| Status | varchar(64) | string | Private owner |
| TrashBatchId | uniqueidentifier | Guid? | Private owner |
| NormalizedTitle | nvarchar(200) | string | Private owner |
| MergedIntoId | uniqueidentifier | Guid? | Private owner |
| PreArchiveState | varchar(64) | string? | Private owner |

<a id="learning-skillevidence"></a>
## learning.SkillEvidence

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| SkillId | uniqueidentifier | Guid | Private owner |
| ResourceId | uniqueidentifier | Guid | Private owner |
| Note | nvarchar(1000) | string? | Private owner |

<a id="learning-course"></a>
## learning.Course

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
| Provider | nvarchar(200) | string? | Private owner |
| Url | nvarchar(2048) | string? | Private owner |
| ProgressMode | varchar(64) | string | Private owner |
| ManualProgress | decimal(28,8) | decimal? | Private owner |
| Status | varchar(64) | string | Private owner |
| StartedOn | date | DateOnly? | Private owner |
| CompletedOn | date | DateOnly? | Private owner |
| Notes | nvarchar(max) | string? | Sensitive personal |
| TrashBatchId | uniqueidentifier | Guid? | Private owner |
| PreArchiveState | varchar(64) | string? | Private owner |

<a id="learning-coursemilestone"></a>
## learning.CourseMilestone

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| CourseId | uniqueidentifier | Guid | Private owner |
| Title | nvarchar(200) | string | Private owner |
| Position | int | int | Private owner |
| Completed | bit | bool | Private owner |
| CompletedAt | datetime2(7) | DateTime (UTC only)? | Private owner |

<a id="learning-certification"></a>
## learning.Certification

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
| Issuer | nvarchar(200) | string | Private owner |
| IssuedOn | date | DateOnly? | Private owner |
| ExpiresOn | date | DateOnly? | Private owner |
| CredentialIdEncrypted | varbinary(max) | byte[]? | Secret/credential envelope |
| VerificationUrl | nvarchar(2048) | string? | Private owner |
| CourseResourceId | uniqueidentifier | Guid? | Private owner |
| Status | varchar(64) | string | Private owner |
| TrashBatchId | uniqueidentifier | Guid? | Private owner |
| PreArchiveState | varchar(64) | string? | Private owner |

<a id="learning-plan"></a>
## learning.Plan

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
| StartsOn | date | DateOnly? | Private owner |
| EndsOn | date | DateOnly? | Private owner |
| Status | varchar(64) | string | Private owner |
| Notes | nvarchar(max) | string? | Sensitive personal |
| TrashBatchId | uniqueidentifier | Guid? | Private owner |
| PreArchiveState | varchar(64) | string? | Private owner |

<a id="learning-planitem"></a>
## learning.PlanItem

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| PlanId | uniqueidentifier | Guid | Private owner |
| ResourceId | uniqueidentifier | Guid | Private owner |
| Position | int | int | Private owner |
| TargetOn | date | DateOnly? | Private owner |

<a id="learning-worklog"></a>
## learning.WorkLog

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
| RecordedOn | date | DateOnly | Private owner |
| Notes | nvarchar(max) | string? | Sensitive personal |
| Minutes | int | int? | Private owner |
| TimeEntryResourceId | uniqueidentifier | Guid? | Private owner |
| CourseResourceId | uniqueidentifier | Guid? | Private owner |
| TaskResourceId | uniqueidentifier | Guid? | Private owner |
| TrashBatchId | uniqueidentifier | Guid? | Private owner |
| Category | nvarchar(100) | string? | Private owner |
| ProjectResourceId | uniqueidentifier | Guid? | Private owner |
| Employer | nvarchar(200) | string? | Private owner |
| Status | varchar(64) | string | Private owner |
| PreArchiveState | varchar(64) | string? | Private owner |

<a id="learning-certificationversion"></a>
## learning.CertificationVersion

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| CertificationId | uniqueidentifier | Guid | Private owner |
| VersionNumber | bigint | long | Private owner |
| SafeSnapshotJson | nvarchar(max) | string | Private owner |
| SensitiveSnapshotEncrypted | varbinary(max) | byte[]? | Secret/credential envelope |
| Reason | nvarchar(2000) | string? | Sensitive personal |

<a id="assets-digitalassetversion"></a>
## assets.DigitalAssetVersion

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| DigitalAssetId | uniqueidentifier | Guid | Private owner |
| VersionNumber | bigint | long | Private owner |
| SnapshotJson | nvarchar(max) | string | Private owner |
| Reason | nvarchar(2000) | string? | Sensitive personal |

<a id="assets-component"></a>
## assets.Component

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| AssetId | uniqueidentifier | Guid | Private owner |
| Title | nvarchar(200) | string | Private owner |
| Kind | nvarchar(100) | string? | Private owner |
| Model | nvarchar(200) | string? | Private owner |
| SerialEnvelope | varbinary(max) | byte[]? | Secret/credential envelope |
| Quantity | decimal(28,8) | decimal | Private owner |
| Notes | nvarchar(max) | string? | Sensitive personal |
