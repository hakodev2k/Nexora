SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

IF SCHEMA_ID(N'finance') IS NULL EXEC(N'CREATE SCHEMA [finance]');
GO

IF OBJECT_ID(N'[finance].[ManualCategory]', N'U') IS NULL
BEGIN
    CREATE TABLE [finance].[ManualCategory]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_ManualCategory_Id] DEFAULT NEWSEQUENTIALID(),
        [OwnerId] uniqueidentifier NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [UpdatedByUserId] uniqueidentifier NOT NULL,
        [Title] nvarchar(100) NOT NULL,
        [NormalizedTitle] nvarchar(100) NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_ManualCategory_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_ManualCategory_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_ManualCategory] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UX_ManualCategory_Owner_Id] UNIQUE ([OwnerId], [Id]),
        CONSTRAINT [UX_ManualCategory_Owner_NormalizedTitle] UNIQUE ([OwnerId], [NormalizedTitle]),
        CONSTRAINT [FK_ManualCategory_Owner] FOREIGN KEY ([OwnerId]) REFERENCES [platform].[PersonalSpace]([Id]),
        CONSTRAINT [FK_ManualCategory_CreatedBy] FOREIGN KEY ([CreatedByUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [FK_ManualCategory_UpdatedBy] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [identity].[User]([Id])
    );

    CREATE INDEX [IX_ManualCategory_Owner_Title] ON [finance].[ManualCategory]([OwnerId], [NormalizedTitle], [Id]);
END
GO

IF OBJECT_ID(N'[finance].[ManualRecord]', N'U') IS NULL
BEGIN
    CREATE TABLE [finance].[ManualRecord]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_ManualRecord_Id] DEFAULT NEWSEQUENTIALID(),
        [OwnerId] uniqueidentifier NOT NULL,
        [CategoryId] uniqueidentifier NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [UpdatedByUserId] uniqueidentifier NOT NULL,
        [Amount] decimal(28,8) NOT NULL,
        [CurrencyCode] char(3) NOT NULL,
        [OccurredOn] date NOT NULL,
        [Note] nvarchar(2000) NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_ManualRecord_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_ManualRecord_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_ManualRecord] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UX_ManualRecord_Owner_Id] UNIQUE ([OwnerId], [Id]),
        CONSTRAINT [FK_ManualRecord_Owner] FOREIGN KEY ([OwnerId]) REFERENCES [platform].[PersonalSpace]([Id]),
        CONSTRAINT [FK_ManualRecord_Category] FOREIGN KEY ([OwnerId], [CategoryId]) REFERENCES [finance].[ManualCategory]([OwnerId], [Id]),
        CONSTRAINT [FK_ManualRecord_CreatedBy] FOREIGN KEY ([CreatedByUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [FK_ManualRecord_UpdatedBy] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [CK_ManualRecord_Amount] CHECK ([Amount] >= 0),
        CONSTRAINT [CK_ManualRecord_Currency] CHECK ([CurrencyCode] COLLATE Latin1_General_100_BIN2 LIKE '[A-Z][A-Z][A-Z]')
    );

    CREATE INDEX [IX_ManualRecord_Owner_Occurred] ON [finance].[ManualRecord]([OwnerId], [OccurredOn] DESC, [Id] DESC);
    CREATE INDEX [IX_ManualRecord_Owner_Category] ON [finance].[ManualRecord]([OwnerId], [CategoryId], [OccurredOn] DESC, [Id] DESC);
    CREATE INDEX [IX_ManualRecord_Owner_Currency] ON [finance].[ManualRecord]([OwnerId], [CurrencyCode], [OccurredOn] DESC, [Id] DESC);
END
GO

IF OBJECT_ID(N'[platform].[Permission]', N'U') IS NOT NULL
BEGIN
    MERGE [platform].[Permission] AS target
    USING (VALUES
        (N'finance.manual_category.read'),
        (N'finance.manual_category.create'),
        (N'finance.manual_category.update'),
        (N'finance.manual_category.remove'),
        (N'finance.manual_record.read'),
        (N'finance.manual_record.create'),
        (N'finance.manual_record.update'),
        (N'finance.manual_summary.read')
    ) AS source ([ActionKey])
    ON target.[ActionKey] = source.[ActionKey]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT ([ActionKey], [EffectiveStatus]) VALUES (source.[ActionKey], 'Resolved');
END
GO

COMMIT TRANSACTION;
GO
