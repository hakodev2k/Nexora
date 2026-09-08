# Field classification — news / shopping / developer

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

[Classification policy and index](../15-field-classification.md). SQL types, nullable CLR mappings and default sensitivity are design specifications, not DTO exposure permissions.

<a id="news-feed"></a>
## news.Feed

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Public-source / internal IDs |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Public-source / internal IDs |
| CreatedByUserId | uniqueidentifier | Guid? | Public-source / internal IDs |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Public-source / internal IDs |
| UpdatedByUserId | uniqueidentifier | Guid? | Public-source / internal IDs |
| RowVersion | rowversion | byte[] | Public-source / internal IDs |
| Url | nvarchar(2048) | string | Public-source / internal IDs |
| UrlDigest | binary(32) | byte[] | Public-source / internal IDs |
| Title | nvarchar(200) | string | Public-source / internal IDs |
| Etag | nvarchar(500) | string? | Public-source / internal IDs |
| LastModified | nvarchar(200) | string? | Public-source / internal IDs |
| LastSuccessAt | datetime2(7) | DateTime (UTC only)? | Public-source / internal IDs |
| LastErrorCode | nvarchar(100) | string? | Public-source / internal IDs |
| State | varchar(64) | string | Public-source / internal IDs |

<a id="news-feedsubscription"></a>
## news.FeedSubscription

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| FeedId | uniqueidentifier | Guid | Private owner |
| TitleOverride | nvarchar(200) | string? | Private owner |
| Category | nvarchar(100) | string? | Private owner |
| Enabled | bit | bool | Private owner |

<a id="news-article"></a>
## news.Article

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Public-source / internal IDs |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Public-source / internal IDs |
| CreatedByUserId | uniqueidentifier | Guid? | Public-source / internal IDs |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Public-source / internal IDs |
| UpdatedByUserId | uniqueidentifier | Guid? | Public-source / internal IDs |
| RowVersion | rowversion | byte[] | Public-source / internal IDs |
| FeedId | uniqueidentifier | Guid | Public-source / internal IDs |
| ExternalKey | nvarchar(1024) | string | Secret/credential envelope |
| ExternalKeyDigest | binary(32) | byte[] | Secret/credential envelope |
| Title | nvarchar(500) | string | Public-source / internal IDs |
| Url | nvarchar(2048) | string | Public-source / internal IDs |
| PublishedAt | datetime2(7) | DateTime (UTC only)? | Public-source / internal IDs |
| FetchedAt | datetime2(7) | DateTime (UTC only) | Public-source / internal IDs |
| SanitizedBody | nvarchar(max) | string? | Public-source / internal IDs |
| ContentDigest | binary(32) | byte[]? | Public-source / internal IDs |

<a id="news-articlestate"></a>
## news.ArticleState

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| ArticleId | uniqueidentifier | Guid | Private owner |
| ReadAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| Saved | bit | bool | Private owner |
| Progress | decimal(28,8) | decimal | Private owner |

<a id="news-topicwatch"></a>
## news.TopicWatch

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
| KeywordsJson | nvarchar(max) | string | Secret/credential envelope |
| Category | nvarchar(100) | string? | Private owner |
| Enabled | bit | bool | Private owner |
| Revision | bigint | long | Private owner |
| SourceIdsJson | nvarchar(max) | string? | Private owner |
| ExcludePhrasesJson | nvarchar(max) | string? | Private owner |

<a id="news-topicmatch"></a>
## news.TopicMatch

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| WatchId | uniqueidentifier | Guid | Private owner |
| ArticleId | uniqueidentifier | Guid | Private owner |
| WatchRevision | bigint | long | Private owner |
| MatchedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| NotificationIntentKey | nvarchar(200) | string | Secret/credential envelope |

<a id="shopping-trackedproduct"></a>
## shopping.TrackedProduct

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| Marketplace | nvarchar(100) | string | Private owner |
| ExternalProductId | nvarchar(200) | string | Private owner |
| VariantKey | nvarchar(300) | string | Secret/credential envelope |
| Title | nvarchar(300) | string | Private owner |
| Url | nvarchar(2048) | string | Private owner |
| Currency | char(3) | string | Private owner |
| State | varchar(64) | string | Private owner |
| ProviderKey | nvarchar(150) | string | Secret/credential envelope |
| LastSuccessAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| TrashBatchId | uniqueidentifier | Guid? | Private owner |
| ExternalShopId | nvarchar(200) | string? | Private owner |
| VariantOptionsJson | nvarchar(max) | string | Private owner |

<a id="shopping-priceobservation"></a>
## shopping.PriceObservation

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| ProductId | uniqueidentifier | Guid | Private owner |
| ObservedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| FetchedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| Price | decimal(28,8) | decimal? | Private owner |
| Currency | char(3) | string | Private owner |
| Availability | varchar(64) | string | Private owner |
| Outcome | varchar(64) | string | Private owner |
| ProviderKey | nvarchar(150) | string | Secret/credential envelope |
| ErrorCode | nvarchar(100) | string? | Private owner |
| ListPrice | decimal(28,8) | decimal? | Private owner |

<a id="shopping-pricealertrule"></a>
## shopping.PriceAlertRule

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| ProductId | uniqueidentifier | Guid | Private owner |
| Kind | varchar(64) | string | Private owner |
| Threshold | decimal(28,8) | decimal? | Private owner |
| Enabled | bit | bool | Private owner |
| Revision | bigint | long | Private owner |
| Armed | bit | bool | Private owner |
| LastTriggeredAt | datetime2(7) | DateTime (UTC only)? | Private owner |

<a id="shopping-pricealertevent"></a>
## shopping.PriceAlertEvent

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| RuleId | uniqueidentifier | Guid | Private owner |
| ObservationId | uniqueidentifier | Guid | Private owner |
| RuleRevision | bigint | long | Private owner |
| NotificationIntentKey | nvarchar(200) | string | Secret/credential envelope |

<a id="shopping-wishlistitem"></a>
## shopping.WishlistItem

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
| TargetAmount | decimal(28,8) | decimal? | Private owner |
| Currency | char(3) | string? | Private owner |
| TrackedProductResourceId | uniqueidentifier | Guid? | Private owner |
| Notes | nvarchar(max) | string? | Sensitive personal |
| Status | varchar(64) | string | Private owner |
| TrashBatchId | uniqueidentifier | Guid? | Private owner |
| Quantity | decimal(28,8) | decimal | Private owner |

<a id="shopping-comparison"></a>
## shopping.Comparison

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
| CriteriaJson | nvarchar(max) | string | Private owner |
| Notes | nvarchar(max) | string? | Sensitive personal |

<a id="shopping-comparisonitem"></a>
## shopping.ComparisonItem

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| ComparisonId | uniqueidentifier | Guid | Private owner |
| WishlistResourceId | uniqueidentifier | Guid | Private owner |
| Position | tinyint | byte | Private owner |
| ValuesJson | nvarchar(max) | string | Private owner |

<a id="shopping-seller"></a>
## shopping.Seller

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
| Contact | nvarchar(1000) | string? | Sensitive personal |
| Notes | nvarchar(max) | string? | Sensitive personal |
| MergedIntoId | uniqueidentifier | Guid? | Private owner |

<a id="shopping-purchaseorder"></a>
## shopping.PurchaseOrder

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| SellerId | uniqueidentifier | Guid? | Private owner |
| OrderNumber | nvarchar(200) | string? | Private owner |
| PurchasedOn | date | DateOnly | Private owner |
| Currency | char(3) | string | Private owner |
| Status | varchar(64) | string | Private owner |
| ShippingAmount | decimal(28,8) | decimal | Private owner |
| Notes | nvarchar(max) | string? | Sensitive personal |
| FinanceResourceId | uniqueidentifier | Guid? | Private owner |
| TrashBatchId | uniqueidentifier | Guid? | Private owner |
| TaxAmount | decimal(28,8) | decimal | Private owner |
| DiscountAmount | decimal(28,8) | decimal | Private owner |

<a id="shopping-orderline"></a>
## shopping.OrderLine

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| OrderId | uniqueidentifier | Guid | Private owner |
| Title | nvarchar(200) | string | Private owner |
| Quantity | decimal(28,8) | decimal | Private owner |
| UnitAmount | decimal(28,8) | decimal | Private owner |
| Variant | nvarchar(300) | string? | Private owner |
| WishlistResourceId | uniqueidentifier | Guid? | Private owner |
| AssetResourceId | uniqueidentifier | Guid? | Private owner |

<a id="shopping-returnrecord"></a>
## shopping.ReturnRecord

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| OrderLineId | uniqueidentifier | Guid | Private owner |
| Quantity | decimal(28,8) | decimal | Private owner |
| RefundAmount | decimal(28,8) | decimal? | Private owner |
| ReturnedOn | date | DateOnly | Private owner |
| Reason | nvarchar(2000) | string? | Sensitive personal |

<a id="shopping-warranty"></a>
## shopping.Warranty

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| OrderLineId | uniqueidentifier | Guid? | Private owner |
| Title | nvarchar(200) | string | Private owner |
| StartsOn | date | DateOnly? | Private owner |
| EndsOn | date | DateOnly? | Private owner |
| Terms | nvarchar(max) | string? | Private owner |
| Provider | nvarchar(200) | string? | Private owner |
| ClaimContact | nvarchar(1000) | string? | Sensitive personal |
| CoverageKind | varchar(64) | string | Private owner |

<a id="developer-tooldefinition"></a>
## developer.ToolDefinition

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Restricted system |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| CreatedByUserId | uniqueidentifier | Guid? | Restricted system |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Restricted system |
| UpdatedByUserId | uniqueidentifier | Guid? | Restricted system |
| RowVersion | rowversion | byte[] | Restricted system |
| Code | nvarchar(100) | string | Restricted system |
| Title | nvarchar(200) | string | Restricted system |
| Category | nvarchar(100) | string | Restricted system |
| ContractVersion | nvarchar(50) | string | Restricted system |
| ExecutionKind | varchar(64) | string | Restricted system |
| NetworkPolicyKey | nvarchar(150) | string? | Secret/credential envelope |

<a id="developer-toolfavorite"></a>
## developer.ToolFavorite

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| ToolId | uniqueidentifier | Guid | Private owner |
| Rank | decimal(28,8) | decimal | Private owner |

<a id="developer-toolhistory"></a>
## developer.ToolHistory

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| ToolId | uniqueidentifier | Guid | Private owner |
| InputJson | nvarchar(max) | string | Private owner |
| OutputJson | nvarchar(max) | string? | Private owner |
| UsedAt | datetime2(7) | DateTime (UTC only) | Private owner |

<a id="developer-repository"></a>
## developer.Repository

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Public-source / internal IDs |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Public-source / internal IDs |
| CreatedByUserId | uniqueidentifier | Guid? | Public-source / internal IDs |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Public-source / internal IDs |
| UpdatedByUserId | uniqueidentifier | Guid? | Public-source / internal IDs |
| RowVersion | rowversion | byte[] | Public-source / internal IDs |
| ExternalId | bigint | long | Public-source / internal IDs |
| FullName | nvarchar(300) | string | Public-source / internal IDs |
| Url | nvarchar(2048) | string | Public-source / internal IDs |
| Description | nvarchar(2000) | string? | Public-source / internal IDs |
| Language | nvarchar(100) | string? | Public-source / internal IDs |
| CreatedAtSource | datetime2(7) | DateTime (UTC only) | Public-source / internal IDs |
| Stars | bigint | long | Public-source / internal IDs |
| Forks | bigint | long | Public-source / internal IDs |
| Subscribers | bigint | long? | Public-source / internal IDs |
| FetchedAt | datetime2(7) | DateTime (UTC only) | Public-source / internal IDs |
| Archived | bit | bool | Public-source / internal IDs |
| OwnerLogin | nvarchar(200) | string | Public-source / internal IDs |
| OwnerAvatarUrl | nvarchar(2048) | string? | Public-source / internal IDs |
| TopicsJson | nvarchar(max) | string? | Public-source / internal IDs |
| LicenseCode | nvarchar(100) | string? | Public-source / internal IDs |
| DefaultBranch | nvarchar(200) | string? | Public-source / internal IDs |
| UpdatedAtSource | datetime2(7) | DateTime (UTC only)? | Public-source / internal IDs |
| PushedAtSource | datetime2(7) | DateTime (UTC only)? | Public-source / internal IDs |

<a id="developer-rankingsnapshot"></a>
## developer.RankingSnapshot

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Public-source / internal IDs |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Public-source / internal IDs |
| CreatedByUserId | uniqueidentifier | Guid? | Public-source / internal IDs |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Public-source / internal IDs |
| UpdatedByUserId | uniqueidentifier | Guid? | Public-source / internal IDs |
| RowVersion | rowversion | byte[] | Public-source / internal IDs |
| Kind | varchar(64) | string | Public-source / internal IDs |
| WindowStart | datetime2(7) | DateTime (UTC only) | Public-source / internal IDs |
| WindowEnd | datetime2(7) | DateTime (UTC only) | Public-source / internal IDs |
| QueryJson | nvarchar(max) | string | Public-source / internal IDs |
| FetchedAt | datetime2(7) | DateTime (UTC only) | Public-source / internal IDs |
| Completeness | varchar(64) | string | Public-source / internal IDs |
| ErrorCode | nvarchar(100) | string? | Public-source / internal IDs |
| RuleVersion | nvarchar(50) | string | Public-source / internal IDs |

<a id="developer-rankingentry"></a>
## developer.RankingEntry

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Public-source / internal IDs |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Public-source / internal IDs |
| CreatedByUserId | uniqueidentifier | Guid? | Public-source / internal IDs |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Public-source / internal IDs |
| UpdatedByUserId | uniqueidentifier | Guid? | Public-source / internal IDs |
| RowVersion | rowversion | byte[] | Public-source / internal IDs |
| SnapshotId | uniqueidentifier | Guid | Public-source / internal IDs |
| RepositoryId | uniqueidentifier | Guid | Public-source / internal IDs |
| Rank | tinyint | byte | Public-source / internal IDs |
| StarsAtSnapshot | bigint | long | Public-source / internal IDs |
| ForksAtSnapshot | bigint | long | Public-source / internal IDs |

<a id="developer-savedrepository"></a>
## developer.SavedRepository

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| RepositoryId | uniqueidentifier | Guid | Private owner |
| Notes | nvarchar(2000) | string? | Sensitive personal |
