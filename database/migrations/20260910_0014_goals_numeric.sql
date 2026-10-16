SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

/* FX16-S01/S02/S04: owner-scoped Goals with numeric targets and explicit
   progress. Task-linked targets and aggregate Trash remain future slices. */
IF OBJECT_ID(N'[productivity].[Goal]', N'U') IS NULL
BEGIN
    CREATE TABLE [productivity].[Goal]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_Goal_Id] DEFAULT NEWSEQUENTIALID(),
        [OwnerId] uniqueidentifier NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(max) NULL,
        [StartDate] date NULL,
        [EndDate] date NULL,
        [Status] varchar(64) NOT NULL CONSTRAINT [DF_Goal_Status] DEFAULT 'Draft',
        [PreArchiveStatus] varchar(64) NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_Goal_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_Goal_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Goal] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UX_Goal_Owner_Id] UNIQUE ([OwnerId], [Id]),
        CONSTRAINT [FK_Goal_Owner] FOREIGN KEY ([OwnerId]) REFERENCES [platform].[PersonalSpace]([Id]),
        CONSTRAINT [FK_Goal_CreatedBy] FOREIGN KEY ([CreatedByUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [FK_Goal_UpdatedBy] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [CK_Goal_Title] CHECK (LEN(LTRIM(RTRIM([Title]))) BETWEEN 1 AND 200),
        CONSTRAINT [CK_Goal_Status] CHECK ([Status] IN ('Draft','Active','Completed','Abandoned','Archived','Deleted')),
        CONSTRAINT [CK_Goal_Dates] CHECK ([StartDate] IS NULL OR [EndDate] IS NULL OR [EndDate] >= [StartDate]),
        CONSTRAINT [CK_Goal_Archive] CHECK (([Status] = 'Archived' AND [PreArchiveStatus] IS NOT NULL) OR ([Status] <> 'Archived'))
    );
    CREATE INDEX [IX_Goal_Owner_Status_EndDate] ON [productivity].[Goal]([OwnerId], [Status], [EndDate], [Id]);
END
GO

IF OBJECT_ID(N'[productivity].[GoalTarget]', N'U') IS NULL
BEGIN
    CREATE TABLE [productivity].[GoalTarget]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_GoalTarget_Id] DEFAULT NEWSEQUENTIALID(),
        [OwnerId] uniqueidentifier NOT NULL,
        [GoalId] uniqueidentifier NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [Kind] varchar(64) NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [InitialValue] decimal(28,8) NULL,
        [CurrentValue] decimal(28,8) NULL,
        [TargetValue] decimal(28,8) NULL,
        [BooleanValue] bit NULL,
        [Position] int NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_GoalTarget_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_GoalTarget_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_GoalTarget] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UX_GoalTarget_Owner_Id] UNIQUE ([OwnerId], [Id]),
        CONSTRAINT [UX_GoalTarget_Owner_Goal_Position] UNIQUE ([OwnerId], [GoalId], [Position]),
        CONSTRAINT [FK_GoalTarget_Owner] FOREIGN KEY ([OwnerId]) REFERENCES [platform].[PersonalSpace]([Id]),
        CONSTRAINT [FK_GoalTarget_Goal] FOREIGN KEY ([OwnerId], [GoalId]) REFERENCES [productivity].[Goal]([OwnerId], [Id]),
        CONSTRAINT [FK_GoalTarget_CreatedBy] FOREIGN KEY ([CreatedByUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [FK_GoalTarget_UpdatedBy] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [CK_GoalTarget_Kind] CHECK ([Kind] IN ('Numeric','Boolean','Tasks')),
        CONSTRAINT [CK_GoalTarget_Title] CHECK (LEN(LTRIM(RTRIM([Title]))) BETWEEN 1 AND 200),
        CONSTRAINT [CK_GoalTarget_Position] CHECK ([Position] >= 0),
        CONSTRAINT [CK_GoalTarget_Values] CHECK
        (
            ([Kind] = 'Numeric' AND [InitialValue] IS NOT NULL AND [CurrentValue] IS NOT NULL AND [TargetValue] IS NOT NULL AND [TargetValue] > [InitialValue] AND [BooleanValue] IS NULL)
            OR ([Kind] = 'Boolean' AND [BooleanValue] IS NOT NULL AND [InitialValue] IS NULL AND [CurrentValue] IS NULL AND [TargetValue] IS NULL)
            OR ([Kind] = 'Tasks' AND [InitialValue] IS NULL AND [CurrentValue] IS NULL AND [TargetValue] IS NULL AND [BooleanValue] IS NULL)
        )
    );
    CREATE INDEX [IX_GoalTarget_Owner_Goal_Position] ON [productivity].[GoalTarget]([OwnerId], [GoalId], [Position], [Id]);
END
GO

IF OBJECT_ID(N'[productivity].[GoalProgress]', N'U') IS NULL
BEGIN
    CREATE TABLE [productivity].[GoalProgress]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_GoalProgress_Id] DEFAULT NEWSEQUENTIALID(),
        [OwnerId] uniqueidentifier NOT NULL,
        [TargetId] uniqueidentifier NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [Value] decimal(28,8) NULL,
        [Checked] bit NULL,
        [RecordedAt] datetime2(7) NOT NULL CONSTRAINT [DF_GoalProgress_RecordedAt] DEFAULT SYSUTCDATETIME(),
        [Note] nvarchar(2000) NULL,
        CONSTRAINT [PK_GoalProgress] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UX_GoalProgress_Owner_Id] UNIQUE ([OwnerId], [Id]),
        CONSTRAINT [FK_GoalProgress_Owner] FOREIGN KEY ([OwnerId]) REFERENCES [platform].[PersonalSpace]([Id]),
        CONSTRAINT [FK_GoalProgress_Target] FOREIGN KEY ([OwnerId], [TargetId]) REFERENCES [productivity].[GoalTarget]([OwnerId], [Id]),
        CONSTRAINT [CK_GoalProgress_ValueKind] CHECK (([Value] IS NOT NULL AND [Checked] IS NULL) OR ([Value] IS NULL AND [Checked] IS NOT NULL))
    );
    CREATE INDEX [IX_GoalProgress_Owner_Target_Recorded] ON [productivity].[GoalProgress]([OwnerId], [TargetId], [RecordedAt] DESC, [Id] DESC);
END
GO

IF OBJECT_ID(N'[platform].[Permission]', N'U') IS NOT NULL
BEGIN
    MERGE [platform].[Permission] AS target
    USING (VALUES
        (N'goals.goal.read'),
        (N'goals.goal.create'),
        (N'goals.goal.update'),
        (N'goals.goal.start'),
        (N'goals.goal.complete'),
        (N'goals.goal.abandon'),
        (N'goals.goal.reopen'),
        (N'goals.target.read'),
        (N'goals.target.create'),
        (N'goals.target.record_progress')
    ) AS source ([ActionKey])
    ON target.[ActionKey] = source.[ActionKey]
    WHEN MATCHED AND target.[EffectiveStatus] <> 'Resolved' THEN
        UPDATE SET [EffectiveStatus] = 'Resolved'
    WHEN NOT MATCHED BY TARGET THEN
        INSERT ([ActionKey], [EffectiveStatus]) VALUES (source.[ActionKey], 'Resolved');
END
GO

COMMIT TRANSACTION;
GO
