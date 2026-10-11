SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID('automation.ScheduleOccurrence','U') IS NULL THROW 51053,'Automation schedule persistence must be installed first.',1;
-- Register local lifecycle contract only. No data purge, grants or worker activation.
UPDATE [platform].[Permission] SET EffectiveStatus='Resolved' WHERE ActionKey='automation.definition.purge';
IF NOT EXISTS(SELECT 1 FROM [platform].[Permission] WITH(UPDLOCK,HOLDLOCK) WHERE ActionKey='automation.definition.purge')
 INSERT [platform].[Permission](ActionKey,EffectiveStatus) VALUES('automation.definition.purge','Resolved');
COMMIT;
