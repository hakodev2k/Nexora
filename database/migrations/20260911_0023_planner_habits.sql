/*
   FX15 Planner and FX17 Habits local slices. Both modules are personal-space
   data only. Planner keeps a Task lens without cloning or changing the Task;
   Habits store effective-dated schedules and owner-scoped local-day check-ins.
   Neither table contains provider credentials or performs provider execution.
*/
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

IF OBJECT_ID(N'[productivity].[PlannerPin]', N'U') IS NULL
BEGIN
    CREATE TABLE [productivity].[PlannerPin]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_PlannerPin_Id] DEFAULT NEWSEQUENTIALID(),
        [OwnerId] uniqueidentifier NOT NULL,
        [TaskId] uniqueidentifier NOT NULL,
        [PlanDate] date NOT NULL,
        [Rank] decimal(28,8) NOT NULL,
        [Notes] nvarchar(2000) NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_PlannerPin_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_PlannerPin_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_PlannerPin] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_PlannerPin_Owner] FOREIGN KEY ([OwnerId]) REFERENCES [platform].[PersonalSpace]([Id]),
        CONSTRAINT [FK_PlannerPin_Task] FOREIGN KEY ([TaskId]) REFERENCES [productivity].[Task]([Id]),
        CONSTRAINT [UX_PlannerPin_Owner_Task_Date] UNIQUE ([OwnerId], [TaskId], [PlanDate])
    );

    CREATE INDEX [IX_PlannerPin_Owner_Date_Rank] ON [productivity].[PlannerPin]([OwnerId], [PlanDate], [Rank], [Id]);
END
GO

IF OBJECT_ID(N'[productivity].[Habit]', N'U') IS NULL
BEGIN
    CREATE TABLE [productivity].[Habit]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_Habit_Id] DEFAULT NEWSEQUENTIALID(),
        [OwnerId] uniqueidentifier NOT NULL,
        [CreatedByUserId] uniqueidentifier NULL,
        [UpdatedByUserId] uniqueidentifier NULL,
        [Title] nvarchar(100) NOT NULL,
        [Kind] varchar(16) NOT NULL,
        [TargetCount] int NULL,
        [Unit] nvarchar(50) NULL,
        [State] varchar(16) NOT NULL CONSTRAINT [DF_Habit_State] DEFAULT 'Active',
        [TimeZoneId] nvarchar(128) NOT NULL,
        [ReminderLocalTime] time(0) NULL,
        [PreArchiveState] varchar(16) NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_Habit_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_Habit_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Habit] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_Habit_Owner] FOREIGN KEY ([OwnerId]) REFERENCES [platform].[PersonalSpace]([Id]),
        CONSTRAINT [FK_Habit_CreatedBy] FOREIGN KEY ([CreatedByUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [FK_Habit_UpdatedBy] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [identity].[User]([Id]),
        CONSTRAINT [CK_Habit_Kind] CHECK ([Kind] IN ('Boolean', 'Count')),
        CONSTRAINT [CK_Habit_State] CHECK ([State] IN ('Active', 'Paused', 'Archived')),
        CONSTRAINT [CK_Habit_Target] CHECK
        (
            ([Kind] = 'Boolean' AND [TargetCount] IS NULL)
            OR ([Kind] = 'Count' AND [TargetCount] > 0)
        ),
        CONSTRAINT [CK_Habit_PreArchiveState] CHECK ([PreArchiveState] IS NULL OR [PreArchiveState] IN ('Active', 'Paused'))
    );

    CREATE INDEX [IX_Habit_Owner_State_Title] ON [productivity].[Habit]([OwnerId], [State], [Title], [Id]);
END
GO

IF OBJECT_ID(N'[productivity].[HabitSchedule]', N'U') IS NULL
BEGIN
    CREATE TABLE [productivity].[HabitSchedule]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_HabitSchedule_Id] DEFAULT NEWSEQUENTIALID(),
        [OwnerId] uniqueidentifier NOT NULL,
        [HabitId] uniqueidentifier NOT NULL,
        [EffectiveFrom] date NOT NULL,
        [EffectiveUntil] date NULL,
        [WeekdayMask] tinyint NOT NULL,
        [TargetCount] int NULL,
        [Paused] bit NOT NULL CONSTRAINT [DF_HabitSchedule_Paused] DEFAULT 0,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_HabitSchedule_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_HabitSchedule_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_HabitSchedule] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_HabitSchedule_Owner] FOREIGN KEY ([OwnerId]) REFERENCES [platform].[PersonalSpace]([Id]),
        CONSTRAINT [FK_HabitSchedule_Habit] FOREIGN KEY ([HabitId]) REFERENCES [productivity].[Habit]([Id]),
        CONSTRAINT [CK_HabitSchedule_Mask] CHECK ([WeekdayMask] BETWEEN 1 AND 127),
        CONSTRAINT [CK_HabitSchedule_Target] CHECK ([TargetCount] IS NULL OR [TargetCount] > 0),
        CONSTRAINT [CK_HabitSchedule_Interval] CHECK ([EffectiveUntil] IS NULL OR [EffectiveUntil] > [EffectiveFrom]),
        CONSTRAINT [UX_HabitSchedule_Owner_Habit_From] UNIQUE ([OwnerId], [HabitId], [EffectiveFrom])
    );

    CREATE INDEX [IX_HabitSchedule_Owner_Habit_Effective] ON [productivity].[HabitSchedule]([OwnerId], [HabitId], [EffectiveFrom], [EffectiveUntil]);
END
GO

IF OBJECT_ID(N'[productivity].[HabitCheckIn]', N'U') IS NULL
BEGIN
    CREATE TABLE [productivity].[HabitCheckIn]
    (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_HabitCheckIn_Id] DEFAULT NEWSEQUENTIALID(),
        [OwnerId] uniqueidentifier NOT NULL,
        [HabitId] uniqueidentifier NOT NULL,
        [ScheduleId] uniqueidentifier NOT NULL,
        [LocalDate] date NOT NULL,
        [Count] int NOT NULL,
        [Note] nvarchar(1000) NULL,
        [CreatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_HabitCheckIn_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2(7) NOT NULL CONSTRAINT [DF_HabitCheckIn_UpdatedAt] DEFAULT SYSUTCDATETIME(),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_HabitCheckIn] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_HabitCheckIn_Owner] FOREIGN KEY ([OwnerId]) REFERENCES [platform].[PersonalSpace]([Id]),
        CONSTRAINT [FK_HabitCheckIn_Habit] FOREIGN KEY ([HabitId]) REFERENCES [productivity].[Habit]([Id]),
        CONSTRAINT [FK_HabitCheckIn_Schedule] FOREIGN KEY ([ScheduleId]) REFERENCES [productivity].[HabitSchedule]([Id]),
        CONSTRAINT [CK_HabitCheckIn_Count] CHECK ([Count] >= 0),
        CONSTRAINT [UX_HabitCheckIn_Owner_Habit_Date] UNIQUE ([OwnerId], [HabitId], [LocalDate])
    );

    CREATE INDEX [IX_HabitCheckIn_Owner_Habit_Date] ON [productivity].[HabitCheckIn]([OwnerId], [HabitId], [LocalDate] DESC, [Id]);
END
GO

/* Do not allow a second schedule interval to overlap the existing history. */
CREATE OR ALTER TRIGGER [productivity].[TR_HabitSchedule_NoOverlap]
ON [productivity].[HabitSchedule]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS
    (
        SELECT 1
        FROM inserted incoming
        INNER JOIN [productivity].[HabitSchedule] existing
          ON existing.[OwnerId] = incoming.[OwnerId]
         AND existing.[HabitId] = incoming.[HabitId]
         AND existing.[Id] <> incoming.[Id]
         AND existing.[EffectiveFrom] < COALESCE(incoming.[EffectiveUntil], CONVERT(date, '9999-12-31'))
         AND incoming.[EffectiveFrom] < COALESCE(existing.[EffectiveUntil], CONVERT(date, '9999-12-31'))
    )
    BEGIN
        THROW 51023, 'Habit schedules must not overlap.', 1;
    END
END
GO

DECLARE @PlannerHabitPermissions TABLE ([ActionKey] nvarchar(160) NOT NULL PRIMARY KEY);
INSERT INTO @PlannerHabitPermissions ([ActionKey]) VALUES
    (N'planner.plan.read'), (N'planner.plan.pin'), (N'planner.plan.unpin'),
    (N'planner.plan.reorder'), (N'planner.plan.reschedule'), (N'planner.plan.notes'),
    (N'habits.habit.read'), (N'habits.habit.create'), (N'habits.habit.update'),
    (N'habits.habit.schedule'), (N'habits.habit.pause'), (N'habits.habit.resume'),
    (N'habits.habit.set_reminder'),
    (N'habits.checkin.record'), (N'habits.checkin.correct'), (N'habits.streak.read'),
    (N'habits.habit.archive'), (N'habits.habit.unarchive');

INSERT INTO [platform].[Permission] ([ActionKey], [EffectiveStatus])
SELECT requested.[ActionKey], 'Resolved'
FROM @PlannerHabitPermissions requested
WHERE NOT EXISTS (SELECT 1 FROM [platform].[Permission] existing WHERE existing.[ActionKey] = requested.[ActionKey]);

UPDATE permissionRow
SET [EffectiveStatus] = 'Resolved'
FROM [platform].[Permission] permissionRow
INNER JOIN @PlannerHabitPermissions requested ON requested.[ActionKey] = permissionRow.[ActionKey]
WHERE permissionRow.[EffectiveStatus] <> 'Resolved';
GO

UPDATE [platform].[Module]
SET [State] = 'Ready', [SystemEnabled] = 1, [RegistrationEnabled] = 1,
    [PolicyRevision] = [PolicyRevision] + 1, [UpdatedAt] = SYSUTCDATETIME()
WHERE [Code] IN ('FX15', 'FX17')
  AND ([State] <> 'Ready' OR [SystemEnabled] = 0 OR [RegistrationEnabled] = 0);

UPDATE grantRow
SET [Enabled] = 1, [UpdatedAt] = SYSUTCDATETIME()
FROM [platform].[UserModuleGrant] grantRow
INNER JOIN [platform].[Module] moduleRow ON moduleRow.[Id] = grantRow.[ModuleId]
INNER JOIN [identity].[User] userRow ON userRow.[Id] = grantRow.[UserId]
WHERE moduleRow.[Code] IN ('FX15', 'FX17')
  AND userRow.[State] = 'Active'
  AND grantRow.[Enabled] = 0;
GO

COMMIT TRANSACTION;
GO
