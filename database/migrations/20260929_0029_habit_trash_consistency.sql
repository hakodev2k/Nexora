/*
   Habit reads already exclude rows placed in the Trash lifecycle.  The local
   FX17 schema omitted the nullable marker, causing every Habit read (and
   therefore create response) to fail at runtime.  Keep the marker nullable
   for existing rows and index the owner-scoped active lookup used by the
   service.
*/
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

IF COL_LENGTH(N'productivity.Habit', N'TrashBatchId') IS NULL
    ALTER TABLE [productivity].[Habit] ADD [TrashBatchId] uniqueidentifier NULL;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [object_id] = OBJECT_ID(N'[productivity].[Habit]')
      AND [name] = N'IX_Habit_Owner_TrashBatch'
)
    CREATE INDEX [IX_Habit_Owner_TrashBatch]
        ON [productivity].[Habit] ([OwnerId], [TrashBatchId], [Id]);
GO

COMMIT TRANSACTION;
GO
