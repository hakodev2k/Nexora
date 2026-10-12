SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF SCHEMA_ID('automation') IS NULL EXEC('CREATE SCHEMA [automation]');
IF OBJECT_ID('[automation].[Connection]','U') IS NULL
BEGIN
 CREATE TABLE [automation].[Connection](
  Id uniqueidentifier NOT NULL CONSTRAINT PK_IntegrationConnection PRIMARY KEY NONCLUSTERED,
  OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id),
  ProviderKey nvarchar(150) NOT NULL CHECK(ProviderKey='n8n'),
  ConnectorVersion int NOT NULL CHECK(ConnectorVersion=1),
  Title nvarchar(200) NOT NULL CHECK(LEN(LTRIM(RTRIM(Title)))>0),
  Endpoint nvarchar(2048) NOT NULL,
  AuthMode varchar(32) NOT NULL DEFAULT 'Unconfigured' CHECK(AuthMode='Unconfigured'),
  VaultResourceId uniqueidentifier NULL CHECK(VaultResourceId IS NULL),
  ScopesJson nvarchar(max) NOT NULL CHECK(ISJSON(ScopesJson)=1 AND ScopesJson=N'{"schemaVersion":1,"inbound":[],"outbound":[]}'),
  State varchar(64) NOT NULL DEFAULT 'Disabled' CHECK(State='Disabled'),
  AuthorityRevision bigint NOT NULL DEFAULT 1 CHECK(AuthorityRevision>0),
  LastTestAt datetime2(7) NULL CHECK(LastTestAt IS NULL),
  CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
  CreatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id), UpdatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id),
  RowVersion rowversion NOT NULL,
  CONSTRAINT UQ_IntegrationConnection_Owner_Id UNIQUE(OwnerId,Id),
  CONSTRAINT FK_IntegrationConnection_Resource FOREIGN KEY(OwnerId,Id) REFERENCES [platform].[Resource](OwnerId,Id),
  CONSTRAINT FK_IntegrationConnection_VaultResource FOREIGN KEY(OwnerId,VaultResourceId) REFERENCES [platform].[Resource](OwnerId,Id));
 CREATE UNIQUE CLUSTERED INDEX CX_IntegrationConnection_Owner_Created_Id ON [automation].[Connection](OwnerId,CreatedAt,Id);
 CREATE INDEX IX_IntegrationConnection_NamePage ON [automation].[Connection](OwnerId,Title,Id);
 CREATE INDEX IX_IntegrationConnection_Provider_State ON [automation].[Connection](OwnerId,ProviderKey,State);
END;
IF NOT EXISTS(SELECT 1 FROM [platform].[ResourceType] rt JOIN [platform].[Module] m ON m.Id=rt.ModuleId WHERE m.Code='FX35' AND rt.Code='IntegrationConnection')
 INSERT [platform].[ResourceType](Id,ModuleId,Code,ContractVersion,CapabilitiesJson)
 SELECT NEWID(),Id,'IntegrationConnection','integration-connection-v1',N'{"schemaVersion":1,"trash":false,"share":false,"support":false,"search":false}' FROM [platform].[Module] WHERE Code='FX35';
IF NOT EXISTS(SELECT 1 FROM [platform].[ResourceType] rt JOIN [platform].[Module] m ON m.Id=rt.ModuleId WHERE m.Code='FX35' AND rt.Code='IntegrationConnection' AND rt.ContractVersion='integration-connection-v1')
 THROW 51054,'Integration connection installation contract is unavailable.',1;
-- Exact PO approval in this continuation: four inactive configuration actions only.
-- Preserve SystemEnabled, RegistrationEnabled, user grants and all provider operation gates.
UPDATE [platform].[Module] SET Name=N'Integrations — Inactive connection configuration',State='Ready',PolicyRevision=PolicyRevision+1,UpdatedAt=SYSUTCDATETIME() WHERE Code='FX35' AND State IN('Paused','Blocked');
UPDATE [platform].[Permission] SET EffectiveStatus='Resolved' WHERE ActionKey IN('integrations.connection.read','integrations.connection.create','integrations.connection.update','integrations.connection.disable');
INSERT [platform].[Permission](ActionKey,EffectiveStatus)
 SELECT s.ActionKey,'Resolved' FROM (VALUES('integrations.connection.read'),('integrations.connection.create'),('integrations.connection.update'),('integrations.connection.disable'))s(ActionKey)
 WHERE NOT EXISTS(SELECT 1 FROM [platform].[Permission] WITH(UPDLOCK,HOLDLOCK) WHERE ActionKey=s.ActionKey);
COMMIT;
