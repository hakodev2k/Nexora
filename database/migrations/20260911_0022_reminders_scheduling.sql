/*
   FX14 local reminder scheduling. A reminder is an owner-scoped configuration
   over one Task or one manually managed Calendar Event. It never contains a
   provider credential or a provider payload: delivery is projected locally
   through the existing notification/outbox tables.
*/
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

IF OBJECT_ID(N'[calendar].[Reminder]', N'U') IS NULL
BEGIN
    CREATE TABLE [calendar].[Reminder]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_Reminder_Id] DEFAULT NEWSEQUENTIALID(),
        [OwnerId] uniqueidentifier NOT NULL,
        [SourceType] varchar(32) NOT NULL,
        [SourceId] uniqueidentifier NOT NULL,
        [ConfigType] varchar(32) NOT NULL,
        [ExactAt] datetime2(7) NULL,
        [TimeZoneId] nvarchar(128) NOT NULL,
        [DueAt] datetime2(7) NULL,
        [SourceRevision] bigint NOT NULL,
        [State] varchar(32) NOT NULL,
        [LastNotificationId] uniqueidentifier NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_Reminder_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_Reminder_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Reminder] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_Reminder_Owner] FOREIGN KEY ([OwnerId]) REFERENCES [platform].[PersonalSpace]([Id]),
        CONSTRAINT [FK_Reminder_LastNotification] FOREIGN KEY ([LastNotificationId]) REFERENCES [notifications].[Notification]([Id]),
        CONSTRAINT [CK_Reminder_SourceType] CHECK ([SourceType] IN ('Task', 'CalendarEvent')),
        CONSTRAINT [CK_Reminder_ConfigType] CHECK ([ConfigType] IN ('None', 'BeforeStart15m', 'Exact')),
        CONSTRAINT [CK_Reminder_State] CHECK ([State] IN ('None', 'Pending', 'Dispatched', 'Canceled', 'Expired', 'Missed')),
        CONSTRAINT [CK_Reminder_Configuration] CHECK
        (
            ([ConfigType] = 'None' AND [ExactAt] IS NULL AND [DueAt] IS NULL AND [State] = 'None')
            OR ([ConfigType] = 'BeforeStart15m' AND [ExactAt] IS NULL AND [DueAt] IS NOT NULL)
            OR ([ConfigType] = 'Exact' AND [ExactAt] IS NOT NULL AND [DueAt] = [ExactAt])
        ),
        CONSTRAINT [UX_Reminder_Owner_Source] UNIQUE ([OwnerId], [SourceType], [SourceId])
    );

    CREATE INDEX [IX_Reminder_State_DueAt] ON [calendar].[Reminder]([State], [DueAt], [Id])
        WHERE [State] = 'Pending' AND [DueAt] IS NOT NULL;
    CREATE INDEX [IX_Reminder_Owner_UpdatedAt] ON [calendar].[Reminder]([OwnerId], [UpdatedAt] DESC, [Id] DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM [platform].[Permission] WHERE [ActionKey] = N'reminders.configuration.read')
    INSERT INTO [platform].[Permission] ([ActionKey], [EffectiveStatus]) VALUES (N'reminders.configuration.read', 'Resolved');
IF NOT EXISTS (SELECT 1 FROM [platform].[Permission] WHERE [ActionKey] = N'reminders.configuration.set')
    INSERT INTO [platform].[Permission] ([ActionKey], [EffectiveStatus]) VALUES (N'reminders.configuration.set', 'Resolved');
IF NOT EXISTS (SELECT 1 FROM [platform].[Permission] WHERE [ActionKey] = N'reminders.configuration.remove')
    INSERT INTO [platform].[Permission] ([ActionKey], [EffectiveStatus]) VALUES (N'reminders.configuration.remove', 'Resolved');
IF NOT EXISTS (SELECT 1 FROM [platform].[Permission] WHERE [ActionKey] = N'reminders.schedule.reconcile')
    INSERT INTO [platform].[Permission] ([ActionKey], [EffectiveStatus]) VALUES (N'reminders.schedule.reconcile', 'Resolved');
IF NOT EXISTS (SELECT 1 FROM [platform].[Permission] WHERE [ActionKey] = N'reminders.schedule.invalidate')
    INSERT INTO [platform].[Permission] ([ActionKey], [EffectiveStatus]) VALUES (N'reminders.schedule.invalidate', 'Resolved');
IF NOT EXISTS (SELECT 1 FROM [platform].[Permission] WHERE [ActionKey] = N'reminders.schedule.dispatch')
    INSERT INTO [platform].[Permission] ([ActionKey], [EffectiveStatus]) VALUES (N'reminders.schedule.dispatch', 'Resolved');
UPDATE [platform].[Permission]
SET [EffectiveStatus] = 'Resolved'
WHERE [ActionKey] IN
(
    N'reminders.configuration.read', N'reminders.configuration.set', N'reminders.configuration.remove',
    N'reminders.schedule.reconcile', N'reminders.schedule.invalidate', N'reminders.schedule.dispatch'
) AND [EffectiveStatus] <> 'Resolved';
GO

UPDATE [platform].[Module]
SET [State] = 'Ready', [SystemEnabled] = 1, [RegistrationEnabled] = 1,
    [PolicyRevision] = [PolicyRevision] + 1, [UpdatedAt] = SYSUTCDATETIME()
WHERE [Code] = 'FX14' AND ([State] <> 'Ready' OR [SystemEnabled] = 0 OR [RegistrationEnabled] = 0);

UPDATE grantRow
SET [Enabled] = 1, [UpdatedAt] = SYSUTCDATETIME()
FROM [platform].[UserModuleGrant] grantRow
INNER JOIN [platform].[Module] moduleRow ON moduleRow.[Id] = grantRow.[ModuleId]
INNER JOIN [identity].[User] userRow ON userRow.[Id] = grantRow.[UserId]
WHERE moduleRow.[Code] = 'FX14' AND userRow.[State] = 'Active' AND grantRow.[Enabled] = 0;
GO

/* Preserve the pre-FX14 Task reminder timestamp as a local Exact configuration. */
INSERT INTO [calendar].[Reminder]
    ([OwnerId], [SourceType], [SourceId], [ConfigType], [ExactAt], [TimeZoneId], [DueAt], [SourceRevision], [State])
SELECT taskRow.[OwnerId], 'Task', taskRow.[Id], 'Exact', taskRow.[ReminderAt], userRow.[TimeZoneId],
       taskRow.[ReminderAt], CONVERT(bigint, taskRow.[RowVersion]),
       CASE
           WHEN taskRow.[Status] IN ('Completed', 'Skipped', 'Deleted')
             OR projectRow.[Status] IN ('Completed', 'Skipped', 'Deleted') THEN 'Canceled'
           WHEN taskRow.[ReminderAt] <= SYSUTCDATETIME() THEN 'Expired'
           ELSE 'Pending'
       END
FROM [productivity].[Task] taskRow
INNER JOIN [productivity].[Project] projectRow ON projectRow.[Id] = taskRow.[ProjectId] AND projectRow.[OwnerId] = taskRow.[OwnerId]
INNER JOIN [platform].[PersonalSpace] spaceRow ON spaceRow.[Id] = taskRow.[OwnerId]
INNER JOIN [identity].[User] userRow ON userRow.[Id] = spaceRow.[UserId]
WHERE taskRow.[ReminderAt] IS NOT NULL
  AND NOT EXISTS
  (
      SELECT 1 FROM [calendar].[Reminder] reminderRow
      WHERE reminderRow.[OwnerId] = taskRow.[OwnerId]
        AND reminderRow.[SourceType] = 'Task'
        AND reminderRow.[SourceId] = taskRow.[Id]
  );
GO

/*
   Source writes immediately reconcile or invalidate the single configuration.
   This keeps a worker from dispatching a stale source revision and prevents a
   terminal Task, Project or Event from leaving an active intent behind.
*/
CREATE OR ALTER TRIGGER [productivity].[TR_Task_ReconcileReminder]
ON [productivity].[Task]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE reminderRow
    SET [SourceRevision] = CONVERT(bigint, taskRow.[RowVersion]),
        [DueAt] = due.[Value],
        [State] = CASE
            WHEN reminderRow.[ConfigType] = 'None' THEN 'None'
            WHEN taskRow.[Status] IN ('Completed', 'Skipped', 'Deleted')
              OR projectRow.[Status] IN ('Completed', 'Skipped', 'Deleted') THEN 'Canceled'
            WHEN due.[Value] <= SYSUTCDATETIME() THEN 'Expired'
            ELSE 'Pending'
        END,
        [LastNotificationId] = CASE
            WHEN taskRow.[Status] NOT IN ('Completed', 'Skipped', 'Deleted')
             AND projectRow.[Status] NOT IN ('Completed', 'Skipped', 'Deleted')
             AND reminderRow.[ConfigType] <> 'None'
             AND due.[Value] > SYSUTCDATETIME() THEN NULL
            ELSE reminderRow.[LastNotificationId]
        END,
        [UpdatedAt] = SYSUTCDATETIME()
    FROM [calendar].[Reminder] reminderRow
    INNER JOIN inserted taskRow ON taskRow.[Id] = reminderRow.[SourceId] AND taskRow.[OwnerId] = reminderRow.[OwnerId]
    INNER JOIN [productivity].[Project] projectRow ON projectRow.[Id] = taskRow.[ProjectId] AND projectRow.[OwnerId] = taskRow.[OwnerId]
    CROSS APPLY (VALUES (CASE WHEN reminderRow.[ConfigType] = 'BeforeStart15m' THEN DATEADD(minute, -15, taskRow.[StartAt]) ELSE reminderRow.[ExactAt] END)) due([Value])
    WHERE reminderRow.[SourceType] = 'Task';
END
GO

CREATE OR ALTER TRIGGER [productivity].[TR_Project_CancelTaskReminders]
ON [productivity].[Project]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE reminderRow
    SET [State] = 'Canceled', [UpdatedAt] = SYSUTCDATETIME()
    FROM [calendar].[Reminder] reminderRow
    INNER JOIN [productivity].[Task] taskRow ON taskRow.[Id] = reminderRow.[SourceId] AND taskRow.[OwnerId] = reminderRow.[OwnerId]
    INNER JOIN inserted projectRow ON projectRow.[Id] = taskRow.[ProjectId] AND projectRow.[OwnerId] = taskRow.[OwnerId]
    WHERE reminderRow.[SourceType] = 'Task'
      AND reminderRow.[State] = 'Pending'
      AND projectRow.[Status] IN ('Completed', 'Skipped', 'Deleted');
END
GO

CREATE OR ALTER TRIGGER [calendar].[TR_Event_ReconcileReminder]
ON [calendar].[Event]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE reminderRow
    SET [SourceRevision] = CONVERT(bigint, eventRow.[RowVersion]),
        [DueAt] = due.[Value],
        [State] = CASE
            WHEN reminderRow.[ConfigType] = 'None' THEN 'None'
            WHEN eventRow.[Status] IN ('Completed', 'Canceled', 'Deleted') THEN 'Canceled'
            WHEN due.[Value] <= SYSUTCDATETIME() THEN 'Expired'
            ELSE 'Pending'
        END,
        [LastNotificationId] = CASE
            WHEN eventRow.[Status] NOT IN ('Completed', 'Canceled', 'Deleted')
             AND reminderRow.[ConfigType] <> 'None'
             AND due.[Value] > SYSUTCDATETIME() THEN NULL
            ELSE reminderRow.[LastNotificationId]
        END,
        [UpdatedAt] = SYSUTCDATETIME()
    FROM [calendar].[Reminder] reminderRow
    INNER JOIN inserted eventRow ON eventRow.[Id] = reminderRow.[SourceId] AND eventRow.[OwnerId] = reminderRow.[OwnerId]
    CROSS APPLY (VALUES (CASE WHEN reminderRow.[ConfigType] = 'BeforeStart15m' THEN DATEADD(minute, -15, eventRow.[StartAt]) ELSE reminderRow.[ExactAt] END)) due([Value])
    WHERE reminderRow.[SourceType] = 'CalendarEvent';
END
GO

COMMIT TRANSACTION;
GO
