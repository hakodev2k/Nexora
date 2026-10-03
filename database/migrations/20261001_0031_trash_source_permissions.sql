SET XACT_ABORT ON;
BEGIN TRANSACTION;
INSERT [platform].[Permission] (ActionKey, EffectiveStatus)
SELECT source.ActionKey, 'Resolved'
FROM (VALUES ('projects.project.restore'),('projects.project.purge'),('tasks.task.restore'),('tasks.task.purge')) AS source(ActionKey)
WHERE NOT EXISTS (SELECT 1 FROM [platform].[Permission] WITH (UPDLOCK,HOLDLOCK) WHERE ActionKey=source.ActionKey);
COMMIT;
