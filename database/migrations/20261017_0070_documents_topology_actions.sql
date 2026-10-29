SET XACT_ABORT ON;
BEGIN TRANSACTION;
INSERT [platform].[Permission](ActionKey,EffectiveStatus)
SELECT ActionKey,'Resolved' FROM (VALUES(N'documents.folder.read'),(N'documents.folder.create'),(N'documents.folder.rename'),(N'documents.folder.trash'),
    (N'documents.folder.restore'),(N'documents.folder.purge'),(N'documents.page.trash'),(N'documents.page.restore'),(N'documents.page.purge'))v(ActionKey)
WHERE NOT EXISTS(SELECT 1 FROM [platform].[Permission] p WITH(UPDLOCK,HOLDLOCK)WHERE p.ActionKey=v.ActionKey);
COMMIT TRANSACTION;
GO
