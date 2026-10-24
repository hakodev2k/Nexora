SET XACT_ABORT ON;
BEGIN TRANSACTION;
INSERT [platform].[Permission]([ActionKey],[EffectiveStatus])
SELECT N'dashboard.widget.refresh','Resolved'
WHERE NOT EXISTS(SELECT 1 FROM [platform].[Permission] WITH(UPDLOCK,HOLDLOCK) WHERE [ActionKey]=N'dashboard.widget.refresh');
COMMIT TRANSACTION;
GO
