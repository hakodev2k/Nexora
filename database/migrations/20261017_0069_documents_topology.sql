SET XACT_ABORT ON;
BEGIN TRANSACTION;
GO
/* Structural identities for actual existing pages only. No hierarchy or
   historical cohort is invented, and no owner/module grants are changed. */
IF EXISTS(SELECT 1 FROM [platform].[ResourceType] rt JOIN [platform].[Module] m ON m.Id=rt.ModuleId
    WHERE m.Code='FX20' AND ((rt.Code='Page' AND rt.ContractVersion<>'documents-page-v1') OR (rt.Code='Folder' AND rt.ContractVersion<>'documents-folder-v1')))
    THROW 51069,'Documents resource contracts require reconciliation.',1;
INSERT [platform].[ResourceType](Id,ModuleId,Code,ContractVersion,CapabilitiesJson)
SELECT NEWID(),m.Id,v.Code,v.Version,v.Capabilities FROM [platform].[Module] m CROSS JOIN (VALUES
    ('Page','documents-page-v1',N'{"schemaVersion":1,"trash":true,"share":false,"support":false,"search":false}'),
    ('Folder','documents-folder-v1',N'{"schemaVersion":1,"trash":true,"share":false,"support":false,"search":false}'))v(Code,Version,Capabilities)
WHERE m.Code='FX20' AND NOT EXISTS(SELECT 1 FROM [platform].[ResourceType] rt WHERE rt.ModuleId=m.Id AND rt.Code=v.Code);
DECLARE @PageType uniqueidentifier=(SELECT rt.Id FROM [platform].[ResourceType] rt JOIN [platform].[Module] m ON m.Id=rt.ModuleId WHERE m.Code='FX20' AND rt.Code='Page' AND rt.ContractVersion='documents-page-v1');
IF @PageType IS NULL THROW 51069,'Documents Page registration is unavailable.',1;
IF EXISTS(SELECT 1 FROM [documents].[Page] p JOIN [platform].[Resource] r ON r.Id=p.Id WHERE r.OwnerId<>p.OwnerId OR r.ResourceTypeId<>@PageType)
    OR EXISTS(SELECT 1 FROM [documents].[Page] WHERE Id='00000000-0000-0000-0000-000000000000')
    THROW 51069,'Documents resource identity conflicts require reconciliation.',1;
INSERT [platform].[Resource](Id,OwnerId,ResourceTypeId,Availability,Revision,CreatedAt,UpdatedAt,CreatedByUserId,UpdatedByUserId)
SELECT p.Id,p.OwnerId,@PageType,CASE WHEN p.DeletedAt IS NOT NULL THEN 'Trash' WHEN p.Status='Archived' THEN 'Archived' ELSE 'Active' END,
    1,p.CreatedAt,p.UpdatedAt,p.CreatedByUserId,p.UpdatedByUserId
FROM [documents].[Page] p WHERE NOT EXISTS(SELECT 1 FROM [platform].[Resource] r WHERE r.Id=p.Id);
IF NOT EXISTS(SELECT 1 FROM sys.key_constraints WHERE parent_object_id=OBJECT_ID(N'documents.Page') AND name=N'UQ_DocumentPage_Owner_Id')
    ALTER TABLE [documents].[Page] ADD CONSTRAINT UQ_DocumentPage_Owner_Id UNIQUE(OwnerId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'documents.Page') AND name=N'FK_DocumentPage_Registry')
    ALTER TABLE [documents].[Page] WITH CHECK ADD CONSTRAINT FK_DocumentPage_Registry FOREIGN KEY(OwnerId,Id) REFERENCES [platform].[Resource](OwnerId,Id);

IF OBJECT_ID(N'documents.Folder',N'U') IS NULL
BEGIN
    CREATE TABLE [documents].[Folder](
        Id uniqueidentifier NOT NULL CONSTRAINT PK_DocumentFolder PRIMARY KEY NONCLUSTERED,
        OwnerId uniqueidentifier NOT NULL, Title nvarchar(200) NOT NULL CHECK(LEN(LTRIM(RTRIM(Title))) BETWEEN 1 AND 200),
        ParentFolderId uniqueidentifier NULL, Depth tinyint NOT NULL, ParentDepth tinyint NULL,
        TrashBatchId uniqueidentifier NULL, DeletedAt datetime2(7) NULL,
        CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id), UpdatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id),
        RowVersion rowversion NOT NULL,
        CONSTRAINT UQ_DocumentFolder_Owner_Id UNIQUE(OwnerId,Id),
        CONSTRAINT UQ_DocumentFolder_Depth UNIQUE(OwnerId,Id,Depth),
        CONSTRAINT CK_DocumentFolder_Id CHECK(Id<>'00000000-0000-0000-0000-000000000000'),
        CONSTRAINT CK_DocumentFolder_Depth CHECK((ParentFolderId IS NULL AND Depth=1 AND ParentDepth IS NULL)
          OR (ParentFolderId IS NOT NULL AND ParentFolderId<>Id AND Depth=2 AND ParentDepth IS NOT NULL AND ParentDepth=1)),
        CONSTRAINT FK_DocumentFolder_Registry FOREIGN KEY(OwnerId,Id) REFERENCES [platform].[Resource](OwnerId,Id),
        CONSTRAINT FK_DocumentFolder_Parent FOREIGN KEY(OwnerId,ParentFolderId,ParentDepth) REFERENCES [documents].[Folder](OwnerId,Id,Depth),
        CONSTRAINT FK_DocumentFolder_TrashBatch FOREIGN KEY(OwnerId,TrashBatchId) REFERENCES [operations].[TrashBatch](OwnerId,Id)
    );
    CREATE UNIQUE CLUSTERED INDEX CX_DocumentFolder_Owner_Created ON [documents].[Folder](OwnerId,CreatedAt,Id);
    CREATE INDEX IX_DocumentFolder_Location ON [documents].[Folder](OwnerId,ParentFolderId,DeletedAt,Title,Id);
END;
IF COL_LENGTH(N'documents.Page',N'FolderId') IS NULL ALTER TABLE [documents].[Page] ADD FolderId uniqueidentifier NULL;
IF COL_LENGTH(N'documents.Page',N'ParentPageId') IS NULL ALTER TABLE [documents].[Page] ADD ParentPageId uniqueidentifier NULL;
IF COL_LENGTH(N'documents.Page',N'Depth') IS NULL ALTER TABLE [documents].[Page] ADD Depth tinyint NOT NULL CONSTRAINT DF_DocumentPage_Depth DEFAULT 1;
IF COL_LENGTH(N'documents.Page',N'ParentDepth') IS NULL ALTER TABLE [documents].[Page] ADD ParentDepth tinyint NULL;
IF COL_LENGTH(N'documents.Page',N'FolderScope') IS NULL ALTER TABLE [documents].[Page] ADD FolderScope uniqueidentifier NOT NULL CONSTRAINT DF_DocumentPage_FolderScope DEFAULT '00000000-0000-0000-0000-000000000000';
IF COL_LENGTH(N'documents.Page',N'ArchiveBatchId') IS NULL ALTER TABLE [documents].[Page] ADD ArchiveBatchId uniqueidentifier NULL;
IF COL_LENGTH(N'documents.Page',N'TrashBatchId') IS NULL ALTER TABLE [documents].[Page] ADD TrashBatchId uniqueidentifier NULL;
GO
IF NOT EXISTS(SELECT 1 FROM sys.key_constraints WHERE parent_object_id=OBJECT_ID(N'documents.Page') AND name=N'UQ_DocumentPage_ParentScope')
    ALTER TABLE [documents].[Page] ADD CONSTRAINT UQ_DocumentPage_ParentScope UNIQUE(OwnerId,Id,Depth,FolderScope);
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'documents.Page') AND name=N'CK_DocumentPage_Topology')
    ALTER TABLE [documents].[Page] WITH CHECK ADD CONSTRAINT CK_DocumentPage_Topology CHECK(
        Id<>'00000000-0000-0000-0000-000000000000' AND FolderScope=ISNULL(FolderId,CONVERT(uniqueidentifier,'00000000-0000-0000-0000-000000000000'))
        AND (FolderId IS NULL OR FolderId<>'00000000-0000-0000-0000-000000000000')
        AND ((ParentPageId IS NULL AND Depth=1 AND ParentDepth IS NULL) OR (ParentPageId IS NOT NULL AND ParentPageId<>Id AND Depth=2 AND ParentDepth IS NOT NULL AND ParentDepth=1)));
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'documents.Page') AND name=N'FK_DocumentPage_Folder')
    ALTER TABLE [documents].[Page] WITH CHECK ADD CONSTRAINT FK_DocumentPage_Folder FOREIGN KEY(OwnerId,FolderId) REFERENCES [documents].[Folder](OwnerId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'documents.Page') AND name=N'FK_DocumentPage_ParentScope')
    ALTER TABLE [documents].[Page] WITH CHECK ADD CONSTRAINT FK_DocumentPage_ParentScope FOREIGN KEY(OwnerId,ParentPageId,ParentDepth,FolderScope) REFERENCES [documents].[Page](OwnerId,Id,Depth,FolderScope);
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'documents.Page') AND name=N'FK_DocumentPage_TrashBatch')
    ALTER TABLE [documents].[Page] WITH CHECK ADD CONSTRAINT FK_DocumentPage_TrashBatch FOREIGN KEY(OwnerId,TrashBatchId) REFERENCES [operations].[TrashBatch](OwnerId,Id);
IF OBJECT_ID(N'documents.ArchiveBatch',N'U') IS NULL
BEGIN
    CREATE TABLE [documents].[ArchiveBatch](
        Id uniqueidentifier NOT NULL CONSTRAINT PK_DocumentArchiveBatch PRIMARY KEY, OwnerId uniqueidentifier NOT NULL,
        RootPageId uniqueidentifier NOT NULL, ArchivedAt datetime2(7) NOT NULL, UnarchivedAt datetime2(7) NULL,
        CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id), UpdatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id), RowVersion rowversion NOT NULL,
        CONSTRAINT UQ_DocumentArchiveBatch_Owner_Id UNIQUE(OwnerId,Id),
        CONSTRAINT FK_DocumentArchiveBatch_Root FOREIGN KEY(OwnerId,RootPageId) REFERENCES [documents].[Page](OwnerId,Id)
    );
END;
IF OBJECT_ID(N'documents.ArchiveMember',N'U') IS NULL
BEGIN
    CREATE TABLE [documents].[ArchiveMember](
        Id uniqueidentifier NOT NULL CONSTRAINT PK_DocumentArchiveMember PRIMARY KEY, OwnerId uniqueidentifier NOT NULL,
        BatchId uniqueidentifier NOT NULL, PageId uniqueidentifier NOT NULL, PreviousStatus varchar(16) NOT NULL CHECK(PreviousStatus IN('Draft','Published')),
        RestoredAt datetime2(7) NULL, CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id), UpdatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id), RowVersion rowversion NOT NULL,
        CONSTRAINT UQ_DocumentArchiveMember_Owner_Id UNIQUE(OwnerId,Id),
        CONSTRAINT UQ_DocumentArchiveMember_Cohort UNIQUE(OwnerId,BatchId,PageId),
        CONSTRAINT FK_DocumentArchiveMember_Batch FOREIGN KEY(OwnerId,BatchId) REFERENCES [documents].[ArchiveBatch](OwnerId,Id),
        CONSTRAINT FK_DocumentArchiveMember_Page FOREIGN KEY(OwnerId,PageId) REFERENCES [documents].[Page](OwnerId,Id)
    );
END;
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'documents.Page') AND name=N'FK_DocumentPage_ArchiveBatch')
    ALTER TABLE [documents].[Page] WITH CHECK ADD CONSTRAINT FK_DocumentPage_ArchiveBatch FOREIGN KEY(OwnerId,ArchiveBatchId) REFERENCES [documents].[ArchiveBatch](OwnerId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'documents.Page') AND name=N'IX_DocumentPage_Location')
    CREATE INDEX IX_DocumentPage_Location ON [documents].[Page](OwnerId,FolderId,ParentPageId,DeletedAt,UpdatedAt DESC,Id);
/* Existing Sharing-owned source invalidation remains in the same source TX.
   Scope both source identity and owner; restoration never revives a link. */
EXEC(N'CREATE OR ALTER TRIGGER [documents].[TR_ShareLink_DocumentInvalidation] ON [documents].[Page] AFTER UPDATE,DELETE AS
BEGIN
    SET NOCOUNT ON;
    WITH invalidated AS (
      SELECT Id,OwnerId,UpdatedByUserId FROM inserted WHERE DeletedAt IS NOT NULL
      UNION ALL SELECT d.Id,d.OwnerId,d.UpdatedByUserId FROM deleted d WHERE NOT EXISTS(SELECT 1 FROM inserted i WHERE i.Id=d.Id AND i.OwnerId=d.OwnerId)
    ) UPDATE link SET IsDeleted=1,RevokedAt=COALESCE(link.RevokedAt,SYSUTCDATETIME()),InvalidatedAt=COALESCE(link.InvalidatedAt,SYSUTCDATETIME()),
        InvalidationReason=COALESCE(link.InvalidationReason,''SourceDeleted''),UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=COALESCE(source.UpdatedByUserId,link.UpdatedByUserId)
      FROM [security].[ShareLink] link JOIN invalidated source ON source.Id=link.ResourceId AND source.OwnerId=link.OwnerId
      WHERE link.ResourceType=''Document'' AND link.IsDeleted=0;
END;');
/* Permissions are registered with completed handlers, not by this schema stage. */
COMMIT TRANSACTION;
GO
