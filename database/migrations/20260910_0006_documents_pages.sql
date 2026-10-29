SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

IF SCHEMA_ID(N'documents') IS NULL EXEC(N'CREATE SCHEMA [documents]');
GO

IF OBJECT_ID(N'[documents].[Page]', N'U') IS NULL
BEGIN
    CREATE TABLE [documents].[Page]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_DocumentPage_Id] DEFAULT NEWSEQUENTIALID(),
        [OwnerId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_DocumentPage_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_DocumentPage_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [DocumentType] varchar(32) NOT NULL,
        [EditorMode] varchar(16) NOT NULL,
        [Body] nvarchar(max) NOT NULL CONSTRAINT [DF_DocumentPage_Body] DEFAULT N'',
        [Status] varchar(16) NOT NULL CONSTRAINT [DF_DocumentPage_Status] DEFAULT 'Draft',
        [PreArchiveStatus] varchar(16) NULL,
        [VersionNumber] bigint NOT NULL CONSTRAINT [DF_DocumentPage_Version] DEFAULT 1,
        [DeletedAt] datetime2(7) NULL,
        CONSTRAINT [PK_DocumentPage] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_DocumentPage_Owner] FOREIGN KEY ([OwnerId]) REFERENCES [platform].[PersonalSpace]([Id]),
        CONSTRAINT [FK_DocumentPage_CreatedBy] FOREIGN KEY ([CreatedByUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [FK_DocumentPage_UpdatedBy] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [CK_DocumentPage_Type] CHECK ([DocumentType] IN ('Document','Note','Knowledge')),
        CONSTRAINT [CK_DocumentPage_Editor] CHECK ([EditorMode] IN ('Markdown','Block')),
        CONSTRAINT [CK_DocumentPage_Status] CHECK ([Status] IN ('Draft','Published','Archived')),
        CONSTRAINT [CK_DocumentPage_PreArchive] CHECK ([PreArchiveStatus] IS NULL OR [PreArchiveStatus] IN ('Draft','Published')),
        CONSTRAINT [CK_DocumentPage_Version] CHECK ([VersionNumber] > 0),
        CONSTRAINT [CK_DocumentPage_Body] CHECK (DATALENGTH([Body]) <= 2097152)
    );
    CREATE INDEX [IX_DocumentPage_Owner_Status_Updated] ON [documents].[Page]([OwnerId], [Status], [UpdatedAt] DESC, [Id]);
END
GO

IF OBJECT_ID(N'[documents].[PageVersion]', N'U') IS NULL
BEGIN
    CREATE TABLE [documents].[PageVersion]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_DocumentPageVersion_Id] DEFAULT NEWSEQUENTIALID(),
        [PageId] uniqueidentifier NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [VersionNumber] bigint NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [DocumentType] varchar(32) NOT NULL,
        [EditorMode] varchar(16) NOT NULL,
        [Status] varchar(16) NOT NULL,
        [ChangeNote] nvarchar(500) NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_DocumentPageVersion_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [CreatedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_DocumentPageVersion] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_DocumentPageVersion_Page] FOREIGN KEY ([PageId]) REFERENCES [documents].[Page]([Id]),
        CONSTRAINT [FK_DocumentPageVersion_Owner] FOREIGN KEY ([OwnerId]) REFERENCES [platform].[PersonalSpace]([Id]),
        CONSTRAINT [FK_DocumentPageVersion_CreatedBy] FOREIGN KEY ([CreatedByUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [UQ_DocumentPageVersion_Number] UNIQUE ([PageId], [VersionNumber]),
        CONSTRAINT [CK_DocumentPageVersion_Body] CHECK (DATALENGTH([Body]) <= 2097152),
        CONSTRAINT [CK_DocumentPageVersion_Status] CHECK ([Status] IN ('Draft','Published','Archived'))
    );
    CREATE INDEX [IX_DocumentPageVersion_Owner_Page_Created] ON [documents].[PageVersion]([OwnerId], [PageId], [CreatedAt] DESC);
END
GO

COMMIT TRANSACTION;
GO
