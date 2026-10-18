SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'operations.DocumentImportJob',N'U') IS NULL
BEGIN
    CREATE TABLE [operations].[DocumentImportJob](
        Id uniqueidentifier NOT NULL CONSTRAINT PK_DocumentImportJob PRIMARY KEY,
        OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id),
        PageId uniqueidentifier NOT NULL,FileId uniqueidentifier NOT NULL,
        SourceDigest char(64) NOT NULL,ParserVersion int NOT NULL CHECK(ParserVersion=1),
        EditorMode varchar(16) NOT NULL CHECK(EditorMode IN('Markdown','Block')),
        WarningsJson nvarchar(4000) NOT NULL CHECK(ISJSON(WarningsJson)=1),
        DraftEdited bit NOT NULL,CreatedAt datetime2(7) NOT NULL,
        CreatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id),RowVersion rowversion NOT NULL,
        CONSTRAINT FK_DocumentImportJob_Page FOREIGN KEY(OwnerId,PageId) REFERENCES [platform].[Resource](OwnerId,Id),
        -- FileId is historical provenance, not a pin/authority. Files physically
        -- purges source rows; owner/source proof is mandatory at import commit.
        CONSTRAINT UQ_DocumentImportJob_Page UNIQUE(OwnerId,PageId),
        CONSTRAINT UQ_DocumentImportJob_Owner UNIQUE(OwnerId,Id)
    );
END;
INSERT [platform].[Permission](ActionKey,EffectiveStatus)
SELECT N'documents.format.import','Resolved'
WHERE NOT EXISTS(SELECT 1 FROM [platform].[Permission] WITH(UPDLOCK,HOLDLOCK) WHERE ActionKey=N'documents.format.import');
COMMIT TRANSACTION;
GO
