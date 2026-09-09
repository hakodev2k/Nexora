# Field classification — finance / vault

> Current specification · reconciled 2026-09-08 · Docs-only. [Previous version](../../history/20260908/snapshot/docs/design-database/classification/07-finance-vault.md) is historical evidence, not implementation input.

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

[Classification policy and index](../15-field-classification.md). SQL types, nullable CLR mappings and default sensitivity are design specifications, not DTO exposure permissions.

<a id="finance-account"></a>
## finance.Account

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Sensitive personal |
| OwnerId | uniqueidentifier | Guid | Sensitive personal |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| CreatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| UpdatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| RowVersion | rowversion | byte[] | Sensitive personal |
| Title | nvarchar(200) | string | Sensitive personal |
| Kind | varchar(64) | string | Sensitive personal |
| Currency | char(3) | string | Sensitive personal |
| OpeningAmount | decimal(28,8) | decimal | Sensitive personal |
| OpeningDate | date | DateOnly | Sensitive personal |
| Status | varchar(64) | string | Sensitive personal |

<a id="finance-category"></a>
## finance.Category

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Sensitive personal |
| OwnerId | uniqueidentifier | Guid | Sensitive personal |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| CreatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| UpdatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| RowVersion | rowversion | byte[] | Sensitive personal |
| Title | nvarchar(100) | string | Sensitive personal |
| Kind | varchar(64) | string | Sensitive personal |
| Archived | bit | bool | Sensitive personal |

<a id="finance-transaction"></a>
## finance.Transaction

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Sensitive personal |
| OwnerId | uniqueidentifier | Guid | Sensitive personal |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| CreatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| UpdatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| RowVersion | rowversion | byte[] | Sensitive personal |
| Kind | varchar(64) | string | Sensitive personal |
| State | varchar(64) | string | Sensitive personal |
| OccurredOn | date | DateOnly | Sensitive personal |
| Payee | nvarchar(200) | string? | Sensitive personal |
| Memo | nvarchar(2000) | string? | Sensitive personal |
| PostedAt | datetime2(7) | DateTime (UTC only)? | Sensitive personal |
| ReversesTransactionId | uniqueidentifier | Guid? | Sensitive personal |
| ExternalImportKey | nvarchar(200) | string? | Secret/credential envelope |
| Revision | bigint | long | Sensitive personal |

<a id="finance-transactionleg"></a>
## finance.TransactionLeg

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Sensitive personal |
| OwnerId | uniqueidentifier | Guid | Sensitive personal |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| CreatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| UpdatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| RowVersion | rowversion | byte[] | Sensitive personal |
| TransactionId | uniqueidentifier | Guid | Sensitive personal |
| AccountId | uniqueidentifier | Guid | Sensitive personal |
| SignedAmount | decimal(28,8) | decimal | Sensitive personal |
| Currency | char(3) | string | Sensitive personal |
| Position | tinyint | byte | Sensitive personal |

<a id="finance-transactionsplit"></a>
## finance.TransactionSplit

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Sensitive personal |
| OwnerId | uniqueidentifier | Guid | Sensitive personal |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| CreatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| UpdatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| RowVersion | rowversion | byte[] | Sensitive personal |
| TransactionId | uniqueidentifier | Guid | Sensitive personal |
| CategoryId | uniqueidentifier | Guid | Sensitive personal |
| Amount | decimal(28,8) | decimal | Sensitive personal |
| Memo | nvarchar(1000) | string? | Sensitive personal |

<a id="finance-transactionversion"></a>
## finance.TransactionVersion

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Sensitive personal |
| OwnerId | uniqueidentifier | Guid | Sensitive personal |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| CreatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| TransactionId | uniqueidentifier | Guid | Sensitive personal |
| VersionNumber | bigint | long | Sensitive personal |
| SnapshotJson | nvarchar(max) | string | Sensitive personal |
| Reason | nvarchar(2000) | string? | Sensitive personal |

<a id="finance-bill"></a>
## finance.Bill

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Sensitive personal |
| OwnerId | uniqueidentifier | Guid | Sensitive personal |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| CreatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| UpdatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| RowVersion | rowversion | byte[] | Sensitive personal |
| Title | nvarchar(200) | string | Sensitive personal |
| Amount | decimal(28,8) | decimal | Sensitive personal |
| Currency | char(3) | string | Sensitive personal |
| DueOn | date | DateOnly | Sensitive personal |
| State | varchar(64) | string | Sensitive personal |
| RecurringRuleId | uniqueidentifier | Guid? | Sensitive personal |
| OccurrenceKey | nvarchar(200) | string? | Secret/credential envelope |

<a id="finance-paymentallocation"></a>
## finance.PaymentAllocation

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Sensitive personal |
| OwnerId | uniqueidentifier | Guid | Sensitive personal |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| CreatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| UpdatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| RowVersion | rowversion | byte[] | Sensitive personal |
| BillId | uniqueidentifier | Guid? | Sensitive personal |
| DebtId | uniqueidentifier | Guid? | Sensitive personal |
| TransactionId | uniqueidentifier | Guid | Sensitive personal |
| Amount | decimal(28,8) | decimal | Sensitive personal |
| CommandKeyHash | binary(32) | byte[] | Secret/credential envelope |

<a id="finance-recurringrule"></a>
## finance.RecurringRule

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Sensitive personal |
| OwnerId | uniqueidentifier | Guid | Sensitive personal |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| CreatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| UpdatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| RowVersion | rowversion | byte[] | Sensitive personal |
| Title | nvarchar(200) | string | Sensitive personal |
| ScheduleJson | nvarchar(max) | string | Sensitive personal |
| TemplateJson | nvarchar(max) | string | Sensitive personal |
| Enabled | bit | bool | Sensitive personal |
| Revision | bigint | long | Sensitive personal |
| NextDueAt | datetime2(7) | DateTime (UTC only)? | Sensitive personal |

<a id="finance-subscription"></a>
## finance.Subscription

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Sensitive personal |
| OwnerId | uniqueidentifier | Guid | Sensitive personal |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| CreatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| UpdatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| RowVersion | rowversion | byte[] | Sensitive personal |
| Title | nvarchar(200) | string | Sensitive personal |
| Currency | char(3) | string | Sensitive personal |
| Amount | decimal(28,8) | decimal | Sensitive personal |
| Cycle | varchar(64) | string | Sensitive personal |
| NextRenewalOn | date | DateOnly? | Sensitive personal |
| Status | varchar(64) | string | Sensitive personal |
| VaultResourceId | uniqueidentifier | Guid? | Sensitive personal |

<a id="finance-subscriptionprice"></a>
## finance.SubscriptionPrice

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Sensitive personal |
| OwnerId | uniqueidentifier | Guid | Sensitive personal |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| CreatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| SubscriptionId | uniqueidentifier | Guid | Sensitive personal |
| EffectiveOn | date | DateOnly | Sensitive personal |
| Amount | decimal(28,8) | decimal | Sensitive personal |
| Currency | char(3) | string | Sensitive personal |
| Note | nvarchar(1000) | string? | Sensitive personal |

<a id="finance-budget"></a>
## finance.Budget

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Sensitive personal |
| OwnerId | uniqueidentifier | Guid | Sensitive personal |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| CreatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| UpdatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| RowVersion | rowversion | byte[] | Sensitive personal |
| Title | nvarchar(200) | string | Sensitive personal |
| PeriodStart | date | DateOnly | Sensitive personal |
| PeriodEnd | date | DateOnly | Sensitive personal |
| Currency | char(3) | string | Sensitive personal |
| Mode | varchar(64) | string | Sensitive personal |

<a id="finance-budgetline"></a>
## finance.BudgetLine

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Sensitive personal |
| OwnerId | uniqueidentifier | Guid | Sensitive personal |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| CreatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| UpdatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| RowVersion | rowversion | byte[] | Sensitive personal |
| BudgetId | uniqueidentifier | Guid | Sensitive personal |
| CategoryId | uniqueidentifier | Guid | Sensitive personal |
| LimitAmount | decimal(28,8) | decimal | Sensitive personal |

<a id="finance-savingsgoal"></a>
## finance.SavingsGoal

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Sensitive personal |
| OwnerId | uniqueidentifier | Guid | Sensitive personal |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| CreatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| UpdatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| RowVersion | rowversion | byte[] | Sensitive personal |
| Title | nvarchar(200) | string | Sensitive personal |
| TargetAmount | decimal(28,8) | decimal | Sensitive personal |
| Currency | char(3) | string | Sensitive personal |
| DueOn | date | DateOnly? | Sensitive personal |
| Mode | varchar(64) | string | Sensitive personal |
| ManualAmount | decimal(28,8) | decimal? | Sensitive personal |
| Status | varchar(64) | string | Sensitive personal |

<a id="finance-savingsaccount"></a>
## finance.SavingsAccount

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Sensitive personal |
| OwnerId | uniqueidentifier | Guid | Sensitive personal |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| CreatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| UpdatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| RowVersion | rowversion | byte[] | Sensitive personal |
| SavingsGoalId | uniqueidentifier | Guid | Sensitive personal |
| AccountId | uniqueidentifier | Guid | Sensitive personal |

<a id="finance-debt"></a>
## finance.Debt

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Sensitive personal |
| OwnerId | uniqueidentifier | Guid | Sensitive personal |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| CreatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| UpdatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| RowVersion | rowversion | byte[] | Sensitive personal |
| Title | nvarchar(200) | string | Sensitive personal |
| Counterparty | nvarchar(200) | string | Sensitive personal |
| Direction | varchar(64) | string | Sensitive personal |
| Principal | decimal(28,8) | decimal | Sensitive personal |
| Currency | char(3) | string | Sensitive personal |
| StartedOn | date | DateOnly | Sensitive personal |
| DueOn | date | DateOnly? | Sensitive personal |
| Status | varchar(64) | string | Sensitive personal |

<a id="vault-item"></a>
## vault.Item

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Sensitive personal |
| OwnerId | uniqueidentifier | Guid | Sensitive personal |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| CreatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| UpdatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| RowVersion | rowversion | byte[] | Sensitive personal |
| ItemType | varchar(64) | string | Sensitive personal |
| CurrentVersion | bigint | long | Sensitive personal |
| Status | varchar(64) | string | Sensitive personal |
| TrashBatchId | uniqueidentifier | Guid? | Sensitive personal |
| IsDeleted | bit | bool | Restricted Vault metadata |
| DeletedAt | datetime2(7) | DateTime (UTC only)? | Restricted Vault metadata |
| DeletedByUserId | uniqueidentifier | Guid? | Restricted Vault metadata |

<a id="vault-itemversion"></a>
## vault.ItemVersion

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Sensitive personal |
| OwnerId | uniqueidentifier | Guid | Sensitive personal |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| CreatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| ItemId | uniqueidentifier | Guid | Sensitive personal |
| VersionNumber | bigint | long | Sensitive personal |
| Ciphertext | varbinary(max) | byte[] | Secret/credential envelope |
| Nonce | varbinary(max) | byte[] | Secret/credential envelope |
| AuthTag | varbinary(max) | byte[] | Secret/credential envelope |
| Algorithm | varchar(64) | string | Sensitive personal |
| KeyReference | nvarchar(200) | string | Secret/credential envelope |
| KeyVersion | nvarchar(100) | string | Secret/credential envelope |
| ContextDigest | binary(32) | byte[] | Sensitive personal |
| PayloadSchemaVersion | int | int | Sensitive personal |
| CommandKeyHash | binary(32) | byte[] | Secret/credential envelope |

<a id="vault-keyenvelope"></a>
## vault.KeyEnvelope

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Sensitive personal |
| OwnerId | uniqueidentifier | Guid | Sensitive personal |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| CreatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| UpdatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| RowVersion | rowversion | byte[] | Sensitive personal |
| KeyVersion | nvarchar(100) | string | Secret/credential envelope |
| ExternalKeyReference | nvarchar(200) | string | Secret/credential envelope |
| WrappedKey | varbinary(max) | byte[]? | Secret/credential envelope |
| State | varchar(64) | string | Sensitive personal |
| ActivatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| RetiredAt | datetime2(7) | DateTime (UTC only)? | Sensitive personal |
| RecoveryWrappedKey | varbinary(max) | byte[] | Secret cryptographic material |
| RecoveryKeyReference | nvarchar(200) | string | Restricted key handle |
| RecoveryKeyVersion | nvarchar(100) | string | Restricted key metadata |

<a id="vault-rotationrun"></a>
## vault.RotationRun

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Sensitive personal |
| OwnerId | uniqueidentifier | Guid | Sensitive personal |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| CreatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Sensitive personal |
| UpdatedByUserId | uniqueidentifier | Guid? | Sensitive personal |
| RowVersion | rowversion | byte[] | Sensitive personal |
| FromKeyVersion | nvarchar(100) | string | Secret/credential envelope |
| ToKeyVersion | nvarchar(100) | string | Secret/credential envelope |
| State | varchar(64) | string | Sensitive personal |
| Cursor | nvarchar(300) | string? | Sensitive personal |
| ProcessedCount | bigint | long | Sensitive personal |
| ErrorCode | nvarchar(100) | string? | Sensitive personal |
