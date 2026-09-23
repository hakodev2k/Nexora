SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

/* Additive upgrade for the first owner-scoped productivity slice. The guards
   make this migration safe to rerun against a database created by 0002. */
IF OBJECT_ID(N'[productivity].[Project]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'productivity.Project', N'StartAt') IS NULL
        ALTER TABLE [productivity].[Project] ADD [StartAt] datetime2(7) NULL;
    IF COL_LENGTH(N'productivity.Project', N'EndAt') IS NULL
        ALTER TABLE [productivity].[Project] ADD [EndAt] datetime2(7) NULL;
    IF COL_LENGTH(N'productivity.Project', N'Priority') IS NULL
        ALTER TABLE [productivity].[Project] ADD [Priority] varchar(2) NOT NULL CONSTRAINT [DF_Project_Priority_R1] DEFAULT 'P3';
    IF COL_LENGTH(N'productivity.Project', N'TagsJson') IS NULL
        ALTER TABLE [productivity].[Project] ADD [TagsJson] nvarchar(max) NOT NULL CONSTRAINT [DF_Project_TagsJson_R1] DEFAULT N'[]';
    IF COL_LENGTH(N'productivity.Project', N'Notes') IS NULL
        ALTER TABLE [productivity].[Project] ADD [Notes] nvarchar(max) NULL;
    IF OBJECT_ID(N'[productivity].[CK_Project_Status]', N'C') IS NOT NULL
        ALTER TABLE [productivity].[Project] DROP CONSTRAINT [CK_Project_Status];
    UPDATE [productivity].[Project]
    SET [StartAt] = COALESCE([StartAt], [CreatedAt]),
        [EndAt] = COALESCE([EndAt], DATEADD(hour, 1, [CreatedAt])),
        [Status] = CASE WHEN [Status] = 'Active' THEN 'NotStarted' ELSE [Status] END
    WHERE [StartAt] IS NULL OR [EndAt] IS NULL OR [Status] = 'Active';
    ALTER TABLE [productivity].[Project] ALTER COLUMN [StartAt] datetime2(7) NOT NULL;
    ALTER TABLE [productivity].[Project] ALTER COLUMN [EndAt] datetime2(7) NOT NULL;
    ALTER TABLE [productivity].[Project] ADD CONSTRAINT [CK_Project_Status_R1]
        CHECK ([Status] IN ('NotStarted','InProgress','Completed','Skipped','Deleted'));
    IF OBJECT_ID(N'[productivity].[CK_Project_Priority_R1]', N'C') IS NULL
        ALTER TABLE [productivity].[Project] ADD CONSTRAINT [CK_Project_Priority_R1] CHECK ([Priority] IN ('P0','P1','P2','P3'));
END
GO

IF OBJECT_ID(N'[productivity].[Task]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'productivity.Task', N'StartAt') IS NULL
        ALTER TABLE [productivity].[Task] ADD [StartAt] datetime2(7) NULL;
    IF COL_LENGTH(N'productivity.Task', N'EndAt') IS NULL
        ALTER TABLE [productivity].[Task] ADD [EndAt] datetime2(7) NULL;
    IF COL_LENGTH(N'productivity.Task', N'Priority') IS NULL
        ALTER TABLE [productivity].[Task] ADD [Priority] varchar(2) NOT NULL CONSTRAINT [DF_Task_Priority_R1] DEFAULT 'P3';
    IF COL_LENGTH(N'productivity.Task', N'TagsJson') IS NULL
        ALTER TABLE [productivity].[Task] ADD [TagsJson] nvarchar(max) NOT NULL CONSTRAINT [DF_Task_TagsJson_R1] DEFAULT N'[]';
    IF COL_LENGTH(N'productivity.Task', N'AcceptanceCriteriaJson') IS NULL
        ALTER TABLE [productivity].[Task] ADD [AcceptanceCriteriaJson] nvarchar(max) NOT NULL CONSTRAINT [DF_Task_Acceptance_R1] DEFAULT N'[]';
    IF COL_LENGTH(N'productivity.Task', N'Rank') IS NULL
        ALTER TABLE [productivity].[Task] ADD [Rank] int NOT NULL CONSTRAINT [DF_Task_Rank_R1] DEFAULT 0;
    IF COL_LENGTH(N'productivity.Task', N'ReminderAt') IS NULL
        ALTER TABLE [productivity].[Task] ADD [ReminderAt] datetime2(7) NULL;
    UPDATE [productivity].[Task]
    SET [StartAt] = COALESCE([StartAt], DATEADD(hour, -1, COALESCE([DueAt], [CreatedAt]))),
        [EndAt] = COALESCE([EndAt], COALESCE([DueAt], DATEADD(hour, 1, [CreatedAt])))
    WHERE [StartAt] IS NULL OR [EndAt] IS NULL;
    ALTER TABLE [productivity].[Task] ALTER COLUMN [StartAt] datetime2(7) NOT NULL;
    ALTER TABLE [productivity].[Task] ALTER COLUMN [EndAt] datetime2(7) NOT NULL;
    IF OBJECT_ID(N'[productivity].[CK_Task_Priority_R1]', N'C') IS NULL
        ALTER TABLE [productivity].[Task] ADD CONSTRAINT [CK_Task_Priority_R1] CHECK ([Priority] IN ('P0','P1','P2','P3'));
    IF OBJECT_ID(N'[productivity].[CK_Task_Time_R1]', N'C') IS NULL
        ALTER TABLE [productivity].[Task] ADD CONSTRAINT [CK_Task_Time_R1] CHECK ([EndAt] > [StartAt]);
END
GO

IF OBJECT_ID(N'[calendar].[Event]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'calendar.Event', N'IsAllDay') IS NULL
        ALTER TABLE [calendar].[Event] ADD [IsAllDay] bit NOT NULL CONSTRAINT [DF_Event_IsAllDay_R1] DEFAULT 0;
    IF COL_LENGTH(N'calendar.Event', N'SourceUid') IS NULL
        ALTER TABLE [calendar].[Event] ADD [SourceUid] nvarchar(255) NULL;
    IF COL_LENGTH(N'calendar.Event', N'SourceKind') IS NULL
        ALTER TABLE [calendar].[Event] ADD [SourceKind] varchar(16) NOT NULL CONSTRAINT [DF_Event_SourceKind_R1] DEFAULT 'Manual';
    IF OBJECT_ID(N'[calendar].[CK_Event_Status]', N'C') IS NOT NULL
        ALTER TABLE [calendar].[Event] DROP CONSTRAINT [CK_Event_Status];
    UPDATE [calendar].[Event] SET [Status] = 'Canceled', [DeletedAt] = NULL WHERE [Status] = 'Deleted';
    IF OBJECT_ID(N'[calendar].[CK_Event_Status_R1]', N'C') IS NULL
        ALTER TABLE [calendar].[Event] ADD CONSTRAINT [CK_Event_Status_R1] CHECK ([Status] IN ('Scheduled','Completed','Canceled'));
END
GO

IF OBJECT_ID(N'[productivity].[ProjectHistory]', N'U') IS NULL
BEGIN
    CREATE TABLE [productivity].[ProjectHistory]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_ProjectHistory_Id] DEFAULT NEWSEQUENTIALID(),
        [ProjectId] uniqueidentifier NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [Name] nvarchar(160) NOT NULL,
        [Description] nvarchar(2000) NULL,
        [StartAt] datetime2(7) NOT NULL,
        [EndAt] datetime2(7) NOT NULL,
        [Priority] varchar(2) NOT NULL,
        [TagsJson] nvarchar(max) NOT NULL,
        [Notes] nvarchar(max) NULL,
        [Status] varchar(16) NOT NULL,
        [Reason] nvarchar(500) NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_ProjectHistory_CreatedAt] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_ProjectHistory] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_ProjectHistory_Project] FOREIGN KEY ([ProjectId]) REFERENCES [productivity].[Project]([Id]),
        CONSTRAINT [FK_ProjectHistory_Owner] FOREIGN KEY ([OwnerId]) REFERENCES [platform].[PersonalSpace]([Id])
    );
    CREATE INDEX [IX_ProjectHistory_Project_Created] ON [productivity].[ProjectHistory]([ProjectId], [CreatedAt] DESC);
END
GO

IF OBJECT_ID(N'[productivity].[TaskHistory]', N'U') IS NULL
BEGIN
    CREATE TABLE [productivity].[TaskHistory]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_TaskHistory_Id] DEFAULT NEWSEQUENTIALID(),
        [TaskId] uniqueidentifier NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [ProjectId] uniqueidentifier NOT NULL,
        [Title] nvarchar(240) NOT NULL,
        [Description] nvarchar(4000) NULL,
        [StartAt] datetime2(7) NOT NULL,
        [EndAt] datetime2(7) NOT NULL,
        [Priority] varchar(2) NOT NULL,
        [TagsJson] nvarchar(max) NOT NULL,
        [AcceptanceCriteriaJson] nvarchar(max) NOT NULL,
        [Rank] int NOT NULL,
        [ReminderAt] datetime2(7) NULL,
        [Status] varchar(16) NOT NULL,
        [Reason] nvarchar(500) NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_TaskHistory_CreatedAt] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_TaskHistory] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_TaskHistory_Task] FOREIGN KEY ([TaskId]) REFERENCES [productivity].[Task]([Id]),
        CONSTRAINT [FK_TaskHistory_Owner] FOREIGN KEY ([OwnerId]) REFERENCES [platform].[PersonalSpace]([Id])
    );
    CREATE INDEX [IX_TaskHistory_Task_Created] ON [productivity].[TaskHistory]([TaskId], [CreatedAt] DESC);
END
GO

IF OBJECT_ID(N'[platform].[TrashItem]', N'U') IS NULL
BEGIN
    CREATE TABLE [platform].[TrashItem]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_TrashItem_Id] DEFAULT NEWSEQUENTIALID(),
        [OwnerId] uniqueidentifier NOT NULL,
        [ResourceType] varchar(64) NOT NULL,
        [ResourceId] uniqueidentifier NOT NULL,
        [DeletionBatchId] uniqueidentifier NOT NULL,
        [PriorStatus] varchar(32) NOT NULL,
        [DeletedAt] datetime2(7) NOT NULL CONSTRAINT [DF_TrashItem_DeletedAt] DEFAULT SYSUTCDATETIME(),
        [RestoredAt] datetime2(7) NULL,
        [PurgedAt] datetime2(7) NULL,
        CONSTRAINT [PK_TrashItem] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_TrashItem_Owner] FOREIGN KEY ([OwnerId]) REFERENCES [platform].[PersonalSpace]([Id]),
        CONSTRAINT [UX_TrashItem_Resource] UNIQUE ([OwnerId], [ResourceType], [ResourceId], [DeletionBatchId])
    );
    CREATE INDEX [IX_TrashItem_Owner_Deleted] ON [platform].[TrashItem]([OwnerId], [DeletedAt] DESC);
END
GO

COMMIT TRANSACTION;
GO
