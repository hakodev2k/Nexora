SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH(N'productivity.GoalTarget', N'RemovedAt') IS NULL
    ALTER TABLE [productivity].[GoalTarget] ADD [RemovedAt] datetime2(7) NULL;
IF COL_LENGTH(N'productivity.GoalTarget', N'RemovedByUserId') IS NULL
    ALTER TABLE [productivity].[GoalTarget] ADD [RemovedByUserId] uniqueidentifier NULL;
GO
IF OBJECT_ID(N'[productivity].[FK_GoalTarget_RemovedBy]', N'F') IS NULL
    ALTER TABLE [productivity].[GoalTarget] WITH CHECK ADD CONSTRAINT [FK_GoalTarget_RemovedBy]
        FOREIGN KEY ([RemovedByUserId]) REFERENCES [identity].[User] ([Id]);
IF OBJECT_ID(N'[productivity].[CK_GoalTarget_Removal]', N'C') IS NULL
    ALTER TABLE [productivity].[GoalTarget] WITH CHECK ADD CONSTRAINT [CK_GoalTarget_Removal]
        CHECK (([RemovedAt] IS NULL AND [RemovedByUserId] IS NULL)
            OR ([RemovedAt] IS NOT NULL AND [RemovedByUserId] IS NOT NULL));
INSERT [platform].[Permission] ([ActionKey], [EffectiveStatus])
SELECT v.[ActionKey], 'Resolved' FROM (VALUES (N'goals.target.update'), (N'goals.target.remove')) v([ActionKey])
WHERE NOT EXISTS (SELECT 1 FROM [platform].[Permission] p WITH (UPDLOCK, HOLDLOCK)
    WHERE p.[ActionKey] = v.[ActionKey]);
COMMIT TRANSACTION;
GO
