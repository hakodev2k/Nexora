SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH(N'productivity.Goal', N'PreArchiveStatus') IS NULL
    ALTER TABLE [productivity].[Goal] ADD [PreArchiveStatus] varchar(64) NULL;
GO
IF OBJECT_ID(N'[productivity].[CK_Goal_PreArchiveStatus]', N'C') IS NULL
    ALTER TABLE [productivity].[Goal] WITH CHECK ADD CONSTRAINT [CK_Goal_PreArchiveStatus]
        CHECK ([PreArchiveStatus] IS NULL OR [PreArchiveStatus] IN ('Draft','Active','Completed','Abandoned'));
INSERT [platform].[Permission] ([ActionKey], [EffectiveStatus])
SELECT v.[ActionKey], 'Resolved' FROM (VALUES (N'goals.goal.archive'), (N'goals.goal.unarchive')) v([ActionKey])
WHERE NOT EXISTS (SELECT 1 FROM [platform].[Permission] p WITH (UPDLOCK, HOLDLOCK)
    WHERE p.[ActionKey] = v.[ActionKey]);
COMMIT TRANSACTION;
GO
