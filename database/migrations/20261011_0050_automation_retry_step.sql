SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID('automation.StepRun','U') IS NULL THROW 51050,'Automation manual runs must be installed first.',1;
-- Local implementation registration only; no grants or execution toggle are enabled.
UPDATE [platform].[Permission] SET EffectiveStatus='Resolved' WHERE ActionKey='automation.run.retry_step';
IF NOT EXISTS(SELECT 1 FROM [platform].[Permission] WITH(UPDLOCK,HOLDLOCK) WHERE ActionKey='automation.run.retry_step')
 INSERT [platform].[Permission](ActionKey,EffectiveStatus) VALUES('automation.run.retry_step','Resolved');
COMMIT;
