/*
   Additive integrity hardening for the local FX15/FX17 slices. The application
   still performs current capability and lifecycle checks, while these
   composite keys make owner mismatches impossible at the SQL boundary.
   Actor columns are nullable for existing local rows and are populated by all
   new Planner, schedule and check-in writes.
*/
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

IF COL_LENGTH(N'productivity.PlannerPin', N'CreatedByUserId') IS NULL
    ALTER TABLE [productivity].[PlannerPin] ADD [CreatedByUserId] uniqueidentifier NULL;
IF COL_LENGTH(N'productivity.PlannerPin', N'UpdatedByUserId') IS NULL
    ALTER TABLE [productivity].[PlannerPin] ADD [UpdatedByUserId] uniqueidentifier NULL;

IF COL_LENGTH(N'productivity.HabitSchedule', N'CreatedByUserId') IS NULL
    ALTER TABLE [productivity].[HabitSchedule] ADD [CreatedByUserId] uniqueidentifier NULL;
IF COL_LENGTH(N'productivity.HabitSchedule', N'UpdatedByUserId') IS NULL
    ALTER TABLE [productivity].[HabitSchedule] ADD [UpdatedByUserId] uniqueidentifier NULL;

IF COL_LENGTH(N'productivity.HabitCheckIn', N'CreatedByUserId') IS NULL
    ALTER TABLE [productivity].[HabitCheckIn] ADD [CreatedByUserId] uniqueidentifier NULL;
IF COL_LENGTH(N'productivity.HabitCheckIn', N'UpdatedByUserId') IS NULL
    ALTER TABLE [productivity].[HabitCheckIn] ADD [UpdatedByUserId] uniqueidentifier NULL;
GO

/* Fail closed on legacy owner mismatches instead of repairing data silently. */
IF EXISTS
(
    SELECT 1
    FROM [productivity].[Task] taskRow
    INNER JOIN [productivity].[Project] projectRow ON projectRow.[Id] = taskRow.[ProjectId]
    WHERE taskRow.[OwnerId] <> projectRow.[OwnerId]
)
    THROW 51024, 'Task and Project owner mismatch blocks the owner-integrity migration.', 1;

IF EXISTS
(
    SELECT 1
    FROM [productivity].[PlannerPin] pinRow
    LEFT JOIN [productivity].[Task] taskRow
        ON taskRow.[Id] = pinRow.[TaskId] AND taskRow.[OwnerId] = pinRow.[OwnerId]
    WHERE taskRow.[Id] IS NULL
)
    THROW 51024, 'PlannerPin owner mismatch blocks the owner-integrity migration.', 1;

IF EXISTS
(
    SELECT 1
    FROM [productivity].[HabitSchedule] scheduleRow
    LEFT JOIN [productivity].[Habit] habitRow
        ON habitRow.[Id] = scheduleRow.[HabitId] AND habitRow.[OwnerId] = scheduleRow.[OwnerId]
    WHERE habitRow.[Id] IS NULL
)
    THROW 51024, 'HabitSchedule owner mismatch blocks the owner-integrity migration.', 1;

IF EXISTS
(
    SELECT 1
    FROM [productivity].[HabitCheckIn] checkInRow
    LEFT JOIN [productivity].[Habit] habitRow
        ON habitRow.[Id] = checkInRow.[HabitId] AND habitRow.[OwnerId] = checkInRow.[OwnerId]
    LEFT JOIN [productivity].[HabitSchedule] scheduleRow
        ON scheduleRow.[Id] = checkInRow.[ScheduleId] AND scheduleRow.[OwnerId] = checkInRow.[OwnerId]
    WHERE habitRow.[Id] IS NULL
       OR scheduleRow.[Id] IS NULL
       OR scheduleRow.[HabitId] <> checkInRow.[HabitId]
)
    THROW 51024, 'HabitCheckIn owner or schedule mismatch blocks the owner-integrity migration.', 1;
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [object_id] = OBJECT_ID(N'[productivity].[Project]')
      AND [name] = N'UX_Project_Owner_Id'
)
    CREATE UNIQUE INDEX [UX_Project_Owner_Id]
        ON [productivity].[Project] ([OwnerId], [Id]);

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [object_id] = OBJECT_ID(N'[productivity].[Task]')
      AND [name] = N'UX_Task_Owner_Id'
)
    CREATE UNIQUE INDEX [UX_Task_Owner_Id]
        ON [productivity].[Task] ([OwnerId], [Id]);

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [object_id] = OBJECT_ID(N'[productivity].[Habit]')
      AND [name] = N'UX_Habit_Owner_Id'
)
    CREATE UNIQUE INDEX [UX_Habit_Owner_Id]
        ON [productivity].[Habit] ([OwnerId], [Id]);

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [object_id] = OBJECT_ID(N'[productivity].[HabitSchedule]')
      AND [name] = N'UX_HabitSchedule_Owner_Id'
)
    CREATE UNIQUE INDEX [UX_HabitSchedule_Owner_Id]
        ON [productivity].[HabitSchedule] ([OwnerId], [Id]);

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [object_id] = OBJECT_ID(N'[productivity].[HabitSchedule]')
      AND [name] = N'UX_HabitSchedule_Owner_Habit_Id'
)
    CREATE UNIQUE INDEX [UX_HabitSchedule_Owner_Habit_Id]
        ON [productivity].[HabitSchedule] ([OwnerId], [HabitId], [Id]);

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE [object_id] = OBJECT_ID(N'[productivity].[HabitCheckIn]')
      AND [name] = N'IX_HabitCheckIn_Owner_Schedule'
)
    CREATE INDEX [IX_HabitCheckIn_Owner_Schedule]
        ON [productivity].[HabitCheckIn] ([OwnerId], [ScheduleId], [Id]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_Task_Project_Owner')
    ALTER TABLE [productivity].[Task] WITH CHECK
        ADD CONSTRAINT [FK_Task_Project_Owner]
        FOREIGN KEY ([OwnerId], [ProjectId])
        REFERENCES [productivity].[Project] ([OwnerId], [Id]);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_PlannerPin_Task_Owner')
    ALTER TABLE [productivity].[PlannerPin] WITH CHECK
        ADD CONSTRAINT [FK_PlannerPin_Task_Owner]
        FOREIGN KEY ([OwnerId], [TaskId])
        REFERENCES [productivity].[Task] ([OwnerId], [Id]);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_HabitSchedule_Habit_Owner')
    ALTER TABLE [productivity].[HabitSchedule] WITH CHECK
        ADD CONSTRAINT [FK_HabitSchedule_Habit_Owner]
        FOREIGN KEY ([OwnerId], [HabitId])
        REFERENCES [productivity].[Habit] ([OwnerId], [Id]);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_HabitCheckIn_Habit_Owner')
    ALTER TABLE [productivity].[HabitCheckIn] WITH CHECK
        ADD CONSTRAINT [FK_HabitCheckIn_Habit_Owner]
        FOREIGN KEY ([OwnerId], [HabitId])
        REFERENCES [productivity].[Habit] ([OwnerId], [Id]);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_HabitCheckIn_Schedule_Owner')
    ALTER TABLE [productivity].[HabitCheckIn] WITH CHECK
        ADD CONSTRAINT [FK_HabitCheckIn_Schedule_Owner]
        FOREIGN KEY ([OwnerId], [ScheduleId])
        REFERENCES [productivity].[HabitSchedule] ([OwnerId], [Id]);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_HabitCheckIn_Schedule_Habit_Owner')
    ALTER TABLE [productivity].[HabitCheckIn] WITH CHECK
        ADD CONSTRAINT [FK_HabitCheckIn_Schedule_Habit_Owner]
        FOREIGN KEY ([OwnerId], [HabitId], [ScheduleId])
        REFERENCES [productivity].[HabitSchedule] ([OwnerId], [HabitId], [Id]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_PlannerPin_CreatedByUser')
    ALTER TABLE [productivity].[PlannerPin] WITH CHECK
        ADD CONSTRAINT [FK_PlannerPin_CreatedByUser]
        FOREIGN KEY ([CreatedByUserId]) REFERENCES [identity].[User] ([Id]);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_PlannerPin_UpdatedByUser')
    ALTER TABLE [productivity].[PlannerPin] WITH CHECK
        ADD CONSTRAINT [FK_PlannerPin_UpdatedByUser]
        FOREIGN KEY ([UpdatedByUserId]) REFERENCES [identity].[User] ([Id]);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_HabitSchedule_CreatedByUser')
    ALTER TABLE [productivity].[HabitSchedule] WITH CHECK
        ADD CONSTRAINT [FK_HabitSchedule_CreatedByUser]
        FOREIGN KEY ([CreatedByUserId]) REFERENCES [identity].[User] ([Id]);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_HabitSchedule_UpdatedByUser')
    ALTER TABLE [productivity].[HabitSchedule] WITH CHECK
        ADD CONSTRAINT [FK_HabitSchedule_UpdatedByUser]
        FOREIGN KEY ([UpdatedByUserId]) REFERENCES [identity].[User] ([Id]);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_HabitCheckIn_CreatedByUser')
    ALTER TABLE [productivity].[HabitCheckIn] WITH CHECK
        ADD CONSTRAINT [FK_HabitCheckIn_CreatedByUser]
        FOREIGN KEY ([CreatedByUserId]) REFERENCES [identity].[User] ([Id]);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_HabitCheckIn_UpdatedByUser')
    ALTER TABLE [productivity].[HabitCheckIn] WITH CHECK
        ADD CONSTRAINT [FK_HabitCheckIn_UpdatedByUser]
        FOREIGN KEY ([UpdatedByUserId]) REFERENCES [identity].[User] ([Id]);
GO

COMMIT TRANSACTION;
GO
