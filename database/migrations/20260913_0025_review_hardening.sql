/*
   PR #4 review hardening. This is a forward-only migration. It adds durable
   coordination state for local reminder dispatch and file cleanup/upload
   attempts, and makes Task priority genuinely optional without rewriting the
   existing P3 values.
*/
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

IF OBJECT_ID(N'[productivity].[Task]', N'U') IS NOT NULL
BEGIN
    IF OBJECT_ID(N'[productivity].[CK_Task_Priority_R1]', N'C') IS NOT NULL
        ALTER TABLE [productivity].[Task] DROP CONSTRAINT [CK_Task_Priority_R1];
    IF OBJECT_ID(N'[productivity].[DF_Task_Priority_R1]', N'D') IS NOT NULL
        ALTER TABLE [productivity].[Task] DROP CONSTRAINT [DF_Task_Priority_R1];
    ALTER TABLE [productivity].[Task] ALTER COLUMN [Priority] varchar(2) NULL;
    IF OBJECT_ID(N'[productivity].[CK_Task_Priority_R2]', N'C') IS NULL
        ALTER TABLE [productivity].[Task] ADD CONSTRAINT [CK_Task_Priority_R2]
            CHECK ([Priority] IS NULL OR [Priority] IN ('P0','P1','P2','P3'));
END
GO

IF OBJECT_ID(N'[productivity].[TaskHistory]', N'U') IS NOT NULL
    ALTER TABLE [productivity].[TaskHistory] ALTER COLUMN [Priority] varchar(2) NULL;
GO

IF OBJECT_ID(N'[calendar].[Reminder]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'calendar.Reminder', N'DispatchLeaseId') IS NULL
        ALTER TABLE [calendar].[Reminder] ADD [DispatchLeaseId] uniqueidentifier NULL;
    IF COL_LENGTH(N'calendar.Reminder', N'DispatchLeaseUntil') IS NULL
        ALTER TABLE [calendar].[Reminder] ADD [DispatchLeaseUntil] datetime2(7) NULL;
    IF COL_LENGTH(N'calendar.Reminder', N'DispatchAttempts') IS NULL
        ALTER TABLE [calendar].[Reminder] ADD [DispatchAttempts] int NOT NULL CONSTRAINT [DF_Reminder_DispatchAttempts_R2] DEFAULT 0;
    IF COL_LENGTH(N'calendar.Reminder', N'NextAttemptAt') IS NULL
        ALTER TABLE [calendar].[Reminder] ADD [NextAttemptAt] datetime2(7) NULL;
    IF COL_LENGTH(N'calendar.Reminder', N'LastErrorCode') IS NULL
        ALTER TABLE [calendar].[Reminder] ADD [LastErrorCode] varchar(64) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_Reminder_DispatchAttempts_R2')
        ALTER TABLE [calendar].[Reminder] ADD CONSTRAINT [CK_Reminder_DispatchAttempts_R2]
            CHECK ([DispatchAttempts] >= 0 AND [DispatchAttempts] <= 8);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Reminder_DispatchClaim_R2')
        CREATE INDEX [IX_Reminder_DispatchClaim_R2]
            ON [calendar].[Reminder]([State], [NextAttemptAt], [DueAt], [DispatchLeaseUntil], [Id]);
END
GO

IF OBJECT_ID(N'[files].[UploadSession]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'files.UploadSession', N'AttemptId') IS NULL
        ALTER TABLE [files].[UploadSession] ADD [AttemptId] uniqueidentifier NULL;
    IF COL_LENGTH(N'files.UploadSession', N'AttemptLeaseUntil') IS NULL
        ALTER TABLE [files].[UploadSession] ADD [AttemptLeaseUntil] datetime2(7) NULL;
    IF COL_LENGTH(N'files.UploadSession', N'StagingStorageKey') IS NULL
        ALTER TABLE [files].[UploadSession] ADD [StagingStorageKey] nvarchar(512) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'UX_UploadSession_Attempt_R2')
        CREATE UNIQUE INDEX [UX_UploadSession_Attempt_R2]
            ON [files].[UploadSession]([OwnerId], [Id], [AttemptId])
            WHERE [AttemptId] IS NOT NULL;
END
GO

IF OBJECT_ID(N'[files].[StorageCleanup]', N'U') IS NULL
BEGIN
    CREATE TABLE [files].[StorageCleanup]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_StorageCleanup_Id] DEFAULT NEWSEQUENTIALID(),
        [OwnerId] uniqueidentifier NOT NULL,
        [FileObjectId] uniqueidentifier NULL,
        [StorageKey] nvarchar(500) NOT NULL,
        [State] varchar(16) NOT NULL CONSTRAINT [DF_StorageCleanup_State] DEFAULT 'Pending',
        [Attempts] int NOT NULL CONSTRAINT [DF_StorageCleanup_Attempts] DEFAULT 0,
        [NextAttemptAt] datetime2(7) NULL,
        [LastErrorCode] varchar(64) NULL,
        [LeaseId] uniqueidentifier NULL,
        [LeaseUntil] datetime2(7) NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_StorageCleanup_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_StorageCleanup_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_StorageCleanup] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_StorageCleanup_Owner] FOREIGN KEY ([OwnerId]) REFERENCES [platform].[PersonalSpace]([Id]),
        CONSTRAINT [CK_StorageCleanup_State] CHECK ([State] IN ('Pending','Completed','Failed')),
        CONSTRAINT [CK_StorageCleanup_Attempts] CHECK ([Attempts] >= 0 AND [Attempts] <= 8),
        CONSTRAINT [UX_StorageCleanup_Key] UNIQUE ([OwnerId], [StorageKey])
    );
    CREATE INDEX [IX_StorageCleanup_Pending]
        ON [files].[StorageCleanup]([State], [NextAttemptAt], [UpdatedAt], [Id]);
END
GO

IF OBJECT_ID(N'[files].[StorageCleanup]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'files.StorageCleanup', N'LeaseId') IS NULL
        ALTER TABLE [files].[StorageCleanup] ADD [LeaseId] uniqueidentifier NULL;
    IF COL_LENGTH(N'files.StorageCleanup', N'LeaseUntil') IS NULL
        ALTER TABLE [files].[StorageCleanup] ADD [LeaseUntil] datetime2(7) NULL;
END
GO

COMMIT TRANSACTION;
GO
