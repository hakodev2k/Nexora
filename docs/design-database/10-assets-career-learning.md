# assets / career / learning — physical data dictionary

> **Current decision amendment — 2026-09-07:** Physical delta17 is current for User/ShareLink/Vault flags, recovery wraps, four new tables and Career CalendarLink replacement. Baseline field counts/encryption/purge/Interview proposals are superseded only where specified; no migration executed. [Normative PO decisions](../requirements/10-owner-decisions-20260907.md). Conflicting older proposal paragraphs below are historical; current field/action overrides are in the linked delta. Docs-only.

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Technical design for SQL Server. Business rules retain their source status; rows marked Q are conditional proposals, not approval. Every physical column, including inherited technical columns, is expanded below. [Conventions](01-conventions-and-integrity.md) · [Relations](11-relations-and-transactions.md) · [Payload contracts](14-payload-contracts.md).

| Table | Purpose | Feature | Gate |
| --- | --- | --- | --- |
| [assets.PersonalAsset](#assets-personalasset) | Physical item owned by one User | FX-37 | Technical decision |
| [assets.AssetAccessory](#assets-assetaccessory) | Acyclic physical accessory relationship | FX-37 | Technical decision |
| [assets.AssetWarranty](#assets-assetwarranty) | Asset coverage record | FX-37 | Technical decision |
| [assets.Repair](#assets-repair) | Manual repair/service history | FX-37 | Technical decision |
| [assets.AssetLoan](#assets-assetloan) | Personal lending note, not team membership | FX-37 | Technical decision |
| [assets.AssetVersion](#assets-assetversion) | Physical asset history | FX-37 | Technical decision |
| [assets.DigitalAsset](#assets-digitalasset) | Typed online-asset metadata, not infrastructure control plane | FX-38 | Technical decision |
| [assets.DomainDetail](#assets-domaindetail) | Domain-specific metadata | FX-38 | Technical decision |
| [assets.HostingDetail](#assets-hostingdetail) | Hosting plan metadata | FX-38 | Technical decision |
| [assets.VpsDetail](#assets-vpsdetail) | Server inventory metadata only | FX-38 | Technical decision |
| [assets.CertificateDetail](#assets-certificatedetail) | Public certificate metadata | FX-38 | Technical decision |
| [assets.LicenseDetail](#assets-licensedetail) | Software license entitlement metadata | FX-38 | Technical decision |
| [assets.ServiceDetail](#assets-servicedetail) | Online-service account metadata | FX-38 | Technical decision |
| [assets.RenewalRecord](#assets-renewalrecord) | Manual metadata renewal history | FX-38 | Technical decision |
| [career.Company](#career-company) | Personal employer/contact directory | FX-39 | Technical decision |
| [career.JobApplication](#career-jobapplication) | Personal application pipeline record | FX-39 | Technical decision |
| [career.Interview](#career-interview) | Personal interview appointment metadata | FX-39 | Proposed: Q-12 Calendar link; standalone interview core |
| [career.ApplicationEvent](#career-applicationevent) | Immutable job pipeline timeline | FX-39 | Technical decision |
| [career.Resume](#career-resume) | Personal resume family and current version pointer | FX-39 | Technical decision |
| [career.ResumeVersion](#career-resumeversion) | Exact immutable uploaded resume file/version | FX-39 | Technical decision |
| [career.ResumeShareVersion](#career-resumeshareversion) | Version pin supplement to common Sharing Engine | FX-39 | Technical decision |
| [learning.Skill](#learning-skill) | Personal skill level and evidence | FX-40 | Technical decision |
| [learning.SkillEvidence](#learning-skillevidence) | Typed evidence linking existing owner resource | FX-40 | Technical decision |
| [learning.Course](#learning-course) | Personal course tracking, not LMS content hosting | FX-40 | Technical decision |
| [learning.CourseMilestone](#learning-coursemilestone) | Course progress unit | FX-40 | Technical decision |
| [learning.Certification](#learning-certification) | Personal certification and expiry evidence | FX-40 | Technical decision |
| [learning.Plan](#learning-plan) | Personal learning plan referencing existing resources | FX-40 | Technical decision |
| [learning.PlanItem](#learning-planitem) | Ordered plan resource reference | FX-40 | Technical decision |
| [learning.WorkLog](#learning-worklog) | Personal learning/work reflection | FX-40 | Technical decision |
| [learning.CertificationVersion](#learning-certificationversion) | Renewal and certification evidence history | FX-40 | Technical decision |
| [assets.DigitalAssetVersion](#assets-digitalassetversion) | Entered and observed metadata change history | FX-38 | Technical decision |
| [assets.Component](#assets-component) | Owned physical component within one Asset aggregate | FX-37 | Technical decision |

<a id="assets-personalasset"></a>
## assets.PersonalAsset

Physical item owned by one User. Profile **R**. Status: **Technical decision**.

| Field | SQL Server type | Nullable | Meaning / validation | Key / default / reference |
| --- | --- | --- | --- | --- |
| Id | uniqueidentifier | No | PK and same identity as platform.Resource.Id; generated by trusted command | No implicit default unless stated |
| OwnerId | uniqueidentifier | No | Required isolation boundary resolved server-side, immutable | FK OwnerId → Id; [platform.PersonalSpace](02-core-identity-platform.md#platform-personalspace); NO ACTION |
| CreatedAt | datetime2(7) | No | Server insert instant, DEFAULT SYSUTCDATETIME() | No implicit default unless stated |
| CreatedByUserId | uniqueidentifier | Yes | Actual actor for attribution, not owner; null only system/anonymized identity | FK CreatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| UpdatedAt | datetime2(7) | No | Server write instant, initially CreatedAt | No implicit default unless stated |
| UpdatedByUserId | uniqueidentifier | Yes | Actual most recent actor; null only system/anonymized identity | FK UpdatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| RowVersion | rowversion | No | SQL-generated 8-byte optimistic concurrency token; not content history or clock | DB generated; exclude from inserts/updates |
| Title | nvarchar(200) | No | Asset name | No implicit default unless stated |
| Kind | varchar(64) | No | Device, Electronics, VehicleMetadata, Other | CHECK allowed codes documented in meaning |
| Brand | nvarchar(100) | Yes | Manufacturer | No implicit default unless stated |
| Model | nvarchar(200) | Yes | Model name | No implicit default unless stated |
| SerialEncrypted | varbinary(max) | Yes | Sensitive serial number | No implicit default unless stated |
| PurchasedOn | date | Yes | Purchase date | No implicit default unless stated |
| PurchaseAmount | decimal(28,8) | Yes | Entered historical amount | No implicit default unless stated |
| Currency | char(3) | Yes | Required with amount | Uppercase registered currency; no float |
| State | varchar(64) | No | Active, Stored, Loaned, Repair, Sold, Disposed, Lost, Archived | CHECK allowed codes documented in meaning |
| Notes | nvarchar(max) | Yes | Owner description | No implicit default unless stated |
| PurchaseResourceId | uniqueidentifier | Yes | Shopping purchase reference | FK (OwnerId, PurchaseResourceId) → (OwnerId, Id); [platform.Resource](02-core-identity-platform.md#platform-resource); NO ACTION |
| FinanceResourceId | uniqueidentifier | Yes | Ledger reference | FK (OwnerId, FinanceResourceId) → (OwnerId, Id); [platform.Resource](02-core-identity-platform.md#platform-resource); NO ACTION |
| VaultResourceId | uniqueidentifier | Yes | Secret reference | FK (OwnerId, VaultResourceId) → (OwnerId, Id); [platform.Resource](02-core-identity-platform.md#platform-resource); NO ACTION |
| TrashBatchId | uniqueidentifier | Yes | Trash | FK (OwnerId, TrashBatchId) → (OwnerId, Id); [operations.TrashBatch](04-files-jobs-notifications.md#operations-trashbatch); NO ACTION |
| Category | nvarchar(100) | Yes | User classification | No implicit default unless stated |
| Seller | nvarchar(200) | Yes | Historical purchase seller | No implicit default unless stated |
| InvoiceFileId | uniqueidentifier | Yes | Clean purchase evidence | FK (OwnerId, InvoiceFileId) → (OwnerId, Id); [files.FileObject](04-files-jobs-notifications.md#files-fileobject); NO ACTION |

**Keys/index candidates:** PK(Id) nonclustered; internal clustering strategy in conventions. UQ(OwnerId,Id); IX(OwnerId,CreatedAt,Id). IX OwnerId,State,Title

**Integrity / transaction:** Metadata tracking only; VehicleMetadata is asset category, not newly committed Vehicle Management module. Sensitive share projections Q-03.

**Lifecycle / classification:** All writes check RowVersion; lifecycle guard also applies to import, automation, bulk and restore. Same-owner FK(OwnerId,Id) to Resource registry identity; payload and registry lifecycle commit atomically. Payload classification defaults Private owner; credential/encrypted/hash columns are never list/search/log data. [Per-field classification](15-field-classification.md#assets-personalasset). No ON DELETE CASCADE; approved purge service orders dependencies, rejects live references, preserves minimal audit. User-owned Trash retention is not inferred from job-log retention.

<a id="assets-assetaccessory"></a>
## assets.AssetAccessory

Acyclic physical accessory relationship. Profile **O**. Status: **Technical decision**.

| Field | SQL Server type | Nullable | Meaning / validation | Key / default / reference |
| --- | --- | --- | --- | --- |
| Id | uniqueidentifier | No | Opaque stable PK generated by trusted command | No implicit default unless stated |
| OwnerId | uniqueidentifier | No | Required isolation boundary resolved server-side, immutable | FK OwnerId → Id; [platform.PersonalSpace](02-core-identity-platform.md#platform-personalspace); NO ACTION |
| CreatedAt | datetime2(7) | No | Server insert instant, DEFAULT SYSUTCDATETIME() | No implicit default unless stated |
| CreatedByUserId | uniqueidentifier | Yes | Actual actor for attribution, not owner; null only system/anonymized identity | FK CreatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| UpdatedAt | datetime2(7) | No | Server write instant, initially CreatedAt | No implicit default unless stated |
| UpdatedByUserId | uniqueidentifier | Yes | Actual most recent actor; null only system/anonymized identity | FK UpdatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| RowVersion | rowversion | No | SQL-generated 8-byte optimistic concurrency token; not content history or clock | DB generated; exclude from inserts/updates |
| ParentAssetId | uniqueidentifier | No | Main asset | FK (OwnerId, ParentAssetId) → (OwnerId, Id); [assets.PersonalAsset](10-assets-career-learning.md#assets-personalasset); NO ACTION |
| ChildAssetId | uniqueidentifier | No | Accessory asset | FK (OwnerId, ChildAssetId) → (OwnerId, Id); [assets.PersonalAsset](10-assets-career-learning.md#assets-personalasset); NO ACTION |
| Notes | nvarchar(1000) | Yes | Relationship explanation | No implicit default unless stated |

**Keys/index candidates:** PK(Id) nonclustered; internal clustering strategy in conventions. UQ(OwnerId,Id); IX(OwnerId,CreatedAt,Id). UQ OwnerId,ParentAssetId,ChildAssetId

**Integrity / transaction:** No self-edge or cycles; association does not transfer ownership or auto-delete accessory.

**Lifecycle / classification:** All writes check RowVersion; lifecycle guard also applies to import, automation, bulk and restore. Payload classification defaults Private owner; credential/encrypted/hash columns are never list/search/log data. [Per-field classification](15-field-classification.md#assets-assetaccessory). No ON DELETE CASCADE; approved purge service orders dependencies, rejects live references, preserves minimal audit. User-owned Trash retention is not inferred from job-log retention.

<a id="assets-assetwarranty"></a>
## assets.AssetWarranty

Asset coverage record. Profile **O**. Status: **Technical decision**.

| Field | SQL Server type | Nullable | Meaning / validation | Key / default / reference |
| --- | --- | --- | --- | --- |
| Id | uniqueidentifier | No | Opaque stable PK generated by trusted command | No implicit default unless stated |
| OwnerId | uniqueidentifier | No | Required isolation boundary resolved server-side, immutable | FK OwnerId → Id; [platform.PersonalSpace](02-core-identity-platform.md#platform-personalspace); NO ACTION |
| CreatedAt | datetime2(7) | No | Server insert instant, DEFAULT SYSUTCDATETIME() | No implicit default unless stated |
| CreatedByUserId | uniqueidentifier | Yes | Actual actor for attribution, not owner; null only system/anonymized identity | FK CreatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| UpdatedAt | datetime2(7) | No | Server write instant, initially CreatedAt | No implicit default unless stated |
| UpdatedByUserId | uniqueidentifier | Yes | Actual most recent actor; null only system/anonymized identity | FK UpdatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| RowVersion | rowversion | No | SQL-generated 8-byte optimistic concurrency token; not content history or clock | DB generated; exclude from inserts/updates |
| AssetId | uniqueidentifier | No | Asset | FK (OwnerId, AssetId) → (OwnerId, Id); [assets.PersonalAsset](10-assets-career-learning.md#assets-personalasset); NO ACTION |
| Provider | nvarchar(200) | Yes | Coverage provider | No implicit default unless stated |
| StartsOn | date | Yes | Coverage start if known | No implicit default unless stated |
| EndsOn | date | Yes | Fixed expiry or null for Lifetime/Unknown | No implicit default unless stated |
| Terms | nvarchar(max) | Yes | Manual terms | No implicit default unless stated |
| ShoppingWarrantyResourceId | uniqueidentifier | Yes | Source warranty link | FK (OwnerId, ShoppingWarrantyResourceId) → (OwnerId, Id); [platform.Resource](02-core-identity-platform.md#platform-resource); NO ACTION |
| CoverageKind | varchar(64) | No | Fixed, Lifetime, Unknown | CHECK allowed codes documented in meaning |

**Keys/index candidates:** PK(Id) nonclustered; internal clustering strategy in conventions. UQ(OwnerId,Id); IX(OwnerId,CreatedAt,Id). IX OwnerId,AssetId,EndsOn

**Integrity / transaction:** No duplicate independent reminders when using source warranty provider; configure one authority.

**Lifecycle / classification:** All writes check RowVersion; lifecycle guard also applies to import, automation, bulk and restore. Payload classification defaults Private owner; credential/encrypted/hash columns are never list/search/log data. [Per-field classification](15-field-classification.md#assets-assetwarranty). No ON DELETE CASCADE; approved purge service orders dependencies, rejects live references, preserves minimal audit. User-owned Trash retention is not inferred from job-log retention.

<a id="assets-repair"></a>
## assets.Repair

Manual repair/service history. Profile **O**. Status: **Technical decision**.

| Field | SQL Server type | Nullable | Meaning / validation | Key / default / reference |
| --- | --- | --- | --- | --- |
| Id | uniqueidentifier | No | Opaque stable PK generated by trusted command | No implicit default unless stated |
| OwnerId | uniqueidentifier | No | Required isolation boundary resolved server-side, immutable | FK OwnerId → Id; [platform.PersonalSpace](02-core-identity-platform.md#platform-personalspace); NO ACTION |
| CreatedAt | datetime2(7) | No | Server insert instant, DEFAULT SYSUTCDATETIME() | No implicit default unless stated |
| CreatedByUserId | uniqueidentifier | Yes | Actual actor for attribution, not owner; null only system/anonymized identity | FK CreatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| UpdatedAt | datetime2(7) | No | Server write instant, initially CreatedAt | No implicit default unless stated |
| UpdatedByUserId | uniqueidentifier | Yes | Actual most recent actor; null only system/anonymized identity | FK UpdatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| RowVersion | rowversion | No | SQL-generated 8-byte optimistic concurrency token; not content history or clock | DB generated; exclude from inserts/updates |
| AssetId | uniqueidentifier | No | Asset | FK (OwnerId, AssetId) → (OwnerId, Id); [assets.PersonalAsset](10-assets-career-learning.md#assets-personalasset); NO ACTION |
| Title | nvarchar(200) | No | Repair description | No implicit default unless stated |
| StartedOn | date | No | Service start | No implicit default unless stated |
| CompletedOn | date | Yes | Completion | No implicit default unless stated |
| Cost | decimal(28,8) | Yes | Entered cost | No implicit default unless stated |
| Currency | char(3) | Yes | Required with cost | Uppercase registered currency; no float |
| Provider | nvarchar(200) | Yes | Service provider | No implicit default unless stated |
| Notes | nvarchar(max) | Yes | Service detail | No implicit default unless stated |
| Result | nvarchar(2000) | Yes | Repair result/evidence summary | No implicit default unless stated |

**Keys/index candidates:** PK(Id) nonclustered; internal clustering strategy in conventions. UQ(OwnerId,Id); IX(OwnerId,CreatedAt,Id). IX OwnerId,AssetId,StartedOn

**Integrity / transaction:** Does not charge account or automatically move Asset state without explicit command.

**Lifecycle / classification:** All writes check RowVersion; lifecycle guard also applies to import, automation, bulk and restore. Payload classification defaults Private owner; credential/encrypted/hash columns are never list/search/log data. [Per-field classification](15-field-classification.md#assets-repair). No ON DELETE CASCADE; approved purge service orders dependencies, rejects live references, preserves minimal audit. User-owned Trash retention is not inferred from job-log retention.

<a id="assets-assetloan"></a>
## assets.AssetLoan

Personal lending note, not team membership. Profile **O**. Status: **Technical decision**.

| Field | SQL Server type | Nullable | Meaning / validation | Key / default / reference |
| --- | --- | --- | --- | --- |
| Id | uniqueidentifier | No | Opaque stable PK generated by trusted command | No implicit default unless stated |
| OwnerId | uniqueidentifier | No | Required isolation boundary resolved server-side, immutable | FK OwnerId → Id; [platform.PersonalSpace](02-core-identity-platform.md#platform-personalspace); NO ACTION |
| CreatedAt | datetime2(7) | No | Server insert instant, DEFAULT SYSUTCDATETIME() | No implicit default unless stated |
| CreatedByUserId | uniqueidentifier | Yes | Actual actor for attribution, not owner; null only system/anonymized identity | FK CreatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| UpdatedAt | datetime2(7) | No | Server write instant, initially CreatedAt | No implicit default unless stated |
| UpdatedByUserId | uniqueidentifier | Yes | Actual most recent actor; null only system/anonymized identity | FK UpdatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| RowVersion | rowversion | No | SQL-generated 8-byte optimistic concurrency token; not content history or clock | DB generated; exclude from inserts/updates |
| AssetId | uniqueidentifier | No | Asset | FK (OwnerId, AssetId) → (OwnerId, Id); [assets.PersonalAsset](10-assets-career-learning.md#assets-personalasset); NO ACTION |
| Borrower | nvarchar(200) | No | Manually entered person label | No implicit default unless stated |
| LentOn | date | No | Start | No implicit default unless stated |
| ExpectedReturnOn | date | Yes | Due | No implicit default unless stated |
| ReturnedOn | date | Yes | Actual return | No implicit default unless stated |
| Notes | nvarchar(2000) | Yes | Private detail | No implicit default unless stated |

**Keys/index candidates:** PK(Id) nonclustered; internal clustering strategy in conventions. UQ(OwnerId,Id); IX(OwnerId,CreatedAt,Id). IX OwnerId,AssetId,LentOn

**Integrity / transaction:** Borrower receives no Nexora permissions.

**Lifecycle / classification:** All writes check RowVersion; lifecycle guard also applies to import, automation, bulk and restore. Payload classification defaults Private owner; credential/encrypted/hash columns are never list/search/log data. [Per-field classification](15-field-classification.md#assets-assetloan). No ON DELETE CASCADE; approved purge service orders dependencies, rejects live references, preserves minimal audit. User-owned Trash retention is not inferred from job-log retention.

<a id="assets-assetversion"></a>
## assets.AssetVersion

Physical asset history. Profile **V**. Status: **Technical decision**.

| Field | SQL Server type | Nullable | Meaning / validation | Key / default / reference |
| --- | --- | --- | --- | --- |
| Id | uniqueidentifier | No | Opaque stable PK generated by trusted command | No implicit default unless stated |
| OwnerId | uniqueidentifier | No | Required isolation boundary resolved server-side, immutable | FK OwnerId → Id; [platform.PersonalSpace](02-core-identity-platform.md#platform-personalspace); NO ACTION |
| CreatedAt | datetime2(7) | No | Server insert instant, DEFAULT SYSUTCDATETIME() | No implicit default unless stated |
| CreatedByUserId | uniqueidentifier | Yes | Actual actor for attribution, not owner; null only system/anonymized identity | FK CreatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| AssetId | uniqueidentifier | No | Asset | FK (OwnerId, AssetId) → (OwnerId, Id); [assets.PersonalAsset](10-assets-career-learning.md#assets-personalasset); NO ACTION |
| VersionNumber | bigint | No | Sequence | No implicit default unless stated |
| SafeSnapshotJson | nvarchar(max) | No | Nonsecret state/metadata snapshot | ISJSON + versioned allowlist; see payload contracts |
| SensitiveSnapshotEncrypted | varbinary(max) | Yes | Serial/contact-sensitive history | No implicit default unless stated |
| Reason | nvarchar(2000) | Yes | State/edit explanation | No implicit default unless stated |

**Keys/index candidates:** PK(Id) nonclustered; internal clustering strategy in conventions. UQ(OwnerId,Id); IX(OwnerId,CreatedAt,Id). UQ OwnerId,AssetId,VersionNumber

**Integrity / transaction:** History never exposes unmasked serial through generic activity/search/support.

**Lifecycle / classification:** Append-only; no in-place update/restore of an old row. Payload classification defaults Private owner; credential/encrypted/hash columns are never list/search/log data. [Per-field classification](15-field-classification.md#assets-assetversion). No ON DELETE CASCADE; approved purge service orders dependencies, rejects live references, preserves minimal audit. User-owned Trash retention is not inferred from job-log retention.

<a id="assets-digitalasset"></a>
## assets.DigitalAsset

Typed online-asset metadata, not infrastructure control plane. Profile **R**. Status: **Technical decision**.

| Field | SQL Server type | Nullable | Meaning / validation | Key / default / reference |
| --- | --- | --- | --- | --- |
| Id | uniqueidentifier | No | PK and same identity as platform.Resource.Id; generated by trusted command | No implicit default unless stated |
| OwnerId | uniqueidentifier | No | Required isolation boundary resolved server-side, immutable | FK OwnerId → Id; [platform.PersonalSpace](02-core-identity-platform.md#platform-personalspace); NO ACTION |
| CreatedAt | datetime2(7) | No | Server insert instant, DEFAULT SYSUTCDATETIME() | No implicit default unless stated |
| CreatedByUserId | uniqueidentifier | Yes | Actual actor for attribution, not owner; null only system/anonymized identity | FK CreatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| UpdatedAt | datetime2(7) | No | Server write instant, initially CreatedAt | No implicit default unless stated |
| UpdatedByUserId | uniqueidentifier | Yes | Actual most recent actor; null only system/anonymized identity | FK UpdatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| RowVersion | rowversion | No | SQL-generated 8-byte optimistic concurrency token; not content history or clock | DB generated; exclude from inserts/updates |
| Title | nvarchar(200) | No | Display label | No implicit default unless stated |
| Kind | varchar(64) | No | Domain, Hosting, Vps, Certificate, License, OnlineService | CHECK allowed codes documented in meaning |
| Provider | nvarchar(200) | Yes | Provider | No implicit default unless stated |
| State | varchar(64) | No | Active, Expired, Canceled, Archived | CHECK allowed codes documented in meaning |
| ExpiresOn | date | Yes | Manually entered expected expiry | No implicit default unless stated |
| ObservedExpiryAt | datetime2(7) | Yes | Provider observation, distinct from entered date | No implicit default unless stated |
| ObservedAt | datetime2(7) | Yes | Freshness | No implicit default unless stated |
| VaultResourceId | uniqueidentifier | Yes | Credential reference | FK (OwnerId, VaultResourceId) → (OwnerId, Id); [platform.Resource](02-core-identity-platform.md#platform-resource); NO ACTION |
| Notes | nvarchar(max) | Yes | Owner notes | No implicit default unless stated |
| TrashBatchId | uniqueidentifier | Yes | Trash | FK (OwnerId, TrashBatchId) → (OwnerId, Id); [operations.TrashBatch](04-files-jobs-notifications.md#operations-trashbatch); NO ACTION |
| PreArchiveState | varchar(64) | Yes | Prior state | CHECK allowed codes documented in meaning |
| Cost | decimal(28,8) | Yes | Entered renewal cost | No implicit default unless stated |
| Currency | char(3) | Yes | Cost currency | Uppercase registered currency; no float |
| RenewalCycle | nvarchar(100) | Yes | Entered billing interval | No implicit default unless stated |

**Keys/index candidates:** PK(Id) nonclustered; internal clustering strategy in conventions. UQ(OwnerId,Id); IX(OwnerId,CreatedAt,Id). IX OwnerId,Kind,ExpiresOn

**Integrity / transaction:** Network observations conditional Q-07. Recording renewed/expired is metadata only, no provider/payment command.

**Lifecycle / classification:** All writes check RowVersion; lifecycle guard also applies to import, automation, bulk and restore. Same-owner FK(OwnerId,Id) to Resource registry identity; payload and registry lifecycle commit atomically. Payload classification defaults Private owner; credential/encrypted/hash columns are never list/search/log data. [Per-field classification](15-field-classification.md#assets-digitalasset). No ON DELETE CASCADE; approved purge service orders dependencies, rejects live references, preserves minimal audit. User-owned Trash retention is not inferred from job-log retention.

<a id="assets-domaindetail"></a>
## assets.DomainDetail

Domain-specific metadata. Profile **O**. Status: **Technical decision**.

| Field | SQL Server type | Nullable | Meaning / validation | Key / default / reference |
| --- | --- | --- | --- | --- |
| Id | uniqueidentifier | No | Opaque stable PK generated by trusted command | No implicit default unless stated |
| OwnerId | uniqueidentifier | No | Required isolation boundary resolved server-side, immutable | FK OwnerId → Id; [platform.PersonalSpace](02-core-identity-platform.md#platform-personalspace); NO ACTION |
| CreatedAt | datetime2(7) | No | Server insert instant, DEFAULT SYSUTCDATETIME() | No implicit default unless stated |
| CreatedByUserId | uniqueidentifier | Yes | Actual actor for attribution, not owner; null only system/anonymized identity | FK CreatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| UpdatedAt | datetime2(7) | No | Server write instant, initially CreatedAt | No implicit default unless stated |
| UpdatedByUserId | uniqueidentifier | Yes | Actual most recent actor; null only system/anonymized identity | FK UpdatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| RowVersion | rowversion | No | SQL-generated 8-byte optimistic concurrency token; not content history or clock | DB generated; exclude from inserts/updates |
| DigitalAssetId | uniqueidentifier | No | Kind Domain | FK (OwnerId, DigitalAssetId) → (OwnerId, Id); [assets.DigitalAsset](10-assets-career-learning.md#assets-digitalasset); NO ACTION |
| AsciiName | nvarchar(253) | No | IDNA normalized domain | No implicit default unless stated |
| UnicodeName | nvarchar(253) | No | Display form | No implicit default unless stated |
| Registrar | nvarchar(200) | Yes | Provider label | No implicit default unless stated |
| AutoRenewRecorded | bit | Yes | Manually observed provider setting, not command | No implicit default unless stated |
| RegisteredOn | date | Yes | Registration date | No implicit default unless stated |
| NameserverNotes | nvarchar(max) | Yes | Entered nameserver metadata | No implicit default unless stated |

**Keys/index candidates:** PK(Id) nonclustered; internal clustering strategy in conventions. UQ(OwnerId,Id); IX(OwnerId,CreatedAt,Id). UQ OwnerId,DigitalAssetId; IX OwnerId,AsciiName

**Integrity / transaction:** Show Unicode+ASCII for confusable domains; never registrar write.

**Lifecycle / classification:** All writes check RowVersion; lifecycle guard also applies to import, automation, bulk and restore. Payload classification defaults Private owner; credential/encrypted/hash columns are never list/search/log data. [Per-field classification](15-field-classification.md#assets-domaindetail). No ON DELETE CASCADE; approved purge service orders dependencies, rejects live references, preserves minimal audit. User-owned Trash retention is not inferred from job-log retention.

<a id="assets-hostingdetail"></a>
## assets.HostingDetail

Hosting plan metadata. Profile **O**. Status: **Technical decision**.

| Field | SQL Server type | Nullable | Meaning / validation | Key / default / reference |
| --- | --- | --- | --- | --- |
| Id | uniqueidentifier | No | Opaque stable PK generated by trusted command | No implicit default unless stated |
| OwnerId | uniqueidentifier | No | Required isolation boundary resolved server-side, immutable | FK OwnerId → Id; [platform.PersonalSpace](02-core-identity-platform.md#platform-personalspace); NO ACTION |
| CreatedAt | datetime2(7) | No | Server insert instant, DEFAULT SYSUTCDATETIME() | No implicit default unless stated |
| CreatedByUserId | uniqueidentifier | Yes | Actual actor for attribution, not owner; null only system/anonymized identity | FK CreatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| UpdatedAt | datetime2(7) | No | Server write instant, initially CreatedAt | No implicit default unless stated |
| UpdatedByUserId | uniqueidentifier | Yes | Actual most recent actor; null only system/anonymized identity | FK UpdatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| RowVersion | rowversion | No | SQL-generated 8-byte optimistic concurrency token; not content history or clock | DB generated; exclude from inserts/updates |
| DigitalAssetId | uniqueidentifier | No | Kind Hosting | FK (OwnerId, DigitalAssetId) → (OwnerId, Id); [assets.DigitalAsset](10-assets-career-learning.md#assets-digitalasset); NO ACTION |
| Plan | nvarchar(200) | Yes | Plan label | No implicit default unless stated |
| ControlPanelUrl | nvarchar(2048) | Yes | External destination, no auto-login | No implicit default unless stated |
| StorageLimitBytes | bigint | Yes | Entered limit | No implicit default unless stated |
| DomainName | nvarchar(253) | Yes | Hosted domain | No implicit default unless stated |
| Region | nvarchar(100) | Yes | Provider region | No implicit default unless stated |

**Keys/index candidates:** PK(Id) nonclustered; internal clustering strategy in conventions. UQ(OwnerId,Id); IX(OwnerId,CreatedAt,Id). UQ OwnerId,DigitalAssetId

**Integrity / transaction:** No hosting provisioning/deploy/remote file manager.

**Lifecycle / classification:** All writes check RowVersion; lifecycle guard also applies to import, automation, bulk and restore. Payload classification defaults Private owner; credential/encrypted/hash columns are never list/search/log data. [Per-field classification](15-field-classification.md#assets-hostingdetail). No ON DELETE CASCADE; approved purge service orders dependencies, rejects live references, preserves minimal audit. User-owned Trash retention is not inferred from job-log retention.

<a id="assets-vpsdetail"></a>
## assets.VpsDetail

Server inventory metadata only. Profile **O**. Status: **Technical decision**.

| Field | SQL Server type | Nullable | Meaning / validation | Key / default / reference |
| --- | --- | --- | --- | --- |
| Id | uniqueidentifier | No | Opaque stable PK generated by trusted command | No implicit default unless stated |
| OwnerId | uniqueidentifier | No | Required isolation boundary resolved server-side, immutable | FK OwnerId → Id; [platform.PersonalSpace](02-core-identity-platform.md#platform-personalspace); NO ACTION |
| CreatedAt | datetime2(7) | No | Server insert instant, DEFAULT SYSUTCDATETIME() | No implicit default unless stated |
| CreatedByUserId | uniqueidentifier | Yes | Actual actor for attribution, not owner; null only system/anonymized identity | FK CreatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| UpdatedAt | datetime2(7) | No | Server write instant, initially CreatedAt | No implicit default unless stated |
| UpdatedByUserId | uniqueidentifier | Yes | Actual most recent actor; null only system/anonymized identity | FK UpdatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| RowVersion | rowversion | No | SQL-generated 8-byte optimistic concurrency token; not content history or clock | DB generated; exclude from inserts/updates |
| DigitalAssetId | uniqueidentifier | No | Kind Vps | FK (OwnerId, DigitalAssetId) → (OwnerId, Id); [assets.DigitalAsset](10-assets-career-learning.md#assets-digitalasset); NO ACTION |
| HostName | nvarchar(253) | Yes | Entered hostname | No implicit default unless stated |
| IpAddress | nvarchar(45) | Yes | Entered IPv4/IPv6 | No implicit default unless stated |
| CpuCount | int | Yes | Positive count | No implicit default unless stated |
| MemoryMiB | bigint | Yes | Entered memory | No implicit default unless stated |
| OperatingSystem | nvarchar(200) | Yes | OS label | No implicit default unless stated |
| Plan | nvarchar(200) | Yes | Provider plan | No implicit default unless stated |
| Region | nvarchar(100) | Yes | Provider region | No implicit default unless stated |

**Keys/index candidates:** PK(Id) nonclustered; internal clustering strategy in conventions. UQ(OwnerId,Id); IX(OwnerId,CreatedAt,Id). UQ OwnerId,DigitalAssetId

**Integrity / transaction:** No SSH/RDP/reboot; private keys/passwords via Vault only.

**Lifecycle / classification:** All writes check RowVersion; lifecycle guard also applies to import, automation, bulk and restore. Payload classification defaults Private owner; credential/encrypted/hash columns are never list/search/log data. [Per-field classification](15-field-classification.md#assets-vpsdetail). No ON DELETE CASCADE; approved purge service orders dependencies, rejects live references, preserves minimal audit. User-owned Trash retention is not inferred from job-log retention.

<a id="assets-certificatedetail"></a>
## assets.CertificateDetail

Public certificate metadata. Profile **O**. Status: **Technical decision**.

| Field | SQL Server type | Nullable | Meaning / validation | Key / default / reference |
| --- | --- | --- | --- | --- |
| Id | uniqueidentifier | No | Opaque stable PK generated by trusted command | No implicit default unless stated |
| OwnerId | uniqueidentifier | No | Required isolation boundary resolved server-side, immutable | FK OwnerId → Id; [platform.PersonalSpace](02-core-identity-platform.md#platform-personalspace); NO ACTION |
| CreatedAt | datetime2(7) | No | Server insert instant, DEFAULT SYSUTCDATETIME() | No implicit default unless stated |
| CreatedByUserId | uniqueidentifier | Yes | Actual actor for attribution, not owner; null only system/anonymized identity | FK CreatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| UpdatedAt | datetime2(7) | No | Server write instant, initially CreatedAt | No implicit default unless stated |
| UpdatedByUserId | uniqueidentifier | Yes | Actual most recent actor; null only system/anonymized identity | FK UpdatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| RowVersion | rowversion | No | SQL-generated 8-byte optimistic concurrency token; not content history or clock | DB generated; exclude from inserts/updates |
| DigitalAssetId | uniqueidentifier | No | Kind Certificate | FK (OwnerId, DigitalAssetId) → (OwnerId, Id); [assets.DigitalAsset](10-assets-career-learning.md#assets-digitalasset); NO ACTION |
| Subject | nvarchar(500) | No | Public subject | No implicit default unless stated |
| Issuer | nvarchar(500) | Yes | Issuer | No implicit default unless stated |
| Fingerprint | nvarchar(200) | Yes | Public certificate digest | No implicit default unless stated |
| NotBefore | datetime2(7) | Yes | Certificate start | No implicit default unless stated |
| NotAfter | datetime2(7) | Yes | Expiry | No implicit default unless stated |
| HostName | nvarchar(253) | Yes | Observed host | No implicit default unless stated |
| SubjectAlternativeNamesJson | nvarchar(max) | Yes | Public SAN list | ISJSON + versioned allowlist; see payload contracts |
| Source | varchar(64) | No | Manual or Observed | CHECK allowed codes documented in meaning |

**Keys/index candidates:** PK(Id) nonclustered; internal clustering strategy in conventions. UQ(OwnerId,Id); IX(OwnerId,CreatedAt,Id). UQ OwnerId,DigitalAssetId

**Integrity / transaction:** Never private-key upload into metadata; observation does not overwrite entered assumptions silently.

**Lifecycle / classification:** All writes check RowVersion; lifecycle guard also applies to import, automation, bulk and restore. Payload classification defaults Private owner; credential/encrypted/hash columns are never list/search/log data. [Per-field classification](15-field-classification.md#assets-certificatedetail). No ON DELETE CASCADE; approved purge service orders dependencies, rejects live references, preserves minimal audit. User-owned Trash retention is not inferred from job-log retention.

<a id="assets-licensedetail"></a>
## assets.LicenseDetail

Software license entitlement metadata. Profile **O**. Status: **Technical decision**.

| Field | SQL Server type | Nullable | Meaning / validation | Key / default / reference |
| --- | --- | --- | --- | --- |
| Id | uniqueidentifier | No | Opaque stable PK generated by trusted command | No implicit default unless stated |
| OwnerId | uniqueidentifier | No | Required isolation boundary resolved server-side, immutable | FK OwnerId → Id; [platform.PersonalSpace](02-core-identity-platform.md#platform-personalspace); NO ACTION |
| CreatedAt | datetime2(7) | No | Server insert instant, DEFAULT SYSUTCDATETIME() | No implicit default unless stated |
| CreatedByUserId | uniqueidentifier | Yes | Actual actor for attribution, not owner; null only system/anonymized identity | FK CreatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| UpdatedAt | datetime2(7) | No | Server write instant, initially CreatedAt | No implicit default unless stated |
| UpdatedByUserId | uniqueidentifier | Yes | Actual most recent actor; null only system/anonymized identity | FK UpdatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| RowVersion | rowversion | No | SQL-generated 8-byte optimistic concurrency token; not content history or clock | DB generated; exclude from inserts/updates |
| DigitalAssetId | uniqueidentifier | No | Kind License | FK (OwnerId, DigitalAssetId) → (OwnerId, Id); [assets.DigitalAsset](10-assets-career-learning.md#assets-digitalasset); NO ACTION |
| Product | nvarchar(200) | No | Product name | No implicit default unless stated |
| Seats | int | Yes | Positive count | No implicit default unless stated |
| LicenseSecretResourceId | uniqueidentifier | Yes | Vault secret reference | FK (OwnerId, LicenseSecretResourceId) → (OwnerId, Id); [platform.Resource](02-core-identity-platform.md#platform-resource); NO ACTION |
| PurchasedOn | date | Yes | Purchase day | No implicit default unless stated |
| Vendor | nvarchar(200) | Yes | Vendor | No implicit default unless stated |
| Edition | nvarchar(200) | Yes | Product edition | No implicit default unless stated |
| DeviceResourceId | uniqueidentifier | Yes | Associated personal Asset | FK (OwnerId, DeviceResourceId) → (OwnerId, Id); [platform.Resource](02-core-identity-platform.md#platform-resource); NO ACTION |

**Keys/index candidates:** PK(Id) nonclustered; internal clustering strategy in conventions. UQ(OwnerId,Id); IX(OwnerId,CreatedAt,Id). UQ OwnerId,DigitalAssetId

**Integrity / transaction:** No executable license activation or shared Vault seats.

**Lifecycle / classification:** All writes check RowVersion; lifecycle guard also applies to import, automation, bulk and restore. Payload classification defaults Private owner; credential/encrypted/hash columns are never list/search/log data. [Per-field classification](15-field-classification.md#assets-licensedetail). No ON DELETE CASCADE; approved purge service orders dependencies, rejects live references, preserves minimal audit. User-owned Trash retention is not inferred from job-log retention.

<a id="assets-servicedetail"></a>
## assets.ServiceDetail

Online-service account metadata. Profile **O**. Status: **Technical decision**.

| Field | SQL Server type | Nullable | Meaning / validation | Key / default / reference |
| --- | --- | --- | --- | --- |
| Id | uniqueidentifier | No | Opaque stable PK generated by trusted command | No implicit default unless stated |
| OwnerId | uniqueidentifier | No | Required isolation boundary resolved server-side, immutable | FK OwnerId → Id; [platform.PersonalSpace](02-core-identity-platform.md#platform-personalspace); NO ACTION |
| CreatedAt | datetime2(7) | No | Server insert instant, DEFAULT SYSUTCDATETIME() | No implicit default unless stated |
| CreatedByUserId | uniqueidentifier | Yes | Actual actor for attribution, not owner; null only system/anonymized identity | FK CreatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| UpdatedAt | datetime2(7) | No | Server write instant, initially CreatedAt | No implicit default unless stated |
| UpdatedByUserId | uniqueidentifier | Yes | Actual most recent actor; null only system/anonymized identity | FK UpdatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| RowVersion | rowversion | No | SQL-generated 8-byte optimistic concurrency token; not content history or clock | DB generated; exclude from inserts/updates |
| DigitalAssetId | uniqueidentifier | No | Kind OnlineService | FK (OwnerId, DigitalAssetId) → (OwnerId, Id); [assets.DigitalAsset](10-assets-career-learning.md#assets-digitalasset); NO ACTION |
| ServiceUrl | nvarchar(2048) | Yes | Public service | No implicit default unless stated |
| Plan | nvarchar(200) | Yes | Entered tier | No implicit default unless stated |
| AccountLabelEncrypted | varbinary(max) | Yes | Sensitive account identifier | No implicit default unless stated |
| FinanceSubscriptionResourceId | uniqueidentifier | Yes | Manual subscription link | FK (OwnerId, FinanceSubscriptionResourceId) → (OwnerId, Id); [platform.Resource](02-core-identity-platform.md#platform-resource); NO ACTION |

**Keys/index candidates:** PK(Id) nonclustered; internal clustering strategy in conventions. UQ(OwnerId,Id); IX(OwnerId,CreatedAt,Id). UQ OwnerId,DigitalAssetId

**Integrity / transaction:** External account login/paid plan changes remain out of scope.

**Lifecycle / classification:** All writes check RowVersion; lifecycle guard also applies to import, automation, bulk and restore. Payload classification defaults Private owner; credential/encrypted/hash columns are never list/search/log data. [Per-field classification](15-field-classification.md#assets-servicedetail). No ON DELETE CASCADE; approved purge service orders dependencies, rejects live references, preserves minimal audit. User-owned Trash retention is not inferred from job-log retention.

<a id="assets-renewalrecord"></a>
## assets.RenewalRecord

Manual metadata renewal history. Profile **O**. Status: **Technical decision**.

| Field | SQL Server type | Nullable | Meaning / validation | Key / default / reference |
| --- | --- | --- | --- | --- |
| Id | uniqueidentifier | No | Opaque stable PK generated by trusted command | No implicit default unless stated |
| OwnerId | uniqueidentifier | No | Required isolation boundary resolved server-side, immutable | FK OwnerId → Id; [platform.PersonalSpace](02-core-identity-platform.md#platform-personalspace); NO ACTION |
| CreatedAt | datetime2(7) | No | Server insert instant, DEFAULT SYSUTCDATETIME() | No implicit default unless stated |
| CreatedByUserId | uniqueidentifier | Yes | Actual actor for attribution, not owner; null only system/anonymized identity | FK CreatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| UpdatedAt | datetime2(7) | No | Server write instant, initially CreatedAt | No implicit default unless stated |
| UpdatedByUserId | uniqueidentifier | Yes | Actual most recent actor; null only system/anonymized identity | FK UpdatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| RowVersion | rowversion | No | SQL-generated 8-byte optimistic concurrency token; not content history or clock | DB generated; exclude from inserts/updates |
| DigitalAssetId | uniqueidentifier | No | Asset | FK (OwnerId, DigitalAssetId) → (OwnerId, Id); [assets.DigitalAsset](10-assets-career-learning.md#assets-digitalasset); NO ACTION |
| RenewedOn | date | No | Recorded renewal day | No implicit default unless stated |
| PreviousExpiry | date | Yes | Old entered date | No implicit default unless stated |
| NewExpiry | date | No | New entered date | No implicit default unless stated |
| Amount | decimal(28,8) | Yes | Entered cost | No implicit default unless stated |
| Currency | char(3) | Yes | Amount currency | Uppercase registered currency; no float |
| Notes | nvarchar(2000) | Yes | Evidence | No implicit default unless stated |

**Keys/index candidates:** PK(Id) nonclustered; internal clustering strategy in conventions. UQ(OwnerId,Id); IX(OwnerId,CreatedAt,Id). IX OwnerId,DigitalAssetId,RenewedOn

**Integrity / transaction:** Updates entered expiry atomically, not observed provider state; no automated payment.

**Lifecycle / classification:** All writes check RowVersion; lifecycle guard also applies to import, automation, bulk and restore. Payload classification defaults Private owner; credential/encrypted/hash columns are never list/search/log data. [Per-field classification](15-field-classification.md#assets-renewalrecord). No ON DELETE CASCADE; approved purge service orders dependencies, rejects live references, preserves minimal audit. User-owned Trash retention is not inferred from job-log retention.

<a id="career-company"></a>
## career.Company

Personal employer/contact directory. Profile **R**. Status: **Technical decision**.

| Field | SQL Server type | Nullable | Meaning / validation | Key / default / reference |
| --- | --- | --- | --- | --- |
| Id | uniqueidentifier | No | PK and same identity as platform.Resource.Id; generated by trusted command | No implicit default unless stated |
| OwnerId | uniqueidentifier | No | Required isolation boundary resolved server-side, immutable | FK OwnerId → Id; [platform.PersonalSpace](02-core-identity-platform.md#platform-personalspace); NO ACTION |
| CreatedAt | datetime2(7) | No | Server insert instant, DEFAULT SYSUTCDATETIME() | No implicit default unless stated |
| CreatedByUserId | uniqueidentifier | Yes | Actual actor for attribution, not owner; null only system/anonymized identity | FK CreatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| UpdatedAt | datetime2(7) | No | Server write instant, initially CreatedAt | No implicit default unless stated |
| UpdatedByUserId | uniqueidentifier | Yes | Actual most recent actor; null only system/anonymized identity | FK UpdatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| RowVersion | rowversion | No | SQL-generated 8-byte optimistic concurrency token; not content history or clock | DB generated; exclude from inserts/updates |
| Title | nvarchar(200) | No | Company name | No implicit default unless stated |
| Url | nvarchar(2048) | Yes | Public website | No implicit default unless stated |
| Location | nvarchar(200) | Yes | Entered location | No implicit default unless stated |
| ContactEncrypted | varbinary(max) | Yes | Sensitive recruiting contact | No implicit default unless stated |
| Notes | nvarchar(max) | Yes | Private notes | No implicit default unless stated |
| MergedIntoId | uniqueidentifier | Yes | Explicit merge target | FK (OwnerId, MergedIntoId) → (OwnerId, Id); [career.Company](10-assets-career-learning.md#career-company); NO ACTION |
| Industry | nvarchar(200) | Yes | Industry classification | No implicit default unless stated |

**Keys/index candidates:** PK(Id) nonclustered; internal clustering strategy in conventions. UQ(OwnerId,Id); IX(OwnerId,CreatedAt,Id). IX OwnerId,Title

**Integrity / transaction:** Merge previews jobs/interviews and preserves source history; not shared corporate CRM.

**Lifecycle / classification:** All writes check RowVersion; lifecycle guard also applies to import, automation, bulk and restore. Same-owner FK(OwnerId,Id) to Resource registry identity; payload and registry lifecycle commit atomically. Payload classification defaults Private owner; credential/encrypted/hash columns are never list/search/log data. [Per-field classification](15-field-classification.md#career-company). No ON DELETE CASCADE; approved purge service orders dependencies, rejects live references, preserves minimal audit. User-owned Trash retention is not inferred from job-log retention.

<a id="career-jobapplication"></a>
## career.JobApplication

Personal application pipeline record. Profile **R**. Status: **Technical decision**.

| Field | SQL Server type | Nullable | Meaning / validation | Key / default / reference |
| --- | --- | --- | --- | --- |
| Id | uniqueidentifier | No | PK and same identity as platform.Resource.Id; generated by trusted command | No implicit default unless stated |
| OwnerId | uniqueidentifier | No | Required isolation boundary resolved server-side, immutable | FK OwnerId → Id; [platform.PersonalSpace](02-core-identity-platform.md#platform-personalspace); NO ACTION |
| CreatedAt | datetime2(7) | No | Server insert instant, DEFAULT SYSUTCDATETIME() | No implicit default unless stated |
| CreatedByUserId | uniqueidentifier | Yes | Actual actor for attribution, not owner; null only system/anonymized identity | FK CreatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| UpdatedAt | datetime2(7) | No | Server write instant, initially CreatedAt | No implicit default unless stated |
| UpdatedByUserId | uniqueidentifier | Yes | Actual most recent actor; null only system/anonymized identity | FK UpdatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| RowVersion | rowversion | No | SQL-generated 8-byte optimistic concurrency token; not content history or clock | DB generated; exclude from inserts/updates |
| Title | nvarchar(200) | No | Role title | No implicit default unless stated |
| CompanyId | uniqueidentifier | Yes | Employer | FK (OwnerId, CompanyId) → (OwnerId, Id); [career.Company](10-assets-career-learning.md#career-company); NO ACTION |
| Url | nvarchar(2048) | Yes | Original listing | No implicit default unless stated |
| Stage | varchar(64) | No | Saved, Preparing, Applied, Screening, Interviewing, Offer, Accepted, Rejected, Withdrawn, Closed | CHECK allowed codes documented in meaning |
| AppliedOn | date | Yes | Actual application day | No implicit default unless stated |
| SalaryMin | decimal(28,8) | Yes | Private range lower | No implicit default unless stated |
| SalaryMax | decimal(28,8) | Yes | Range upper | No implicit default unless stated |
| Currency | char(3) | Yes | Required with salary | Uppercase registered currency; no float |
| Notes | nvarchar(max) | Yes | Private notes | No implicit default unless stated |
| ResumeVersionReference | uniqueidentifier | Yes | Exact pinned version FK, nullable before application | FK (OwnerId, ResumeVersionReference) → (OwnerId, Id); [career.ResumeVersion](10-assets-career-learning.md#career-resumeversion); NO ACTION |
| TrashBatchId | uniqueidentifier | Yes | Trash | FK (OwnerId, TrashBatchId) → (OwnerId, Id); [operations.TrashBatch](04-files-jobs-notifications.md#operations-trashbatch); NO ACTION |
| Location | nvarchar(200) | Yes | Job location | No implicit default unless stated |
| WorkMode | varchar(64) | Yes | Onsite, Hybrid, Remote | CHECK allowed codes documented in meaning |
| EmploymentType | nvarchar(100) | Yes | Owner-entered type | No implicit default unless stated |
| SalaryText | nvarchar(1000) | Yes | Private original salary terms | No implicit default unless stated |
| Description | nvarchar(max) | Yes | Sanitized saved listing | No implicit default unless stated |
| Source | nvarchar(200) | Yes | Job source label | No implicit default unless stated |

**Keys/index candidates:** PK(Id) nonclustered; internal clustering strategy in conventions. UQ(OwnerId,Id); IX(OwnerId,CreatedAt,Id). IX OwnerId,Stage,UpdatedAt; IX OwnerId,CompanyId

**Integrity / transaction:** No automatic application submission/scraping/outreach/AI. Stage changes require timeline; terminal reopen semantics follow feature not Project policy.

**Lifecycle / classification:** All writes check RowVersion; lifecycle guard also applies to import, automation, bulk and restore. Same-owner FK(OwnerId,Id) to Resource registry identity; payload and registry lifecycle commit atomically. Payload classification defaults Private owner; credential/encrypted/hash columns are never list/search/log data. [Per-field classification](15-field-classification.md#career-jobapplication). No ON DELETE CASCADE; approved purge service orders dependencies, rejects live references, preserves minimal audit. User-owned Trash retention is not inferred from job-log retention.

<a id="career-interview"></a>
## career.Interview

Personal interview appointment metadata. Profile **R**. Status: **Proposed: Q-12 Calendar link; standalone interview core**.

| Field | SQL Server type | Nullable | Meaning / validation | Key / default / reference |
| --- | --- | --- | --- | --- |
| Id | uniqueidentifier | No | PK and same identity as platform.Resource.Id; generated by trusted command | No implicit default unless stated |
| OwnerId | uniqueidentifier | No | Required isolation boundary resolved server-side, immutable | FK OwnerId → Id; [platform.PersonalSpace](02-core-identity-platform.md#platform-personalspace); NO ACTION |
| CreatedAt | datetime2(7) | No | Server insert instant, DEFAULT SYSUTCDATETIME() | No implicit default unless stated |
| CreatedByUserId | uniqueidentifier | Yes | Actual actor for attribution, not owner; null only system/anonymized identity | FK CreatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| UpdatedAt | datetime2(7) | No | Server write instant, initially CreatedAt | No implicit default unless stated |
| UpdatedByUserId | uniqueidentifier | Yes | Actual most recent actor; null only system/anonymized identity | FK UpdatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| RowVersion | rowversion | No | SQL-generated 8-byte optimistic concurrency token; not content history or clock | DB generated; exclude from inserts/updates |
| ApplicationId | uniqueidentifier | No | Application | FK (OwnerId, ApplicationId) → (OwnerId, Id); [career.JobApplication](10-assets-career-learning.md#career-jobapplication); NO ACTION |
| Title | nvarchar(200) | No | Interview label | No implicit default unless stated |
| StartAt | datetime2(7) | No | Start | No implicit default unless stated |
| EndAt | datetime2(7) | No | End | No implicit default unless stated |
| TimeZoneId | nvarchar(100) | No | Display interpretation | No implicit default unless stated |
| Status | varchar(64) | No | Scheduled, Completed, Canceled | CHECK allowed codes documented in meaning |
| Location | nvarchar(500) | Yes | Private meeting detail | No implicit default unless stated |
| NotesEncrypted | varbinary(max) | Yes | Sensitive notes | No implicit default unless stated |
| CalendarResourceId | uniqueidentifier | Yes | Conditional owner-created ManualEvent link | FK (OwnerId, CalendarResourceId) → (OwnerId, Id); [platform.Resource](02-core-identity-platform.md#platform-resource); NO ACTION |
| RoundLabel | nvarchar(100) | Yes | Interview round | No implicit default unless stated |
| InterviewType | nvarchar(100) | Yes | Phone, video, onsite or entered label | No implicit default unless stated |
| MeetingUrl | nvarchar(2048) | Yes | Sensitive destination | No implicit default unless stated |
| ParticipantsEncrypted | varbinary(max) | Yes | Private manually entered participants, no permissions | No implicit default unless stated |

**Keys/index candidates:** PK(Id) nonclustered; internal clustering strategy in conventions. UQ(OwnerId,Id); IX(OwnerId,CreatedAt,Id). IX OwnerId,ApplicationId,StartAt

**Integrity / transaction:** End>Start. Q-12 blocks synchronization/third Calendar source. No assumption of automatic reminder duplication.

**Lifecycle / classification:** All writes check RowVersion; lifecycle guard also applies to import, automation, bulk and restore. Same-owner FK(OwnerId,Id) to Resource registry identity; payload and registry lifecycle commit atomically. Payload classification defaults Private owner; credential/encrypted/hash columns are never list/search/log data. [Per-field classification](15-field-classification.md#career-interview). No ON DELETE CASCADE; approved purge service orders dependencies, rejects live references, preserves minimal audit. User-owned Trash retention is not inferred from job-log retention.

<a id="career-applicationevent"></a>
## career.ApplicationEvent

Immutable job pipeline timeline. Profile **V**. Status: **Technical decision**.

| Field | SQL Server type | Nullable | Meaning / validation | Key / default / reference |
| --- | --- | --- | --- | --- |
| Id | uniqueidentifier | No | Opaque stable PK generated by trusted command | No implicit default unless stated |
| OwnerId | uniqueidentifier | No | Required isolation boundary resolved server-side, immutable | FK OwnerId → Id; [platform.PersonalSpace](02-core-identity-platform.md#platform-personalspace); NO ACTION |
| CreatedAt | datetime2(7) | No | Server insert instant, DEFAULT SYSUTCDATETIME() | No implicit default unless stated |
| CreatedByUserId | uniqueidentifier | Yes | Actual actor for attribution, not owner; null only system/anonymized identity | FK CreatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| ApplicationId | uniqueidentifier | No | Application | FK (OwnerId, ApplicationId) → (OwnerId, Id); [career.JobApplication](10-assets-career-learning.md#career-jobapplication); NO ACTION |
| FromStage | varchar(64) | Yes | Prior stage | CHECK allowed codes documented in meaning |
| ToStage | varchar(64) | No | New stage | CHECK allowed codes documented in meaning |
| OccurredAt | datetime2(7) | No | Change instant | No implicit default unless stated |
| Note | nvarchar(2000) | Yes | Owner explanation | No implicit default unless stated |
| ResumeVersionReference | uniqueidentifier | Yes | Exact historical submitted version | FK (OwnerId, ResumeVersionReference) → (OwnerId, Id); [career.ResumeVersion](10-assets-career-learning.md#career-resumeversion); NO ACTION |
| CompanyLabelSnapshot | nvarchar(200) | Yes | Historical company label | No implicit default unless stated |

**Keys/index candidates:** PK(Id) nonclustered; internal clustering strategy in conventions. UQ(OwnerId,Id); IX(OwnerId,CreatedAt,Id). IX OwnerId,ApplicationId,OccurredAt

**Integrity / transaction:** Never repoint historical application resume to latest revision.

**Lifecycle / classification:** Append-only; no in-place update/restore of an old row. Payload classification defaults Private owner; credential/encrypted/hash columns are never list/search/log data. [Per-field classification](15-field-classification.md#career-applicationevent). No ON DELETE CASCADE; approved purge service orders dependencies, rejects live references, preserves minimal audit. User-owned Trash retention is not inferred from job-log retention.

<a id="career-resume"></a>
## career.Resume

Personal resume family and current version pointer. Profile **R**. Status: **Technical decision**.

| Field | SQL Server type | Nullable | Meaning / validation | Key / default / reference |
| --- | --- | --- | --- | --- |
| Id | uniqueidentifier | No | PK and same identity as platform.Resource.Id; generated by trusted command | No implicit default unless stated |
| OwnerId | uniqueidentifier | No | Required isolation boundary resolved server-side, immutable | FK OwnerId → Id; [platform.PersonalSpace](02-core-identity-platform.md#platform-personalspace); NO ACTION |
| CreatedAt | datetime2(7) | No | Server insert instant, DEFAULT SYSUTCDATETIME() | No implicit default unless stated |
| CreatedByUserId | uniqueidentifier | Yes | Actual actor for attribution, not owner; null only system/anonymized identity | FK CreatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| UpdatedAt | datetime2(7) | No | Server write instant, initially CreatedAt | No implicit default unless stated |
| UpdatedByUserId | uniqueidentifier | Yes | Actual most recent actor; null only system/anonymized identity | FK UpdatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| RowVersion | rowversion | No | SQL-generated 8-byte optimistic concurrency token; not content history or clock | DB generated; exclude from inserts/updates |
| Title | nvarchar(200) | No | Resume name | No implicit default unless stated |
| CurrentVersion | bigint | No | Latest immutable version | No implicit default unless stated |
| Status | varchar(64) | No | Active, Archived | CHECK allowed codes documented in meaning |
| TrashBatchId | uniqueidentifier | Yes | Trash | FK (OwnerId, TrashBatchId) → (OwnerId, Id); [operations.TrashBatch](04-files-jobs-notifications.md#operations-trashbatch); NO ACTION |
| Language | nvarchar(35) | Yes | Resume language | No implicit default unless stated |

**Keys/index candidates:** PK(Id) nonclustered; internal clustering strategy in conventions. UQ(OwnerId,Id); IX(OwnerId,CreatedAt,Id). IX OwnerId,Status,Title

**Integrity / transaction:** Sharing pins exact selected version, not latest automatic. Formats/fidelity Q-11; uploaded-file resume core retained.

**Lifecycle / classification:** All writes check RowVersion; lifecycle guard also applies to import, automation, bulk and restore. Same-owner FK(OwnerId,Id) to Resource registry identity; payload and registry lifecycle commit atomically. Payload classification defaults Private owner; credential/encrypted/hash columns are never list/search/log data. [Per-field classification](15-field-classification.md#career-resume). No ON DELETE CASCADE; approved purge service orders dependencies, rejects live references, preserves minimal audit. User-owned Trash retention is not inferred from job-log retention.

<a id="career-resumeversion"></a>
## career.ResumeVersion

Exact immutable uploaded resume file/version. Profile **V**. Status: **Technical decision**.

| Field | SQL Server type | Nullable | Meaning / validation | Key / default / reference |
| --- | --- | --- | --- | --- |
| Id | uniqueidentifier | No | Opaque stable PK generated by trusted command | No implicit default unless stated |
| OwnerId | uniqueidentifier | No | Required isolation boundary resolved server-side, immutable | FK OwnerId → Id; [platform.PersonalSpace](02-core-identity-platform.md#platform-personalspace); NO ACTION |
| CreatedAt | datetime2(7) | No | Server insert instant, DEFAULT SYSUTCDATETIME() | No implicit default unless stated |
| CreatedByUserId | uniqueidentifier | Yes | Actual actor for attribution, not owner; null only system/anonymized identity | FK CreatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| ResumeId | uniqueidentifier | No | Family | FK (OwnerId, ResumeId) → (OwnerId, Id); [career.Resume](10-assets-career-learning.md#career-resume); NO ACTION |
| VersionNumber | bigint | No | Sequence | No implicit default unless stated |
| FileObjectId | uniqueidentifier | Yes | Exactly one Clean file or immutable Document version | FK (OwnerId, FileObjectId) → (OwnerId, Id); [files.FileObject](04-files-jobs-notifications.md#files-fileobject); NO ACTION |
| Label | nvarchar(200) | Yes | Version label | No implicit default unless stated |
| ContentDigest | binary(32) | No | File checksum | No implicit default unless stated |
| DocumentResourceId | uniqueidentifier | Yes | Source Document page identity | FK (OwnerId, DocumentResourceId) → (OwnerId, Id); [platform.Resource](02-core-identity-platform.md#platform-resource); NO ACTION |
| DocumentVersion | bigint | Yes | Exact version number required with DocumentResourceId | No implicit default unless stated |

**Keys/index candidates:** PK(Id) nonclustered; internal clustering strategy in conventions. UQ(OwnerId,Id); IX(OwnerId,CreatedAt,Id). UQ OwnerId,ResumeId,VersionNumber

**Integrity / transaction:** Exactly one source: FileObjectId OR (DocumentResourceId AND DocumentVersion). ContentDigest required for selected immutable source representation. Same-owner registered version provider pins Document version; clean immutable upload pins FileReference. No retarget to latest.

**Lifecycle / classification:** Append-only; no in-place update/restore of an old row. Payload classification defaults Private owner; credential/encrypted/hash columns are never list/search/log data. [Per-field classification](15-field-classification.md#career-resumeversion). No ON DELETE CASCADE; approved purge service orders dependencies, rejects live references, preserves minimal audit. User-owned Trash retention is not inferred from job-log retention.

<a id="career-resumeshareversion"></a>
## career.ResumeShareVersion

Version pin supplement to common Sharing Engine. Profile **O**. Status: **Technical decision**.

| Field | SQL Server type | Nullable | Meaning / validation | Key / default / reference |
| --- | --- | --- | --- | --- |
| Id | uniqueidentifier | No | Opaque stable PK generated by trusted command | No implicit default unless stated |
| OwnerId | uniqueidentifier | No | Required isolation boundary resolved server-side, immutable | FK OwnerId → Id; [platform.PersonalSpace](02-core-identity-platform.md#platform-personalspace); NO ACTION |
| CreatedAt | datetime2(7) | No | Server insert instant, DEFAULT SYSUTCDATETIME() | No implicit default unless stated |
| CreatedByUserId | uniqueidentifier | Yes | Actual actor for attribution, not owner; null only system/anonymized identity | FK CreatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| UpdatedAt | datetime2(7) | No | Server write instant, initially CreatedAt | No implicit default unless stated |
| UpdatedByUserId | uniqueidentifier | Yes | Actual most recent actor; null only system/anonymized identity | FK UpdatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| RowVersion | rowversion | No | SQL-generated 8-byte optimistic concurrency token; not content history or clock | DB generated; exclude from inserts/updates |
| ResumeResourceId | uniqueidentifier | No | Resume identity | FK (OwnerId, ResumeResourceId) → (OwnerId, Id); [platform.Resource](02-core-identity-platform.md#platform-resource); NO ACTION |
| ShareLinkId | uniqueidentifier | No | Read-only link | FK (OwnerId, ShareLinkId) → (OwnerId, Id); [security.ShareLink](03-security-sharing.md#security-sharelink); NO ACTION |
| ResumeVersionId | uniqueidentifier | No | Exact exposed version | FK (OwnerId, ResumeVersionId) → (OwnerId, Id); [career.ResumeVersion](10-assets-career-learning.md#career-resumeversion); NO ACTION |

**Keys/index candidates:** PK(Id) nonclustered; internal clustering strategy in conventions. UQ(OwnerId,Id); IX(OwnerId,CreatedAt,Id). UQ OwnerId,ShareLinkId

**Integrity / transaction:** No email/contact/salary preview beyond explicitly approved file disclosure; owner preview full selected file.

**Lifecycle / classification:** All writes check RowVersion; lifecycle guard also applies to import, automation, bulk and restore. Payload classification defaults Private owner; credential/encrypted/hash columns are never list/search/log data. [Per-field classification](15-field-classification.md#career-resumeshareversion). No ON DELETE CASCADE; approved purge service orders dependencies, rejects live references, preserves minimal audit. User-owned Trash retention is not inferred from job-log retention.

<a id="learning-skill"></a>
## learning.Skill

Personal skill level and evidence. Profile **R**. Status: **Technical decision**.

| Field | SQL Server type | Nullable | Meaning / validation | Key / default / reference |
| --- | --- | --- | --- | --- |
| Id | uniqueidentifier | No | PK and same identity as platform.Resource.Id; generated by trusted command | No implicit default unless stated |
| OwnerId | uniqueidentifier | No | Required isolation boundary resolved server-side, immutable | FK OwnerId → Id; [platform.PersonalSpace](02-core-identity-platform.md#platform-personalspace); NO ACTION |
| CreatedAt | datetime2(7) | No | Server insert instant, DEFAULT SYSUTCDATETIME() | No implicit default unless stated |
| CreatedByUserId | uniqueidentifier | Yes | Actual actor for attribution, not owner; null only system/anonymized identity | FK CreatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| UpdatedAt | datetime2(7) | No | Server write instant, initially CreatedAt | No implicit default unless stated |
| UpdatedByUserId | uniqueidentifier | Yes | Actual most recent actor; null only system/anonymized identity | FK UpdatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| RowVersion | rowversion | No | SQL-generated 8-byte optimistic concurrency token; not content history or clock | DB generated; exclude from inserts/updates |
| Title | nvarchar(200) | No | Skill | No implicit default unless stated |
| Level | varchar(64) | No | Beginner, Intermediate, Advanced, Expert | CHECK allowed codes documented in meaning |
| Description | nvarchar(max) | Yes | Owner definition | No implicit default unless stated |
| Status | varchar(64) | No | Active, Archived | CHECK allowed codes documented in meaning |
| TrashBatchId | uniqueidentifier | Yes | Trash | FK (OwnerId, TrashBatchId) → (OwnerId, Id); [operations.TrashBatch](04-files-jobs-notifications.md#operations-trashbatch); NO ACTION |
| NormalizedTitle | nvarchar(200) | No | Normalized unique owner name | No implicit default unless stated |
| MergedIntoId | uniqueidentifier | Yes | Explicit merge tombstone | FK (OwnerId, MergedIntoId) → (OwnerId, Id); [learning.Skill](10-assets-career-learning.md#learning-skill); NO ACTION |
| PreArchiveState | varchar(64) | Yes | State before Archive | CHECK allowed codes documented in meaning |

**Keys/index candidates:** PK(Id) nonclustered; internal clustering strategy in conventions. UQ(OwnerId,Id); IX(OwnerId,CreatedAt,Id). UQ OwnerId,NormalizedTitle; IX OwnerId,Status,Title

**Integrity / transaction:** Level is self assessment, not verified certification.

**Lifecycle / classification:** All writes check RowVersion; lifecycle guard also applies to import, automation, bulk and restore. Same-owner FK(OwnerId,Id) to Resource registry identity; payload and registry lifecycle commit atomically. Payload classification defaults Private owner; credential/encrypted/hash columns are never list/search/log data. [Per-field classification](15-field-classification.md#learning-skill). No ON DELETE CASCADE; approved purge service orders dependencies, rejects live references, preserves minimal audit. User-owned Trash retention is not inferred from job-log retention.

<a id="learning-skillevidence"></a>
## learning.SkillEvidence

Typed evidence linking existing owner resource. Profile **O**. Status: **Technical decision**.

| Field | SQL Server type | Nullable | Meaning / validation | Key / default / reference |
| --- | --- | --- | --- | --- |
| Id | uniqueidentifier | No | Opaque stable PK generated by trusted command | No implicit default unless stated |
| OwnerId | uniqueidentifier | No | Required isolation boundary resolved server-side, immutable | FK OwnerId → Id; [platform.PersonalSpace](02-core-identity-platform.md#platform-personalspace); NO ACTION |
| CreatedAt | datetime2(7) | No | Server insert instant, DEFAULT SYSUTCDATETIME() | No implicit default unless stated |
| CreatedByUserId | uniqueidentifier | Yes | Actual actor for attribution, not owner; null only system/anonymized identity | FK CreatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| UpdatedAt | datetime2(7) | No | Server write instant, initially CreatedAt | No implicit default unless stated |
| UpdatedByUserId | uniqueidentifier | Yes | Actual most recent actor; null only system/anonymized identity | FK UpdatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| RowVersion | rowversion | No | SQL-generated 8-byte optimistic concurrency token; not content history or clock | DB generated; exclude from inserts/updates |
| SkillId | uniqueidentifier | No | Skill | FK (OwnerId, SkillId) → (OwnerId, Id); [learning.Skill](10-assets-career-learning.md#learning-skill); NO ACTION |
| ResourceId | uniqueidentifier | No | Course/certificate/document/work-log evidence | FK (OwnerId, ResourceId) → (OwnerId, Id); [platform.Resource](02-core-identity-platform.md#platform-resource); NO ACTION |
| Note | nvarchar(1000) | Yes | Interpretation | No implicit default unless stated |

**Keys/index candidates:** PK(Id) nonclustered; internal clustering strategy in conventions. UQ(OwnerId,Id); IX(OwnerId,CreatedAt,Id). UQ OwnerId,SkillId,ResourceId

**Integrity / transaction:** Link does not auto-upgrade skill or widen visibility.

**Lifecycle / classification:** All writes check RowVersion; lifecycle guard also applies to import, automation, bulk and restore. Payload classification defaults Private owner; credential/encrypted/hash columns are never list/search/log data. [Per-field classification](15-field-classification.md#learning-skillevidence). No ON DELETE CASCADE; approved purge service orders dependencies, rejects live references, preserves minimal audit. User-owned Trash retention is not inferred from job-log retention.

<a id="learning-course"></a>
## learning.Course

Personal course tracking, not LMS content hosting. Profile **R**. Status: **Technical decision**.

| Field | SQL Server type | Nullable | Meaning / validation | Key / default / reference |
| --- | --- | --- | --- | --- |
| Id | uniqueidentifier | No | PK and same identity as platform.Resource.Id; generated by trusted command | No implicit default unless stated |
| OwnerId | uniqueidentifier | No | Required isolation boundary resolved server-side, immutable | FK OwnerId → Id; [platform.PersonalSpace](02-core-identity-platform.md#platform-personalspace); NO ACTION |
| CreatedAt | datetime2(7) | No | Server insert instant, DEFAULT SYSUTCDATETIME() | No implicit default unless stated |
| CreatedByUserId | uniqueidentifier | Yes | Actual actor for attribution, not owner; null only system/anonymized identity | FK CreatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| UpdatedAt | datetime2(7) | No | Server write instant, initially CreatedAt | No implicit default unless stated |
| UpdatedByUserId | uniqueidentifier | Yes | Actual most recent actor; null only system/anonymized identity | FK UpdatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| RowVersion | rowversion | No | SQL-generated 8-byte optimistic concurrency token; not content history or clock | DB generated; exclude from inserts/updates |
| Title | nvarchar(200) | No | Course name | No implicit default unless stated |
| Provider | nvarchar(200) | Yes | Provider | No implicit default unless stated |
| Url | nvarchar(2048) | Yes | Course URL | No implicit default unless stated |
| ProgressMode | varchar(64) | No | ManualPercent or Milestones, immutable after first recorded progress | CHECK allowed codes documented in meaning |
| ManualProgress | decimal(28,8) | Yes | 0..1 only Manual | No implicit default unless stated |
| Status | varchar(64) | No | Planned, InProgress, Completed, Abandoned, Archived | CHECK allowed codes documented in meaning |
| StartedOn | date | Yes | Actual start | No implicit default unless stated |
| CompletedOn | date | Yes | Manual completion day | No implicit default unless stated |
| Notes | nvarchar(max) | Yes | Owner notes | No implicit default unless stated |
| TrashBatchId | uniqueidentifier | Yes | Trash | FK (OwnerId, TrashBatchId) → (OwnerId, Id); [operations.TrashBatch](04-files-jobs-notifications.md#operations-trashbatch); NO ACTION |
| PreArchiveState | varchar(64) | Yes | State before Archive | CHECK allowed codes documented in meaning |

**Keys/index candidates:** PK(Id) nonclustered; internal clustering strategy in conventions. UQ(OwnerId,Id); IX(OwnerId,CreatedAt,Id). IX OwnerId,Status,Title

**Integrity / transaction:** Milestones progress computed, no automatic course completion/paid-provider integration.

**Lifecycle / classification:** All writes check RowVersion; lifecycle guard also applies to import, automation, bulk and restore. Same-owner FK(OwnerId,Id) to Resource registry identity; payload and registry lifecycle commit atomically. Payload classification defaults Private owner; credential/encrypted/hash columns are never list/search/log data. [Per-field classification](15-field-classification.md#learning-course). No ON DELETE CASCADE; approved purge service orders dependencies, rejects live references, preserves minimal audit. User-owned Trash retention is not inferred from job-log retention.

<a id="learning-coursemilestone"></a>
## learning.CourseMilestone

Course progress unit. Profile **O**. Status: **Technical decision**.

| Field | SQL Server type | Nullable | Meaning / validation | Key / default / reference |
| --- | --- | --- | --- | --- |
| Id | uniqueidentifier | No | Opaque stable PK generated by trusted command | No implicit default unless stated |
| OwnerId | uniqueidentifier | No | Required isolation boundary resolved server-side, immutable | FK OwnerId → Id; [platform.PersonalSpace](02-core-identity-platform.md#platform-personalspace); NO ACTION |
| CreatedAt | datetime2(7) | No | Server insert instant, DEFAULT SYSUTCDATETIME() | No implicit default unless stated |
| CreatedByUserId | uniqueidentifier | Yes | Actual actor for attribution, not owner; null only system/anonymized identity | FK CreatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| UpdatedAt | datetime2(7) | No | Server write instant, initially CreatedAt | No implicit default unless stated |
| UpdatedByUserId | uniqueidentifier | Yes | Actual most recent actor; null only system/anonymized identity | FK UpdatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| RowVersion | rowversion | No | SQL-generated 8-byte optimistic concurrency token; not content history or clock | DB generated; exclude from inserts/updates |
| CourseId | uniqueidentifier | No | Course | FK (OwnerId, CourseId) → (OwnerId, Id); [learning.Course](10-assets-career-learning.md#learning-course); NO ACTION |
| Title | nvarchar(200) | No | Milestone | No implicit default unless stated |
| Position | int | No | Order | No implicit default unless stated |
| Completed | bit | No | Owner marker | No implicit default unless stated |
| CompletedAt | datetime2(7) | Yes | Marker instant | No implicit default unless stated |

**Keys/index candidates:** PK(Id) nonclustered; internal clustering strategy in conventions. UQ(OwnerId,Id); IX(OwnerId,CreatedAt,Id). UQ OwnerId,CourseId,Position

**Integrity / transaction:** Equal-weight completed fraction; completed flag/time agree. Empty milestones show not configured, not100%.

**Lifecycle / classification:** All writes check RowVersion; lifecycle guard also applies to import, automation, bulk and restore. Payload classification defaults Private owner; credential/encrypted/hash columns are never list/search/log data. [Per-field classification](15-field-classification.md#learning-coursemilestone). No ON DELETE CASCADE; approved purge service orders dependencies, rejects live references, preserves minimal audit. User-owned Trash retention is not inferred from job-log retention.

<a id="learning-certification"></a>
## learning.Certification

Personal certification and expiry evidence. Profile **R**. Status: **Technical decision**.

| Field | SQL Server type | Nullable | Meaning / validation | Key / default / reference |
| --- | --- | --- | --- | --- |
| Id | uniqueidentifier | No | PK and same identity as platform.Resource.Id; generated by trusted command | No implicit default unless stated |
| OwnerId | uniqueidentifier | No | Required isolation boundary resolved server-side, immutable | FK OwnerId → Id; [platform.PersonalSpace](02-core-identity-platform.md#platform-personalspace); NO ACTION |
| CreatedAt | datetime2(7) | No | Server insert instant, DEFAULT SYSUTCDATETIME() | No implicit default unless stated |
| CreatedByUserId | uniqueidentifier | Yes | Actual actor for attribution, not owner; null only system/anonymized identity | FK CreatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| UpdatedAt | datetime2(7) | No | Server write instant, initially CreatedAt | No implicit default unless stated |
| UpdatedByUserId | uniqueidentifier | Yes | Actual most recent actor; null only system/anonymized identity | FK UpdatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| RowVersion | rowversion | No | SQL-generated 8-byte optimistic concurrency token; not content history or clock | DB generated; exclude from inserts/updates |
| Title | nvarchar(200) | No | Certification name | No implicit default unless stated |
| Issuer | nvarchar(200) | No | Issuer | No implicit default unless stated |
| IssuedOn | date | Yes | Issue date if known | No implicit default unless stated |
| ExpiresOn | date | Yes | Expiry, null nonexpiring | No implicit default unless stated |
| CredentialIdEncrypted | varbinary(max) | Yes | Sensitive identifier | No implicit default unless stated |
| VerificationUrl | nvarchar(2048) | Yes | External verification link | No implicit default unless stated |
| CourseResourceId | uniqueidentifier | Yes | Related course | FK (OwnerId, CourseResourceId) → (OwnerId, Id); [platform.Resource](02-core-identity-platform.md#platform-resource); NO ACTION |
| Status | varchar(64) | No | Active, Expired, Archived | CHECK allowed codes documented in meaning |
| TrashBatchId | uniqueidentifier | Yes | Trash | FK (OwnerId, TrashBatchId) → (OwnerId, Id); [operations.TrashBatch](04-files-jobs-notifications.md#operations-trashbatch); NO ACTION |
| PreArchiveState | varchar(64) | Yes | State before Archive | CHECK allowed codes documented in meaning |

**Keys/index candidates:** PK(Id) nonclustered; internal clustering strategy in conventions. UQ(OwnerId,Id); IX(OwnerId,CreatedAt,Id). IX OwnerId,ExpiresOn,Status

**Integrity / transaction:** Expiry display derived against date/zone; auto-status materialization must not invent renewal. No claim authenticity verification by Nexora.

**Lifecycle / classification:** All writes check RowVersion; lifecycle guard also applies to import, automation, bulk and restore. Same-owner FK(OwnerId,Id) to Resource registry identity; payload and registry lifecycle commit atomically. Payload classification defaults Private owner; credential/encrypted/hash columns are never list/search/log data. [Per-field classification](15-field-classification.md#learning-certification). No ON DELETE CASCADE; approved purge service orders dependencies, rejects live references, preserves minimal audit. User-owned Trash retention is not inferred from job-log retention.

<a id="learning-plan"></a>
## learning.Plan

Personal learning plan referencing existing resources. Profile **R**. Status: **Technical decision**.

| Field | SQL Server type | Nullable | Meaning / validation | Key / default / reference |
| --- | --- | --- | --- | --- |
| Id | uniqueidentifier | No | PK and same identity as platform.Resource.Id; generated by trusted command | No implicit default unless stated |
| OwnerId | uniqueidentifier | No | Required isolation boundary resolved server-side, immutable | FK OwnerId → Id; [platform.PersonalSpace](02-core-identity-platform.md#platform-personalspace); NO ACTION |
| CreatedAt | datetime2(7) | No | Server insert instant, DEFAULT SYSUTCDATETIME() | No implicit default unless stated |
| CreatedByUserId | uniqueidentifier | Yes | Actual actor for attribution, not owner; null only system/anonymized identity | FK CreatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| UpdatedAt | datetime2(7) | No | Server write instant, initially CreatedAt | No implicit default unless stated |
| UpdatedByUserId | uniqueidentifier | Yes | Actual most recent actor; null only system/anonymized identity | FK UpdatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| RowVersion | rowversion | No | SQL-generated 8-byte optimistic concurrency token; not content history or clock | DB generated; exclude from inserts/updates |
| Title | nvarchar(200) | No | Plan name | No implicit default unless stated |
| StartsOn | date | Yes | Optional period | No implicit default unless stated |
| EndsOn | date | Yes | Optional end | No implicit default unless stated |
| Status | varchar(64) | No | Planned, Active, Completed, Archived | CHECK allowed codes documented in meaning |
| Notes | nvarchar(max) | Yes | Intent | No implicit default unless stated |
| TrashBatchId | uniqueidentifier | Yes | Trash | FK (OwnerId, TrashBatchId) → (OwnerId, Id); [operations.TrashBatch](04-files-jobs-notifications.md#operations-trashbatch); NO ACTION |
| PreArchiveState | varchar(64) | Yes | State before Archive | CHECK allowed codes documented in meaning |

**Keys/index candidates:** PK(Id) nonclustered; internal clustering strategy in conventions. UQ(OwnerId,Id); IX(OwnerId,CreatedAt,Id). IX OwnerId,Status,EndsOn

**Integrity / transaction:** No duplicate Course/Task creation without explicit user action.

**Lifecycle / classification:** All writes check RowVersion; lifecycle guard also applies to import, automation, bulk and restore. Same-owner FK(OwnerId,Id) to Resource registry identity; payload and registry lifecycle commit atomically. Payload classification defaults Private owner; credential/encrypted/hash columns are never list/search/log data. [Per-field classification](15-field-classification.md#learning-plan). No ON DELETE CASCADE; approved purge service orders dependencies, rejects live references, preserves minimal audit. User-owned Trash retention is not inferred from job-log retention.

<a id="learning-planitem"></a>
## learning.PlanItem

Ordered plan resource reference. Profile **O**. Status: **Technical decision**.

| Field | SQL Server type | Nullable | Meaning / validation | Key / default / reference |
| --- | --- | --- | --- | --- |
| Id | uniqueidentifier | No | Opaque stable PK generated by trusted command | No implicit default unless stated |
| OwnerId | uniqueidentifier | No | Required isolation boundary resolved server-side, immutable | FK OwnerId → Id; [platform.PersonalSpace](02-core-identity-platform.md#platform-personalspace); NO ACTION |
| CreatedAt | datetime2(7) | No | Server insert instant, DEFAULT SYSUTCDATETIME() | No implicit default unless stated |
| CreatedByUserId | uniqueidentifier | Yes | Actual actor for attribution, not owner; null only system/anonymized identity | FK CreatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| UpdatedAt | datetime2(7) | No | Server write instant, initially CreatedAt | No implicit default unless stated |
| UpdatedByUserId | uniqueidentifier | Yes | Actual most recent actor; null only system/anonymized identity | FK UpdatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| RowVersion | rowversion | No | SQL-generated 8-byte optimistic concurrency token; not content history or clock | DB generated; exclude from inserts/updates |
| PlanId | uniqueidentifier | No | Plan | FK (OwnerId, PlanId) → (OwnerId, Id); [learning.Plan](10-assets-career-learning.md#learning-plan); NO ACTION |
| ResourceId | uniqueidentifier | No | Course/skill/task target | FK (OwnerId, ResourceId) → (OwnerId, Id); [platform.Resource](02-core-identity-platform.md#platform-resource); NO ACTION |
| Position | int | No | Order | No implicit default unless stated |
| TargetOn | date | Yes | Planning target, not source reschedule | No implicit default unless stated |

**Keys/index candidates:** PK(Id) nonclustered; internal clustering strategy in conventions. UQ(OwnerId,Id); IX(OwnerId,CreatedAt,Id). UQ OwnerId,PlanId,Position; UQ OwnerId,PlanId,ResourceId

**Integrity / transaction:** Current source access/lifecycle rechecked; target date does not change Task Start/End.

**Lifecycle / classification:** All writes check RowVersion; lifecycle guard also applies to import, automation, bulk and restore. Payload classification defaults Private owner; credential/encrypted/hash columns are never list/search/log data. [Per-field classification](15-field-classification.md#learning-planitem). No ON DELETE CASCADE; approved purge service orders dependencies, rejects live references, preserves minimal audit. User-owned Trash retention is not inferred from job-log retention.

<a id="learning-worklog"></a>
## learning.WorkLog

Personal learning/work reflection. Profile **R**. Status: **Technical decision**.

| Field | SQL Server type | Nullable | Meaning / validation | Key / default / reference |
| --- | --- | --- | --- | --- |
| Id | uniqueidentifier | No | PK and same identity as platform.Resource.Id; generated by trusted command | No implicit default unless stated |
| OwnerId | uniqueidentifier | No | Required isolation boundary resolved server-side, immutable | FK OwnerId → Id; [platform.PersonalSpace](02-core-identity-platform.md#platform-personalspace); NO ACTION |
| CreatedAt | datetime2(7) | No | Server insert instant, DEFAULT SYSUTCDATETIME() | No implicit default unless stated |
| CreatedByUserId | uniqueidentifier | Yes | Actual actor for attribution, not owner; null only system/anonymized identity | FK CreatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| UpdatedAt | datetime2(7) | No | Server write instant, initially CreatedAt | No implicit default unless stated |
| UpdatedByUserId | uniqueidentifier | Yes | Actual most recent actor; null only system/anonymized identity | FK UpdatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| RowVersion | rowversion | No | SQL-generated 8-byte optimistic concurrency token; not content history or clock | DB generated; exclude from inserts/updates |
| Title | nvarchar(200) | No | Log title | No implicit default unless stated |
| RecordedOn | date | No | Local activity day | No implicit default unless stated |
| Notes | nvarchar(max) | Yes | Reflection | No implicit default unless stated |
| Minutes | int | Yes | Manual duration1..1440 or null if linked TimeEntry | No implicit default unless stated |
| TimeEntryResourceId | uniqueidentifier | Yes | Existing time record instead of duplicate time total | FK (OwnerId, TimeEntryResourceId) → (OwnerId, Id); [platform.Resource](02-core-identity-platform.md#platform-resource); NO ACTION |
| CourseResourceId | uniqueidentifier | Yes | Related course | FK (OwnerId, CourseResourceId) → (OwnerId, Id); [platform.Resource](02-core-identity-platform.md#platform-resource); NO ACTION |
| TaskResourceId | uniqueidentifier | Yes | Related Task | FK (OwnerId, TaskResourceId) → (OwnerId, Id); [platform.Resource](02-core-identity-platform.md#platform-resource); NO ACTION |
| TrashBatchId | uniqueidentifier | Yes | Trash | FK (OwnerId, TrashBatchId) → (OwnerId, Id); [operations.TrashBatch](04-files-jobs-notifications.md#operations-trashbatch); NO ACTION |
| Category | nvarchar(100) | Yes | User-entered category | No implicit default unless stated |
| ProjectResourceId | uniqueidentifier | Yes | Related same-owner Project | FK (OwnerId, ProjectResourceId) → (OwnerId, Id); [platform.Resource](02-core-identity-platform.md#platform-resource); NO ACTION |
| Employer | nvarchar(200) | Yes | Private employer text | No implicit default unless stated |
| Status | varchar(64) | No | Active, Archived | CHECK allowed codes documented in meaning |
| PreArchiveState | varchar(64) | Yes | State before Archive | CHECK allowed codes documented in meaning |

**Keys/index candidates:** PK(Id) nonclustered; internal clustering strategy in conventions. UQ(OwnerId,Id); IX(OwnerId,CreatedAt,Id). IX OwnerId,RecordedOn; FILTERED UQ OwnerId,TimeEntryResourceId WHERE NOT NULL

**Integrity / transaction:** Manual Minutes or linked TimeEntry duration, not both counted; no payroll/timesheet collaboration.

**Lifecycle / classification:** All writes check RowVersion; lifecycle guard also applies to import, automation, bulk and restore. Same-owner FK(OwnerId,Id) to Resource registry identity; payload and registry lifecycle commit atomically. Payload classification defaults Private owner; credential/encrypted/hash columns are never list/search/log data. [Per-field classification](15-field-classification.md#learning-worklog). No ON DELETE CASCADE; approved purge service orders dependencies, rejects live references, preserves minimal audit. User-owned Trash retention is not inferred from job-log retention.

<a id="learning-certificationversion"></a>
## learning.CertificationVersion

Renewal and certification evidence history. Profile **V**. Status: **Technical decision**.

| Field | SQL Server type | Nullable | Meaning / validation | Key / default / reference |
| --- | --- | --- | --- | --- |
| Id | uniqueidentifier | No | Opaque stable PK generated by trusted command | No implicit default unless stated |
| OwnerId | uniqueidentifier | No | Required isolation boundary resolved server-side, immutable | FK OwnerId → Id; [platform.PersonalSpace](02-core-identity-platform.md#platform-personalspace); NO ACTION |
| CreatedAt | datetime2(7) | No | Server insert instant, DEFAULT SYSUTCDATETIME() | No implicit default unless stated |
| CreatedByUserId | uniqueidentifier | Yes | Actual actor for attribution, not owner; null only system/anonymized identity | FK CreatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| CertificationId | uniqueidentifier | No | Certificate | FK (OwnerId, CertificationId) → (OwnerId, Id); [learning.Certification](10-assets-career-learning.md#learning-certification); NO ACTION |
| VersionNumber | bigint | No | Sequence | No implicit default unless stated |
| SafeSnapshotJson | nvarchar(max) | No | Issuer/date/expiry and file references | ISJSON + versioned allowlist; see payload contracts |
| SensitiveSnapshotEncrypted | varbinary(max) | Yes | Credential ID history | No implicit default unless stated |
| Reason | nvarchar(2000) | Yes | Renewal/edit explanation | No implicit default unless stated |

**Keys/index candidates:** PK(Id) nonclustered; internal clustering strategy in conventions. UQ(OwnerId,Id); IX(OwnerId,CreatedAt,Id). UQ OwnerId,CertificationId,VersionNumber

**Integrity / transaction:** Renewal retains prior evidence, invalidates stale expiry alerts; file references pin historical evidence.

**Lifecycle / classification:** Append-only; no in-place update/restore of an old row. Payload classification defaults Private owner; credential/encrypted/hash columns are never list/search/log data. [Per-field classification](15-field-classification.md#learning-certificationversion). No ON DELETE CASCADE; approved purge service orders dependencies, rejects live references, preserves minimal audit. User-owned Trash retention is not inferred from job-log retention.

<a id="assets-digitalassetversion"></a>
## assets.DigitalAssetVersion

Entered and observed metadata change history. Profile **V**. Status: **Technical decision**.

| Field | SQL Server type | Nullable | Meaning / validation | Key / default / reference |
| --- | --- | --- | --- | --- |
| Id | uniqueidentifier | No | Opaque stable PK generated by trusted command | No implicit default unless stated |
| OwnerId | uniqueidentifier | No | Required isolation boundary resolved server-side, immutable | FK OwnerId → Id; [platform.PersonalSpace](02-core-identity-platform.md#platform-personalspace); NO ACTION |
| CreatedAt | datetime2(7) | No | Server insert instant, DEFAULT SYSUTCDATETIME() | No implicit default unless stated |
| CreatedByUserId | uniqueidentifier | Yes | Actual actor for attribution, not owner; null only system/anonymized identity | FK CreatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| DigitalAssetId | uniqueidentifier | No | Asset | FK (OwnerId, DigitalAssetId) → (OwnerId, Id); [assets.DigitalAsset](10-assets-career-learning.md#assets-digitalasset); NO ACTION |
| VersionNumber | bigint | No | Sequence | No implicit default unless stated |
| SnapshotJson | nvarchar(max) | No | Typed nonsecret subtype/current/observed fields with provenance | ISJSON + versioned allowlist; see payload contracts |
| Reason | nvarchar(2000) | Yes | Manual correction/renewal note | No implicit default unless stated |

**Keys/index candidates:** PK(Id) nonclustered; internal clustering strategy in conventions. UQ(OwnerId,Id); IX(OwnerId,CreatedAt,Id). UQ OwnerId,DigitalAssetId,VersionNumber

**Integrity / transaction:** Credentials/secret license keys remain Vault references only, including historical snapshot.

**Lifecycle / classification:** Append-only; no in-place update/restore of an old row. Payload classification defaults Private owner; credential/encrypted/hash columns are never list/search/log data. [Per-field classification](15-field-classification.md#assets-digitalassetversion). No ON DELETE CASCADE; approved purge service orders dependencies, rejects live references, preserves minimal audit. User-owned Trash retention is not inferred from job-log retention.

<a id="assets-component"></a>
## assets.Component

Owned physical component within one Asset aggregate. Profile **O**. Status: **Technical decision**.

| Field | SQL Server type | Nullable | Meaning / validation | Key / default / reference |
| --- | --- | --- | --- | --- |
| Id | uniqueidentifier | No | Opaque stable PK generated by trusted command | No implicit default unless stated |
| OwnerId | uniqueidentifier | No | Required isolation boundary resolved server-side, immutable | FK OwnerId → Id; [platform.PersonalSpace](02-core-identity-platform.md#platform-personalspace); NO ACTION |
| CreatedAt | datetime2(7) | No | Server insert instant, DEFAULT SYSUTCDATETIME() | No implicit default unless stated |
| CreatedByUserId | uniqueidentifier | Yes | Actual actor for attribution, not owner; null only system/anonymized identity | FK CreatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| UpdatedAt | datetime2(7) | No | Server write instant, initially CreatedAt | No implicit default unless stated |
| UpdatedByUserId | uniqueidentifier | Yes | Actual most recent actor; null only system/anonymized identity | FK UpdatedByUserId → Id; [identity.User](02-core-identity-platform.md#identity-user); NO ACTION |
| RowVersion | rowversion | No | SQL-generated 8-byte optimistic concurrency token; not content history or clock | DB generated; exclude from inserts/updates |
| AssetId | uniqueidentifier | No | Parent inventory item | FK (OwnerId, AssetId) → (OwnerId, Id); [assets.PersonalAsset](10-assets-career-learning.md#assets-personalasset); NO ACTION |
| Title | nvarchar(200) | No | Component label | No implicit default unless stated |
| Kind | nvarchar(100) | Yes | Component category | No implicit default unless stated |
| Model | nvarchar(200) | Yes | Part model | No implicit default unless stated |
| SerialEnvelope | varbinary(max) | Yes | Encrypted sensitive part serial | No implicit default unless stated |
| Quantity | decimal(28,8) | No | Positive quantity | No implicit default unless stated |
| Notes | nvarchar(max) | Yes | Part detail | No implicit default unless stated |

**Keys/index candidates:** PK(Id) nonclustered; internal clustering strategy in conventions. UQ(OwnerId,Id); IX(OwnerId,CreatedAt,Id). IX OwnerId,AssetId,Title

**Integrity / transaction:** Owned component included in parent history/Trash, unlike independent AssetAccessory link which never cascades source deletion.

**Lifecycle / classification:** All writes check RowVersion; lifecycle guard also applies to import, automation, bulk and restore. Payload classification defaults Private owner; credential/encrypted/hash columns are never list/search/log data. [Per-field classification](15-field-classification.md#assets-component). No ON DELETE CASCADE; approved purge service orders dependencies, rejects live references, preserves minimal audit. User-owned Trash retention is not inferred from job-log retention.

## Scoped relationship diagrams

Each edge describes a declared FK, not authorization or cascade deletion. Composite owner keys and one-to-one/unique restrictions are normative in the dictionary. Diagrams split into small neighborhoods; the exhaustive edge index is in [relations](11-relations-and-transactions.md).

~~~mermaid
erDiagram
    direction TB
    assets_PersonalAsset ||--o{ assets_AssetAccessory : "ParentAssetId"
    assets_PersonalAsset ||--o{ assets_AssetAccessory : "ChildAssetId"
    assets_PersonalAsset ||--o{ assets_AssetWarranty : "AssetId"
~~~

~~~mermaid
erDiagram
    direction TB
    assets_PersonalAsset ||--o{ assets_Repair : "AssetId"
    assets_PersonalAsset ||--o{ assets_AssetLoan : "AssetId"
    assets_PersonalAsset ||--o{ assets_AssetVersion : "AssetId"
~~~

~~~mermaid
erDiagram
    direction TB
    assets_DigitalAsset ||--o{ assets_DomainDetail : "DigitalAssetId"
    assets_DigitalAsset ||--o{ assets_HostingDetail : "DigitalAssetId"
    assets_DigitalAsset ||--o{ assets_VpsDetail : "DigitalAssetId"
~~~

~~~mermaid
erDiagram
    direction TB
    assets_DigitalAsset ||--o{ assets_CertificateDetail : "DigitalAssetId"
    assets_DigitalAsset ||--o{ assets_LicenseDetail : "DigitalAssetId"
    assets_DigitalAsset ||--o{ assets_ServiceDetail : "DigitalAssetId"
~~~

~~~mermaid
erDiagram
    direction TB
    assets_DigitalAsset ||--o{ assets_RenewalRecord : "DigitalAssetId"
    career_Company o|--o{ career_Company : "MergedIntoId"
    career_Company o|--o{ career_JobApplication : "CompanyId"
~~~

~~~mermaid
erDiagram
    direction TB
    career_ResumeVersion o|--o{ career_JobApplication : "ResumeVersionReference"
    career_JobApplication ||--o{ career_Interview : "ApplicationId"
    career_JobApplication ||--o{ career_ApplicationEvent : "ApplicationId"
~~~

~~~mermaid
erDiagram
    direction TB
    career_ResumeVersion o|--o{ career_ApplicationEvent : "ResumeVersionReference"
    career_Resume ||--o{ career_ResumeVersion : "ResumeId"
    career_ResumeVersion ||--o{ career_ResumeShareVersion : "ResumeVersionId"
~~~

~~~mermaid
erDiagram
    direction TB
    learning_Skill o|--o{ learning_Skill : "MergedIntoId"
    learning_Skill ||--o{ learning_SkillEvidence : "SkillId"
    learning_Course ||--o{ learning_CourseMilestone : "CourseId"
~~~

~~~mermaid
erDiagram
    direction TB
    learning_Plan ||--o{ learning_PlanItem : "PlanId"
    learning_Certification ||--o{ learning_CertificationVersion : "CertificationId"
    assets_DigitalAsset ||--o{ assets_DigitalAssetVersion : "DigitalAssetId"
~~~

~~~mermaid
erDiagram
    direction TB
    assets_PersonalAsset ||--o{ assets_Component : "AssetId"
~~~
