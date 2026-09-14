SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

IF SCHEMA_ID(N'productivity') IS NULL EXEC(N'CREATE SCHEMA [productivity]');
IF SCHEMA_ID(N'calendar') IS NULL EXEC(N'CREATE SCHEMA [calendar]');
GO

/*
   Reference catalog only. These rows describe installed Release 1 packages; they
   are not demo/business data. FX30/FX34/FX35 remain disabled and paused under
   DEC-20260909-014 because only their local-safe code is approved.
*/
IF OBJECT_ID(N'[platform].[Module]', N'U') IS NOT NULL
BEGIN
    MERGE [platform].[Module] AS target
    USING (VALUES
        (CONVERT(uniqueidentifier, '00000001-0000-0000-0000-000000000001'), 'FX01', N'Identity and Profile', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000002-0000-0000-0000-000000000002'), 'FX02', N'Users, Roles and Permissions', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000003-0000-0000-0000-000000000003'), 'FX03', N'Module Platform', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000004-0000-0000-0000-000000000004'), 'FX04', N'Read-only Sharing', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000005-0000-0000-0000-000000000005'), 'FX05', N'Support and Emergency Access', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000006-0000-0000-0000-000000000006'), 'FX06', N'Notifications', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000007-0000-0000-0000-000000000007'), 'FX07', N'Files and Attachments', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000008-0000-0000-0000-000000000008'), 'FX08', N'Trash, Activity and Audit', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000009-0000-0000-0000-000000000009'), 'FX09', N'Settings and Application Shell', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000010-0000-0000-0000-000000000010'), 'FX10', N'Import, Export and Backup', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000011-0000-0000-0000-000000000011'), 'FX11', N'Projects', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000012-0000-0000-0000-000000000012'), 'FX12', N'Tasks', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000013-0000-0000-0000-000000000013'), 'FX13', N'Calendar and Events', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000014-0000-0000-0000-000000000014'), 'FX14', N'Reminders', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000015-0000-0000-0000-000000000015'), 'FX15', N'Planner', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000016-0000-0000-0000-000000000016'), 'FX16', N'Goals and Targets', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000017-0000-0000-0000-000000000017'), 'FX17', N'Habits', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000018-0000-0000-0000-000000000018'), 'FX18', N'Time Tracking', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000019-0000-0000-0000-000000000019'), 'FX19', N'Pomodoro and Focus', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000020-0000-0000-0000-000000000020'), 'FX20', N'Documents, Notes and Knowledge', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000021-0000-0000-0000-000000000021'), 'FX21', N'Bookmarks', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000022-0000-0000-0000-000000000022'), 'FX22', N'Code Snippets', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000023-0000-0000-0000-000000000023'), 'FX23', N'Read Later', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000024-0000-0000-0000-000000000024'), 'FX24', N'Tags, Collections and Templates', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000025-0000-0000-0000-000000000025'), 'FX25', N'Search, Favorites and Command Palette', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000026-0000-0000-0000-000000000026'), 'FX26', N'Dashboard and Widgets', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000027-0000-0000-0000-000000000027'), 'FX27', N'Finance', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000028-0000-0000-0000-000000000028'), 'FX28', N'Vault', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000029-0000-0000-0000-000000000029'), 'FX29', N'News and Feeds', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000030-0000-0000-0000-000000000030'), 'FX30', N'Price Tracking', 'Paused', 0, 0),
        (CONVERT(uniqueidentifier, '00000031-0000-0000-0000-000000000031'), 'FX31', N'Shopping Records', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000032-0000-0000-0000-000000000032'), 'FX32', N'Developer Toolbox', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000033-0000-0000-0000-000000000033'), 'FX33', N'GitHub Discovery', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000034-0000-0000-0000-000000000034'), 'FX34', N'Automation and Workflows', 'Paused', 0, 0),
        (CONVERT(uniqueidentifier, '00000035-0000-0000-0000-000000000035'), 'FX35', N'Integrations and Webhooks', 'Paused', 0, 0),
        (CONVERT(uniqueidentifier, '00000036-0000-0000-0000-000000000036'), 'FX36', N'Monitoring and Job Operations', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000037-0000-0000-0000-000000000037'), 'FX37', N'Personal Assets', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000038-0000-0000-0000-000000000038'), 'FX38', N'Digital Assets', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000039-0000-0000-0000-000000000039'), 'FX39', N'Career and Resumes', 'Ready', 1, 1),
        (CONVERT(uniqueidentifier, '00000040-0000-0000-0000-000000000040'), 'FX40', N'Learning and Work Log', 'Ready', 1, 1)
    ) AS source ([Id], [Code], [Name], [State], [SystemEnabled], [RegistrationEnabled])
    ON target.[Code] = source.[Code]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT ([Id], [Code], [Name], [State], [SystemEnabled], [RegistrationEnabled])
        VALUES (source.[Id], source.[Code], source.[Name], source.[State], source.[SystemEnabled], source.[RegistrationEnabled]);
END
GO

/*
   Action registry used by the server-side grant editor. A permission row is
   metadata only: an explicit AdminPermission row is still required for an
   Admin account and the module/resource guard is evaluated on every request.
*/
IF OBJECT_ID(N'[platform].[Permission]', N'U') IS NOT NULL
BEGIN
    MERGE [platform].[Permission] AS target
    USING (VALUES
        (N'access.user.read', 'Resolved'),
        (N'access.user.disable', 'Resolved'),
        (N'access.role.set', 'Resolved'),
        (N'access.permission.read', 'Resolved'),
        (N'access.permission.set', 'Resolved'),
        (N'access.entitlement.set', 'Resolved'),
        (N'modules.catalog.read', 'Resolved'),
        (N'modules.policy.enable', 'Resolved'),
        (N'modules.policy.disable', 'Resolved'),
        (N'modules.policy.defaults', 'Resolved'),
        (N'notifications.dispatch.publish', 'Resolved'),
        (N'notifications.dispatch.deliver', 'Resolved'),
        (N'settings.preference.read', 'Resolved'),
        (N'settings.preference.update', 'Resolved'),
        (N'projects.view', 'Resolved'),
        (N'projects.create', 'Resolved'),
        (N'projects.update', 'Resolved'),
        (N'projects.delete', 'Resolved'),
        (N'projects.restore', 'Resolved'),
        (N'projects.purge', 'Resolved'),
        (N'tasks.view', 'Resolved'),
        (N'tasks.create', 'Resolved'),
        (N'tasks.update', 'Resolved'),
        (N'tasks.delete', 'Resolved'),
        (N'tasks.restore', 'Resolved'),
        (N'tasks.purge', 'Resolved'),
        (N'calendar.view', 'Resolved'),
        (N'calendar.create', 'Resolved'),
        (N'calendar.update', 'Resolved'),
        (N'calendar.cancel', 'Resolved'),
        (N'calendar.import', 'Resolved'),
        (N'calendar.export', 'Resolved'),
        (N'documents.library.read', 'Resolved'),
        (N'documents.page.read', 'Resolved'),
        (N'documents.page.create', 'Resolved'),
        (N'documents.page.save', 'Resolved'),
        (N'documents.page.publish', 'Resolved'),
        (N'documents.page.unpublish', 'Resolved'),
        (N'documents.page.archive', 'Resolved'),
        (N'documents.page.unarchive', 'Resolved'),
        (N'files.view', 'Resolved'),
        (N'files.create', 'Resolved'),
        (N'files.delete', 'Resolved'),
        (N'files.restore', 'Resolved'),
        (N'files.purge', 'Resolved'),
        (N'notifications.view', 'Resolved'),
        (N'notifications.update', 'Resolved'),
        (N'notifications.delete', 'Resolved'),
        (N'settings.view', 'Resolved'),
        (N'settings.update', 'Resolved'),
        (N'backup.create', 'Resolved'),
        (N'backup.restore', 'Resolved')
    ) AS source ([ActionKey], [EffectiveStatus])
    ON target.[ActionKey] = source.[ActionKey]
    WHEN MATCHED AND target.[EffectiveStatus] <> source.[EffectiveStatus] THEN
        UPDATE SET [EffectiveStatus] = source.[EffectiveStatus]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT ([ActionKey], [EffectiveStatus]) VALUES (source.[ActionKey], source.[EffectiveStatus]);
END
GO

/*
   The first local slice depends on the identity/session and module platform
   being present. These rows are reference metadata only; they do not grant
   access and are safe to re-run.
*/
IF OBJECT_ID(N'[platform].[ModuleDependency]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[platform].[Module]', N'U') IS NOT NULL
BEGIN
    INSERT INTO [platform].[ModuleDependency] ([ModuleId], [DependsOnModuleId], [DependencyKind])
    SELECT child.[Id], parent.[Id], 'Hard'
    FROM (VALUES
        ('FX02', 'FX01'),
        ('FX03', 'FX01'),
        ('FX04', 'FX01'),
        ('FX05', 'FX01'),
        ('FX06', 'FX01'),
        ('FX07', 'FX01'),
        ('FX08', 'FX01'),
        ('FX09', 'FX01'),
        ('FX10', 'FX01'),
        ('FX11', 'FX01'),
        ('FX12', 'FX11'),
        ('FX13', 'FX01'),
        ('FX14', 'FX13'),
        ('FX15', 'FX11'),
        ('FX16', 'FX11'),
        ('FX17', 'FX16'),
        ('FX18', 'FX11'),
        ('FX19', 'FX18'),
        ('FX20', 'FX01'),
        ('FX21', 'FX20'),
        ('FX22', 'FX20'),
        ('FX23', 'FX20'),
        ('FX24', 'FX20'),
        ('FX25', 'FX01'),
        ('FX26', 'FX01'),
        ('FX27', 'FX01'),
        ('FX28', 'FX01'),
        ('FX29', 'FX01'),
        ('FX30', 'FX01'),
        ('FX31', 'FX01'),
        ('FX32', 'FX01'),
        ('FX33', 'FX01'),
        ('FX34', 'FX01'),
        ('FX35', 'FX01'),
        ('FX36', 'FX01'),
        ('FX37', 'FX01'),
        ('FX38', 'FX01'),
        ('FX39', 'FX01'),
        ('FX40', 'FX01')
    ) AS dependency ([ChildCode], [ParentCode])
    INNER JOIN [platform].[Module] child ON child.[Code] = dependency.[ChildCode]
    INNER JOIN [platform].[Module] parent ON parent.[Code] = dependency.[ParentCode]
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM [platform].[ModuleDependency] existing
        WHERE existing.[ModuleId] = child.[Id]
          AND existing.[DependsOnModuleId] = parent.[Id]
    );
END
GO

IF OBJECT_ID(N'[productivity].[Project]', N'U') IS NULL
BEGIN
    CREATE TABLE [productivity].[Project]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_Project_Id] DEFAULT NEWSEQUENTIALID(),
        [OwnerId] uniqueidentifier NOT NULL,
        [Name] nvarchar(160) NOT NULL,
        [Description] nvarchar(2000) NULL,
        [Status] varchar(16) NOT NULL CONSTRAINT [DF_Project_Status] DEFAULT 'Active',
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_Project_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_Project_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        [DeletedAt] datetime2(7) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Project] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_Project_Owner] FOREIGN KEY ([OwnerId]) REFERENCES [platform].[PersonalSpace]([Id]),
        CONSTRAINT [CK_Project_Status] CHECK ([Status] IN ('Active','Completed','Archived','Deleted')),
        CONSTRAINT [CK_Project_Deleted] CHECK (([Status] = 'Deleted' AND [DeletedAt] IS NOT NULL) OR ([Status] <> 'Deleted' AND [DeletedAt] IS NULL))
    );
    CREATE INDEX [IX_Project_Owner_Status_Updated] ON [productivity].[Project]([OwnerId], [Status], [UpdatedAt] DESC);
END
GO

IF OBJECT_ID(N'[productivity].[Task]', N'U') IS NULL
BEGIN
    CREATE TABLE [productivity].[Task]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_Task_Id] DEFAULT NEWSEQUENTIALID(),
        [OwnerId] uniqueidentifier NOT NULL,
        [ProjectId] uniqueidentifier NOT NULL,
        [Title] nvarchar(240) NOT NULL,
        [Description] nvarchar(4000) NULL,
        [Status] varchar(16) NOT NULL CONSTRAINT [DF_Task_Status] DEFAULT 'NotStarted',
        [DueAt] datetime2(7) NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_Task_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_Task_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        [DeletedAt] datetime2(7) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Task] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_Task_Owner] FOREIGN KEY ([OwnerId]) REFERENCES [platform].[PersonalSpace]([Id]),
        CONSTRAINT [FK_Task_Project] FOREIGN KEY ([ProjectId]) REFERENCES [productivity].[Project]([Id]),
        CONSTRAINT [CK_Task_Status] CHECK ([Status] IN ('NotStarted','InProgress','Completed','Skipped','Deleted')),
        CONSTRAINT [CK_Task_Deleted] CHECK (([Status] = 'Deleted' AND [DeletedAt] IS NOT NULL) OR ([Status] <> 'Deleted' AND [DeletedAt] IS NULL))
    );
    CREATE INDEX [IX_Task_Owner_Project_Status_Due] ON [productivity].[Task]([OwnerId], [ProjectId], [Status], [DueAt]);
END
GO

IF OBJECT_ID(N'[calendar].[Event]', N'U') IS NULL
BEGIN
    CREATE TABLE [calendar].[Event]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_Event_Id] DEFAULT NEWSEQUENTIALID(),
        [OwnerId] uniqueidentifier NOT NULL,
        [Title] nvarchar(240) NOT NULL,
        [Description] nvarchar(4000) NULL,
        [StartAt] datetime2(7) NOT NULL,
        [EndAt] datetime2(7) NOT NULL,
        [TimeZoneId] nvarchar(128) NOT NULL,
        [Status] varchar(16) NOT NULL CONSTRAINT [DF_Event_Status] DEFAULT 'Scheduled',
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_Event_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_Event_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        [DeletedAt] datetime2(7) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Event] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_Event_Owner] FOREIGN KEY ([OwnerId]) REFERENCES [platform].[PersonalSpace]([Id]),
        CONSTRAINT [CK_Event_Time] CHECK ([EndAt] > [StartAt]),
        CONSTRAINT [CK_Event_Status] CHECK ([Status] IN ('Scheduled','Completed','Canceled','Deleted')),
        CONSTRAINT [CK_Event_Deleted] CHECK (([Status] = 'Deleted' AND [DeletedAt] IS NOT NULL) OR ([Status] <> 'Deleted' AND [DeletedAt] IS NULL))
    );
    CREATE INDEX [IX_Event_Owner_Start] ON [calendar].[Event]([OwnerId], [StartAt]);
END
GO

COMMIT TRANSACTION;
GO
