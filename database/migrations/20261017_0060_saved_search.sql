SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF SCHEMA_ID(N'discovery') IS NULL EXEC(N'CREATE SCHEMA [discovery]');
IF OBJECT_ID(N'discovery.SavedQuery',N'U') IS NULL
BEGIN
    CREATE TABLE [discovery].[SavedQuery] (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_SavedQuery] PRIMARY KEY,
        [OwnerId] uniqueidentifier NOT NULL CONSTRAINT [FK_SavedQuery_Owner] REFERENCES [platform].[PersonalSpace]([Id]),
        [Title] nvarchar(100) NOT NULL,
        [SchemaVersion] int NOT NULL,
        [QueryJson] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_SavedQuery_Created] DEFAULT SYSUTCDATETIME(),
        [CreatedByUserId] uniqueidentifier NOT NULL CONSTRAINT [FK_SavedQuery_CreatedBy] REFERENCES [identity].[User]([Id]),
        [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_SavedQuery_Updated] DEFAULT SYSUTCDATETIME(),
        [UpdatedByUserId] uniqueidentifier NOT NULL CONSTRAINT [FK_SavedQuery_UpdatedBy] REFERENCES [identity].[User]([Id]),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [CK_SavedQuery_Title] CHECK (LEN(LTRIM(RTRIM([Title]))) BETWEEN 1 AND 100),
        CONSTRAINT [CK_SavedQuery_Version] CHECK ([SchemaVersion]>0),
        CONSTRAINT [CK_SavedQuery_Json] CHECK (ISJSON([QueryJson])=1 AND DATALENGTH([QueryJson])<=8192),
        CONSTRAINT [UQ_SavedQuery_Owner_Id] UNIQUE ([OwnerId],[Id])
    );
    CREATE INDEX [IX_SavedQuery_Owner_Updated] ON [discovery].[SavedQuery]([OwnerId],[UpdatedAt] DESC,[Id] DESC);
END;
INSERT INTO [platform].[Permission] ([ActionKey],[EffectiveStatus])
SELECT [ActionKey], 'Resolved' FROM (VALUES
    (N'discovery.saved_search.read'),(N'discovery.saved_search.create'),(N'discovery.saved_search.update'),
    (N'discovery.saved_search.delete'),(N'discovery.saved_search.run')) v([ActionKey])
WHERE NOT EXISTS (SELECT 1 FROM [platform].[Permission] p WITH(UPDLOCK,HOLDLOCK) WHERE p.[ActionKey]=v.[ActionKey]);
COMMIT TRANSACTION;
GO
