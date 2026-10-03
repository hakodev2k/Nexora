SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

IF SCHEMA_ID(N'knowledge') IS NULL EXEC(N'CREATE SCHEMA [knowledge]');
GO

IF OBJECT_ID(N'[knowledge].[Bookmark]', N'U') IS NULL
BEGIN
    CREATE TABLE [knowledge].[Bookmark]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_Bookmark_Id] DEFAULT NEWSEQUENTIALID(),
        [OwnerId] uniqueidentifier NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [UpdatedByUserId] uniqueidentifier NOT NULL,
        [Url] nvarchar(2048) NOT NULL,
        [CanonicalUrl] nvarchar(2048) NOT NULL,
        [UrlDigest] binary(32) NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(max) NULL,
        [MetadataJson] nvarchar(max) NOT NULL CONSTRAINT [DF_Bookmark_MetadataJson] DEFAULT N'{}',
        [Health] varchar(64) NOT NULL CONSTRAINT [DF_Bookmark_Health] DEFAULT 'Unknown',
        [LastCheckedAt] datetime2(7) NULL,
        [Status] varchar(64) NOT NULL CONSTRAINT [DF_Bookmark_Status] DEFAULT 'Active',
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_Bookmark_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_Bookmark_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Bookmark] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UX_Bookmark_Owner_Id] UNIQUE ([OwnerId], [Id]),
        CONSTRAINT [FK_Bookmark_Owner] FOREIGN KEY ([OwnerId]) REFERENCES [platform].[PersonalSpace]([Id]),
        CONSTRAINT [FK_Bookmark_CreatedBy] FOREIGN KEY ([CreatedByUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [FK_Bookmark_UpdatedBy] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [CK_Bookmark_Health] CHECK ([Health] IN ('Unknown','Available','Unavailable','Stale')),
        CONSTRAINT [CK_Bookmark_Status] CHECK ([Status] IN ('Active','Archived')),
        CONSTRAINT [CK_Bookmark_MetadataJson] CHECK (ISJSON([MetadataJson]) = 1)
    );

    CREATE INDEX [IX_Bookmark_Owner_Status_Updated] ON [knowledge].[Bookmark]([OwnerId], [Status], [UpdatedAt] DESC, [Id] DESC);
    CREATE INDEX [IX_Bookmark_Owner_UrlDigest] ON [knowledge].[Bookmark]([OwnerId], [UrlDigest]);
END
GO

IF OBJECT_ID(N'[platform].[Permission]', N'U') IS NOT NULL
BEGIN
    MERGE [platform].[Permission] AS target
    USING (VALUES
        (N'bookmarks.bookmark.read'),
        (N'bookmarks.bookmark.create'),
        (N'bookmarks.bookmark.update'),
        (N'bookmarks.bookmark.archive'),
        (N'bookmarks.bookmark.unarchive')
    ) AS source ([ActionKey])
    ON target.[ActionKey] = source.[ActionKey]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT ([ActionKey], [EffectiveStatus]) VALUES (source.[ActionKey], 'Resolved');
END
GO

COMMIT TRANSACTION;
GO
