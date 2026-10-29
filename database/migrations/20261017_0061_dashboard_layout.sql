SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'discovery.Dashboard',N'U') IS NULL
BEGIN
    CREATE TABLE [discovery].[Dashboard] (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_Dashboard] PRIMARY KEY,
        [OwnerId] uniqueidentifier NOT NULL CONSTRAINT [FK_Dashboard_Owner] REFERENCES [platform].[PersonalSpace]([Id]),
        [Title] nvarchar(200) NOT NULL,
        [LayoutVersion] bigint NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(), [UpdatedAt] datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        [CreatedByUserId] uniqueidentifier NOT NULL REFERENCES [identity].[User]([Id]),
        [UpdatedByUserId] uniqueidentifier NOT NULL REFERENCES [identity].[User]([Id]),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [UQ_Dashboard_Owner] UNIQUE ([OwnerId]), CONSTRAINT [UQ_Dashboard_Owner_Id] UNIQUE ([OwnerId],[Id]),
        CONSTRAINT [CK_Dashboard_Version] CHECK ([LayoutVersion]>0)
    );
END;
IF OBJECT_ID(N'discovery.DashboardWidget',N'U') IS NULL
BEGIN
    CREATE TABLE [discovery].[DashboardWidget] (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_DashboardWidget] PRIMARY KEY,
        [OwnerId] uniqueidentifier NOT NULL, [DashboardId] uniqueidentifier NOT NULL,
        [WidgetKey] nvarchar(150) NOT NULL, [ProviderVersion] nvarchar(50) NOT NULL, [SchemaVersion] int NOT NULL,
        [WidthUnits] tinyint NOT NULL, [Enabled] bit NOT NULL, [Position] int NOT NULL, [OptionsJson] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(), [UpdatedAt] datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        [CreatedByUserId] uniqueidentifier NOT NULL REFERENCES [identity].[User]([Id]),
        [UpdatedByUserId] uniqueidentifier NOT NULL REFERENCES [identity].[User]([Id]), [RowVersion] rowversion NOT NULL,
        CONSTRAINT [FK_DashboardWidget_Root] FOREIGN KEY ([OwnerId],[DashboardId]) REFERENCES [discovery].[Dashboard]([OwnerId],[Id]),
        CONSTRAINT [UQ_DashboardWidget_Position] UNIQUE ([OwnerId],[DashboardId],[Position]),
        CONSTRAINT [UQ_DashboardWidget_Owner_Id] UNIQUE ([OwnerId],[Id]),
        CONSTRAINT [CK_DashboardWidget_Config] CHECK ([SchemaVersion]>0 AND [WidthUnits] BETWEEN 1 AND 2
            AND [Position] BETWEEN 0 AND 2047 AND ISJSON([OptionsJson])=1 AND DATALENGTH([OptionsJson])<=4096)
    );
END;
INSERT INTO [platform].[Permission] ([ActionKey],[EffectiveStatus])
SELECT [ActionKey],'Resolved' FROM (VALUES (N'dashboard.layout.update'),(N'dashboard.layout.add_widget'),
    (N'dashboard.layout.configure_widget'),(N'dashboard.layout.remove_widget'),(N'dashboard.layout.reorder_widget')) v([ActionKey])
WHERE NOT EXISTS (SELECT 1 FROM [platform].[Permission] p WITH(UPDLOCK,HOLDLOCK) WHERE p.[ActionKey]=v.[ActionKey]);
COMMIT TRANSACTION;
GO
