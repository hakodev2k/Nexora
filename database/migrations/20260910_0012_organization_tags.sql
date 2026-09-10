SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

IF SCHEMA_ID(N'organization') IS NULL EXEC(N'CREATE SCHEMA [organization]');
GO

/*
   FX24-S01 local slice: namespace-scoped owner tags. ResourceTag is a
   relationship boundary for future provider-owned assignment; this slice
   does not expose assignment writes, but it protects a tag from deletion
   while a provider has references.
*/
IF OBJECT_ID(N'[organization].[Tag]', N'U') IS NULL
BEGIN
    CREATE TABLE [organization].[Tag]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_OrganizationTag_Id] DEFAULT NEWSEQUENTIALID(),
        [OwnerId] uniqueidentifier NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [UpdatedByUserId] uniqueidentifier NOT NULL,
        [Namespace] varchar(64) NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [NormalizedName] nvarchar(50) NOT NULL,
        [Color] varchar(7) NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_OrganizationTag_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_OrganizationTag_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_OrganizationTag] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UX_OrganizationTag_Owner_Id] UNIQUE ([OwnerId], [Id]),
        CONSTRAINT [UX_OrganizationTag_Owner_Namespace_Name] UNIQUE ([OwnerId], [Namespace], [NormalizedName]),
        CONSTRAINT [FK_OrganizationTag_Owner] FOREIGN KEY ([OwnerId]) REFERENCES [platform].[PersonalSpace]([Id]),
        CONSTRAINT [FK_OrganizationTag_CreatedBy] FOREIGN KEY ([CreatedByUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [FK_OrganizationTag_UpdatedBy] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [CK_OrganizationTag_Name] CHECK (LEN(LTRIM(RTRIM([Name]))) BETWEEN 1 AND 50),
        CONSTRAINT [CK_OrganizationTag_Color] CHECK ([Color] IS NULL OR [Color] LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]')
    );

    CREATE INDEX [IX_OrganizationTag_Owner_Namespace_Name]
        ON [organization].[Tag]([OwnerId], [Namespace], [Name], [Id]);
END
GO

IF OBJECT_ID(N'[organization].[ResourceTag]', N'U') IS NULL
BEGIN
    CREATE TABLE [organization].[ResourceTag]
    (
        [OwnerId] uniqueidentifier NOT NULL,
        [TagId] uniqueidentifier NOT NULL,
        [ResourceType] varchar(32) NOT NULL,
        [ResourceId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_OrganizationResourceTag_CreatedAt] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_OrganizationResourceTag] PRIMARY KEY CLUSTERED ([OwnerId], [TagId], [ResourceType], [ResourceId]),
        CONSTRAINT [FK_OrganizationResourceTag_Tag] FOREIGN KEY ([OwnerId], [TagId]) REFERENCES [organization].[Tag]([OwnerId], [Id]),
        CONSTRAINT [CK_OrganizationResourceTag_Type] CHECK ([ResourceType] IN ('Project','Task','Document','Bookmark','Snippet'))
    );

    CREATE INDEX [IX_OrganizationResourceTag_Owner_Resource]
        ON [organization].[ResourceTag]([OwnerId], [ResourceType], [ResourceId]);
END
GO

IF OBJECT_ID(N'[platform].[Permission]', N'U') IS NOT NULL
BEGIN
    MERGE [platform].[Permission] AS target
    USING (VALUES
        (N'organization.tag.read'),
        (N'organization.tag.create'),
        (N'organization.tag.rename'),
        (N'organization.tag.remove')
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
