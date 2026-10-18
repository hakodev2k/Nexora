SET XACT_ABORT ON;
BEGIN TRANSACTION;
-- Add provenance without rewriting any existing immutable snapshot.
IF COL_LENGTH(N'documents.PageVersion', N'SourceVersionNumber') IS NULL
    ALTER TABLE [documents].[PageVersion] ADD [SourceVersionNumber] bigint NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE [name]=N'UQ_DocumentVersion_Owner_Page_Number')
    ALTER TABLE [documents].[PageVersion] ADD CONSTRAINT [UQ_DocumentVersion_Owner_Page_Number] UNIQUE ([OwnerId],[PageId],[VersionNumber]);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_DocumentVersion_RestoredFrom')
    ALTER TABLE [documents].[PageVersion] ADD CONSTRAINT [FK_DocumentVersion_RestoredFrom]
        FOREIGN KEY ([OwnerId],[PageId],[SourceVersionNumber]) REFERENCES [documents].[PageVersion]([OwnerId],[PageId],[VersionNumber]);
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name]=N'CK_DocumentVersion_RestoredFrom')
    ALTER TABLE [documents].[PageVersion] ADD CONSTRAINT [CK_DocumentVersion_RestoredFrom]
        CHECK ([SourceVersionNumber] IS NULL OR ([SourceVersionNumber]>0 AND [SourceVersionNumber]<[VersionNumber]));
INSERT INTO [platform].[Permission] ([ActionKey],[EffectiveStatus])
SELECT N'documents.page.restore_version','Resolved'
WHERE NOT EXISTS (SELECT 1 FROM [platform].[Permission] WITH(UPDLOCK,HOLDLOCK) WHERE [ActionKey]=N'documents.page.restore_version');
-- Existing denied/blocked permission rows and actual user/module grants stay intact.
COMMIT TRANSACTION;
GO
