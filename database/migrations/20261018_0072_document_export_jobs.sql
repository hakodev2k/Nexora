SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'operations.DocumentExportJob',N'U') IS NULL
BEGIN
    CREATE TABLE [operations].[DocumentExportJob](
        Id uniqueidentifier NOT NULL CONSTRAINT PK_DocumentExportJob PRIMARY KEY,
        OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id),
        PageId uniqueidentifier NOT NULL,VersionNumber bigint NOT NULL CHECK(VersionNumber>0),
        Format varchar(8) NOT NULL CHECK(Format IN('MD','DOCX')),CodecVersion int NOT NULL CHECK(CodecVersion=1),
        SourceDigest char(64) NOT NULL,ArtifactDigest char(64) NOT NULL,
        Artifact varbinary(max) NOT NULL CHECK(DATALENGTH(Artifact) BETWEEN 0 AND 5242880),
        WarningsJson nvarchar(2000) NOT NULL CHECK(ISJSON(WarningsJson)=1),
        CreatedAt datetime2(7) NOT NULL,ExpiresAt datetime2(7) NOT NULL,
        CreatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id),RowVersion rowversion NOT NULL,
        CONSTRAINT FK_DocumentExportJob_Source FOREIGN KEY(OwnerId,PageId) REFERENCES [platform].[Resource](OwnerId,Id),
        CONSTRAINT CK_DocumentExportJob_Expiry CHECK(ExpiresAt>CreatedAt),
        CONSTRAINT UQ_DocumentExportJob_Owner UNIQUE(OwnerId,Id)
    );
    CREATE INDEX IX_DocumentExportJob_Owner_Created ON [operations].[DocumentExportJob](OwnerId,CreatedAt DESC,Id DESC);
END;
INSERT [platform].[Permission](ActionKey,EffectiveStatus)
SELECT N'documents.format.export','Resolved'
WHERE NOT EXISTS(SELECT 1 FROM [platform].[Permission] WITH(UPDLOCK,HOLDLOCK) WHERE ActionKey=N'documents.format.export');
COMMIT TRANSACTION;
GO
