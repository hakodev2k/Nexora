SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'productivity.GoalTarget') AND name=N'UQ_GoalTarget_Owner_Goal_Id')
    ALTER TABLE [productivity].[GoalTarget] ADD CONSTRAINT [UQ_GoalTarget_Owner_Goal_Id] UNIQUE ([OwnerId],[GoalId],[Id]);
IF OBJECT_ID(N'[productivity].[GoalTargetDefinitionVersion]',N'U') IS NULL
BEGIN
    CREATE TABLE [productivity].[GoalTargetDefinitionVersion](
        [Id] uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
        [OwnerId] uniqueidentifier NOT NULL,
        [GoalId] uniqueidentifier NOT NULL,
        [TargetId] uniqueidentifier NOT NULL,
        [CapturedByUserId] uniqueidentifier NOT NULL REFERENCES [identity].[User]([Id]),
        [CapturedAt] datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        [SourceUpdatedAt] datetime2(7) NOT NULL,
        [SourceRowVersion] binary(8) NOT NULL,
        [Event] varchar(32) NOT NULL CHECK ([Event] IN ('Created','Updated','Removed','HistoryStarted')),
        [Kind] varchar(64) NOT NULL,
        [Title] nvarchar(200) NOT NULL CHECK (LEN(LTRIM(RTRIM([Title]))) BETWEEN 1 AND 200),
        [InitialValue] decimal(28,8) NULL,
        [TargetValue] decimal(28,8) NULL,
        [Removed] bit NOT NULL,
        CONSTRAINT [PK_GoalTargetDefinitionVersion] PRIMARY KEY NONCLUSTERED ([Id]),
        CONSTRAINT [FK_GoalTargetDefinitionVersion_Target] FOREIGN KEY ([OwnerId],[GoalId],[TargetId]) REFERENCES [productivity].[GoalTarget]([OwnerId],[GoalId],[Id]),
        CONSTRAINT [UQ_GoalTargetDefinitionVersion_Revision] UNIQUE ([OwnerId],[TargetId],[SourceRowVersion]),
        CONSTRAINT [CK_GoalTargetDefinitionVersion_Kind] CHECK (([Kind]='Numeric' AND [InitialValue] IS NOT NULL AND [TargetValue] IS NOT NULL AND [TargetValue]>[InitialValue])
            OR ([Kind]='Boolean' AND [InitialValue] IS NULL AND [TargetValue] IS NULL)),
        CONSTRAINT [CK_GoalTargetDefinitionVersion_Removed] CHECK (([Event]='Removed' AND [Removed]=1) OR ([Event]<>'Removed' AND [Removed]=0))
    );
    CREATE UNIQUE CLUSTERED INDEX [CX_GoalTargetDefinitionVersion_Seek] ON [productivity].[GoalTargetDefinitionVersion]([OwnerId],[GoalId],[CapturedAt] DESC,[Id] DESC);
END;
COMMIT TRANSACTION;
GO
