SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

IF SCHEMA_ID(N'knowledge') IS NULL EXEC(N'CREATE SCHEMA [knowledge]');
GO

IF OBJECT_ID(N'[knowledge].[Snippet]', N'U') IS NULL
BEGIN
    CREATE TABLE [knowledge].[Snippet]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_Snippet_Id] DEFAULT NEWSEQUENTIALID(),
        [OwnerId] uniqueidentifier NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [UpdatedByUserId] uniqueidentifier NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Language] nvarchar(50) NOT NULL,
        [CurrentVersion] bigint NOT NULL CONSTRAINT [DF_Snippet_CurrentVersion] DEFAULT 1,
        [Status] varchar(64) NOT NULL CONSTRAINT [DF_Snippet_Status] DEFAULT 'Active',
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_Snippet_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_Snippet_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Snippet] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UX_Snippet_Owner_Id] UNIQUE ([OwnerId], [Id]),
        CONSTRAINT [FK_Snippet_Owner] FOREIGN KEY ([OwnerId]) REFERENCES [platform].[PersonalSpace]([Id]),
        CONSTRAINT [FK_Snippet_CreatedBy] FOREIGN KEY ([CreatedByUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [FK_Snippet_UpdatedBy] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [CK_Snippet_CurrentVersion] CHECK ([CurrentVersion] > 0),
        CONSTRAINT [CK_Snippet_Status] CHECK ([Status] IN ('Active','Archived'))
    );

    CREATE INDEX [IX_Snippet_Owner_Status_Updated] ON [knowledge].[Snippet]([OwnerId], [Status], [UpdatedAt] DESC, [Id] DESC);
    CREATE INDEX [IX_Snippet_Owner_Language_Title] ON [knowledge].[Snippet]([OwnerId], [Language], [Title]);
END
GO

IF OBJECT_ID(N'[knowledge].[SnippetVersion]', N'U') IS NULL
BEGIN
    CREATE TABLE [knowledge].[SnippetVersion]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_SnippetVersion_Id] DEFAULT NEWSEQUENTIALID(),
        [OwnerId] uniqueidentifier NOT NULL,
        [SnippetId] uniqueidentifier NOT NULL,
        [VersionNumber] bigint NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Language] nvarchar(50) NOT NULL,
        [SourceText] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [SourceVersion] bigint NULL,
        [CommandKeyHash] binary(32) NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_SnippetVersion_CreatedAt] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_SnippetVersion] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UX_SnippetVersion_Owner_Snippet_Version] UNIQUE ([OwnerId], [SnippetId], [VersionNumber]),
        CONSTRAINT [FK_SnippetVersion_Snippet] FOREIGN KEY ([OwnerId], [SnippetId]) REFERENCES [knowledge].[Snippet]([OwnerId], [Id]),
        CONSTRAINT [FK_SnippetVersion_Owner] FOREIGN KEY ([OwnerId]) REFERENCES [platform].[PersonalSpace]([Id]),
        CONSTRAINT [FK_SnippetVersion_CreatedBy] FOREIGN KEY ([CreatedByUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [CK_SnippetVersion_Version] CHECK ([VersionNumber] > 0),
        CONSTRAINT [CK_SnippetVersion_SourceVersion] CHECK ([SourceVersion] IS NULL OR [SourceVersion] > 0)
    );

    CREATE INDEX [IX_SnippetVersion_Owner_Snippet_Created] ON [knowledge].[SnippetVersion]([OwnerId], [SnippetId], [CreatedAt] DESC);
END
GO

IF OBJECT_ID(N'[platform].[Permission]', N'U') IS NOT NULL
BEGIN
    MERGE [platform].[Permission] AS target
    USING (VALUES
        (N'snippets.snippet.read'),
        (N'snippets.snippet.create'),
        (N'snippets.snippet.save'),
        (N'snippets.snippet.archive'),
        (N'snippets.snippet.unarchive')
    ) AS source ([ActionKey])
    ON target.[ActionKey] = source.[ActionKey]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT ([ActionKey], [EffectiveStatus]) VALUES (source.[ActionKey], 'Resolved');
END
GO

COMMIT TRANSACTION;
GO
