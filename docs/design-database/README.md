# Nexora database design

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Đây là data model vật lý đề xuất cho toàn bộ catalog hiện tại và extension contracts cho module tương lai, **không phải database đã tạo**. Phần không phụ thuộc quyết định lớn được giải quyết ở cấp technical design; Q-01…Q-12 vẫn giữ nguyên quyền phê duyệt của Product Owner.

| Document | Purpose |
| --- | --- |
| [Overview](00-overview.md) | Ownership, scope, feature/table mapping |
| [Conventions](01-conventions-and-integrity.md) | Types, nullability, keys, concurrency, classifications |
| [identity / platform](02-core-identity-platform.md) | Full tables/fields/keys and scoped ERDs |
| [security](03-security-sharing.md) | Full tables/fields/keys and scoped ERDs |
| [files / notifications / operations](04-files-jobs-notifications.md) | Full tables/fields/keys and scoped ERDs |
| [productivity / calendar](05-productivity-calendar.md) | Full tables/fields/keys and scoped ERDs |
| [documents / knowledge / organization / discovery](06-documents-knowledge-discovery.md) | Full tables/fields/keys and scoped ERDs |
| [finance / vault](07-finance-vault.md) | Full tables/fields/keys and scoped ERDs |
| [news / shopping / developer](08-news-shopping-developer.md) | Full tables/fields/keys and scoped ERDs |
| [automation / monitoring](09-automation-monitoring.md) | Full tables/fields/keys and scoped ERDs |
| [assets / career / learning](10-assets-career-learning.md) | Full tables/fields/keys and scoped ERDs |
| [Relations and transactions](11-relations-and-transactions.md) | Every FK plus cross-module authority and transaction boundaries |
| [Evolution and recovery](12-evolution-and-recovery.md) | New-module proof, migrations, rollout, purge/backup |
| [Query and invariant tests](13-query-and-invariant-tests.md) | Read shapes, indexes and failure/race test specification |
| [Payload contracts](14-payload-contracts.md) | Versioned JSON/encryption envelope payload fields |
| [Field classification](15-field-classification.md) | Per-field SQL/.NET mapping and sensitivity |

Tổng số bảng thiết kế: **181**. Không cố định core theo số module này: core chỉ biết stable Module/ResourceType/contracts, không chứa cột riêng cho mỗi module. Số bảng không phải thước đo implementation readiness.

[Architecture](../architecture/README.md) · [UX/UI](../ux-ui/README.md) · [Consistency review](../design-review/README.md)

## Action catalog v1

[714 operation contracts /40 feature scopes](../action-catalog/README.md), [screen bindings](../action-catalog/06-screen-bindings.md), and [database binding](../design-database/16-action-catalog-binding.md). Docs-only; separate PO approval required before implementation.
