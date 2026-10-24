SET XACT_ABORT ON;
BEGIN TRANSACTION;
INSERT [platform].[Permission]([ActionKey],[EffectiveStatus])
SELECT N'notifications.source.open','Resolved'
WHERE NOT EXISTS(SELECT 1 FROM [platform].[Permission] WITH(UPDLOCK,HOLDLOCK) WHERE [ActionKey]=N'notifications.source.open');
COMMIT TRANSACTION;
GO
