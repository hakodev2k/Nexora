SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

/* FX25-S03: owner-scoped typed favorite references. The local implementation
   intentionally stores a typed reference instead of introducing a generic
   platform.Resource registry that does not exist in the current schema. The
   source owner, lifecycle and current read capability are rechecked by the
   service. */
IF SCHEMA_ID(N'discovery') IS NULL EXEC(N'CREATE SCHEMA [discovery]');
GO

IF OBJECT_ID(N'[discovery].[Favorite]', N'U') IS NULL
BEGIN
    CREATE TABLE [discovery].[Favorite]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_DiscoveryFavorite_Id] DEFAULT NEWSEQUENTIALID(),
        [OwnerId] uniqueidentifier NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [UpdatedByUserId] uniqueidentifier NOT NULL,
        [ResourceType] varchar(32) NOT NULL,
        [ResourceId] uniqueidentifier NOT NULL,
        [Rank] decimal(28,8) NOT NULL CONSTRAINT [DF_DiscoveryFavorite_Rank] DEFAULT 0,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_DiscoveryFavorite_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_DiscoveryFavorite_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_DiscoveryFavorite] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UX_DiscoveryFavorite_Owner_Id] UNIQUE ([OwnerId], [Id]),
        CONSTRAINT [UX_DiscoveryFavorite_Owner_Resource] UNIQUE ([OwnerId], [ResourceType], [ResourceId]),
        CONSTRAINT [FK_DiscoveryFavorite_Owner] FOREIGN KEY ([OwnerId]) REFERENCES [platform].[PersonalSpace]([Id]),
        CONSTRAINT [FK_DiscoveryFavorite_CreatedBy] FOREIGN KEY ([CreatedByUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [FK_DiscoveryFavorite_UpdatedBy] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [CK_DiscoveryFavorite_ResourceType] CHECK ([ResourceType] IN ('Project','Task','Event','Document','Bookmark','Snippet','Goal')),
        CONSTRAINT [CK_DiscoveryFavorite_Rank] CHECK ([Rank] >= 0 AND [Rank] <= 1000000000)
    );

    CREATE INDEX [IX_DiscoveryFavorite_Owner_Rank] ON [discovery].[Favorite]([OwnerId], [Rank], [CreatedAt], [Id]);
    CREATE INDEX [IX_DiscoveryFavorite_Owner_Resource] ON [discovery].[Favorite]([OwnerId], [ResourceType], [ResourceId]);
END
GO

/* RequestReceipt is shared by idempotent services. These optional columns hold
   only a safe response projection for slices that need exact replay. */
IF OBJECT_ID(N'[identity].[RequestReceipt]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'identity.RequestReceipt', N'ResultStatusCode') IS NULL
        ALTER TABLE [identity].[RequestReceipt] ADD [ResultStatusCode] int NULL;
    IF COL_LENGTH(N'identity.RequestReceipt', N'ResultJson') IS NULL
        ALTER TABLE [identity].[RequestReceipt] ADD [ResultJson] nvarchar(max) NULL;
END
GO

IF OBJECT_ID(N'[platform].[Permission]', N'U') IS NOT NULL
BEGIN
    MERGE [platform].[Permission] AS target
    USING (VALUES
        (N'discovery.favorite.read'),
        (N'discovery.favorite.add'),
        (N'discovery.favorite.remove'),
        (N'discovery.favorite.reorder')
    ) AS source ([ActionKey])
    ON target.[ActionKey] = source.[ActionKey]
    WHEN MATCHED AND target.[EffectiveStatus] <> 'Resolved' THEN
        UPDATE SET [EffectiveStatus] = 'Resolved'
    WHEN NOT MATCHED BY TARGET THEN
        INSERT ([ActionKey], [EffectiveStatus]) VALUES (source.[ActionKey], 'Resolved');
END
GO

COMMIT TRANSACTION;
GO
