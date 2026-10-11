SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID('[automation].[Definition]','U') IS NULL THROW 51048,'Automation definitions must be installed first.',1;
IF COL_LENGTH('automation.Definition','DeletionBatchId') IS NULL
 ALTER TABLE [automation].[Definition] ADD DeletionBatchId uniqueidentifier NULL;
UPDATE [platform].[ResourceType] SET CapabilitiesJson=N'{"schemaVersion":1,"trash":true,"share":false,"support":false,"search":false}'
 WHERE Code='AutomationDefinition' AND ContractVersion='automation-definition-v1' AND ModuleId=(SELECT Id FROM [platform].[Module] WHERE Code='FX34');
UPDATE [platform].[Permission] SET EffectiveStatus='Resolved' WHERE ActionKey IN
 ('automation.definition.trash','automation.definition.restore','automation.definition.import','automation.definition.export');
INSERT [platform].[Permission](ActionKey,EffectiveStatus)
 SELECT s.ActionKey,'Resolved' FROM (VALUES('automation.definition.trash'),('automation.definition.restore'),('automation.definition.import'),('automation.definition.export'))s(ActionKey)
 WHERE NOT EXISTS(SELECT 1 FROM [platform].[Permission] WITH(UPDLOCK,HOLDLOCK) WHERE ActionKey=s.ActionKey);
COMMIT;
