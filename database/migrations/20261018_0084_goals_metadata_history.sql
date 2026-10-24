SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'[productivity].[GoalMetadataVersion]',N'U') IS NULL
BEGIN
    CREATE TABLE [productivity].[GoalMetadataVersion](
        [Id] uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
        [OwnerId] uniqueidentifier NOT NULL,
        [GoalId] uniqueidentifier NOT NULL,
        [CapturedByUserId] uniqueidentifier NOT NULL REFERENCES [identity].[User]([Id]),
        [CapturedAt] datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        [SourceUpdatedAt] datetime2(7) NOT NULL,
        [SourceRowVersion] binary(8) NOT NULL,
        [Event] varchar(32) NOT NULL CHECK ([Event] IN ('Created','Updated','HistoryStarted')),
        [Title] nvarchar(200) NOT NULL CHECK (LEN(LTRIM(RTRIM([Title]))) BETWEEN 1 AND 200),
        [Description] nvarchar(max) NULL CHECK ([Description] IS NULL OR DATALENGTH([Description])<=40000),
        [StartDate] date NULL,
        [EndDate] date NULL,
        CONSTRAINT [PK_GoalMetadataVersion] PRIMARY KEY NONCLUSTERED ([Id]),
        CONSTRAINT [FK_GoalMetadataVersion_Goal] FOREIGN KEY ([OwnerId],[GoalId]) REFERENCES [productivity].[Goal]([OwnerId],[Id]),
        CONSTRAINT [UQ_GoalMetadataVersion_Revision] UNIQUE ([OwnerId],[GoalId],[SourceRowVersion]),
        CONSTRAINT [CK_GoalMetadataVersion_Dates] CHECK ([StartDate] IS NULL OR [EndDate] IS NULL OR [EndDate]>=[StartDate])
    );
    CREATE UNIQUE CLUSTERED INDEX [CX_GoalMetadataVersion_Seek] ON [productivity].[GoalMetadataVersion]([OwnerId],[GoalId],[CapturedAt] DESC,[Id] DESC);
END;
COMMIT TRANSACTION;
GO
