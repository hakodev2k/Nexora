# Field classification — documents / knowledge / organization / discovery

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

[Classification policy and index](../15-field-classification.md). SQL types, nullable CLR mappings and default sensitivity are design specifications, not DTO exposure permissions.

<a id="documents-folder"></a>
## documents.Folder

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| ParentFolderId | uniqueidentifier | Guid? | Private owner |
| Title | nvarchar(200) | string | Private owner |
| TrashBatchId | uniqueidentifier | Guid? | Private owner |

<a id="documents-tag"></a>
## documents.Tag

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| Name | nvarchar(80) | string | Private owner |
| NormalizedName | nvarchar(80) | string | Private owner |

<a id="documents-page"></a>
## documents.Page

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
| DocumentType | varchar(64) | string | Private owner |
| EditorMode | varchar(64) | string | Private owner |
| FolderId | uniqueidentifier | Guid? | Private owner |
| ParentPageId | uniqueidentifier | Guid? | Private owner |
| TagId | uniqueidentifier | Guid? | Private owner |
| Status | varchar(64) | string | Private owner |
| PreArchiveStatus | varchar(64) | string? | Private owner |
| ArchiveBatchId | uniqueidentifier | Guid? | Private owner |
| IconKind | varchar(64) | string? | Private owner |
| IconValue | nvarchar(100) | string? | Private owner |
| CoverFileId | uniqueidentifier | Guid? | Private owner |
| CoverCropJson | nvarchar(max) | string? | Private owner |
| CurrentVersion | bigint | long | Private owner |
| TrashBatchId | uniqueidentifier | Guid? | Private owner |

<a id="documents-pageversion"></a>
## documents.PageVersion

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| PageId | uniqueidentifier | Guid | Private owner |
| VersionNumber | bigint | long | Private owner |
| SchemaVersion | int | int | Private owner |
| Body | nvarchar(max) | string | Private owner |
| MetadataJson | nvarchar(max) | string | Private owner |
| SourceVersion | bigint | long? | Private owner |
| CommandKeyHash | binary(32) | byte[] | Secret/credential envelope |

<a id="documents-archivebatch"></a>
## documents.ArchiveBatch

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| RootPageId | uniqueidentifier | Guid | Private owner |
| ArchivedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UnarchivedAt | datetime2(7) | DateTime (UTC only)? | Private owner |

<a id="documents-archivemember"></a>
## documents.ArchiveMember

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
| PageId | uniqueidentifier | Guid | Private owner |
| PreviousStatus | varchar(64) | string | Private owner |
| RestoredAt | datetime2(7) | DateTime (UTC only)? | Private owner |

<a id="knowledge-bookmark"></a>
## knowledge.Bookmark

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| Url | nvarchar(2048) | string | Private owner |
| CanonicalUrl | nvarchar(2048) | string | Private owner |
| UrlDigest | binary(32) | byte[] | Private owner |
| Title | nvarchar(200) | string | Private owner |
| Description | nvarchar(max) | string? | Private owner |
| MetadataJson | nvarchar(max) | string | Private owner |
| Health | varchar(64) | string | Private owner |
| LastCheckedAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| Status | varchar(64) | string | Private owner |
| TrashBatchId | uniqueidentifier | Guid? | Private owner |

<a id="knowledge-snippet"></a>
## knowledge.Snippet

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
| Language | nvarchar(50) | string | Private owner |
| CurrentVersion | bigint | long | Private owner |
| Status | varchar(64) | string | Private owner |
| TrashBatchId | uniqueidentifier | Guid? | Private owner |

<a id="knowledge-snippetversion"></a>
## knowledge.SnippetVersion

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| SnippetId | uniqueidentifier | Guid | Private owner |
| VersionNumber | bigint | long | Private owner |
| SourceText | nvarchar(max) | string | Private owner |
| Description | nvarchar(max) | string? | Private owner |
| Language | nvarchar(50) | string | Private owner |
| CommandKeyHash | binary(32) | byte[] | Secret/credential envelope |
| Title | nvarchar(200) | string | Private owner |
| SourceVersion | bigint | long? | Private owner |

<a id="knowledge-readingitem"></a>
## knowledge.ReadingItem

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| SourceResourceId | uniqueidentifier | Guid? | Private owner |
| PublicArticleReference | uniqueidentifier | Guid? | Private owner |
| State | varchar(64) | string | Private owner |
| Progress | decimal(28,8) | decimal | Private owner |
| SavedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| ReadAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| TrashBatchId | uniqueidentifier | Guid? | Private owner |
| SafeTitleSnapshot | nvarchar(500) | string | Private owner |
| SafeUrlSnapshot | nvarchar(2048) | string | Private owner |

<a id="organization-tag"></a>
## organization.Tag

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| Namespace | nvarchar(100) | string | Private owner |
| Name | nvarchar(50) | string | Private owner |
| NormalizedName | nvarchar(50) | string | Private owner |
| Color | nvarchar(30) | string? | Private owner |

<a id="organization-resourcetag"></a>
## organization.ResourceTag

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| TagId | uniqueidentifier | Guid | Private owner |
| ResourceId | uniqueidentifier | Guid | Private owner |

<a id="organization-collection"></a>
## organization.Collection

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| Title | nvarchar(100) | string | Private owner |
| ModuleId | uniqueidentifier | Guid | Private owner |
| Description | nvarchar(max) | string? | Private owner |
| Status | varchar(64) | string | Private owner |
| TrashBatchId | uniqueidentifier | Guid? | Private owner |

<a id="organization-collectionmember"></a>
## organization.CollectionMember

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| CollectionId | uniqueidentifier | Guid | Private owner |
| ResourceId | uniqueidentifier | Guid | Private owner |
| Rank | decimal(28,8) | decimal | Private owner |

<a id="organization-template"></a>
## organization.Template

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
| ResourceTypeId | uniqueidentifier | Guid | Private owner |
| Title | nvarchar(200) | string | Private owner |
| SchemaVersion | int | int | Private owner |
| SeedJson | nvarchar(max) | string | Private owner |
| Status | varchar(64) | string | Private owner |
| TrashBatchId | uniqueidentifier | Guid? | Private owner |

<a id="discovery-searchprojection"></a>
## discovery.SearchProjection

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
| SourceRevision | bigint | long | Private owner |
| Title | nvarchar(200) | string | Private owner |
| SearchText | nvarchar(max) | string | Private owner |
| FacetJson | nvarchar(max) | string | Private owner |
| IndexedAt | datetime2(7) | DateTime (UTC only) | Private owner |

<a id="discovery-savedquery"></a>
## discovery.SavedQuery

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
| Title | nvarchar(100) | string | Private owner |
| SchemaVersion | int | int | Private owner |
| QueryJson | nvarchar(max) | string | Private owner |
| QueryText | nvarchar(500) | string? | Private owner |

<a id="discovery-favorite"></a>
## discovery.Favorite

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
| Rank | decimal(28,8) | decimal | Private owner |

<a id="discovery-recentitem"></a>
## discovery.RecentItem

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
| LastOpenedAt | datetime2(7) | DateTime (UTC only) | Private owner |

<a id="discovery-dashboardwidget"></a>
## discovery.DashboardWidget

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| WidgetKey | nvarchar(150) | string | Secret/credential envelope |
| Position | int | int | Private owner |
| WidthUnits | tinyint | byte | Private owner |
| SchemaVersion | int | int | Private owner |
| OptionsJson | nvarchar(max) | string | Private owner |
| Enabled | bit | bool | Private owner |
| DashboardId | uniqueidentifier | Guid | Private owner |
| ProviderVersion | nvarchar(50) | string | Private owner |

<a id="discovery-dashboard"></a>
## discovery.Dashboard

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| LayoutVersion | bigint | long | Private owner |
| Title | nvarchar(200) | string | Private owner |
