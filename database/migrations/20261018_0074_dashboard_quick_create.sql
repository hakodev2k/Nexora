SET XACT_ABORT ON;
BEGIN TRANSACTION;
INSERT [platform].[Permission]([ActionKey],[EffectiveStatus])
SELECT N'dashboard.quick_create.open','Resolved'
WHERE NOT EXISTS(SELECT 1 FROM [platform].[Permission] WITH(UPDLOCK,HOLDLOCK) WHERE [ActionKey]=N'dashboard.quick_create.open');
COMMIT TRANSACTION;
GO
