SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

IF SCHEMA_ID(N'identity') IS NULL EXEC(N'CREATE SCHEMA [identity]');
IF SCHEMA_ID(N'platform') IS NULL EXEC(N'CREATE SCHEMA [platform]');
IF SCHEMA_ID(N'security') IS NULL EXEC(N'CREATE SCHEMA [security]');
IF SCHEMA_ID(N'operations') IS NULL EXEC(N'CREATE SCHEMA [operations]');
IF SCHEMA_ID(N'notifications') IS NULL EXEC(N'CREATE SCHEMA [notifications]');
GO

IF OBJECT_ID(N'[identity].[User]', N'U') IS NULL
BEGIN
    CREATE TABLE [identity].[User]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_User_Id] DEFAULT NEWSEQUENTIALID(),
        [Email] nvarchar(320) NOT NULL,
        [NormalizedEmail] nvarchar(320) NOT NULL,
        [PasswordHash] nvarchar(1024) NOT NULL,
        [SecurityStamp] nvarchar(128) NOT NULL,
        [State] varchar(32) NOT NULL,
        [EmailConfirmed] bit NOT NULL CONSTRAINT [DF_User_EmailConfirmed] DEFAULT 0,
        [VerifiedAt] datetime2(7) NULL,
        [IsDeleted] bit NOT NULL CONSTRAINT [DF_User_IsDeleted] DEFAULT 0,
        [DeletedAt] datetime2(7) NULL,
        [DeletedByUserId] uniqueidentifier NULL,
        [DisplayName] nvarchar(100) NOT NULL,
        [TimeZoneId] nvarchar(128) NOT NULL,
        [Locale] varchar(8) NOT NULL CONSTRAINT [DF_User_Locale] DEFAULT 'vi',
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_User_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_User_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [CK_User_State] CHECK ([State] IN ('PendingVerification','Active','Disabled','Deleted')),
        CONSTRAINT [CK_User_Locale] CHECK ([Locale] IN ('vi','en')),
        CONSTRAINT [CK_User_DeletedConsistency] CHECK (([State] = 'Deleted' AND [IsDeleted] = 1 AND [DeletedAt] IS NOT NULL) OR ([State] <> 'Deleted' AND [IsDeleted] = 0)),
        CONSTRAINT [CK_User_VerifiedConsistency] CHECK (([EmailConfirmed] = 1 AND [VerifiedAt] IS NOT NULL) OR ([EmailConfirmed] = 0 AND [VerifiedAt] IS NULL))
    );

    CREATE UNIQUE INDEX [UX_User_NormalizedEmail_AllStates] ON [identity].[User]([NormalizedEmail]);
    CREATE INDEX [IX_User_State_Id] ON [identity].[User]([State], [Id]);
END
GO

IF OBJECT_ID(N'[identity].[Role]', N'U') IS NULL
BEGIN
    CREATE TABLE [identity].[Role]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_Role_Id] DEFAULT NEWSEQUENTIALID(),
        [Code] varchar(32) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_Role_CreatedAt] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_Role] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UX_Role_Code] UNIQUE ([Code]),
        CONSTRAINT [CK_Role_Code] CHECK ([Code] IN ('User','Admin','SuperAdmin'))
    );

    INSERT INTO [identity].[Role] ([Code], [Name])
    VALUES ('User', N'User'), ('Admin', N'Admin'), ('SuperAdmin', N'SuperAdmin');
END
GO

IF OBJECT_ID(N'[identity].[UserRole]', N'U') IS NULL
BEGIN
    CREATE TABLE [identity].[UserRole]
    (
        [UserId] uniqueidentifier NOT NULL,
        [RoleId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_UserRole_CreatedAt] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_UserRole] PRIMARY KEY CLUSTERED ([UserId], [RoleId]),
        CONSTRAINT [FK_UserRole_User] FOREIGN KEY ([UserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [FK_UserRole_Role] FOREIGN KEY ([RoleId]) REFERENCES [identity].[Role]([Id])
    );
END
GO

IF OBJECT_ID(N'[platform].[PersonalSpace]', N'U') IS NULL
BEGIN
    CREATE TABLE [platform].[PersonalSpace]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_PersonalSpace_Id] DEFAULT NEWSEQUENTIALID(),
        [UserId] uniqueidentifier NOT NULL,
        [State] varchar(32) NOT NULL CONSTRAINT [DF_PersonalSpace_State] DEFAULT 'Active',
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_PersonalSpace_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_PersonalSpace_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_PersonalSpace] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UX_PersonalSpace_UserId] UNIQUE ([UserId]),
        CONSTRAINT [FK_PersonalSpace_User] FOREIGN KEY ([UserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [CK_PersonalSpace_State] CHECK ([State] IN ('Active','Suspended'))
    );
END
GO

IF OBJECT_ID(N'[platform].[SecurityInvariant]', N'U') IS NULL
BEGIN
    CREATE TABLE [platform].[SecurityInvariant]
    (
        [Id] tinyint NOT NULL CONSTRAINT [PK_SecurityInvariant] PRIMARY KEY CONSTRAINT [CK_SecurityInvariant_Singleton] CHECK ([Id] = 1),
        [LastActiveSuperAdminGuard] bit NOT NULL CONSTRAINT [DF_SecurityInvariant_Guard] DEFAULT 1,
        [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_SecurityInvariant_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        [RowVersion] rowversion NOT NULL
    );

    INSERT INTO [platform].[SecurityInvariant] ([Id]) VALUES (1);
END
GO

IF OBJECT_ID(N'[identity].[Session]', N'U') IS NULL
BEGIN
    CREATE TABLE [identity].[Session]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_Session_Id] DEFAULT NEWSEQUENTIALID(),
        [UserId] uniqueidentifier NOT NULL,
        [HandleHash] binary(32) NOT NULL,
        [DeviceLabel] nvarchar(160) NOT NULL,
        [SecurityStamp] nvarchar(128) NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_Session_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [LastSeenAt] datetime2(7) NOT NULL CONSTRAINT [DF_Session_LastSeenAt] DEFAULT SYSUTCDATETIME(),
        [IdleExpiresAt] datetime2(7) NOT NULL,
        [AbsoluteExpiresAt] datetime2(7) NOT NULL,
        [RecentAuthenticatedAt] datetime2(7) NULL,
        [RevokedAt] datetime2(7) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Session] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_Session_User] FOREIGN KEY ([UserId]) REFERENCES [identity].[User]([Id])
    );

    CREATE UNIQUE INDEX [UX_Session_HandleHash] ON [identity].[Session]([HandleHash]);
    CREATE INDEX [IX_Session_User_Active] ON [identity].[Session]([UserId], [RevokedAt], [CreatedAt] DESC);
END
GO

IF OBJECT_ID(N'[identity].[MfaCredential]', N'U') IS NULL
BEGIN
    CREATE TABLE [identity].[MfaCredential]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_MfaCredential_Id] DEFAULT NEWSEQUENTIALID(),
        [UserId] uniqueidentifier NOT NULL,
        [Method] varchar(32) NOT NULL,
        [State] varchar(32) NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_MfaCredential_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_MfaCredential] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_MfaCredential_User] FOREIGN KEY ([UserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [CK_MfaCredential_Method] CHECK ([Method] IN ('Totp')),
        CONSTRAINT [CK_MfaCredential_State] CHECK ([State] IN ('Enabled','Disabled'))
    );

    CREATE INDEX [IX_MfaCredential_User_State] ON [identity].[MfaCredential]([UserId], [State]);
END
GO

IF OBJECT_ID(N'[identity].[OneTimeToken]', N'U') IS NULL
BEGIN
    CREATE TABLE [identity].[OneTimeToken]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_OneTimeToken_Id] DEFAULT NEWSEQUENTIALID(),
        [UserId] uniqueidentifier NOT NULL,
        [Purpose] varchar(32) NOT NULL,
        [TokenHash] binary(32) NOT NULL,
        [EmailSnapshot] nvarchar(320) NOT NULL,
        [ExpiresAt] datetime2(7) NOT NULL,
        [ConsumedAt] datetime2(7) NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_OneTimeToken_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_OneTimeToken] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_OneTimeToken_User] FOREIGN KEY ([UserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [CK_OneTimeToken_Purpose] CHECK ([Purpose] IN ('EmailVerification','PasswordReset'))
    );

    CREATE UNIQUE INDEX [UX_OneTimeToken_TokenHash] ON [identity].[OneTimeToken]([TokenHash]);
    CREATE INDEX [IX_OneTimeToken_User_Purpose_Expires] ON [identity].[OneTimeToken]([UserId], [Purpose], [ExpiresAt]);
END
GO

IF OBJECT_ID(N'[identity].[RequestReceipt]', N'U') IS NULL
BEGIN
    CREATE TABLE [identity].[RequestReceipt]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_RequestReceipt_Id] DEFAULT NEWSEQUENTIALID(),
        [SubjectHash] binary(32) NOT NULL,
        [OperationKey] nvarchar(150) NOT NULL,
        [KeyHash] binary(32) NOT NULL,
        [RequestDigest] binary(32) NOT NULL,
        [State] varchar(16) NOT NULL,
        [ResultCode] nvarchar(64) NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_RequestReceipt_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [ExpiresAt] datetime2(7) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_RequestReceipt] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UX_RequestReceipt_Key] UNIQUE ([SubjectHash], [OperationKey], [KeyHash]),
        CONSTRAINT [CK_RequestReceipt_State] CHECK ([State] IN ('Running','Succeeded','Failed'))
    );

    CREATE INDEX [IX_RequestReceipt_ExpiresAt] ON [identity].[RequestReceipt]([ExpiresAt]);
END
GO

IF OBJECT_ID(N'[identity].[AccountMessageIntent]', N'U') IS NULL
BEGIN
    CREATE TABLE [identity].[AccountMessageIntent]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_AccountMessageIntent_Id] DEFAULT NEWSEQUENTIALID(),
        [UserId] uniqueidentifier NULL,
        [NormalizedEmailHash] binary(32) NOT NULL,
        [Purpose] varchar(64) NOT NULL,
        [State] varchar(32) NOT NULL CONSTRAINT [DF_AccountMessageIntent_State] DEFAULT 'Pending',
        [NotBeforeAt] datetime2(7) NOT NULL CONSTRAINT [DF_AccountMessageIntent_NotBeforeAt] DEFAULT SYSUTCDATETIME(),
        [Attempts] int NOT NULL CONSTRAINT [DF_AccountMessageIntent_Attempts] DEFAULT 0,
        [LastErrorCode] nvarchar(64) NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_AccountMessageIntent_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_AccountMessageIntent_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_AccountMessageIntent] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_AccountMessageIntent_User] FOREIGN KEY ([UserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [CK_AccountMessageIntent_State] CHECK ([State] IN ('Pending','RetryScheduled','Delivered','NotApplicable','Failed'))
    );

    CREATE INDEX [IX_AccountMessageIntent_State_NotBefore] ON [identity].[AccountMessageIntent]([State], [NotBeforeAt]);
END
GO

IF OBJECT_ID(N'[platform].[Module]', N'U') IS NULL
BEGIN
    CREATE TABLE [platform].[Module]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_Module_Id] DEFAULT NEWSEQUENTIALID(),
        [Code] varchar(64) NOT NULL,
        [Name] nvarchar(160) NOT NULL,
        [State] varchar(32) NOT NULL,
        [SystemEnabled] bit NOT NULL CONSTRAINT [DF_Module_SystemEnabled] DEFAULT 1,
        [RegistrationEnabled] bit NOT NULL CONSTRAINT [DF_Module_RegistrationEnabled] DEFAULT 1,
        [PolicyRevision] bigint NOT NULL CONSTRAINT [DF_Module_PolicyRevision] DEFAULT 1,
        [SharingEpoch] bigint NOT NULL CONSTRAINT [DF_Module_SharingEpoch] DEFAULT 1,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_Module_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_Module_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Module] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UX_Module_Code] UNIQUE ([Code]),
        CONSTRAINT [CK_Module_State] CHECK ([State] IN ('Ready','Disabled','Paused','Uninstalled','MigrationFailed','Blocked')),
        CONSTRAINT [CK_Module_Revisions] CHECK ([PolicyRevision] > 0 AND [SharingEpoch] > 0)
    );
END
GO

IF OBJECT_ID(N'[platform].[ModuleDependency]', N'U') IS NULL
BEGIN
    CREATE TABLE [platform].[ModuleDependency]
    (
        [ModuleId] uniqueidentifier NOT NULL,
        [DependsOnModuleId] uniqueidentifier NOT NULL,
        [DependencyKind] varchar(16) NOT NULL,
        CONSTRAINT [PK_ModuleDependency] PRIMARY KEY CLUSTERED ([ModuleId], [DependsOnModuleId]),
        CONSTRAINT [FK_ModuleDependency_Module] FOREIGN KEY ([ModuleId]) REFERENCES [platform].[Module]([Id]),
        CONSTRAINT [FK_ModuleDependency_DependsOn] FOREIGN KEY ([DependsOnModuleId]) REFERENCES [platform].[Module]([Id]),
        CONSTRAINT [CK_ModuleDependency_Kind] CHECK ([DependencyKind] IN ('Hard','Soft'))
    );
END
GO

IF OBJECT_ID(N'[platform].[Permission]', N'U') IS NULL
BEGIN
    CREATE TABLE [platform].[Permission]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_Permission_Id] DEFAULT NEWSEQUENTIALID(),
        [ActionKey] nvarchar(160) NOT NULL,
        [EffectiveStatus] varchar(64) NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_Permission_CreatedAt] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_Permission] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UX_Permission_ActionKey] UNIQUE ([ActionKey])
    );
END
GO

IF OBJECT_ID(N'[platform].[AdminPermission]', N'U') IS NULL
BEGIN
    CREATE TABLE [platform].[AdminPermission]
    (
        [UserId] uniqueidentifier NOT NULL,
        [PermissionId] uniqueidentifier NOT NULL,
        [Effect] varchar(16) NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_AdminPermission_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_AdminPermission_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_AdminPermission] PRIMARY KEY CLUSTERED ([UserId], [PermissionId]),
        CONSTRAINT [FK_AdminPermission_User] FOREIGN KEY ([UserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [FK_AdminPermission_Permission] FOREIGN KEY ([PermissionId]) REFERENCES [platform].[Permission]([Id]),
        CONSTRAINT [CK_AdminPermission_Effect] CHECK ([Effect] IN ('Allow','Deny'))
    );
END
GO

IF OBJECT_ID(N'[platform].[UserModuleGrant]', N'U') IS NULL
BEGIN
    CREATE TABLE [platform].[UserModuleGrant]
    (
        [UserId] uniqueidentifier NOT NULL,
        [ModuleId] uniqueidentifier NOT NULL,
        [Enabled] bit NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_UserModuleGrant_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_UserModuleGrant_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_UserModuleGrant] PRIMARY KEY CLUSTERED ([UserId], [ModuleId]),
        CONSTRAINT [FK_UserModuleGrant_User] FOREIGN KEY ([UserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [FK_UserModuleGrant_Module] FOREIGN KEY ([ModuleId]) REFERENCES [platform].[Module]([Id])
    );
END
GO

IF OBJECT_ID(N'[security].[AuditEvent]', N'U') IS NULL
BEGIN
    CREATE TABLE [security].[AuditEvent]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_AuditEvent_Id] DEFAULT NEWSEQUENTIALID(),
        [ActorUserId] uniqueidentifier NULL,
        [OwnerUserId] uniqueidentifier NULL,
        [ActionKey] nvarchar(160) NOT NULL,
        [TargetType] nvarchar(100) NOT NULL,
        [TargetId] uniqueidentifier NULL,
        [Result] varchar(32) NOT NULL,
        [RedactedDiffJson] nvarchar(max) NULL,
        [TraceId] nvarchar(128) NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_AuditEvent_CreatedAt] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_AuditEvent] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_AuditEvent_Actor] FOREIGN KEY ([ActorUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [CK_AuditEvent_Result] CHECK ([Result] IN ('Succeeded','Denied','Failed'))
    );

    CREATE INDEX [IX_AuditEvent_Owner_CreatedAt] ON [security].[AuditEvent]([OwnerUserId], [CreatedAt] DESC);
END
GO

IF OBJECT_ID(N'[operations].[Outbox]', N'U') IS NULL
BEGIN
    CREATE TABLE [operations].[Outbox]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_Outbox_Id] DEFAULT NEWSEQUENTIALID(),
        [OwnerUserId] uniqueidentifier NULL,
        [LogicalKey] nvarchar(200) NOT NULL,
        [Kind] nvarchar(100) NOT NULL,
        [PayloadJson] nvarchar(max) NOT NULL,
        [State] varchar(32) NOT NULL CONSTRAINT [DF_Outbox_State] DEFAULT 'Pending',
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_Outbox_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [NotBeforeAt] datetime2(7) NOT NULL CONSTRAINT [DF_Outbox_NotBeforeAt] DEFAULT SYSUTCDATETIME(),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Outbox] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UX_Outbox_LogicalKey] UNIQUE ([LogicalKey]),
        CONSTRAINT [CK_Outbox_State] CHECK ([State] IN ('Pending','Leased','Completed','RetryScheduled','Failed'))
    );

    CREATE INDEX [IX_Outbox_State_NotBefore] ON [operations].[Outbox]([State], [NotBeforeAt]);
END
GO

IF OBJECT_ID(N'[notifications].[Notification]', N'U') IS NULL
BEGIN
    CREATE TABLE [notifications].[Notification]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_Notification_Id] DEFAULT NEWSEQUENTIALID(),
        [OwnerUserId] uniqueidentifier NOT NULL,
        [LogicalKey] nvarchar(200) NOT NULL,
        [Kind] nvarchar(100) NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Body] nvarchar(1000) NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_Notification_CreatedAt] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_Notification] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UX_Notification_LogicalKey] UNIQUE ([LogicalKey]),
        CONSTRAINT [FK_Notification_User] FOREIGN KEY ([OwnerUserId]) REFERENCES [identity].[User]([Id])
    );
END
GO

IF OBJECT_ID(N'[notifications].[Delivery]', N'U') IS NULL
BEGIN
    CREATE TABLE [notifications].[Delivery]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_Delivery_Id] DEFAULT NEWSEQUENTIALID(),
        [NotificationId] uniqueidentifier NOT NULL,
        [Channel] varchar(32) NOT NULL,
        [State] varchar(32) NOT NULL,
        [Attempts] int NOT NULL CONSTRAINT [DF_Delivery_Attempts] DEFAULT 0,
        [LastErrorCode] nvarchar(64) NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_Delivery_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_Delivery_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Delivery] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_Delivery_Notification] FOREIGN KEY ([NotificationId]) REFERENCES [notifications].[Notification]([Id]),
        CONSTRAINT [UX_Delivery_Notification_Channel] UNIQUE ([NotificationId], [Channel]),
        CONSTRAINT [CK_Delivery_Channel] CHECK ([Channel] IN ('InApp','Email','BrowserPush')),
        CONSTRAINT [CK_Delivery_State] CHECK ([State] IN ('Pending','RetryScheduled','Delivered','NotApplicable','PermissionUnavailable','Failed'))
    );
END
GO

COMMIT TRANSACTION;
GO
