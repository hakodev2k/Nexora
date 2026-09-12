SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

/* Additive notification inbox fields. Existing security notifications remain
   visible and receive a rowversion so mark-read uses the same If-Match rule as
   every other mutable projection. */
IF OBJECT_ID(N'[notifications].[Notification]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'notifications.Notification', N'SourceRef') IS NULL
        ALTER TABLE [notifications].[Notification] ADD [SourceRef] nvarchar(200) NULL;
    IF COL_LENGTH(N'notifications.Notification', N'ReadAt') IS NULL
        ALTER TABLE [notifications].[Notification] ADD [ReadAt] datetime2(7) NULL;
    IF COL_LENGTH(N'notifications.Notification', N'DeletedAt') IS NULL
        ALTER TABLE [notifications].[Notification] ADD [DeletedAt] datetime2(7) NULL;
    IF COL_LENGTH(N'notifications.Notification', N'RowVersion') IS NULL
        ALTER TABLE [notifications].[Notification] ADD [RowVersion] rowversion NOT NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Notification_Owner_Created' AND [object_id] = OBJECT_ID(N'[notifications].[Notification]'))
        CREATE INDEX [IX_Notification_Owner_Created] ON [notifications].[Notification]([OwnerUserId], [DeletedAt], [CreatedAt] DESC);
END
GO

/* The channel contract schedules InApp, Email and BrowserPush together. Push
   starts as PermissionUnavailable until a local subscription/provider is
   explicitly configured; no external call is made by this migration. */
IF OBJECT_ID(N'[notifications].[Delivery]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[notifications].[Notification]', N'U') IS NOT NULL
BEGIN
    INSERT INTO [notifications].[Delivery] ([NotificationId], [Channel], [State], [LastErrorCode])
    SELECT n.[Id], 'Email', 'Pending', NULL
    FROM [notifications].[Notification] n
    WHERE NOT EXISTS (SELECT 1 FROM [notifications].[Delivery] d WHERE d.[NotificationId] = n.[Id] AND d.[Channel] = 'Email');

    INSERT INTO [notifications].[Delivery] ([NotificationId], [Channel], [State], [LastErrorCode])
    SELECT n.[Id], 'BrowserPush', 'PermissionUnavailable', N'PushPermissionUnavailable'
    FROM [notifications].[Notification] n
    WHERE NOT EXISTS (SELECT 1 FROM [notifications].[Delivery] d WHERE d.[NotificationId] = n.[Id] AND d.[Channel] = 'BrowserPush');
END
GO

COMMIT TRANSACTION;
GO
