SET XACT_ABORT ON;
GO
BEGIN TRANSACTION;
GO
IF COL_LENGTH(N'documents.PageVersion',N'TagIdSnapshot') IS NULL
    ALTER TABLE [documents].[PageVersion] ADD [TagIdSnapshot] uniqueidentifier NULL;
IF COL_LENGTH(N'documents.PageVersion',N'TagNameSnapshot') IS NULL
    ALTER TABLE [documents].[PageVersion] ADD [TagNameSnapshot] nvarchar(80) NULL;
IF COL_LENGTH(N'documents.PageVersion',N'TagSnapshotCaptured') IS NULL
    ALTER TABLE [documents].[PageVersion] ADD [TagSnapshotCaptured] bit NOT NULL
        CONSTRAINT [DF_PageVersion_TagSnapshotCaptured] DEFAULT 0 WITH VALUES;
GO
-- Legacy rows explicitly remain unknown; never infer historical metadata from current Page.TagId.
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name]=N'CK_PageVersion_TagSnapshot')
    ALTER TABLE [documents].[PageVersion] WITH CHECK ADD CONSTRAINT [CK_PageVersion_TagSnapshot] CHECK (
        ([TagSnapshotCaptured]=0 AND [TagIdSnapshot] IS NULL AND [TagNameSnapshot] IS NULL) OR
        ([TagSnapshotCaptured]=1 AND (([TagIdSnapshot] IS NULL AND [TagNameSnapshot] IS NULL) OR
            ([TagIdSnapshot] IS NOT NULL AND [TagNameSnapshot] IS NOT NULL AND LEN(LTRIM(RTRIM([TagNameSnapshot]))) BETWEEN 1 AND 80))));
-- No Tag FK: history-only labels survive vocabulary deletion.
GO
COMMIT TRANSACTION;
GO
