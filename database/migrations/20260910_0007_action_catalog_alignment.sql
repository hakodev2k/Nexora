SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

/*
   DEC-014 action vocabulary alignment. The first productivity/document
   implementation used a few compact legacy keys in audit/receipt records;
   the current catalog uses resource-qualified keys. Keep both during the
   local migration window, but register the canonical keys so explicit Admin
   grants can be evaluated without creating metadata as a side effect.
*/
IF OBJECT_ID(N'[platform].[Permission]', N'U') IS NOT NULL
BEGIN
    MERGE [platform].[Permission] AS target
    USING (VALUES
        (N'projects.project.read'),
        (N'projects.project.create'),
        (N'projects.project.update'),
        (N'projects.project.start'),
        (N'projects.project.revert'),
        (N'projects.project.complete'),
        (N'projects.project.skip'),
        (N'projects.project.trash'),
        (N'tasks.task.read'),
        (N'tasks.task.create'),
        (N'tasks.task.update'),
        (N'tasks.task.start'),
        (N'tasks.task.complete'),
        (N'tasks.task.skip'),
        (N'tasks.task.revert'),
        (N'tasks.task.trash'),
        (N'calendar.event.read'),
        (N'calendar.event.create'),
        (N'calendar.event.update'),
        (N'calendar.event.complete'),
        (N'calendar.event.cancel'),
        (N'notifications.inbox.read'),
        (N'notifications.inbox.mark_read'),
        (N'notifications.inbox.mark_unread'),
        (N'notifications.inbox.mark_all_read'),
        (N'notifications.inbox.delete'),
        (N'lifecycle.trash.read'),
        (N'lifecycle.resource.restore'),
        (N'lifecycle.resource.purge')
    ) AS source ([ActionKey])
    ON target.[ActionKey] = source.[ActionKey]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT ([ActionKey], [EffectiveStatus]) VALUES (source.[ActionKey], 'Resolved');
END
GO

COMMIT TRANSACTION;
GO
