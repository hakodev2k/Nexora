SET XACT_ABORT ON;
BEGIN TRANSACTION;
-- Synchronous local ICS output is Operations-private; never an ordinary Files object.
IF OBJECT_ID('[operations].[ExportJob]','U') IS NULL
BEGIN
 CREATE TABLE [operations].[ExportJob](
  Id uniqueidentifier NOT NULL CONSTRAINT PK_ExportJob PRIMARY KEY NONCLUSTERED,
  OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id),ModuleId uniqueidentifier NOT NULL REFERENCES [platform].[Module](Id),
  Format varchar(64) NOT NULL CHECK(Format='ICS'),FilterJson nvarchar(4000) NOT NULL,
  State varchar(64) NOT NULL CHECK(State IN('Queued','Running','Ready','Failed','Expired')),
  FileObjectId uniqueidentifier NULL,ExpiresAt datetime2(7) NOT NULL,SourceWatermark nvarchar(200) NOT NULL,
  TimeZoneId nvarchar(128) NOT NULL,EventCount int NOT NULL CHECK(EventCount BETWEEN 0 AND 1000),
  CreatedAt datetime2(7) NOT NULL,UpdatedAt datetime2(7) NOT NULL,CreatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id),UpdatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id),RowVersion rowversion NOT NULL,
  CONSTRAINT UQ_ExportJob_Owner_Id UNIQUE(OwnerId,Id),
  CONSTRAINT FK_ExportJob_File FOREIGN KEY(OwnerId,FileObjectId) REFERENCES [files].[FileObject](OwnerId,Id),
  CONSTRAINT CK_ExportJob_PrivateOutput CHECK(FileObjectId IS NULL AND ExpiresAt>CreatedAt),
  CONSTRAINT CK_ExportJob_Filter CHECK(ISJSON(FilterJson)=1 AND JSON_VALUE(FilterJson,'$.schemaVersion') IS NOT NULL AND JSON_VALUE(FilterJson,'$.schemaVersion')='1'
    AND JSON_VALUE(FilterJson,'$.containmentMode') IS NOT NULL AND JSON_VALUE(FilterJson,'$.containmentMode')='FullyContained'));
 CREATE UNIQUE CLUSTERED INDEX CX_ExportJob_Owner_Created_Id ON [operations].[ExportJob](OwnerId,CreatedAt,Id);
 CREATE INDEX IX_ExportJob_Page ON [operations].[ExportJob](OwnerId,CreatedAt DESC,Id DESC);
END;
IF OBJECT_ID('[operations].[ExportArtifact]','U') IS NULL
BEGIN
 CREATE TABLE [operations].[ExportArtifact](OwnerId uniqueidentifier NOT NULL,JobId uniqueidentifier NOT NULL,Content varbinary(max) NOT NULL,
  CONSTRAINT PK_ExportArtifact PRIMARY KEY(OwnerId,JobId),
  CONSTRAINT FK_ExportArtifact_Job FOREIGN KEY(OwnerId,JobId) REFERENCES [operations].[ExportJob](OwnerId,Id),
  CONSTRAINT CK_ExportArtifact_Bounds CHECK(DATALENGTH(Content) BETWEEN 1 AND 1048576));
END;
IF OBJECT_ID('[operations].[ExportSource]','U') IS NULL
BEGIN
 CREATE TABLE [operations].[ExportSource](OwnerId uniqueidentifier NOT NULL,JobId uniqueidentifier NOT NULL,EventId uniqueidentifier NOT NULL,
  SourceKind varchar(16) NOT NULL,TaskId uniqueidentifier NULL,ProjectId uniqueidentifier NULL,
  CONSTRAINT PK_ExportSource PRIMARY KEY(OwnerId,JobId,EventId),
  CONSTRAINT FK_ExportSource_Job FOREIGN KEY(OwnerId,JobId) REFERENCES [operations].[ExportJob](OwnerId,Id),
  CONSTRAINT CK_ExportSource_Kind CHECK((SourceKind='Manual' AND TaskId IS NULL AND ProjectId IS NULL) OR(SourceKind='Task' AND TaskId IS NOT NULL AND ProjectId IS NOT NULL)));
 -- Source identities are private inventory, not source pins. Deleted sources make artifacts unavailable.
END;
INSERT [platform].[Permission](ActionKey,EffectiveStatus)
SELECT s.ActionKey,'Resolved' FROM(VALUES('transfer.export.request'),('transfer.export.read'),('transfer.export.download'),('calendar.ics.export'))s(ActionKey)
WHERE NOT EXISTS(SELECT 1 FROM [platform].[Permission] WHERE ActionKey=s.ActionKey);
UPDATE [platform].[Module] SET Name=N'Import / Export — Calendar ICS local',PolicyRevision=PolicyRevision+1,UpdatedAt=SYSUTCDATETIME()
WHERE Code='FX10' AND State='Ready';
COMMIT;
