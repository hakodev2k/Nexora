SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF SCHEMA_ID('automation') IS NULL EXEC('CREATE SCHEMA [automation]');
IF OBJECT_ID('[automation].[Definition]','U') IS NULL
BEGIN
 CREATE TABLE [automation].[Definition](
  Id uniqueidentifier NOT NULL CONSTRAINT PK_AutomationDefinition PRIMARY KEY NONCLUSTERED,
  OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id),
  Title nvarchar(200) NOT NULL CHECK(LEN(LTRIM(RTRIM(Title)))>0),
  CurrentVersion bigint NULL,
  State varchar(64) NOT NULL DEFAULT 'Draft' CHECK(State IN('Draft','Disabled')),
  Enabled bit NOT NULL DEFAULT 0 CHECK(Enabled=0),
  CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
  CreatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id), UpdatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id),
  RowVersion rowversion NOT NULL,
  CONSTRAINT UQ_AutomationDefinition_Owner_Id UNIQUE(OwnerId,Id),
  CONSTRAINT FK_AutomationDefinition_Resource FOREIGN KEY(OwnerId,Id) REFERENCES [platform].[Resource](OwnerId,Id));
 CREATE UNIQUE CLUSTERED INDEX CX_AutomationDefinition_Owner_Created_Id ON [automation].[Definition](OwnerId,CreatedAt,Id);
 CREATE INDEX IX_AutomationDefinition_Page ON [automation].[Definition](OwnerId,UpdatedAt DESC,Id DESC);
END;
IF OBJECT_ID('[automation].[DefinitionVersion]','U') IS NULL
BEGIN
 CREATE TABLE [automation].[DefinitionVersion](
  Id uniqueidentifier NOT NULL CONSTRAINT PK_AutomationDefinitionVersion PRIMARY KEY NONCLUSTERED,
  OwnerId uniqueidentifier NOT NULL,
  DefinitionId uniqueidentifier NOT NULL,
  VersionNumber bigint NOT NULL CHECK(VersionNumber>0),
  Title nvarchar(200) NOT NULL,
  SchemaVersion int NOT NULL CHECK(SchemaVersion=1),
  DefinitionJson nvarchar(max) NOT NULL CHECK(ISJSON(DefinitionJson)=1 AND DATALENGTH(DefinitionJson)<=131072),
  CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
  CreatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id),
  CONSTRAINT UQ_AutomationDefinitionVersion_Owner_Definition_Number UNIQUE(OwnerId,DefinitionId,VersionNumber),
  CONSTRAINT FK_AutomationDefinitionVersion_Definition FOREIGN KEY(OwnerId,DefinitionId) REFERENCES [automation].[Definition](OwnerId,Id));
 CREATE UNIQUE CLUSTERED INDEX CX_AutomationDefinitionVersion_Owner_Definition_Number ON [automation].[DefinitionVersion](OwnerId,DefinitionId,VersionNumber DESC);
END;
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name='FK_AutomationDefinition_CurrentVersion')
 ALTER TABLE [automation].[Definition] ADD CONSTRAINT FK_AutomationDefinition_CurrentVersion
 FOREIGN KEY(OwnerId,Id,CurrentVersion) REFERENCES [automation].[DefinitionVersion](OwnerId,DefinitionId,VersionNumber);
IF NOT EXISTS(SELECT 1 FROM [platform].[ResourceType] rt JOIN [platform].[Module] m ON m.Id=rt.ModuleId WHERE m.Code='FX34' AND rt.Code='AutomationDefinition')
 INSERT [platform].[ResourceType](Id,ModuleId,Code,ContractVersion,CapabilitiesJson)
 SELECT NEWID(),Id,'AutomationDefinition','automation-definition-v1',N'{"schemaVersion":1,"trash":false,"share":false,"support":false,"search":false}'
 FROM [platform].[Module] WHERE Code='FX34';
IF NOT EXISTS(SELECT 1 FROM [platform].[ResourceType] rt JOIN [platform].[Module] m ON m.Id=rt.ModuleId WHERE m.Code='FX34' AND rt.Code='AutomationDefinition' AND rt.ContractVersion='automation-definition-v1')
 THROW 51047,'Automation definition installation contract is unavailable.',1;
-- Preserve disabled defaults and owner grants; this installs configuration only.
UPDATE [platform].[Module] SET Name=N'Automation — Definition configuration',State='Ready',PolicyRevision=PolicyRevision+1,UpdatedAt=SYSUTCDATETIME()
 WHERE Code='FX34' AND State IN('Paused','Blocked');
UPDATE [platform].[Permission] SET EffectiveStatus='Resolved' WHERE ActionKey IN
 ('automation.definition.read','automation.definition.create','automation.definition.save','automation.definition.history','automation.definition.validate');
INSERT [platform].[Permission](ActionKey,EffectiveStatus)
 SELECT s.ActionKey,'Resolved' FROM (VALUES('automation.definition.read'),('automation.definition.create'),('automation.definition.save'),('automation.definition.history'),('automation.definition.validate'))s(ActionKey)
 WHERE NOT EXISTS(SELECT 1 FROM [platform].[Permission] WITH(UPDLOCK,HOLDLOCK) WHERE ActionKey=s.ActionKey);
COMMIT;
