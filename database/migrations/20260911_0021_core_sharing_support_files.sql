SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

IF SCHEMA_ID(N'files') IS NULL EXEC(N'CREATE SCHEMA [files]');
GO

/*
   Additive Release 1 core tables. OwnerId is always PersonalSpace.Id; UserId
   is only used for actors/allow-lists. Raw share and upload capabilities are
   never persisted, only their SHA-256 digests are.
*/
IF OBJECT_ID(N'[security].[ShareLink]', N'U') IS NULL
BEGIN
    CREATE TABLE [security].[ShareLink]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_ShareLink_Id] DEFAULT NEWSEQUENTIALID(),
        [OwnerId] uniqueidentifier NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [UpdatedByUserId] uniqueidentifier NOT NULL,
        [ResourceType] varchar(32) NOT NULL,
        [ResourceId] uniqueidentifier NOT NULL,
        [TokenHash] binary(32) NOT NULL,
        [Mode] varchar(32) NOT NULL,
        [ExpiresAt] datetime2(7) NULL,
        [RevokedAt] datetime2(7) NULL,
        [ProjectionVersion] varchar(32) NOT NULL CONSTRAINT [DF_ShareLink_ProjectionVersion] DEFAULT 'v1',
        [IsDeleted] bit NOT NULL CONSTRAINT [DF_ShareLink_IsDeleted] DEFAULT 0,
        [InvalidatedAt] datetime2(7) NULL,
        [InvalidationReason] varchar(64) NULL,
        [IssuedSharingEpoch] bigint NOT NULL CONSTRAINT [DF_ShareLink_IssuedEpoch] DEFAULT 1,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_ShareLink_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_ShareLink_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_ShareLink] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UX_ShareLink_Owner_Id] UNIQUE ([OwnerId], [Id]),
        CONSTRAINT [FK_ShareLink_Owner] FOREIGN KEY ([OwnerId]) REFERENCES [platform].[PersonalSpace]([Id]),
        CONSTRAINT [FK_ShareLink_CreatedBy] FOREIGN KEY ([CreatedByUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [FK_ShareLink_UpdatedBy] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [UX_ShareLink_TokenHash] UNIQUE ([TokenHash]),
        CONSTRAINT [CK_ShareLink_ResourceType] CHECK ([ResourceType] IN ('Project','Document')),
        CONSTRAINT [CK_ShareLink_Mode] CHECK ([Mode] IN ('PublicLink','AuthenticatedLink','RestrictedUsers')),
        CONSTRAINT [CK_ShareLink_Epoch] CHECK ([IssuedSharingEpoch] > 0)
    );

    CREATE INDEX [IX_ShareLink_Owner_Updated] ON [security].[ShareLink]([OwnerId], [IsDeleted], [UpdatedAt] DESC, [Id]);
    CREATE INDEX [IX_ShareLink_Resource] ON [security].[ShareLink]([OwnerId], [ResourceType], [ResourceId], [IsDeleted]);
END
GO

IF OBJECT_ID(N'[security].[ShareAllowedUser]', N'U') IS NULL
BEGIN
    CREATE TABLE [security].[ShareAllowedUser]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_ShareAllowedUser_Id] DEFAULT NEWSEQUENTIALID(),
        [OwnerId] uniqueidentifier NOT NULL,
        [ShareLinkId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_ShareAllowedUser_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_ShareAllowedUser_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_ShareAllowedUser] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_ShareAllowedUser_Owner] FOREIGN KEY ([OwnerId]) REFERENCES [platform].[PersonalSpace]([Id]),
        CONSTRAINT [FK_ShareAllowedUser_Link] FOREIGN KEY ([OwnerId], [ShareLinkId]) REFERENCES [security].[ShareLink]([OwnerId], [Id]),
        CONSTRAINT [FK_ShareAllowedUser_User] FOREIGN KEY ([UserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [UX_ShareAllowedUser_Link_User] UNIQUE ([ShareLinkId], [UserId])
    );

    CREATE INDEX [IX_ShareAllowedUser_Owner_Link] ON [security].[ShareAllowedUser]([OwnerId], [ShareLinkId]);
END
GO

IF OBJECT_ID(N'[security].[SupportGrant]', N'U') IS NULL
BEGIN
    CREATE TABLE [security].[SupportGrant]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_SupportGrant_Id] DEFAULT NEWSEQUENTIALID(),
        [OwnerId] uniqueidentifier NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [UpdatedByUserId] uniqueidentifier NOT NULL,
        [ModuleId] uniqueidentifier NOT NULL,
        [DurationMode] varchar(32) NOT NULL,
        [ExpiresAt] datetime2(7) NULL,
        [RevokedAt] datetime2(7) NULL,
        [ConsentVersion] varchar(32) NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_SupportGrant_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_SupportGrant_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_SupportGrant] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UX_SupportGrant_Owner_Id] UNIQUE ([OwnerId], [Id]),
        CONSTRAINT [FK_SupportGrant_Owner] FOREIGN KEY ([OwnerId]) REFERENCES [platform].[PersonalSpace]([Id]),
        CONSTRAINT [FK_SupportGrant_CreatedBy] FOREIGN KEY ([CreatedByUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [FK_SupportGrant_UpdatedBy] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [FK_SupportGrant_Module] FOREIGN KEY ([ModuleId]) REFERENCES [platform].[Module]([Id]),
        CONSTRAINT [CK_SupportGrant_Duration] CHECK ([DurationMode] IN ('24Hours','Custom','UntilRevoked')),
        CONSTRAINT [CK_SupportGrant_Expiry] CHECK (([DurationMode] = 'UntilRevoked' AND [ExpiresAt] IS NULL) OR ([DurationMode] IN ('24Hours','Custom') AND [ExpiresAt] IS NOT NULL))
    );

    CREATE INDEX [IX_SupportGrant_Owner_Module] ON [security].[SupportGrant]([OwnerId], [ModuleId], [RevokedAt], [ExpiresAt]);
END
GO

IF OBJECT_ID(N'[security].[AccessSession]', N'U') IS NULL
BEGIN
    CREATE TABLE [security].[AccessSession]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_AccessSession_Id] DEFAULT NEWSEQUENTIALID(),
        [OwnerId] uniqueidentifier NOT NULL,
        [ActorUserId] uniqueidentifier NOT NULL,
        [TargetUserId] uniqueidentifier NOT NULL,
        [ModuleId] uniqueidentifier NOT NULL,
        [Mode] varchar(16) NOT NULL,
        [SupportGrantId] uniqueidentifier NULL,
        [Reason] nvarchar(1000) NULL,
        [ExpiresAt] datetime2(7) NOT NULL,
        [EndedAt] datetime2(7) NULL,
        [OpeningAuditId] uniqueidentifier NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_AccessSession_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_AccessSession] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_AccessSession_Owner] FOREIGN KEY ([OwnerId]) REFERENCES [platform].[PersonalSpace]([Id]),
        CONSTRAINT [FK_AccessSession_Actor] FOREIGN KEY ([ActorUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [FK_AccessSession_Target] FOREIGN KEY ([TargetUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [FK_AccessSession_Module] FOREIGN KEY ([ModuleId]) REFERENCES [platform].[Module]([Id]),
        CONSTRAINT [FK_AccessSession_Grant] FOREIGN KEY ([OwnerId], [SupportGrantId]) REFERENCES [security].[SupportGrant]([OwnerId], [Id]),
        CONSTRAINT [FK_AccessSession_Audit] FOREIGN KEY ([OpeningAuditId]) REFERENCES [security].[AuditEvent]([Id]),
        CONSTRAINT [CK_AccessSession_Mode] CHECK ([Mode] IN ('Support','Emergency')),
        CONSTRAINT [CK_AccessSession_Grant] CHECK (([Mode] = 'Support' AND [SupportGrantId] IS NOT NULL) OR [Mode] = 'Emergency')
    );

    CREATE INDEX [IX_AccessSession_Owner_Active] ON [security].[AccessSession]([OwnerId], [EndedAt], [ExpiresAt]);
    CREATE INDEX [IX_AccessSession_Actor_Active] ON [security].[AccessSession]([ActorUserId], [EndedAt], [ExpiresAt]);
END
GO

IF OBJECT_ID(N'[files].[FileObject]', N'U') IS NULL
BEGIN
    CREATE TABLE [files].[FileObject]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_FileObject_Id] DEFAULT NEWSEQUENTIALID(),
        [OwnerId] uniqueidentifier NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [UpdatedByUserId] uniqueidentifier NOT NULL,
        [StorageKey] nvarchar(500) NOT NULL,
        [OriginalName] nvarchar(255) NOT NULL,
        [MediaType] varchar(127) NOT NULL,
        [ByteLength] bigint NOT NULL,
        [Digest] binary(32) NOT NULL,
        [ScanState] varchar(32) NOT NULL,
        [Lifecycle] varchar(16) NOT NULL CONSTRAINT [DF_FileObject_Lifecycle] DEFAULT 'Active',
        [CurrentRevision] bigint NOT NULL CONSTRAINT [DF_FileObject_Revision] DEFAULT 1,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_FileObject_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_FileObject_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_FileObject] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UX_FileObject_Owner_Id] UNIQUE ([OwnerId], [Id]),
        CONSTRAINT [FK_FileObject_Owner] FOREIGN KEY ([OwnerId]) REFERENCES [platform].[PersonalSpace]([Id]),
        CONSTRAINT [FK_FileObject_CreatedBy] FOREIGN KEY ([CreatedByUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [FK_FileObject_UpdatedBy] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [UX_FileObject_StorageKey] UNIQUE ([StorageKey]),
        CONSTRAINT [CK_FileObject_Bytes] CHECK ([ByteLength] >= 0 AND [ByteLength] <= 26214400),
        CONSTRAINT [CK_FileObject_Scan] CHECK ([ScanState] IN ('Pending','Clean','Quarantined','Failed')),
        CONSTRAINT [CK_FileObject_Lifecycle] CHECK ([Lifecycle] IN ('Active','Trash','Purged')),
        CONSTRAINT [CK_FileObject_Revision] CHECK ([CurrentRevision] > 0)
    );

    CREATE INDEX [IX_FileObject_Owner_Lifecycle_Updated] ON [files].[FileObject]([OwnerId], [Lifecycle], [UpdatedAt] DESC, [Id]);
END
GO

IF OBJECT_ID(N'[files].[FileReference]', N'U') IS NULL
BEGIN
    CREATE TABLE [files].[FileReference]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_FileReference_Id] DEFAULT NEWSEQUENTIALID(),
        [OwnerId] uniqueidentifier NOT NULL,
        [FileObjectId] uniqueidentifier NOT NULL,
        [ResourceType] varchar(32) NOT NULL,
        [ResourceId] uniqueidentifier NOT NULL,
        [VersionNumber] bigint NULL,
        [Purpose] varchar(64) NOT NULL,
        [ReferenceKey] nvarchar(128) NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_FileReference_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_FileReference] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_FileReference_Owner] FOREIGN KEY ([OwnerId]) REFERENCES [platform].[PersonalSpace]([Id]),
        CONSTRAINT [FK_FileReference_File] FOREIGN KEY ([OwnerId], [FileObjectId]) REFERENCES [files].[FileObject]([OwnerId], [Id]),
        CONSTRAINT [CK_FileReference_Resource] CHECK ([ResourceType] IN ('Project','Task','Document')),
        CONSTRAINT [CK_FileReference_Version] CHECK ([VersionNumber] IS NULL OR [VersionNumber] > 0),
        CONSTRAINT [UX_FileReference_Resource] UNIQUE ([OwnerId], [FileObjectId], [ResourceType], [ResourceId], [VersionNumber], [Purpose], [ReferenceKey])
    );

    CREATE INDEX [IX_FileReference_Owner_Resource] ON [files].[FileReference]([OwnerId], [ResourceType], [ResourceId]);
END
GO

IF OBJECT_ID(N'[files].[UploadSession]', N'U') IS NULL
BEGIN
    CREATE TABLE [files].[UploadSession]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_UploadSession_Id] DEFAULT NEWSEQUENTIALID(),
        [OwnerId] uniqueidentifier NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [FileObjectId] uniqueidentifier NULL,
        [UploadHandleHash] binary(32) NOT NULL,
        [OriginalName] nvarchar(255) NOT NULL,
        [MediaType] varchar(127) NOT NULL,
        [ExpectedBytes] bigint NOT NULL,
        [ReceivedBytes] bigint NOT NULL CONSTRAINT [DF_UploadSession_Received] DEFAULT 0,
        [State] varchar(32) NOT NULL CONSTRAINT [DF_UploadSession_State] DEFAULT 'Created',
        [ExpiresAt] datetime2(7) NOT NULL,
        [ErrorCode] varchar(64) NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_UploadSession_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_UploadSession_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_UploadSession] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_UploadSession_Owner] FOREIGN KEY ([OwnerId]) REFERENCES [platform].[PersonalSpace]([Id]),
        CONSTRAINT [FK_UploadSession_CreatedBy] FOREIGN KEY ([CreatedByUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [FK_UploadSession_File] FOREIGN KEY ([OwnerId], [FileObjectId]) REFERENCES [files].[FileObject]([OwnerId], [Id]),
        CONSTRAINT [UX_UploadSession_HandleHash] UNIQUE ([UploadHandleHash]),
        CONSTRAINT [CK_UploadSession_Bytes] CHECK ([ExpectedBytes] >= 0 AND [ExpectedBytes] <= 26214400 AND [ReceivedBytes] >= 0 AND [ReceivedBytes] <= [ExpectedBytes]),
        CONSTRAINT [CK_UploadSession_State] CHECK ([State] IN ('Created','Uploading','Scanning','Ready','Failed','Canceled'))
    );

    CREATE INDEX [IX_UploadSession_Owner_State_Expiry] ON [files].[UploadSession]([OwnerId], [State], [ExpiresAt]);
END
GO

/* Keep the migration rerunnable if the table was created by an earlier local
   checkout of this additive migration. */
IF OBJECT_ID(N'[files].[UploadSession]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'files.UploadSession', N'OriginalName') IS NULL
        ALTER TABLE [files].[UploadSession] ADD [OriginalName] nvarchar(255) NOT NULL CONSTRAINT [DF_UploadSession_OriginalName] DEFAULT N'upload.bin';
    IF COL_LENGTH(N'files.UploadSession', N'MediaType') IS NULL
        ALTER TABLE [files].[UploadSession] ADD [MediaType] varchar(127) NOT NULL CONSTRAINT [DF_UploadSession_MediaType] DEFAULT 'application/octet-stream';
END
GO

/* The existing catalog is authoritative for action existence and status. */
IF OBJECT_ID(N'[platform].[Permission]', N'U') IS NOT NULL
BEGIN
    MERGE [platform].[Permission] AS target
    USING (VALUES
        (N'sharing.link.read'), (N'sharing.link.create'), (N'sharing.link.update'),
        (N'sharing.link.revoke'), (N'sharing.link.resolve'), (N'sharing.link.copy_created'),
        (N'support.consent.read'), (N'support.consent.grant'), (N'support.consent.revoke'),
        (N'support.session.open'), (N'support.session.end'),
        (N'support.emergency.open'), (N'support.emergency.end'),
        (N'files.file.read'), (N'files.file.upload'), (N'files.file.cancel_upload'),
        (N'files.file.rename'), (N'files.file.replace'), (N'files.file.preview'),
        (N'files.file.download'), (N'files.reference.attach'), (N'files.reference.detach'),
        (N'files.file.trash'), (N'files.file.restore'), (N'files.file.purge'),
        (N'files.scan.complete'), (N'files.support.read')
    ) AS source ([ActionKey])
    ON target.[ActionKey] = source.[ActionKey]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT ([ActionKey], [EffectiveStatus]) VALUES (source.[ActionKey], 'Resolved');
END
GO

/* Source deletion/trash permanently invalidates links; restoration cannot
   revive the old capability. */
CREATE OR ALTER TRIGGER [productivity].[TR_ShareLink_ProjectInvalidation]
ON [productivity].[Project]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE link
    SET [IsDeleted] = 1,
        [RevokedAt] = COALESCE([RevokedAt], SYSUTCDATETIME()),
        [InvalidatedAt] = COALESCE([InvalidatedAt], SYSUTCDATETIME()),
        [InvalidationReason] = COALESCE([InvalidationReason], 'SourceDeleted'),
        [UpdatedAt] = SYSUTCDATETIME()
    FROM [security].[ShareLink] link
    INNER JOIN inserted source ON source.[Id] = link.[ResourceId]
    WHERE link.[ResourceType] = 'Project'
      AND link.[IsDeleted] = 0
      AND source.[Status] = 'Deleted';
END
GO

CREATE OR ALTER TRIGGER [documents].[TR_ShareLink_DocumentInvalidation]
ON [documents].[Page]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE link
    SET [IsDeleted] = 1,
        [RevokedAt] = COALESCE([RevokedAt], SYSUTCDATETIME()),
        [InvalidatedAt] = COALESCE([InvalidatedAt], SYSUTCDATETIME()),
        [InvalidationReason] = COALESCE([InvalidationReason], 'SourceDeleted'),
        [UpdatedAt] = SYSUTCDATETIME()
    FROM [security].[ShareLink] link
    INNER JOIN inserted source ON source.[Id] = link.[ResourceId]
    WHERE link.[ResourceType] = 'Document'
      AND link.[IsDeleted] = 0
      AND source.[DeletedAt] IS NOT NULL;
END
GO

CREATE OR ALTER TRIGGER [platform].[TR_ShareLink_ModuleInvalidation]
ON [platform].[Module]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE link
    SET [IsDeleted] = 1,
        [RevokedAt] = COALESCE([RevokedAt], SYSUTCDATETIME()),
        [InvalidatedAt] = COALESCE([InvalidatedAt], SYSUTCDATETIME()),
        [InvalidationReason] = COALESCE([InvalidationReason], 'SharingDisabled'),
        [UpdatedAt] = SYSUTCDATETIME()
    FROM [security].[ShareLink] link
    INNER JOIN inserted moduleRow ON moduleRow.[Code] = 'FX04'
    WHERE (moduleRow.[State] <> 'Ready' OR moduleRow.[SystemEnabled] <> 1 OR moduleRow.[RegistrationEnabled] <> 1)
      AND link.[IsDeleted] = 0;
END
GO

CREATE OR ALTER TRIGGER [identity].[TR_ShareLink_UserInvalidation]
ON [identity].[User]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE link
    SET [IsDeleted] = 1,
        [RevokedAt] = COALESCE([RevokedAt], SYSUTCDATETIME()),
        [InvalidatedAt] = COALESCE([InvalidatedAt], SYSUTCDATETIME()),
        [InvalidationReason] = COALESCE([InvalidationReason], 'AccountDeleted'),
        [UpdatedAt] = SYSUTCDATETIME()
    FROM [security].[ShareLink] link
    INNER JOIN [platform].[PersonalSpace] ownerSpace ON ownerSpace.[Id] = link.[OwnerId]
    INNER JOIN inserted ownerUser ON ownerUser.[Id] = ownerSpace.[UserId]
    WHERE (ownerUser.[State] <> 'Active' OR ownerUser.[IsDeleted] <> 0)
      AND link.[IsDeleted] = 0;

    UPDATE grantRow
    SET [RevokedAt] = COALESCE([RevokedAt], SYSUTCDATETIME()),
        [UpdatedAt] = SYSUTCDATETIME()
    FROM [security].[SupportGrant] grantRow
    INNER JOIN [platform].[PersonalSpace] ownerSpace ON ownerSpace.[Id] = grantRow.[OwnerId]
    INNER JOIN inserted ownerUser ON ownerUser.[Id] = ownerSpace.[UserId]
    WHERE (ownerUser.[State] <> 'Active' OR ownerUser.[IsDeleted] <> 0)
      AND grantRow.[RevokedAt] IS NULL;

    UPDATE sessionRow
    SET [EndedAt] = COALESCE([EndedAt], SYSUTCDATETIME())
    FROM [security].[AccessSession] sessionRow
    INNER JOIN inserted changedUser
      ON changedUser.[Id] = sessionRow.[TargetUserId]
      OR changedUser.[Id] = sessionRow.[ActorUserId]
    WHERE (changedUser.[State] <> 'Active' OR changedUser.[IsDeleted] <> 0)
      AND sessionRow.[EndedAt] IS NULL;
END
GO

COMMIT TRANSACTION;
GO
