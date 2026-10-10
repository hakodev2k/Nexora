SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF EXISTS(SELECT 1 FROM [platform].[Module] WITH(UPDLOCK,HOLDLOCK) WHERE Code='FX36' AND State='Blocked' AND SystemEnabled=1)
 THROW 51045,'Monitoring installation requires explicit policy activation.',1;
IF SCHEMA_ID('monitoring') IS NULL EXEC('CREATE SCHEMA [monitoring]');
IF OBJECT_ID('[monitoring].[Monitor]','U') IS NULL
BEGIN
 CREATE TABLE [monitoring].[Monitor](
  Id uniqueidentifier NOT NULL CONSTRAINT PK_Monitor PRIMARY KEY NONCLUSTERED,
  OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id),
  CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(), CreatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id),
  UpdatedAt datetime2(7) NOT NULL, UpdatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id), RowVersion rowversion NOT NULL,
  Title nvarchar(200) NOT NULL CHECK(LEN(LTRIM(RTRIM(Title)))>0), Kind varchar(64) NOT NULL CHECK(Kind='Http'),
  Target nvarchar(2048) NOT NULL CHECK(LEN(Target)>0), IntervalSeconds int NOT NULL CHECK(IntervalSeconds>0),
  ExpectedStatus int NULL CHECK(ExpectedStatus IS NULL OR ExpectedStatus BETWEEN 100 AND 599),
  Enabled bit NOT NULL, State varchar(64) NOT NULL,
  LastObservedAt datetime2(7) NULL CHECK(LastObservedAt IS NULL), HeartbeatTokenHash binary(32) NULL CHECK(HeartbeatTokenHash IS NULL),
  CONSTRAINT CK_Monitor_ConfigOnly CHECK((Enabled=1 AND State='Unknown') OR (Enabled=0 AND State='Paused')),
  CONSTRAINT UQ_Monitor_Owner_Id UNIQUE(OwnerId,Id),
  CONSTRAINT FK_Monitor_Resource FOREIGN KEY(OwnerId,Id) REFERENCES [platform].[Resource](OwnerId,Id));
 CREATE UNIQUE CLUSTERED INDEX CX_Monitor_Owner_Created_Id ON [monitoring].[Monitor](OwnerId,CreatedAt,Id);
 CREATE INDEX IX_Monitor_Page ON [monitoring].[Monitor](OwnerId,Title,Id) INCLUDE(State,Kind);
END;
IF NOT EXISTS(SELECT 1 FROM [platform].[ResourceType] WHERE ModuleId=(SELECT Id FROM [platform].[Module] WHERE Code='FX36') AND Code='Monitor')
 INSERT [platform].[ResourceType](Id,ModuleId,Code,ContractVersion,CapabilitiesJson)
 SELECT NEWID(),Id,'Monitor','monitoring-http-config-v1',N'{"schemaVersion":1,"trash":false,"share":false,"support":false,"search":false}'
 FROM [platform].[Module] WHERE Code='FX36';
INSERT [platform].[Permission](ActionKey,EffectiveStatus)
SELECT s.ActionKey,'Resolved' FROM (VALUES('monitoring.monitor.read'),('monitoring.monitor.create'),('monitoring.monitor.update'),('monitoring.monitor.pause'),('monitoring.monitor.resume'))s(ActionKey)
WHERE NOT EXISTS(SELECT 1 FROM [platform].[Permission] WITH(UPDLOCK,HOLDLOCK) WHERE ActionKey=s.ActionKey);
UPDATE [platform].[Module] SET Name=N'Monitoring — Local HTTP Configuration only',State='Ready',PolicyRevision=PolicyRevision+1,UpdatedAt=SYSUTCDATETIME()
WHERE Code='FX36' AND State='Blocked';
COMMIT;
