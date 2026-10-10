SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF SCHEMA_ID('assets') IS NULL EXEC('CREATE SCHEMA [assets]');
IF OBJECT_ID('[assets].[DigitalAsset]', 'U') IS NULL
BEGIN
 CREATE TABLE [assets].[DigitalAsset](
  Id uniqueidentifier NOT NULL CONSTRAINT PK_DigitalAsset PRIMARY KEY NONCLUSTERED,
  OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id),
  Title nvarchar(200) NOT NULL CHECK(LEN(LTRIM(RTRIM(Title)))>0),
  Kind varchar(64) NOT NULL CHECK(Kind IN ('Domain','Hosting','Vps','Certificate','License','OnlineService')),
  Provider nvarchar(200) NULL, ExpiresOn date NULL, Cost decimal(28,8) NULL CHECK(Cost IS NULL OR Cost>=0),
  Currency char(3) NULL, RenewalCycle nvarchar(100) NULL, TrashChildFingerprint binary(32) NULL,
  CONSTRAINT CK_DigitalAsset_Cost CHECK((Cost IS NULL AND Currency IS NULL) OR (Cost IS NOT NULL AND Currency IS NOT NULL AND Currency COLLATE Latin1_General_100_BIN2 LIKE '[A-Z][A-Z][A-Z]')),
  Notes nvarchar(max) NULL CHECK(Notes IS NULL OR DATALENGTH(Notes)<=40000),
  State varchar(64) NOT NULL CHECK(State IN ('Active','Canceled','Archived','Trash')),
  PreArchiveState varchar(64) NULL, PreTrashState varchar(64) NULL, TrashBatchId uniqueidentifier NULL,
  CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt datetime2(7) NOT NULL,
  CreatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id),
  UpdatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id), RowVersion rowversion NOT NULL,
  CONSTRAINT UQ_DigitalAsset_Owner_Id UNIQUE(OwnerId,Id),
  CONSTRAINT UQ_DigitalAsset_Kind UNIQUE(OwnerId,Id,Kind),
  CONSTRAINT FK_DigitalAsset_Resource FOREIGN KEY(OwnerId,Id) REFERENCES [platform].[Resource](OwnerId,Id),
  CONSTRAINT FK_DigitalAsset_TrashBatch FOREIGN KEY(OwnerId,TrashBatchId) REFERENCES [operations].[TrashBatch](OwnerId,Id),
  CONSTRAINT CK_DigitalAsset_Archive CHECK(
    (State='Archived' AND PreArchiveState IS NOT NULL AND PreArchiveState IN ('Active','Canceled')) OR
    (State='Trash' AND ((PreTrashState='Archived' AND PreArchiveState IS NOT NULL AND PreArchiveState IN ('Active','Canceled')) OR
      (PreTrashState IN ('Active','Canceled') AND PreArchiveState IS NULL))) OR
    (State IN ('Active','Canceled') AND PreArchiveState IS NULL)),
  CONSTRAINT CK_DigitalAsset_Trash CHECK(
    (State='Trash' AND TrashChildFingerprint IS NOT NULL AND TrashBatchId IS NOT NULL AND PreTrashState IS NOT NULL AND PreTrashState IN ('Active','Canceled','Archived')) OR
    (State<>'Trash' AND TrashChildFingerprint IS NULL AND TrashBatchId IS NULL AND PreTrashState IS NULL)));
 CREATE UNIQUE CLUSTERED INDEX CX_DigitalAsset_Owner_Created_Id ON [assets].[DigitalAsset](OwnerId,CreatedAt,Id);
 CREATE INDEX IX_DigitalAsset_Page ON [assets].[DigitalAsset](OwnerId,State,Title,Id) INCLUDE(Kind,ExpiresOn);
END;
IF OBJECT_ID('[assets].[DomainDetail]','U') IS NULL
BEGIN
 CREATE TABLE [assets].[DomainDetail](
  Id uniqueidentifier NOT NULL CONSTRAINT PK_DomainDetail PRIMARY KEY NONCLUSTERED,
  OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id), DigitalAssetId uniqueidentifier NOT NULL,
  Kind varchar(64) NOT NULL CHECK(Kind='Domain'),
  AsciiName nvarchar(253) NOT NULL,
  UnicodeName nvarchar(253) NOT NULL,
  Registrar nvarchar(200) NULL,
  AutoRenewRecorded bit NULL,
  RegisteredOn date NULL,
  NameserverNotes nvarchar(max) NULL CHECK(NameserverNotes IS NULL OR DATALENGTH(NameserverNotes)<=40000),
  CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt datetime2(7) NOT NULL,
  CreatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id), UpdatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id),
  RowVersion rowversion NOT NULL,
  CONSTRAINT UQ_DomainDetail_Owner_Id UNIQUE(OwnerId,Id), CONSTRAINT UQ_DomainDetail_Asset UNIQUE(OwnerId,DigitalAssetId),
  CONSTRAINT FK_DomainDetail_AssetKind FOREIGN KEY(OwnerId,DigitalAssetId,Kind) REFERENCES [assets].[DigitalAsset](OwnerId,Id,Kind));
 CREATE UNIQUE CLUSTERED INDEX CX_DomainDetail_Owner_Created_Id ON [assets].[DomainDetail](OwnerId,CreatedAt,Id);
END;
IF OBJECT_ID('[assets].[HostingDetail]','U') IS NULL
BEGIN
 CREATE TABLE [assets].[HostingDetail](
  Id uniqueidentifier NOT NULL CONSTRAINT PK_HostingDetail PRIMARY KEY NONCLUSTERED,
  OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id), DigitalAssetId uniqueidentifier NOT NULL,
  Kind varchar(64) NOT NULL CHECK(Kind='Hosting'),
  [Plan] nvarchar(200) NULL,
  Region nvarchar(100) NULL,
  ControlPanelUrl nvarchar(2048) NULL,
  StorageLimitBytes bigint NULL CHECK(StorageLimitBytes IS NULL OR StorageLimitBytes>=0),
  DomainName nvarchar(253) NULL,
  CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt datetime2(7) NOT NULL,
  CreatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id), UpdatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id),
  RowVersion rowversion NOT NULL,
  CONSTRAINT UQ_HostingDetail_Owner_Id UNIQUE(OwnerId,Id), CONSTRAINT UQ_HostingDetail_Asset UNIQUE(OwnerId,DigitalAssetId),
  CONSTRAINT FK_HostingDetail_AssetKind FOREIGN KEY(OwnerId,DigitalAssetId,Kind) REFERENCES [assets].[DigitalAsset](OwnerId,Id,Kind));
 CREATE UNIQUE CLUSTERED INDEX CX_HostingDetail_Owner_Created_Id ON [assets].[HostingDetail](OwnerId,CreatedAt,Id);
END;
IF OBJECT_ID('[assets].[VpsDetail]','U') IS NULL
BEGIN
 CREATE TABLE [assets].[VpsDetail](
  Id uniqueidentifier NOT NULL CONSTRAINT PK_VpsDetail PRIMARY KEY NONCLUSTERED,
  OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id), DigitalAssetId uniqueidentifier NOT NULL,
  Kind varchar(64) NOT NULL CHECK(Kind='Vps'),
  HostName nvarchar(253) NULL,
  IpAddress nvarchar(45) NULL,
  CpuCount int NULL CHECK(CpuCount IS NULL OR CpuCount>0),
  MemoryMiB bigint NULL CHECK(MemoryMiB IS NULL OR MemoryMiB>=0),
  OperatingSystem nvarchar(200) NULL,
  [Plan] nvarchar(200) NULL,
  Region nvarchar(100) NULL,
  CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt datetime2(7) NOT NULL,
  CreatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id), UpdatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id),
  RowVersion rowversion NOT NULL,
  CONSTRAINT UQ_VpsDetail_Owner_Id UNIQUE(OwnerId,Id), CONSTRAINT UQ_VpsDetail_Asset UNIQUE(OwnerId,DigitalAssetId),
  CONSTRAINT FK_VpsDetail_AssetKind FOREIGN KEY(OwnerId,DigitalAssetId,Kind) REFERENCES [assets].[DigitalAsset](OwnerId,Id,Kind));
 CREATE UNIQUE CLUSTERED INDEX CX_VpsDetail_Owner_Created_Id ON [assets].[VpsDetail](OwnerId,CreatedAt,Id);
END;
IF OBJECT_ID('[assets].[CertificateDetail]','U') IS NULL
BEGIN
 CREATE TABLE [assets].[CertificateDetail](
  Id uniqueidentifier NOT NULL CONSTRAINT PK_CertificateDetail PRIMARY KEY NONCLUSTERED,
  OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id), DigitalAssetId uniqueidentifier NOT NULL,
  Kind varchar(64) NOT NULL CHECK(Kind='Certificate'),
  Subject nvarchar(500) NOT NULL CHECK(LEN(LTRIM(RTRIM(Subject)))>0),
  Issuer nvarchar(500) NULL,
  Fingerprint nvarchar(200) NULL,
  NotBefore datetime2(7) NULL,
  NotAfter datetime2(7) NULL,
  HostName nvarchar(253) NULL,
  SubjectAlternativeNamesJson nvarchar(max) NOT NULL CHECK(ISJSON(SubjectAlternativeNamesJson)=1 AND LEFT(LTRIM(SubjectAlternativeNamesJson),1)='[' AND DATALENGTH(SubjectAlternativeNamesJson)<=60000),
  Source varchar(64) NOT NULL CHECK(Source='Manual'),
  CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt datetime2(7) NOT NULL,
  CreatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id), UpdatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id),
  RowVersion rowversion NOT NULL,
  CONSTRAINT UQ_CertificateDetail_Owner_Id UNIQUE(OwnerId,Id), CONSTRAINT UQ_CertificateDetail_Asset UNIQUE(OwnerId,DigitalAssetId),
  CONSTRAINT FK_CertificateDetail_AssetKind FOREIGN KEY(OwnerId,DigitalAssetId,Kind) REFERENCES [assets].[DigitalAsset](OwnerId,Id,Kind),
  CONSTRAINT CK_CertificateDetail_Dates CHECK(NotBefore IS NULL OR NotAfter IS NULL OR NotBefore<NotAfter));
 CREATE UNIQUE CLUSTERED INDEX CX_CertificateDetail_Owner_Created_Id ON [assets].[CertificateDetail](OwnerId,CreatedAt,Id);
END;
IF OBJECT_ID('[assets].[LicenseDetail]','U') IS NULL
BEGIN
 CREATE TABLE [assets].[LicenseDetail](
  Id uniqueidentifier NOT NULL CONSTRAINT PK_LicenseDetail PRIMARY KEY NONCLUSTERED,
  OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id), DigitalAssetId uniqueidentifier NOT NULL,
  Kind varchar(64) NOT NULL CHECK(Kind='License'),
  Product nvarchar(200) NOT NULL CHECK(LEN(LTRIM(RTRIM(Product)))>0),
  Seats int NULL CHECK(Seats IS NULL OR Seats>0),
  PurchasedOn date NULL,
  Vendor nvarchar(200) NULL,
  Edition nvarchar(200) NULL,
  CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt datetime2(7) NOT NULL,
  CreatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id), UpdatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id),
  RowVersion rowversion NOT NULL,
  CONSTRAINT UQ_LicenseDetail_Owner_Id UNIQUE(OwnerId,Id), CONSTRAINT UQ_LicenseDetail_Asset UNIQUE(OwnerId,DigitalAssetId),
  CONSTRAINT FK_LicenseDetail_AssetKind FOREIGN KEY(OwnerId,DigitalAssetId,Kind) REFERENCES [assets].[DigitalAsset](OwnerId,Id,Kind));
 CREATE UNIQUE CLUSTERED INDEX CX_LicenseDetail_Owner_Created_Id ON [assets].[LicenseDetail](OwnerId,CreatedAt,Id);
END;
IF OBJECT_ID('[assets].[ServiceDetail]','U') IS NULL
BEGIN
 CREATE TABLE [assets].[ServiceDetail](
  Id uniqueidentifier NOT NULL CONSTRAINT PK_ServiceDetail PRIMARY KEY NONCLUSTERED,
  OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id), DigitalAssetId uniqueidentifier NOT NULL,
  Kind varchar(64) NOT NULL CHECK(Kind='OnlineService'),
  ServiceUrl nvarchar(2048) NULL,
  [Plan] nvarchar(200) NULL,
  CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt datetime2(7) NOT NULL,
  CreatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id), UpdatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id),
  RowVersion rowversion NOT NULL,
  CONSTRAINT UQ_ServiceDetail_Owner_Id UNIQUE(OwnerId,Id), CONSTRAINT UQ_ServiceDetail_Asset UNIQUE(OwnerId,DigitalAssetId),
  CONSTRAINT FK_ServiceDetail_AssetKind FOREIGN KEY(OwnerId,DigitalAssetId,Kind) REFERENCES [assets].[DigitalAsset](OwnerId,Id,Kind));
 CREATE UNIQUE CLUSTERED INDEX CX_ServiceDetail_Owner_Created_Id ON [assets].[ServiceDetail](OwnerId,CreatedAt,Id);
END;
IF OBJECT_ID('[assets].[RenewalRecord]','U') IS NULL
BEGIN
 CREATE TABLE [assets].[RenewalRecord](
  Id uniqueidentifier NOT NULL CONSTRAINT PK_RenewalRecord PRIMARY KEY NONCLUSTERED,
  OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id), DigitalAssetId uniqueidentifier NOT NULL,
  RenewedOn date NOT NULL, PreviousExpiry date NULL, NewExpiry date NOT NULL,
  Amount decimal(28,8) NULL CHECK(Amount IS NULL OR Amount>=0), Currency char(3) NULL, Notes nvarchar(2000) NULL,
  CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt datetime2(7) NOT NULL,
  CreatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id), UpdatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id), RowVersion rowversion NOT NULL,
  CONSTRAINT CK_RenewalRecord_Dates CHECK(NewExpiry>=RenewedOn),
  CONSTRAINT CK_RenewalRecord_Amount CHECK((Amount IS NULL AND Currency IS NULL) OR (Amount IS NOT NULL AND Currency IS NOT NULL AND Currency COLLATE Latin1_General_100_BIN2 LIKE '[A-Z][A-Z][A-Z]')),
  CONSTRAINT UQ_RenewalRecord_Owner_Id UNIQUE(OwnerId,Id), CONSTRAINT FK_RenewalRecord_Asset FOREIGN KEY(OwnerId,DigitalAssetId) REFERENCES [assets].[DigitalAsset](OwnerId,Id));
 CREATE UNIQUE CLUSTERED INDEX CX_RenewalRecord_Owner_Created_Id ON [assets].[RenewalRecord](OwnerId,CreatedAt,Id);
 CREATE INDEX IX_RenewalRecord_Page ON [assets].[RenewalRecord](OwnerId,DigitalAssetId,RenewedOn DESC,CreatedAt DESC,Id DESC);
END;
EXEC('CREATE OR ALTER TRIGGER [assets].[TR_RenewalRecord_Immutable] ON [assets].[RenewalRecord] INSTEAD OF UPDATE AS
 BEGIN SET NOCOUNT ON; THROW 51040, ''Recorded renewals are immutable.'', 1; END');
IF OBJECT_ID('[assets].[DigitalAssetVersion]', 'U') IS NULL
BEGIN
 CREATE TABLE [assets].[DigitalAssetVersion](
  Id uniqueidentifier NOT NULL CONSTRAINT PK_DigitalAssetVersion PRIMARY KEY NONCLUSTERED,
  OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id),
  DigitalAssetId uniqueidentifier NOT NULL, VersionNumber bigint NOT NULL CHECK(VersionNumber>0),
  CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
  CreatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id),
  ActionKey varchar(160) NOT NULL CHECK(ActionKey IN ('digital.asset.create','digital.asset.update','digital.asset.cancel','digital.renewal.record','digital.asset.archive','digital.asset.unarchive','digital.asset.trash','digital.asset.restore')),
  Reason nvarchar(2000) NULL, SafeSnapshotJson nvarchar(max) NOT NULL,
  CONSTRAINT CK_DigitalAssetVersion_Snapshot CHECK(ISJSON(SafeSnapshotJson)=1 AND
    JSON_VALUE(SafeSnapshotJson,'$.schemaVersion') IS NOT NULL AND JSON_VALUE(SafeSnapshotJson,'$.schemaVersion')='1' AND
    JSON_VALUE(SafeSnapshotJson,'$.resourceType') IS NOT NULL AND JSON_VALUE(SafeSnapshotJson,'$.resourceType')='DigitalAsset'),
  CONSTRAINT UQ_DigitalAssetVersion_Owner_Id UNIQUE(OwnerId,Id),
  CONSTRAINT UQ_DigitalAssetVersion_Number UNIQUE(OwnerId,DigitalAssetId,VersionNumber),
  CONSTRAINT FK_DigitalAssetVersion_Asset FOREIGN KEY(OwnerId,DigitalAssetId) REFERENCES [assets].[DigitalAsset](OwnerId,Id));
 CREATE UNIQUE CLUSTERED INDEX CX_DigitalAssetVersion_Owner_Created_Id ON [assets].[DigitalAssetVersion](OwnerId,CreatedAt,Id);
 CREATE INDEX IX_DigitalAssetVersion_Page ON [assets].[DigitalAssetVersion](OwnerId,DigitalAssetId,VersionNumber DESC,Id DESC);
END;
EXEC('CREATE OR ALTER TRIGGER [assets].[TR_DigitalAssetVersion_Immutable] ON [assets].[DigitalAssetVersion] INSTEAD OF UPDATE AS
 BEGIN SET NOCOUNT ON; THROW 51039, ''Asset versions are immutable.'', 1; END');
IF NOT EXISTS(SELECT 1 FROM [platform].[ResourceType] WHERE ModuleId=(SELECT Id FROM [platform].[Module] WHERE Code='FX38') AND Code='DigitalAsset')
 INSERT [platform].[ResourceType](Id,ModuleId,Code,ContractVersion,CapabilitiesJson)
 SELECT NEWID(),Id,'DigitalAsset','digital-assets-v1',N'{"schemaVersion":1,"trash":true,"share":false,"support":false,"search":false}'
 FROM [platform].[Module] WHERE Code='FX38';
INSERT [platform].[Permission](ActionKey,EffectiveStatus)
SELECT s.ActionKey,'Resolved' FROM (VALUES
 ('digital.asset.read'),('digital.asset.create'),('digital.asset.update'),('digital.asset.cancel'),('digital.asset.history'),('digital.renewal.record'),
 ('digital.asset.archive'),('digital.asset.unarchive'),('digital.asset.trash'),('digital.asset.restore'),('digital.asset.purge'))s(ActionKey)
WHERE NOT EXISTS(SELECT 1 FROM [platform].[Permission] WITH(UPDLOCK,HOLDLOCK) WHERE ActionKey=s.ActionKey);
UPDATE [platform].[Module] SET Name=N'Digital Assets — Manual metadata and renewals only',State='Ready',PolicyRevision=PolicyRevision+1,UpdatedAt=SYSUTCDATETIME()
WHERE Code='FX38' AND State='Blocked';
COMMIT;
