SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'[learning].[WorkLogVersion]', N'U') IS NULL
BEGIN
    CREATE TABLE [learning].[WorkLogVersion] (
        [OwnerId] uniqueidentifier NOT NULL,
        [WorkLogId] uniqueidentifier NOT NULL,
        [Sequence] bigint NOT NULL CHECK ([Sequence] > 0),
        [Event] varchar(32) NOT NULL CHECK ([Event] IN ('Created','Updated','Archived','Unarchived','HistoryStarted')),
        [CapturedAt] datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        [CapturedByUserId] uniqueidentifier NOT NULL REFERENCES [identity].[User]([Id]),
        [SourceUpdatedAt] datetime2(7) NOT NULL,
        [SourceRowVersion] binary(8) NOT NULL,
        [Title] nvarchar(200) NOT NULL CHECK (LEN(LTRIM(RTRIM([Title]))) BETWEEN 1 AND 200),
        [RecordedOn] date NOT NULL,
        [Minutes] int NOT NULL CHECK ([Minutes] BETWEEN 1 AND 1440),
        [Notes] nvarchar(max) NULL CHECK ([Notes] IS NULL OR DATALENGTH([Notes]) <= 40000),
        [Category] nvarchar(100) NULL,
        [Employer] nvarchar(200) NULL,
        [Status] varchar(64) NOT NULL CHECK ([Status] IN ('Active','Archived')),
        CONSTRAINT [PK_WorkLogVersion] PRIMARY KEY CLUSTERED ([OwnerId],[WorkLogId],[Sequence]),
        CONSTRAINT [UQ_WorkLogVersion_SourceRevision] UNIQUE ([OwnerId],[WorkLogId],[SourceRowVersion]),
        CONSTRAINT [FK_WorkLogVersion_WorkLog] FOREIGN KEY ([OwnerId],[WorkLogId]) REFERENCES [learning].[WorkLog]([OwnerId],[Id])
    );
END;
COMMIT TRANSACTION;
GO
