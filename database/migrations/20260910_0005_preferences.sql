SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

IF OBJECT_ID(N'[platform].[Preference]', N'U') IS NULL
BEGIN
    CREATE TABLE [platform].[Preference]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_Preference_Id] DEFAULT NEWSEQUENTIALID(),
        [OwnerId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_Preference_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_Preference_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedByUserId] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        [ModuleId] uniqueidentifier NULL,
        [PreferenceKey] nvarchar(100) NOT NULL,
        [SchemaVersion] int NOT NULL,
        [ValueJson] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Preference] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_Preference_Owner] FOREIGN KEY ([OwnerId]) REFERENCES [platform].[PersonalSpace]([Id]),
        CONSTRAINT [FK_Preference_CreatedBy] FOREIGN KEY ([CreatedByUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [FK_Preference_UpdatedBy] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [FK_Preference_Module] FOREIGN KEY ([ModuleId]) REFERENCES [platform].[Module]([Id]),
        CONSTRAINT [CK_Preference_SchemaVersion] CHECK ([SchemaVersion] > 0),
        CONSTRAINT [CK_Preference_ValueJson] CHECK (ISJSON([ValueJson]) = 1)
    );
    CREATE INDEX [IX_Preference_Owner_Created] ON [platform].[Preference]([OwnerId], [CreatedAt], [Id]);
    CREATE UNIQUE INDEX [UX_Preference_Global_Key] ON [platform].[Preference]([OwnerId], [PreferenceKey]) WHERE [ModuleId] IS NULL;
    CREATE UNIQUE INDEX [UX_Preference_Module_Key] ON [platform].[Preference]([OwnerId], [ModuleId], [PreferenceKey]) WHERE [ModuleId] IS NOT NULL;
END
GO

COMMIT TRANSACTION;
GO
