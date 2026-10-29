SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'[productivity].[GoalStateHistory]', N'U') IS NULL
BEGIN
    CREATE TABLE [productivity].[GoalStateHistory] (
        [Id] uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
        [OwnerId] uniqueidentifier NOT NULL,
        [GoalId] uniqueidentifier NOT NULL,
        [ActorUserId] uniqueidentifier NOT NULL REFERENCES [identity].[User]([Id]),
        [RecordedAt] datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        [ActionKey] nvarchar(160) NOT NULL,
        [FromStatus] varchar(64) NOT NULL,
        [ToStatus] varchar(64) NOT NULL,
        [SourceRowVersion] binary(8) NOT NULL,
        CONSTRAINT [PK_GoalStateHistory] PRIMARY KEY NONCLUSTERED ([Id]),
        CONSTRAINT [FK_GoalStateHistory_Goal] FOREIGN KEY ([OwnerId],[GoalId]) REFERENCES [productivity].[Goal]([OwnerId],[Id]),
        CONSTRAINT [UQ_GoalStateHistory_Revision] UNIQUE ([OwnerId],[GoalId],[SourceRowVersion]),
        CONSTRAINT [CK_GoalStateHistory_Transition] CHECK (
            ([ActionKey]='goals.goal.start' AND [FromStatus]='Draft' AND [ToStatus]='Active') OR
            ([ActionKey]='goals.goal.reopen' AND [FromStatus] IN ('Completed','Abandoned') AND [ToStatus]='Active') OR
            ([ActionKey]='goals.goal.complete' AND [FromStatus]='Active' AND [ToStatus]='Completed') OR
            ([ActionKey]='goals.goal.abandon' AND [FromStatus] IN ('Draft','Active') AND [ToStatus]='Abandoned') OR
            ([ActionKey]='goals.goal.archive' AND [FromStatus] IN ('Draft','Active','Completed','Abandoned') AND [ToStatus]='Archived') OR
            ([ActionKey]='goals.goal.unarchive' AND [FromStatus]='Archived' AND [ToStatus] IN ('Draft','Active','Completed','Abandoned')))
    );
    CREATE UNIQUE CLUSTERED INDEX [CX_GoalStateHistory_Seek] ON [productivity].[GoalStateHistory]
        ([OwnerId],[GoalId],[RecordedAt] DESC,[Id] DESC);
END;
COMMIT TRANSACTION;
GO
