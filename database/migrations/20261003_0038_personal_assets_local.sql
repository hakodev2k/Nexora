SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF SCHEMA_ID('assets') IS NULL EXEC('CREATE SCHEMA [assets]');
IF OBJECT_ID('[assets].[PersonalAsset]', 'U') IS NULL
BEGIN
 CREATE TABLE [assets].[PersonalAsset](
  Id uniqueidentifier NOT NULL CONSTRAINT PK_PersonalAsset PRIMARY KEY NONCLUSTERED,
  OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id),
  Title nvarchar(200) NOT NULL CHECK(LEN(LTRIM(RTRIM(Title)))>0),
  Kind varchar(64) NOT NULL CHECK(Kind IN ('Device','Electronics','VehicleMetadata','Other')),
  Brand nvarchar(100) NULL, Model nvarchar(200) NULL, Category nvarchar(100) NULL,
  Notes nvarchar(max) NULL CHECK(Notes IS NULL OR DATALENGTH(Notes)<=40000),
  State varchar(64) NOT NULL CHECK(State IN ('Active','Stored','Repair','Sold','Disposed','Lost','Archived','Trash')),
  PreArchiveState varchar(64) NULL, PreTrashState varchar(64) NULL, TrashBatchId uniqueidentifier NULL,
  CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt datetime2(7) NOT NULL,
  CreatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id),
  UpdatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id), RowVersion rowversion NOT NULL,
  CONSTRAINT UQ_PersonalAsset_Owner_Id UNIQUE(OwnerId,Id),
  CONSTRAINT FK_PersonalAsset_Resource FOREIGN KEY(OwnerId,Id) REFERENCES [platform].[Resource](OwnerId,Id),
  CONSTRAINT FK_PersonalAsset_TrashBatch FOREIGN KEY(OwnerId,TrashBatchId) REFERENCES [operations].[TrashBatch](OwnerId,Id),
  CONSTRAINT CK_PersonalAsset_Archive CHECK(
    (State='Archived' AND PreArchiveState IS NOT NULL AND PreArchiveState IN ('Active','Stored','Repair','Sold','Disposed','Lost')) OR
    (State='Trash' AND ((PreTrashState='Archived' AND PreArchiveState IS NOT NULL AND PreArchiveState IN ('Active','Stored','Repair','Sold','Disposed','Lost')) OR
      (PreTrashState IN ('Active','Stored','Repair','Sold','Disposed','Lost') AND PreArchiveState IS NULL))) OR
    (State IN ('Active','Stored','Repair','Sold','Disposed','Lost') AND PreArchiveState IS NULL)),
  CONSTRAINT CK_PersonalAsset_Trash CHECK(
    (State='Trash' AND TrashBatchId IS NOT NULL AND PreTrashState IS NOT NULL AND PreTrashState IN ('Active','Stored','Repair','Sold','Disposed','Lost','Archived')) OR
    (State<>'Trash' AND TrashBatchId IS NULL AND PreTrashState IS NULL)));
 CREATE UNIQUE CLUSTERED INDEX CX_PersonalAsset_Owner_Created_Id ON [assets].[PersonalAsset](OwnerId,CreatedAt,Id);
 CREATE INDEX IX_PersonalAsset_Page ON [assets].[PersonalAsset](OwnerId,State,Title,Id) INCLUDE(Kind,Category);
END;
IF OBJECT_ID('[assets].[AssetVersion]', 'U') IS NULL
BEGIN
 CREATE TABLE [assets].[AssetVersion](
  Id uniqueidentifier NOT NULL CONSTRAINT PK_AssetVersion PRIMARY KEY NONCLUSTERED,
  OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id),
  AssetId uniqueidentifier NOT NULL, VersionNumber bigint NOT NULL CHECK(VersionNumber>0),
  CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
  CreatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id),
  ActionKey varchar(160) NOT NULL CHECK(ActionKey IN ('assets.asset.create','assets.asset.update','assets.asset.transition','assets.asset.archive','assets.asset.unarchive','assets.asset.trash','assets.asset.restore')),
  Reason nvarchar(2000) NULL, SafeSnapshotJson nvarchar(max) NOT NULL,
  CONSTRAINT CK_AssetVersion_Snapshot CHECK(ISJSON(SafeSnapshotJson)=1 AND
    JSON_VALUE(SafeSnapshotJson,'$.schemaVersion') IS NOT NULL AND JSON_VALUE(SafeSnapshotJson,'$.schemaVersion')='1' AND
    JSON_VALUE(SafeSnapshotJson,'$.resourceType') IS NOT NULL AND JSON_VALUE(SafeSnapshotJson,'$.resourceType')='PersonalAsset'),
  CONSTRAINT UQ_AssetVersion_Owner_Id UNIQUE(OwnerId,Id),
  CONSTRAINT UQ_AssetVersion_Number UNIQUE(OwnerId,AssetId,VersionNumber),
  CONSTRAINT FK_AssetVersion_Asset FOREIGN KEY(OwnerId,AssetId) REFERENCES [assets].[PersonalAsset](OwnerId,Id));
 CREATE UNIQUE CLUSTERED INDEX CX_AssetVersion_Owner_Created_Id ON [assets].[AssetVersion](OwnerId,CreatedAt,Id);
 CREATE INDEX IX_AssetVersion_Page ON [assets].[AssetVersion](OwnerId,AssetId,VersionNumber DESC,Id DESC);
END;
EXEC('CREATE OR ALTER TRIGGER [assets].[TR_AssetVersion_Immutable] ON [assets].[AssetVersion] INSTEAD OF UPDATE AS
 BEGIN SET NOCOUNT ON; THROW 51038, ''Asset versions are immutable.'', 1; END');
IF NOT EXISTS(SELECT 1 FROM [platform].[ResourceType] WHERE ModuleId=(SELECT Id FROM [platform].[Module] WHERE Code='FX37') AND Code='PersonalAsset')
 INSERT [platform].[ResourceType](Id,ModuleId,Code,ContractVersion,CapabilitiesJson)
 SELECT NEWID(),Id,'PersonalAsset','personal-assets-v1',N'{"schemaVersion":1,"trash":true,"share":false,"support":false,"search":false}'
 FROM [platform].[Module] WHERE Code='FX37';
INSERT [platform].[Permission](ActionKey,EffectiveStatus)
SELECT s.ActionKey,'Resolved' FROM (VALUES
 ('assets.asset.read'),('assets.asset.create'),('assets.asset.update'),('assets.asset.transition'),('assets.asset.history'),
 ('assets.asset.archive'),('assets.asset.unarchive'),('assets.asset.trash'),('assets.asset.restore'),('assets.asset.purge'))s(ActionKey)
WHERE NOT EXISTS(SELECT 1 FROM [platform].[Permission] WITH(UPDLOCK,HOLDLOCK) WHERE ActionKey=s.ActionKey);
UPDATE [platform].[Module] SET Name=N'Personal Assets — Metadata and history only',State='Ready',PolicyRevision=PolicyRevision+1,UpdatedAt=SYSUTCDATETIME()
WHERE Code='FX37' AND State='Blocked';
COMMIT;
