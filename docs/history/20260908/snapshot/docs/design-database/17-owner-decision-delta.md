# Physical design delta — Product Owner decisions 2026-09-07

Source [DEC-20260907](../requirements/10-owner-decisions-20260907.md). **Design only; không DDL/migration/DB runtime.** Baseline181 table specifications retained as historical design inventory; add4 tables below =185 documented table specifications including conditional/paused/superseded proposals. This is not185 active tables or readiness confirmation. Current fields below override overlapping earlier proposal fields; indexes are candidates to verify against real SQL.

## Existing-table changes

| Table.field | SQL type / null / default | Meaning / constraint / sensitivity |
| --- | --- | --- |
| identity.User.IsDeleted | bit NOT NULL DEFAULT0 | Authoritative account disable/delete guard; restricted metadata |
| identity.User.DeletedAt | datetime2(7) NULL | Required iff IsDeleted=true; UTC server instant |
| identity.User.DeletedByUserId | uniqueidentifier NULL | FK identity.User.Id NO ACTION; actor may be same account; attribution not ownership |
| identity.User.State | varchar(64) NOT NULL | PendingVerification/Active/Disabled/Deleted; replaces unapproved DeletionPending grace state; State=Deleted iff IsDeleted=true |
| identity.User.Locale | nvarchar(35) NOT NULL DEFAULT 'vi' | Allowlist vi/en for UI; existing null becomes vi in future planned migration; no currency/timezone mutation |
| platform.Module.SharingEpoch | bigint NOT NULL DEFAULT1 | Positive monotonic epoch, increment every sharing disable; not reset on re-enable |
| security.ShareLink.IsDeleted | bit NOT NULL DEFAULT0 | Invalidated link object; no owner active listing/resolve |
| security.ShareLink.InvalidatedAt | datetime2(7) NULL | Required iff IsDeleted=true |
| security.ShareLink.InvalidationReason | varchar(64) NULL | OwnerRevoke/SharingDisabled/SourceDeleted/AccountDeleted; codes only, no resource body |
| security.ShareLink.IssuedSharingEpoch | bigint NOT NULL | Snapshot at create; resolve must equal current Module.SharingEpoch; no retroactive epoch update |
| security.ShareLink.RevokedAt | datetime2(7) NULL | Existing marker synchronized with irreversible invalidation |
| security.ShareLink.SuspendedByTrash | RETIRED proposal, no current column | Do not implement suspended/revivable link; replaced by irreversible tombstone/epoch |
| vault.Item.IsDeleted | bit NOT NULL DEFAULT0 | Delete marker for all contained values; registry Trash agreement in same transaction |
| vault.Item.DeletedAt | datetime2(7) NULL | Required iff IsDeleted=true; current marker cleared only by authorized Recovery |
| vault.Item.DeletedByUserId | uniqueidentifier NULL | Actual actor FK identity.User.Id NO ACTION |
| vault.KeyEnvelope.RecoveryWrappedKey | varbinary(max) NOT NULL | Wrapped owner data key under separate recovery KEK; never plaintext |
| vault.KeyEnvelope.RecoveryKeyReference | nvarchar(200) NOT NULL | Protected provider handle, not key bytes |
| vault.KeyEnvelope.RecoveryKeyVersion | nvarchar(100) NOT NULL | Versioned recovery wrap; retain versions needed to read kept ciphertext |

Existing User NormalizedEmail UQ remains across deleted accounts. Query User.IsDeleted+State before auth and owner resolution; do not use filtered email uniqueness to silently reuse another retained account. Index User(State,IsDeleted,Id), Vault(Item OwnerId,IsDeleted,Id), ShareLink(OwnerId,IsDeleted,ResourceId) candidates. ModuleId from ResourceType/Module binding is trusted; token cannot supply its own policy scope.

Vault ItemVersion ciphertext remains append-only; Item.IsDeleted is aggregate visibility authority, not plaintext password-value deletion. A removed field/value in a newer encrypted payload keeps its previous encrypted version; logical field tombstone can be represented inside versioned encrypted payload. Do not add IsDeleted to every historical ciphertext row or overwrite a historical value to achieve “soft delete”. No root/wrapped-key destruction follows Item/Account delete.

## New table conventions

All tables below have explicit fields, PK, own-scoped FKs, NO ACTION deletion, UTC server times and rowversion. No ON DELETE CASCADE. R-profile resource identity agrees with platform.Resource; O-profile ordinary owned metadata does not require a resource registry record. Actor is separate from OwnerId. SQL types follow existing dictionary and .NET mapping: GUID→Guid, bit→bool, date→DateOnly, datetime2 UTC→DateTime UTC, decimal→decimal, rowversion→byte[8]. Nullable maps nullable CLR. Strings bounded, not generic data JSON.

<a id="finance-manualcategory"></a>
## finance.ManualCategory — new O-profile, current basic scope

| Field | SQL type | Null/default | Contract |
| --- | --- | --- | --- |
| Id | uniqueidentifier | No; generated | PK |
| OwnerId | uniqueidentifier | No | FK PersonalSpace.Id; immutable |
| Title | nvarchar(100) | No | Trim1–100; user-defined, unique normalized owner/name |
| NormalizedTitle | nvarchar(100) | No | Server normalization, not user-patchable |
| CreatedAt | datetime2(7) | No; SYSUTCDATETIME() | UTC |
| CreatedByUserId | uniqueidentifier | Yes | FK User.Id actor |
| UpdatedAt | datetime2(7) | No | UTC latest metadata edit |
| UpdatedByUserId | uniqueidentifier | Yes | FK User.Id actor |
| RowVersion | rowversion | No; generated | Concurrency |

PK Id; UQ(OwnerId,Id); UQ(OwnerId,NormalizedTitle). No Income/Expense/Account classification required by this basic form. Remove unused category only; block if any ManualRecord references it. Historical record label stays source history, do not rewrite amounts. Sensitivity: private owner metadata; no public projection by default.

<a id="finance-manualrecord"></a>
## finance.ManualRecord — new R-profile, current basic scope

| Field | SQL type | Null/default | Contract |
| --- | --- | --- | --- |
| Id | uniqueidentifier | No; generated | PK, same identity as Resource.Id |
| OwnerId | uniqueidentifier | No | FK PersonalSpace.Id; immutable |
| CategoryId | uniqueidentifier | No | FK(OwnerId,CategoryId)→ManualCategory(OwnerId,Id) |
| Amount | decimal(28,8) | No | User input nonnegative finite price; no float/implicit FX |
| CurrencyCode | char(3) | No | Explicit unit selected by User; default currency remains open; never infer from language |
| OccurredOn | date | No | Default user-local current date at form opening, editable; not timezone-shifted on UI locale change |
| Note | nvarchar(2000) | Yes | Private manual note, no secret auto-import |
| CreatedAt | datetime2(7) | No; SYSUTCDATETIME() | UTC |
| CreatedByUserId | uniqueidentifier | Yes | FK User.Id |
| UpdatedAt | datetime2(7) | No | UTC |
| UpdatedByUserId | uniqueidentifier | Yes | FK User.Id |
| Revision | bigint | No;1 | Positive semantic history sequence |
| RowVersion | rowversion | No; generated | Concurrency |

UQ(OwnerId,Id); same-owner FK to Resource; IX(OwnerId,OccurredOn,Id), IX(OwnerId,CategoryId,CurrencyCode,OccurredOn). Category/amount required by PO; date/unit/validation are technical details making price meaningful. CRUD basic scope currently read/create/update; deletion/ledger correction semantics remain Q-05-R. Edits record safe old/new amount/category in restricted owner Activity, not public audit/search. Summaries only group same currency/category, no income/expense/net worth/interest or account balance claims. Old finance.Account/Transaction/Leg/Budget/Debt proposals remain conditional and are not dependencies of basic ManualRecord Save.

<a id="vault-recoveryrequest"></a>
## vault.RecoveryRequest — new O-profile, explicit recovery context

| Field | SQL type | Null/default | Contract |
| --- | --- | --- | --- |
| Id | uniqueidentifier | No; generated | PK |
| OwnerId | uniqueidentifier | No | FK PersonalSpace.Id; target owner immutable |
| RequestedByUserId | uniqueidentifier | No | FK User.Id; must own OwnerId |
| Kind | varchar(32) | No | DeletedItem/HistoricalVersion/KeyAccess |
| ItemId | uniqueidentifier | Yes | Same-owner FK Item; required for first2kinds |
| ItemVersionId | uniqueidentifier | Yes | Same-owner FK ItemVersion; HistoricalVersion only; belongs ItemId |
| State | varchar(32) | No;Requested | Requested/Authorized/Running/Succeeded/Rejected/Canceled/Failed |
| OwnerProofReference | nvarchar(200) | No | Opaque current verified proof reference; no token/secret |
| Reason | nvarchar(1000) | No |20–1000characters; human reason, no secret values |
| AuthorizedByUserId | uniqueidentifier | Yes | FK User.Id; must be current SuperAdmin at authorization/execution |
| AuthorizedAt | datetime2(7) | Yes | UTC, required once Authorized |
| AuthorizationExpiresAt | datetime2(7) | Yes | Technical default30min scoped execution lease, no permanent recovery authority |
| CompletedAt | datetime2(7) | Yes | UTC terminal outcome |
| FailureCode | varchar(64) | Yes | Allowlisted redacted diagnostic code |
| CommandKeyHash | binary(32) | No | Owner+request idempotency; no raw bearer token |
| CreatedAt | datetime2(7) | No;SYSUTCDATETIME() | UTC |
| CreatedByUserId | uniqueidentifier | Yes | FK User.Id actual actor |
| UpdatedAt | datetime2(7) | No | UTC |
| UpdatedByUserId | uniqueidentifier | Yes | FK User.Id actual actor |
| RowVersion | rowversion | No;generated | Optimistic concurrency |

UQ(OwnerId,Id), UQ(OwnerId,CommandKeyHash), IX(State,CreatedAt,Id). State graph terminal cannot silently authorize retry; revalidate actor/proof/keys and new execution attempt under same intent. Use existing audit/outbox/operation-attempt storage, no plaintext result field. Request cannot authorize restore for deleted account until account recovery policy resolved. Row is restricted security metadata, not share/search payload.

<a id="career-calendarlink"></a>
## career.CalendarLink — new O-profile, replaces Interview workflow proposal

| Field | SQL type | Null/default | Contract |
| --- | --- | --- | --- |
| Id | uniqueidentifier | No;generated | PK |
| OwnerId | uniqueidentifier | No | FK PersonalSpace.Id |
| JobApplicationId | uniqueidentifier | No | Same-owner FK JobApplication |
| CalendarEventId | uniqueidentifier | No | Same-owner FK calendar.ManualEvent; never Task projection |
| CreatedAt | datetime2(7) | No;SYSUTCDATETIME() | UTC |
| CreatedByUserId | uniqueidentifier | Yes | FK User.Id actor |
| UpdatedAt | datetime2(7) | No | UTC |
| UpdatedByUserId | uniqueidentifier | Yes | FK User.Id actor |
| RowVersion | rowversion | No;generated | Concurrency |

UQ(OwnerId,Id); UQ(OwnerId,JobApplicationId,CalendarEventId). No Start/End/Reminder/meetingURL/participants/feedback columns: Calendar owns them. Cross-module API contract performs same-owner check even though database defense includes composite FK. Create-and-link transaction preserves idempotency; unlink removes reference only. Source Event may later become terminal; reading link shows current state, no forced reopen. career.Interview proposal is superseded for current scope, not an extra live workflow.

## Scoped ERDs

~~~mermaid
erDiagram
  PersonalSpace ||--o{ ManualCategory : owns
  ManualCategory ||--o{ ManualRecord : categorizes
  PersonalSpace ||--o{ ManualRecord : owns
  Resource ||--o| ManualRecord : identity
~~~

~~~mermaid
erDiagram
  PersonalSpace ||--o{ RecoveryRequest : target
  User ||--o{ RecoveryRequest : actor
  Item ||--o{ RecoveryRequest : optional_item
  ItemVersion ||--o{ RecoveryRequest : optional_version
~~~

~~~mermaid
erDiagram
  JobApplication ||--o{ CalendarLink : references
  ManualEvent ||--o{ CalendarLink : references
  PersonalSpace ||--o{ CalendarLink : owns
~~~

## Migration, retention and verification design

No production data exists in this docs phase. Future planned migration adds fields with safe backfill: old Locale null→vi; no existing live links get a newer epoch silently; mark unsafe legacy links invalid until reissued; Vault recovery wrap must exist before claiming recoverable. Do not erase old keys/history, reverse revoked accounts/links from backup or silently map old Admin restore permissions to new SuperAdmin recovery actions.

Test real SQL: State/IsDeleted agreement, same-owner FK enforcement, concurrent share disable/create, deletion cohort invalidation, old token after source restore, duplicate recovery request and failed decrypt rollback, manual record no ledger side effect, linked Event single reminder. Per-field new-column classifications are explicit above; original field-classification report is a baseline snapshot supplemented by this delta, not a complete current-field count.
