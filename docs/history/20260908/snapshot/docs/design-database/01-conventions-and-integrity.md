# Physical conventions and enforcement

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

## Types and defaults — Technical decision

| Concern | SQL / .NET | Rule |
| --- | --- | --- |
| Identity | uniqueidentifier / Guid | Server-generated opaque identifiers. Nonclustered PK; OwnerId+Id composite unique for all private rows. No ID as authorization. |
| Clustering | Unique clustered Id for G/A catalogs; unique clustered OwnerId,CreatedAt,Id for R/O/V tables | Baseline clustering keys use only expanded dictionary columns, separate from nonclustered PK. Benchmark fragmentation and write/query locality before migration. Any later bigint clustering key requires an explicit dictionary/ADR change; it is not an undocumented hidden field. |
| Instant | datetime2(7) / DateTime UTC | All instants UTC on input/output; timezone separate IANA string. Reject unspecified/local DateTime in write boundary. DEFAULT SYSUTCDATETIME on CreatedAt only. |
| Calendar day | date / DateOnly; time(0) / TimeOnly | All-day [StartDate,EndDateExclusive), never timezone-shift stored day. UI inclusive last-day maps +1day. Local recurring times resolved with explicit DST policy. |
| Text | nvarchar(n) / string | Required text trim+nonblank and upper bound; preserve user Unicode. General names200 unless feature specifies50/100. URL2048, email320, bounded enum varchar64. |
| Money and quantities | decimal(28,8) / decimal | Storage capacity is technical proposal, not approved financial rounding. Currency ISO code explicit. Validate meaningful scale by currency policy; no float/money SQL type. API decimal strings prevent JS precision loss. Q-05 must close quantization, FX and ledger sign policy. |
| Concurrency | rowversion / byte[] | Exactly one per mutable table; ETag opaque Base64; not datetime or version history. Immutable versions use bigint sequence independently. |
| JSON | nvarchar(max), ISJSON / validated DTO | SchemaVersion or enclosing versioned contract required; reject unknown/disallowed keys and oversized payloads. Never EAV fallback for field/type obligations. No assumption of SQL Server native json support. |
| Binary | varbinary(max); digest binary(32) | Cipher envelopes carry algorithm/key version/context, not plaintext keys. Object binaries live in Files storage. Digest algorithm SHA-256 unless security ADR changes versioned contract. |


Unless explicitly stated, required values have **no SQL DEFAULT**: command supplies validated value. Optional fields default NULL. State defaults are set only by documented creation command (Project NotStarted, Document Draft, ManualEvent Scheduled). Bit flags are explicit per command, not accidental zero defaults. Identity integer counters initialize0, business version/revision1; worker attempts0.

## Common profiles

R = owner registered aggregate. O = owner child/configuration. V = owner immutable history. G = system/public mutable. A = system append-only. Every table section expands profile columns. R.Id equals registry identity and has FK (OwnerId,Id)→platform.Resource(OwnerId,Id). O/V tables are not automatically shareable resources. G is **not** public by default: identity/security/operations are restricted. Public cache exceptions are explicitly listed. CreatedBy/UpdatedBy null means System or anonymized deleted principal, never owner inference.

## Reference enforcement

All private tables have non-null immutable OwnerId→PersonalSpace.Id. Private-parent references use composite (OwnerId,ParentId)→(OwnerId,Id), even when Id itself is globally unique. Actor/User references use identity key alone; these are attribution, not ownership. References to explicitly public Feed/Article/Repository cache use public key alone. NO ACTION delete/update on FKs; explicit transactional purge orders dependencies.

Same-module typed entity FKs are encouraged. Cross-module business tables cannot be mapped/read directly: use platform.Resource identity plus registered provider type validation. Core infrastructure references (PersonalSpace, Resource, FileObject, grants, operations) are allowed through published kernel contracts. A FK to a FileObject does not authorize its download.

Polymorphic references must declare permitted ResourceType, version pin policy and unavailable behavior. Registry locks plus source provider guards serialize validation/linking against purge; validation followed by unguarded insert is forbidden. Parent max-depth/cycle/immutable fields require transactional domain guards; CHECK cannot enforce cross-row trees.

## Unique indexes and nulls

Nullable optional keys use filtered unique indexes, not a naive composite unique that admits/blocks the wrong rows. SQL Server filter syntax is restricted; do not translate pseudo index annotations literally. Focus uses ActiveSlot=1 iff Running/Paused and unique(OwnerId,ActiveSlot) WHERE ActiveSlot IS NOT NULL. Owner rows with positional unique keys reorder under parent lock with two-stage temporary ranks/positions; temporary values must be outside visible positions and commit atomically. Default string collation case sensitivity is **not assumed**; normalized key columns and explicit comparisons are specified per field. Title display sort uses selected locale with stable Id tie; Documents titles never unique.

## JSON snapshots and immutability

Full semantic version contains all editable fields, child checklist/tag order and relevant config. Immutable owner/parent/type/editor identity appears as assertions and cannot be restored to a different value. Snapshots retain historical tag labels without an FK that prevents deleting an otherwise unused tag forever. Historical media pins FileReference; source purge removes references only when all aggregate/version pins can be removed under approved lifecycle.

## Security and source evidence

SQL authority plus mandatory scoped repository/query object and command guards provide owner isolation. Global EF filters are defense-in-depth, never sole authorization; raw SQL/background/import paths must use same scope. No positive permission cache can outlive revoke. DB RLS is an optional security ADR requiring context-pooling/administrative bypass tests, not falsely claimed already enforced.

Microsoft documents rowversion as an8-byte generated value rather than a clock, and EF cross-context transactions require a shared connection/transaction. These inform the design; no source claims Nexora is implemented. [rowversion](https://learn.microsoft.com/en-us/sql/t-sql/data-types/rowversion-transact-sql?view=sql-server-ver17), [EF transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions). Referenced2026-09-07; text documentation, no SQL execution.

## Acceptance

Reject cross-owner parent with real SQL FK in later integration test; reject invalid status/code/null/decimal/date representation; stale ETag produces conflict without half history; two simultaneous terminal changes serialize; retry command produces one version/one intent; nullable unique keys, 2-level trees and purge races tested against SQL Server, not in-memory EF.
