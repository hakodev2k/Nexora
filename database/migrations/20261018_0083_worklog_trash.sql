SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH(N'learning.WorkLog',N'PreTrashState') IS NULL
    ALTER TABLE [learning].[WorkLog] ADD [PreTrashState] varchar(64) NULL;
IF COL_LENGTH(N'learning.WorkLog',N'TrashBatchId') IS NULL
    ALTER TABLE [learning].[WorkLog] ADD [TrashBatchId] uniqueidentifier NULL;
GO
IF OBJECT_ID(N'[learning].[CK_WorkLog_Trash]',N'C') IS NULL
BEGIN
    --0080 used a system-named column CHECK. Resolve only that exact column.
    DECLARE @Constraint sysname, @Sql nvarchar(1000);
    SELECT @Constraint=name FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'learning.WorkLog')
        AND parent_column_id=COLUMNPROPERTY(OBJECT_ID(N'learning.WorkLog'),N'Status','ColumnId');
    IF @Constraint IS NULL THROW 51083,'WorkLog status constraint requires reconciliation.',1;
    SET @Sql=N'ALTER TABLE [learning].[WorkLog] DROP CONSTRAINT '+QUOTENAME(@Constraint); EXEC sp_executesql @Sql;
    ALTER TABLE [learning].[WorkLog] DROP CONSTRAINT [CK_WorkLog_Archive];
    ALTER TABLE [learning].[WorkLog] ADD
        CONSTRAINT [CK_WorkLog_Status] CHECK ([Status] IN ('Active','Archived','Trash')),
        CONSTRAINT [CK_WorkLog_Trash] CHECK (([Status]='Trash' AND [PreTrashState] IS NOT NULL
            AND [PreTrashState] IN ('Active','Archived') AND [TrashBatchId] IS NOT NULL)
            OR ([Status]<>'Trash' AND [PreTrashState] IS NULL AND [TrashBatchId] IS NULL)),
        CONSTRAINT [CK_WorkLog_Archive] CHECK (([Status]='Active' AND [PreArchiveState] IS NULL)
            OR ([Status]='Archived' AND [PreArchiveState] IS NOT NULL AND [PreArchiveState]='Active')
            OR ([Status]='Trash' AND (([PreTrashState]='Active' AND [PreArchiveState] IS NULL)
                OR ([PreTrashState]='Archived' AND [PreArchiveState] IS NOT NULL AND [PreArchiveState]='Active')))),
        CONSTRAINT [FK_WorkLog_TrashBatch] FOREIGN KEY ([OwnerId],[TrashBatchId]) REFERENCES [operations].[TrashBatch]([OwnerId],[Id]);
END;
IF OBJECT_ID(N'[learning].[CK_WorkLogVersion_LifecycleEvent]',N'C') IS NULL
BEGIN
    DECLARE @HistoryConstraint sysname, @HistorySql nvarchar(1000);
    SELECT @HistoryConstraint=name FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'learning.WorkLogVersion')
        AND parent_column_id=COLUMNPROPERTY(OBJECT_ID(N'learning.WorkLogVersion'),N'Event','ColumnId');
    IF @HistoryConstraint IS NULL THROW 51083,'WorkLog history event constraint requires reconciliation.',1;
    SET @HistorySql=N'ALTER TABLE [learning].[WorkLogVersion] DROP CONSTRAINT '+QUOTENAME(@HistoryConstraint); EXEC sp_executesql @HistorySql;
    SET @HistoryConstraint=NULL;
    SELECT @HistoryConstraint=name FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'learning.WorkLogVersion')
        AND parent_column_id=COLUMNPROPERTY(OBJECT_ID(N'learning.WorkLogVersion'),N'Status','ColumnId');
    IF @HistoryConstraint IS NULL THROW 51083,'WorkLog history status constraint requires reconciliation.',1;
    SET @HistorySql=N'ALTER TABLE [learning].[WorkLogVersion] DROP CONSTRAINT '+QUOTENAME(@HistoryConstraint); EXEC sp_executesql @HistorySql;
    ALTER TABLE [learning].[WorkLogVersion] ADD
        CONSTRAINT [CK_WorkLogVersion_LifecycleEvent] CHECK ([Event] IN ('Created','Updated','Archived','Unarchived','HistoryStarted','Trashed','Restored')),
        CONSTRAINT [CK_WorkLogVersion_LifecycleStatus] CHECK ([Status] IN ('Active','Archived','Trash'));
END;
UPDATE rt SET CapabilitiesJson=N'{"schemaVersion":1,"trash":true,"share":false,"support":false,"search":false}'
FROM [platform].[ResourceType] rt JOIN [platform].[Module] m ON m.Id=rt.ModuleId
WHERE m.Code='FX40' AND rt.Code='WorkLog' AND rt.ContractVersion='worklog-manual-v1';
INSERT [platform].[Permission](ActionKey,EffectiveStatus)
SELECT v.ActionKey,'Resolved' FROM (VALUES(N'learning.worklog.trash'),(N'learning.worklog.restore'),(N'learning.worklog.purge')) v(ActionKey)
WHERE NOT EXISTS(SELECT 1 FROM [platform].[Permission] p WITH(UPDLOCK,HOLDLOCK) WHERE p.ActionKey=v.ActionKey);
COMMIT TRANSACTION;
GO
