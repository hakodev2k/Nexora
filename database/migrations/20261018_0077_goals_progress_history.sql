SET XACT_ABORT ON;
BEGIN TRANSACTION;
INSERT [platform].[Permission] ([ActionKey], [EffectiveStatus])
SELECT N'goals.goal.history', 'Resolved'
WHERE NOT EXISTS (SELECT 1 FROM [platform].[Permission] WITH (UPDLOCK, HOLDLOCK)
    WHERE [ActionKey] = N'goals.goal.history');
COMMIT TRANSACTION;
GO
