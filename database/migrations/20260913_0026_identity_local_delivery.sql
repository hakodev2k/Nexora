/*
   M01 forward migration for durable local account-message delivery. Existing
   migrations remain immutable. DeliveryEnvelope is authenticated ciphertext;
   the lease columns fence a worker after a crash. No raw token is stored.
*/
SET XACT_ABORT ON;
BEGIN TRANSACTION;
GO

IF OBJECT_ID(N'[identity].[AccountMessageIntent]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'identity.AccountMessageIntent', N'DeliveryEnvelope') IS NULL
        ALTER TABLE [identity].[AccountMessageIntent] ADD [DeliveryEnvelope] varbinary(max) NULL;
    IF COL_LENGTH(N'identity.AccountMessageIntent', N'DeliveryLeaseId') IS NULL
        ALTER TABLE [identity].[AccountMessageIntent] ADD [DeliveryLeaseId] uniqueidentifier NULL;
    IF COL_LENGTH(N'identity.AccountMessageIntent', N'DeliveryLeaseUntil') IS NULL
        ALTER TABLE [identity].[AccountMessageIntent] ADD [DeliveryLeaseUntil] datetime2(7) NULL;
    IF NOT EXISTS
       (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_AccountMessageIntent_DeliveryLease')
        CREATE INDEX [IX_AccountMessageIntent_DeliveryLease]
            ON [identity].[AccountMessageIntent]([State], [NotBeforeAt], [DeliveryLeaseUntil], [Id]);
END;
GO

COMMIT TRANSACTION;
GO
