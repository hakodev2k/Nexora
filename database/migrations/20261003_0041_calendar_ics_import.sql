SET XACT_ABORT ON;
BEGIN TRANSACTION;
-- The normal migration runner prepares this map with the source-owned Windows/IANA preflight.
IF OBJECT_ID('tempdb..#NexoraCalendarDateMap') IS NULL THROW 51043,'Calendar date migration preflight is required.',1;
IF EXISTS(SELECT 1 FROM [calendar].[Event] e WITH(UPDLOCK,HOLDLOCK) LEFT JOIN #NexoraCalendarDateMap d ON d.Id=e.Id
 WHERE e.IsAllDay=1 AND (d.Id IS NULL OR d.SourceRevision<>e.RowVersion))
 OR EXISTS(SELECT 1 FROM #NexoraCalendarDateMap d LEFT JOIN [calendar].[Event] e ON e.Id=d.Id AND e.IsAllDay=1 WHERE e.Id IS NULL)
 THROW 51043,'Calendar source cohort changed during migration.',1;
IF COL_LENGTH('[calendar].[Event]','StartDate') IS NULL ALTER TABLE [calendar].[Event] ADD StartDate date NULL,EndDateExclusive date NULL;
IF COL_LENGTH('[calendar].[Event]','CalendarUid') IS NULL ALTER TABLE [calendar].[Event] ADD CalendarUid nvarchar(255) NULL;
GO
UPDATE e SET StartDate=d.StartDate,EndDateExclusive=d.EndDateExclusive
FROM [calendar].[Event] e JOIN #NexoraCalendarDateMap d ON d.Id=e.Id
WHERE e.StartDate IS NULL OR e.EndDateExclusive IS NULL;
UPDATE [calendar].[Event] SET CalendarUid=CONVERT(nvarchar(36),NEWID())+N'@nexora.local' WHERE CalendarUid IS NULL;
ALTER TABLE [calendar].[Event] ALTER COLUMN CalendarUid nvarchar(255) NOT NULL;
IF NOT EXISTS(SELECT 1 FROM sys.default_constraints WHERE parent_object_id=OBJECT_ID('[calendar].[Event]') AND name='DF_Event_CalendarUid')
 ALTER TABLE [calendar].[Event] ADD CONSTRAINT DF_Event_CalendarUid DEFAULT(CONVERT(nvarchar(36),NEWID())+N'@nexora.local') FOR CalendarUid;
ALTER TABLE [calendar].[Event] ALTER COLUMN Description nvarchar(max) NULL;
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('[calendar].[Event]') AND name='CK_Event_DateRepresentation')
 ALTER TABLE [calendar].[Event] WITH CHECK ADD CONSTRAINT CK_Event_DateRepresentation CHECK(
  (IsAllDay=0 AND StartDate IS NULL AND EndDateExclusive IS NULL) OR
  (IsAllDay=1 AND StartDate IS NOT NULL AND EndDateExclusive IS NOT NULL AND EndDateExclusive>StartDate));
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('[calendar].[Event]') AND name='UQ_Event_Owner_Id')
 CREATE UNIQUE INDEX UQ_Event_Owner_Id ON [calendar].[Event](OwnerId,Id);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('[calendar].[Event]') AND name='UX_Event_CalendarUid')
 CREATE UNIQUE INDEX UX_Event_CalendarUid ON [calendar].[Event](CalendarUid);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('[files].[FileObject]') AND name='UQ_FileObject_Owner_Id')
 CREATE UNIQUE INDEX UQ_FileObject_Owner_Id ON [files].[FileObject](OwnerId,Id);
IF SCHEMA_ID('operations') IS NULL EXEC('CREATE SCHEMA [operations]');
IF OBJECT_ID('[operations].[ImportBatch]','U') IS NULL
BEGIN
 CREATE TABLE [operations].[ImportBatch](
  Id uniqueidentifier NOT NULL CONSTRAINT PK_ImportBatch PRIMARY KEY NONCLUSTERED,
  OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id),ModuleId uniqueidentifier NOT NULL REFERENCES [platform].[Module](Id),
  Format varchar(64) NOT NULL CHECK(Format='ICS'),FileObjectId uniqueidentifier NOT NULL,
  State varchar(64) NOT NULL CHECK(State IN('PreviewReady','Completed','Canceled')),
  OptionsJson nvarchar(max) NOT NULL CHECK(ISJSON(OptionsJson)=1 AND JSON_VALUE(OptionsJson,'$.schemaVersion') IS NOT NULL AND JSON_VALUE(OptionsJson,'$.schemaVersion')='1' AND DATALENGTH(OptionsJson)<=4096),
  TotalCount int NOT NULL CHECK(TotalCount BETWEEN 0 AND 1000),AcceptedCount int NOT NULL CHECK(AcceptedCount BETWEEN 0 AND 1000),SkippedCount int NOT NULL CHECK(SkippedCount BETWEEN 0 AND 1000),AppliedCount int NOT NULL CHECK(AppliedCount BETWEEN 0 AND 1000),
  CreatedAt datetime2(7) NOT NULL,UpdatedAt datetime2(7) NOT NULL,CreatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id),UpdatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id),RowVersion rowversion NOT NULL,
  CONSTRAINT UQ_ImportBatch_Owner_Id UNIQUE(OwnerId,Id),CONSTRAINT FK_ImportBatch_File FOREIGN KEY(OwnerId,FileObjectId) REFERENCES [files].[FileObject](OwnerId,Id));
 CREATE UNIQUE CLUSTERED INDEX CX_ImportBatch_Owner_Created_Id ON [operations].[ImportBatch](OwnerId,CreatedAt,Id);
 CREATE INDEX IX_ImportBatch_Page ON [operations].[ImportBatch](OwnerId,State,CreatedAt DESC,Id DESC);
END;
IF OBJECT_ID('[operations].[ImportRow]','U') IS NULL
BEGIN
 CREATE TABLE [operations].[ImportRow](
  Id uniqueidentifier NOT NULL CONSTRAINT PK_ImportRow PRIMARY KEY NONCLUSTERED,OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id),BatchId uniqueidentifier NOT NULL,
  RowNumber int NOT NULL CHECK(RowNumber BETWEEN 1 AND 1000),ExternalKeyHash binary(32) NULL,
  Outcome varchar(64) NOT NULL CHECK(Outcome IN('Valid','Invalid','Duplicate','Applied','Failed')),ReasonCode nvarchar(100) NULL,
  ResultResourceId uniqueidentifier NULL,WarningsJson nvarchar(1000) NOT NULL CHECK(ISJSON(WarningsJson)=1),CandidateJson nvarchar(max) NULL,
  CreatedAt datetime2(7) NOT NULL,UpdatedAt datetime2(7) NOT NULL,CreatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id),UpdatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id),RowVersion rowversion NOT NULL,
  CONSTRAINT UQ_ImportRow_Owner_Id UNIQUE(OwnerId,Id),CONSTRAINT UQ_ImportRow_Number UNIQUE(OwnerId,BatchId,RowNumber),
  CONSTRAINT FK_ImportRow_Batch FOREIGN KEY(OwnerId,BatchId) REFERENCES [operations].[ImportBatch](OwnerId,Id),
  CONSTRAINT FK_ImportRow_Result FOREIGN KEY(OwnerId,ResultResourceId) REFERENCES [platform].[Resource](OwnerId,Id),
  CONSTRAINT CK_ImportRow_Candidate CHECK(CandidateJson IS NULL OR(ISJSON(CandidateJson)=1 AND DATALENGTH(CandidateJson)<=262144 AND JSON_VALUE(CandidateJson,'$.schemaVersion') IS NOT NULL AND JSON_VALUE(CandidateJson,'$.schemaVersion')='1')),
  CONSTRAINT CK_ImportRow_Result CHECK((Outcome='Applied' AND ResultResourceId IS NOT NULL) OR(Outcome<>'Applied' AND ResultResourceId IS NULL)));
 CREATE UNIQUE CLUSTERED INDEX CX_ImportRow_Owner_Created_Id ON [operations].[ImportRow](OwnerId,CreatedAt,Id);
 CREATE INDEX IX_ImportRow_Page ON [operations].[ImportRow](OwnerId,BatchId,Outcome,RowNumber);
END;
IF OBJECT_ID('[calendar].[ImportedUid]','U') IS NULL
BEGIN
 CREATE TABLE [calendar].[ImportedUid](
  Id uniqueidentifier NOT NULL CONSTRAINT PK_ImportedUid PRIMARY KEY NONCLUSTERED,OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id),
  Uid nvarchar(1024) COLLATE Latin1_General_100_BIN2 NOT NULL CHECK(LEN(Uid)>0),UidDigest binary(32) NOT NULL,
  ManualEventId uniqueidentifier NOT NULL,ImportBatchId uniqueidentifier NOT NULL,
  CreatedAt datetime2(7) NOT NULL,UpdatedAt datetime2(7) NOT NULL,CreatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id),UpdatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id),RowVersion rowversion NOT NULL,
  CONSTRAINT UQ_ImportedUid_Owner_Id UNIQUE(OwnerId,Id),CONSTRAINT UQ_ImportedUid_Digest UNIQUE(OwnerId,UidDigest),
  CONSTRAINT FK_ImportedUid_Event FOREIGN KEY(OwnerId,ManualEventId) REFERENCES [calendar].[Event](OwnerId,Id),
  CONSTRAINT FK_ImportedUid_Batch FOREIGN KEY(OwnerId,ImportBatchId) REFERENCES [operations].[ImportBatch](OwnerId,Id));
 CREATE UNIQUE CLUSTERED INDEX CX_ImportedUid_Owner_Created_Id ON [calendar].[ImportedUid](OwnerId,CreatedAt,Id);
END;
IF NOT EXISTS(SELECT 1 FROM [platform].[ResourceType] t JOIN [platform].[Module] m ON m.Id=t.ModuleId WHERE m.Code='FX13' AND t.Code='ManualEvent')
 INSERT [platform].[ResourceType](Id,ModuleId,Code,ContractVersion,CapabilitiesJson)
 SELECT NEWID(),Id,'ManualEvent','calendar-ics-import-v1',N'{"schemaVersion":1,"trash":false,"share":false,"support":false,"search":false}' FROM [platform].[Module] WHERE Code='FX13';
INSERT [platform].[Resource](Id,OwnerId,ResourceTypeId,Availability,Revision,CreatedAt,UpdatedAt)
SELECT e.Id,e.OwnerId,t.Id,'Active',1,e.CreatedAt,e.UpdatedAt FROM [calendar].[Event] e
JOIN [platform].[Module] m ON m.Code='FX13' JOIN [platform].[ResourceType] t ON t.ModuleId=m.Id AND t.Code='ManualEvent'
WHERE e.SourceKind='Manual' AND NOT EXISTS(SELECT 1 FROM [platform].[Resource] r WHERE r.Id=e.Id);
INSERT [platform].[Permission](ActionKey,EffectiveStatus)
SELECT s.ActionKey,'Resolved' FROM(VALUES('transfer.import.preview'),('transfer.import.commit'),('transfer.import.read'),('transfer.import.cancel'),('calendar.ics.preview'),('calendar.ics.import'))s(ActionKey)
WHERE NOT EXISTS(SELECT 1 FROM [platform].[Permission] WHERE ActionKey=s.ActionKey);
UPDATE [platform].[Module] SET Name=N'Files — Private Local Upload/Read/Download only',State='Ready',PolicyRevision=PolicyRevision+1,UpdatedAt=SYSUTCDATETIME()
WHERE Code='FX07' AND State='Blocked';
UPDATE [platform].[Module] SET Name=N'Import / Export — Calendar ICS Import only',State='Ready',PolicyRevision=PolicyRevision+1,UpdatedAt=SYSUTCDATETIME()
WHERE Code='FX10' AND State='Blocked';
COMMIT;
