SET XACT_ABORT ON;
GO
BEGIN TRANSACTION;
GO
-- Additive local handoff only: stop old writers before applying. No legacy rows are deleted.
DECLARE @InitialCatalog bit = CASE WHEN OBJECT_ID(N'[documents].[Tag]', N'U') IS NULL THEN 1 ELSE 0 END;
IF @InitialCatalog = 1
BEGIN
    CREATE TABLE [documents].[Tag]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_DocumentTag_Id] DEFAULT NEWSEQUENTIALID(),
        [OwnerId] uniqueidentifier NOT NULL,
        [Name] nvarchar(80) NOT NULL,
        [NormalizedName] nvarchar(80) NOT NULL,
        [Color] varchar(7) NULL,
        [Namespace] AS CONVERT(varchar(64), 'documents') PERSISTED,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_DocumentTag_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_DocumentTag_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_DocumentTag] PRIMARY KEY ([Id]),
        CONSTRAINT [UQ_DocumentTag_Owner_Id] UNIQUE ([OwnerId], [Id]),
        CONSTRAINT [UQ_DocumentTag_Owner_Name] UNIQUE ([OwnerId], [NormalizedName]),
        CONSTRAINT [FK_DocumentTag_Owner] FOREIGN KEY ([OwnerId]) REFERENCES [platform].[PersonalSpace]([Id]),
        CONSTRAINT [FK_DocumentTag_CreatedBy] FOREIGN KEY ([CreatedByUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [FK_DocumentTag_UpdatedBy] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [CK_DocumentTag_Name] CHECK (LEN(LTRIM(RTRIM([Name]))) BETWEEN 1 AND 80),
        CONSTRAINT [CK_DocumentTag_Color] CHECK ([Color] IS NULL OR [Color] LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]')
    );
END;
IF COL_LENGTH(N'documents.Page', N'TagId') IS NULL
    ALTER TABLE [documents].[Page] ADD [TagId] uniqueidentifier NULL;
IF @InitialCatalog = 1
BEGIN
    -- Reject inconsistent owner/source/cardinality; do not pick a winner or repair user data.
    IF EXISTS (
        SELECT 1 FROM [organization].[ResourceTag] r
        JOIN [organization].[Tag] t ON t.[OwnerId]=r.[OwnerId] AND t.[Id]=r.[TagId]
        LEFT JOIN [documents].[Page] p ON p.[OwnerId]=r.[OwnerId] AND p.[Id]=r.[ResourceId]
        WHERE t.[Namespace]='documents' AND (r.[ResourceType]<>'Document' OR p.[Id] IS NULL))
        THROW 51056, 'Document tag legacy references require owner reconciliation.', 1;
    IF EXISTS (
        SELECT r.[OwnerId], r.[ResourceId] FROM [organization].[ResourceTag] r
        JOIN [organization].[Tag] t ON t.[OwnerId]=r.[OwnerId] AND t.[Id]=r.[TagId]
        WHERE r.[ResourceType]='Document'
        GROUP BY r.[OwnerId], r.[ResourceId]
        HAVING COUNT_BIG(*)>1 OR MIN(t.[Namespace])<>'documents' OR MAX(t.[Namespace])<>'documents')
        THROW 51056, 'Document tag legacy namespace or cardinality requires reconciliation.', 1;
    INSERT INTO [documents].[Tag] ([Id],[OwnerId],[Name],[NormalizedName],[Color],[CreatedByUserId],[UpdatedByUserId],[CreatedAt],[UpdatedAt])
        SELECT [Id],[OwnerId],[Name],[NormalizedName],[Color],[CreatedByUserId],[UpdatedByUserId],[CreatedAt],[UpdatedAt]
        FROM [organization].[Tag] WITH(UPDLOCK,HOLDLOCK) WHERE [Namespace]='documents';
    EXEC(N'IF EXISTS (SELECT 1 FROM [documents].[Page] p JOIN [organization].[ResourceTag] r ON r.[OwnerId]=p.[OwnerId] AND r.[ResourceId]=p.[Id] AND r.[ResourceType]=''Document'' WHERE p.[TagId] IS NOT NULL AND p.[TagId]<>r.[TagId]) THROW 51056, ''Document tag current assignment requires reconciliation.'', 1;
    UPDATE p SET [TagId]=r.[TagId] FROM [documents].[Page] p JOIN [organization].[ResourceTag] r ON r.[OwnerId]=p.[OwnerId] AND r.[ResourceId]=p.[Id] AND r.[ResourceType]=''Document'' WHERE p.[TagId] IS NULL;');
END;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name]=N'FK_DocumentPage_Tag')
    EXEC(N'ALTER TABLE [documents].[Page] WITH CHECK ADD CONSTRAINT [FK_DocumentPage_Tag]
        FOREIGN KEY ([OwnerId],[TagId]) REFERENCES [documents].[Tag]([OwnerId],[Id]);');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id]=OBJECT_ID(N'documents.Page') AND [name]=N'IX_DocumentPage_Owner_Tag')
    EXEC(N'CREATE INDEX [IX_DocumentPage_Owner_Tag] ON [documents].[Page]([OwnerId],[TagId]);');
GO
-- Explicit provider-owned read-only catalog contract; never exposes page payloads.
EXEC(N'CREATE OR ALTER VIEW [documents].[TagCatalogProjection] AS
 SELECT t.[Id],t.[OwnerId],t.[Namespace],t.[Name],t.[NormalizedName],t.[Color],t.[CreatedAt],t.[UpdatedAt],t.[RowVersion],
 CONVERT(int,(SELECT COUNT_BIG(*) FROM [documents].[Page] p WHERE p.[OwnerId]=t.[OwnerId] AND p.[TagId]=t.[Id])) AS [UsageCount]
 FROM [documents].[Tag] t;');
GO
INSERT INTO [platform].[Permission] ([ActionKey],[EffectiveStatus])
SELECT v.[ActionKey], 'Resolved' FROM (VALUES
 (N'documents.tag.read'),(N'documents.tag.create'),(N'documents.tag.rename'),(N'documents.tag.remove')) v([ActionKey])
WHERE NOT EXISTS (SELECT 1 FROM [platform].[Permission] p WHERE p.[ActionKey]=v.[ActionKey]);
-- Existing non-Resolved rows remain denied; no grant or module policy is updated.
GO
COMMIT TRANSACTION;
GO
