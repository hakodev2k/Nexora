SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

IF SCHEMA_ID(N'knowledge') IS NULL EXEC(N'CREATE SCHEMA [knowledge]');
GO

IF OBJECT_ID(N'[knowledge].[ReadingItem]', N'U') IS NULL
BEGIN
    CREATE TABLE [knowledge].[ReadingItem]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_ReadingItem_Id] DEFAULT NEWSEQUENTIALID(),
        [OwnerId] uniqueidentifier NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [UpdatedByUserId] uniqueidentifier NOT NULL,
        [SourceType] varchar(32) NOT NULL,
        [SourceId] uniqueidentifier NOT NULL,
        [State] varchar(64) NOT NULL CONSTRAINT [DF_ReadingItem_State] DEFAULT 'Unread',
        [Progress] decimal(28,8) NOT NULL CONSTRAINT [DF_ReadingItem_Progress] DEFAULT 0,
        [SavedAt] datetime2(7) NOT NULL CONSTRAINT [DF_ReadingItem_SavedAt] DEFAULT SYSUTCDATETIME(),
        [ReadAt] datetime2(7) NULL,
        [SafeTitleSnapshot] nvarchar(500) NOT NULL,
        [SafeUrlSnapshot] nvarchar(2048) NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_ReadingItem_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_ReadingItem_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_ReadingItem] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UX_ReadingItem_Owner_Id] UNIQUE ([OwnerId], [Id]),
        CONSTRAINT [UX_ReadingItem_Owner_Source] UNIQUE ([OwnerId], [SourceType], [SourceId]),
        CONSTRAINT [FK_ReadingItem_Owner] FOREIGN KEY ([OwnerId]) REFERENCES [platform].[PersonalSpace]([Id]),
        CONSTRAINT [FK_ReadingItem_CreatedBy] FOREIGN KEY ([CreatedByUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [FK_ReadingItem_UpdatedBy] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [CK_ReadingItem_SourceType] CHECK ([SourceType] IN ('Bookmark')),
        CONSTRAINT [CK_ReadingItem_State] CHECK ([State] IN ('Unread','Reading','Read','Archived')),
        CONSTRAINT [CK_ReadingItem_Progress] CHECK ([Progress] >= 0 AND [Progress] <= 1),
        CONSTRAINT [CK_ReadingItem_ReadAt] CHECK (([State] = 'Read' AND [ReadAt] IS NOT NULL) OR ([State] <> 'Read'))
    );

    CREATE INDEX [IX_ReadingItem_Owner_State_Saved] ON [knowledge].[ReadingItem]([OwnerId], [State], [SavedAt] DESC, [Id] DESC);
END
GO

IF OBJECT_ID(N'[platform].[Permission]', N'U') IS NOT NULL
BEGIN
    MERGE [platform].[Permission] AS target
    USING (VALUES
        (N'reading.queue.read'),
        (N'reading.item.save'),
        (N'reading.item.remove'),
        (N'reading.item.read'),
        (N'reading.item.unread'),
        (N'reading.item.position')
    ) AS source ([ActionKey])
    ON target.[ActionKey] = source.[ActionKey]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT ([ActionKey], [EffectiveStatus]) VALUES (source.[ActionKey], 'Resolved');
END
GO

COMMIT TRANSACTION;
GO
