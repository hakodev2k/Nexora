/* Add exact shipped local command metadata. No user grants, module activation or resource changes.
   Existing permission state is preserved; stale Blocked rows are not promoted. */
IF NOT EXISTS (SELECT 1 FROM [platform].[Permission] WHERE [ActionKey] = N'tasks.task.criteria')
    INSERT INTO [platform].[Permission] ([ActionKey], [EffectiveStatus]) VALUES (N'tasks.task.criteria', 'Resolved');
IF NOT EXISTS (SELECT 1 FROM [platform].[Permission] WHERE [ActionKey] = N'tasks.task.reorder')
    INSERT INTO [platform].[Permission] ([ActionKey], [EffectiveStatus]) VALUES (N'tasks.task.reorder', 'Resolved');
IF NOT EXISTS (SELECT 1 FROM [platform].[Permission] WHERE [ActionKey] = N'tasks.task.set_reminder')
    INSERT INTO [platform].[Permission] ([ActionKey], [EffectiveStatus]) VALUES (N'tasks.task.set_reminder', 'Resolved');
IF NOT EXISTS (SELECT 1 FROM [platform].[Permission] WHERE [ActionKey] = N'tasks.task.remove_reminder')
    INSERT INTO [platform].[Permission] ([ActionKey], [EffectiveStatus]) VALUES (N'tasks.task.remove_reminder', 'Resolved');
IF NOT EXISTS (SELECT 1 FROM [platform].[Permission] WHERE [ActionKey] = N'calendar.event.set_reminder')
    INSERT INTO [platform].[Permission] ([ActionKey], [EffectiveStatus]) VALUES (N'calendar.event.set_reminder', 'Resolved');
IF NOT EXISTS (SELECT 1 FROM [platform].[Permission] WHERE [ActionKey] = N'calendar.event.remove_reminder')
    INSERT INTO [platform].[Permission] ([ActionKey], [EffectiveStatus]) VALUES (N'calendar.event.remove_reminder', 'Resolved');
