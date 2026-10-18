SET XACT_ABORT ON;
BEGIN TRANSACTION;
-- Source-owned identity-only projection. Callers must still prove current
-- actor/owner/module/action rights before joining or projecting source fields.
EXEC(N'CREATE OR ALTER VIEW [documents].[VisiblePageV1] AS
    SELECT p.OwnerId,p.Id
    FROM [documents].[Page] p
    WHERE p.DeletedAt IS NULL
      AND NOT EXISTS(SELECT 1 FROM [platform].[TrashItem] t WHERE t.OwnerId=p.OwnerId AND t.ResourceType=''Document''
          AND t.ResourceId=p.Id AND t.RestoredAt IS NULL AND t.PurgedAt IS NULL)
      AND(p.ParentPageId IS NULL OR EXISTS(SELECT 1 FROM [documents].[Page] parent
          WHERE parent.OwnerId=p.OwnerId AND parent.Id=p.ParentPageId AND parent.DeletedAt IS NULL
            AND NOT EXISTS(SELECT 1 FROM [platform].[TrashItem] t WHERE t.OwnerId=parent.OwnerId AND t.ResourceType=''Document''
              AND t.ResourceId=parent.Id AND t.RestoredAt IS NULL AND t.PurgedAt IS NULL)))
      AND(p.FolderId IS NULL OR EXISTS(SELECT 1 FROM [documents].[Folder] f WHERE f.OwnerId=p.OwnerId AND f.Id=p.FolderId
          AND f.DeletedAt IS NULL
          AND NOT EXISTS(SELECT 1 FROM [platform].[TrashItem] t WHERE t.OwnerId=f.OwnerId AND t.ResourceType=''Folder''
              AND t.ResourceId=f.Id AND t.RestoredAt IS NULL AND t.PurgedAt IS NULL)
          AND(f.ParentFolderId IS NULL OR EXISTS(SELECT 1 FROM [documents].[Folder] parentFolder
              WHERE parentFolder.OwnerId=f.OwnerId AND parentFolder.Id=f.ParentFolderId AND parentFolder.DeletedAt IS NULL
                AND NOT EXISTS(SELECT 1 FROM [platform].[TrashItem] t WHERE t.OwnerId=parentFolder.OwnerId AND t.ResourceType=''Folder''
                  AND t.ResourceId=parentFolder.Id AND t.RestoredAt IS NULL AND t.PurgedAt IS NULL)))));');
COMMIT TRANSACTION;
GO
