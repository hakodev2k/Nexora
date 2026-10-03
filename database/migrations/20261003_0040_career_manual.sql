SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF SCHEMA_ID('career') IS NULL EXEC('CREATE SCHEMA [career]');
IF OBJECT_ID('[career].[Company]','U') IS NULL
BEGIN
 CREATE TABLE [career].[Company](
  Id uniqueidentifier NOT NULL CONSTRAINT PK_CareerCompany PRIMARY KEY NONCLUSTERED,
  OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id),
  Title nvarchar(200) NOT NULL CHECK(LEN(LTRIM(RTRIM(Title)))>0), Url nvarchar(2048) NULL,
  Industry nvarchar(200) NULL, Location nvarchar(200) NULL, Notes nvarchar(max) NULL CHECK(Notes IS NULL OR DATALENGTH(Notes)<=40000),
  MergedIntoId uniqueidentifier NULL,
  CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt datetime2(7) NOT NULL,
  CreatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id),
  UpdatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id), RowVersion rowversion NOT NULL,
  CONSTRAINT UQ_CareerCompany_Owner_Id UNIQUE(OwnerId,Id),
  CONSTRAINT CK_CareerCompany_Merged CHECK(MergedIntoId IS NULL OR MergedIntoId<>Id),
  CONSTRAINT FK_CareerCompany_Resource FOREIGN KEY(OwnerId,Id) REFERENCES [platform].[Resource](OwnerId,Id),
  CONSTRAINT FK_CareerCompany_Merged FOREIGN KEY(OwnerId,MergedIntoId) REFERENCES [career].[Company](OwnerId,Id));
 CREATE UNIQUE CLUSTERED INDEX CX_CareerCompany_Owner_Created_Id ON [career].[Company](OwnerId,CreatedAt,Id);
 CREATE INDEX IX_CareerCompany_Name ON [career].[Company](OwnerId,Title,Id);
END;
IF OBJECT_ID('[career].[JobApplication]','U') IS NULL
BEGIN
 CREATE TABLE [career].[JobApplication](
  Id uniqueidentifier NOT NULL CONSTRAINT PK_JobApplication PRIMARY KEY NONCLUSTERED,
  OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id),
  Title nvarchar(200) NOT NULL CHECK(LEN(LTRIM(RTRIM(Title)))>0), CompanyId uniqueidentifier NULL,
  Url nvarchar(2048) NULL, Location nvarchar(200) NULL, WorkMode varchar(64) NULL CHECK(WorkMode IS NULL OR WorkMode IN ('Onsite','Hybrid','Remote')),
  EmploymentType nvarchar(100) NULL, SalaryText nvarchar(1000) NULL,
  SalaryMin decimal(28,8) NULL, SalaryMax decimal(28,8) NULL, Currency char(3) NULL,
  Description nvarchar(max) NULL CHECK(Description IS NULL OR DATALENGTH(Description)<=40000),
  Notes nvarchar(max) NULL CHECK(Notes IS NULL OR DATALENGTH(Notes)<=40000), Source nvarchar(200) NULL,
  DiscoveredOn date NULL, AppliedOn date NULL,
  Stage varchar(64) NOT NULL CHECK(Stage IN ('Saved','Preparing','Applied','Screening','Interviewing','Offer','Accepted','Rejected','Withdrawn','Closed')), StageChangedAt datetime2(7) NOT NULL,
  IsTrash bit NOT NULL, PreTrashStage varchar(64) NULL, TrashBatchId uniqueidentifier NULL, TrashFingerprint binary(32) NULL,
  CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt datetime2(7) NOT NULL,
  CreatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id),
  UpdatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id), RowVersion rowversion NOT NULL,
  CONSTRAINT UQ_JobApplication_Owner_Id UNIQUE(OwnerId,Id),
  CONSTRAINT FK_JobApplication_Resource FOREIGN KEY(OwnerId,Id) REFERENCES [platform].[Resource](OwnerId,Id),
  CONSTRAINT FK_JobApplication_Company FOREIGN KEY(OwnerId,CompanyId) REFERENCES [career].[Company](OwnerId,Id),
  CONSTRAINT FK_JobApplication_TrashBatch FOREIGN KEY(OwnerId,TrashBatchId) REFERENCES [operations].[TrashBatch](OwnerId,Id),
  CONSTRAINT CK_JobApplication_Salary CHECK(
    (SalaryMin IS NULL OR SalaryMax IS NULL OR SalaryMin<=SalaryMax) AND
    ((SalaryMin IS NULL AND SalaryMax IS NULL) OR Currency IS NOT NULL) AND
    (Currency IS NULL OR Currency COLLATE Latin1_General_100_BIN2 LIKE '[A-Z][A-Z][A-Z]')),
  CONSTRAINT CK_JobApplication_Trash CHECK(
    (IsTrash=0 AND PreTrashStage IS NULL AND TrashBatchId IS NULL AND TrashFingerprint IS NULL) OR
    (IsTrash=1 AND PreTrashStage IS NOT NULL AND PreTrashStage IN ('Saved','Preparing','Applied','Screening','Interviewing','Offer','Accepted','Rejected','Withdrawn','Closed') AND PreTrashStage=Stage AND TrashBatchId IS NOT NULL AND TrashFingerprint IS NOT NULL)));
 CREATE UNIQUE CLUSTERED INDEX CX_JobApplication_Owner_Created_Id ON [career].[JobApplication](OwnerId,CreatedAt,Id);
 CREATE INDEX IX_JobApplication_Page ON [career].[JobApplication](OwnerId,IsTrash,UpdatedAt DESC,Id DESC) INCLUDE(Stage,CompanyId);
 CREATE INDEX IX_JobApplication_Company ON [career].[JobApplication](OwnerId,CompanyId,Id) INCLUDE(IsTrash);
END;
IF OBJECT_ID('[career].[ApplicationEvent]','U') IS NULL
BEGIN
 CREATE TABLE [career].[ApplicationEvent](
  Id uniqueidentifier NOT NULL CONSTRAINT PK_ApplicationEvent PRIMARY KEY NONCLUSTERED,
  OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id), ApplicationId uniqueidentifier NOT NULL,
  VersionNumber bigint NOT NULL CHECK(VersionNumber>0),
  ActionKey varchar(160) NOT NULL CHECK(ActionKey IN ('career.job.create','career.job.update','career.job.transition','career.job.trash','career.job.restore','career.company.merge')),
  FromStage varchar(64) NULL CHECK(FromStage IS NULL OR FromStage IN ('Saved','Preparing','Applied','Screening','Interviewing','Offer','Accepted','Rejected','Withdrawn','Closed')),
  ToStage varchar(64) NOT NULL CHECK(ToStage IN ('Saved','Preparing','Applied','Screening','Interviewing','Offer','Accepted','Rejected','Withdrawn','Closed')), OccurredAt datetime2(7) NOT NULL,
  Note nvarchar(2000) NULL, CompanyLabelSnapshot nvarchar(200) NULL, SnapshotJson nvarchar(max) NOT NULL,
  CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(), CreatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id), RowVersion rowversion NOT NULL,
  CONSTRAINT UQ_ApplicationEvent_Owner_Id UNIQUE(OwnerId,Id),
  CONSTRAINT UQ_ApplicationEvent_Version UNIQUE(OwnerId,ApplicationId,VersionNumber),
  CONSTRAINT FK_ApplicationEvent_Job FOREIGN KEY(OwnerId,ApplicationId) REFERENCES [career].[JobApplication](OwnerId,Id),
  CONSTRAINT CK_ApplicationEvent_Snapshot CHECK(ISJSON(SnapshotJson)=1 AND
    JSON_VALUE(SnapshotJson,'$.schemaVersion') IS NOT NULL AND JSON_VALUE(SnapshotJson,'$.schemaVersion')='1' AND
    JSON_VALUE(SnapshotJson,'$.resourceType') IS NOT NULL AND JSON_VALUE(SnapshotJson,'$.resourceType')='JobApplication'));
 CREATE UNIQUE CLUSTERED INDEX CX_ApplicationEvent_Owner_Created_Id ON [career].[ApplicationEvent](OwnerId,CreatedAt,Id);
 CREATE INDEX IX_ApplicationEvent_Page ON [career].[ApplicationEvent](OwnerId,ApplicationId,VersionNumber DESC,Id DESC);
END;
EXEC('CREATE OR ALTER TRIGGER [career].[TR_ApplicationEvent_Immutable] ON [career].[ApplicationEvent] INSTEAD OF UPDATE AS
 BEGIN SET NOCOUNT ON; THROW 51042, ''Application events are immutable.'', 1; END');
IF NOT EXISTS(SELECT 1 FROM [platform].[ResourceType] WHERE ModuleId=(SELECT Id FROM [platform].[Module] WHERE Code='FX39') AND Code='Company')
 INSERT [platform].[ResourceType](Id,ModuleId,Code,ContractVersion,CapabilitiesJson)
 SELECT NEWID(),Id,'Company','career-manual-v1',N'{"schemaVersion":1,"trash":false,"share":false,"support":false,"search":false}'
 FROM [platform].[Module] WHERE Code='FX39';
IF NOT EXISTS(SELECT 1 FROM [platform].[ResourceType] WHERE ModuleId=(SELECT Id FROM [platform].[Module] WHERE Code='FX39') AND Code='JobApplication')
 INSERT [platform].[ResourceType](Id,ModuleId,Code,ContractVersion,CapabilitiesJson)
 SELECT NEWID(),Id,'JobApplication','career-manual-v1',N'{"schemaVersion":1,"trash":true,"share":false,"support":false,"search":false}'
 FROM [platform].[Module] WHERE Code='FX39';
INSERT [platform].[Permission](ActionKey,EffectiveStatus)
SELECT s.ActionKey,'Resolved' FROM (VALUES ('career.company.read'),('career.company.create'),('career.company.update'),('career.company.merge'),('career.job.read'),('career.job.create'),('career.job.update'),('career.job.transition'),('career.job.history'),('career.job.trash'),('career.job.restore'),('career.job.purge'))s(ActionKey)
WHERE NOT EXISTS(SELECT 1 FROM [platform].[Permission] WITH(UPDLOCK,HOLDLOCK) WHERE ActionKey=s.ActionKey);
UPDATE [platform].[Module] SET Name=N'Career — Manual Companies and Jobs only',State='Ready',PolicyRevision=PolicyRevision+1,UpdatedAt=SYSUTCDATETIME()
WHERE Code='FX39' AND State='Blocked';
COMMIT;
