/*
   One-way Task -> Calendar projection boundary. Task owns the source fields;
   Calendar stores a read-only projection identified by TaskId. The filtered
   unique index makes retries converge on one projection per owner/task.
*/
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

IF OBJECT_ID(N'[calendar].[Event]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'calendar.Event', N'TaskId') IS NULL
        ALTER TABLE [calendar].[Event] ADD [TaskId] uniqueidentifier NULL;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.foreign_keys
        WHERE [name] = N'FK_Event_TaskProjection'
          AND [parent_object_id] = OBJECT_ID(N'[calendar].[Event]')
    )
        ALTER TABLE [calendar].[Event] ADD CONSTRAINT [FK_Event_TaskProjection]
            FOREIGN KEY ([TaskId]) REFERENCES [productivity].[Task]([Id]);

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE [name] = N'UX_Event_Owner_TaskProjection'
          AND [object_id] = OBJECT_ID(N'[calendar].[Event]')
    )
        CREATE UNIQUE INDEX [UX_Event_Owner_TaskProjection]
            ON [calendar].[Event]([OwnerId], [TaskId])
            WHERE [TaskId] IS NOT NULL;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.check_constraints
        WHERE [name] = N'CK_Event_SourceKind_TaskId'
          AND [parent_object_id] = OBJECT_ID(N'[calendar].[Event]')
    )
        ALTER TABLE [calendar].[Event] ADD CONSTRAINT [CK_Event_SourceKind_TaskId]
            CHECK (([SourceKind] = 'Task' AND [TaskId] IS NOT NULL) OR ([SourceKind] <> 'Task' AND [TaskId] IS NULL));
END

COMMIT TRANSACTION;
GO

