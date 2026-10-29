SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID('[learning].[Course]', 'U') IS NULL
BEGIN
 CREATE TABLE [learning].[Course](
  Id uniqueidentifier NOT NULL CONSTRAINT PK_Course PRIMARY KEY NONCLUSTERED,
  OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id),
  Title nvarchar(200) NOT NULL CHECK(LEN(LTRIM(RTRIM(Title)))>0), Provider nvarchar(200) NULL, Url nvarchar(2048) NULL,
  ProgressMode varchar(64) NOT NULL CHECK(ProgressMode IN ('ManualPercent','Milestones')),
  ManualProgress decimal(28,8) NULL, FirstProgressAt datetime2(7) NULL,
  Status varchar(64) NOT NULL CHECK(Status IN ('Planned','InProgress','Completed','Abandoned','Archived','Trash')),
  StartedOn date NULL, CompletedOn date NULL, Notes nvarchar(max) NULL,
  PreArchiveState varchar(64) NULL, PreTrashState varchar(64) NULL, TrashBatchId uniqueidentifier NULL,
  TrashMilestoneFingerprint binary(32) NULL,
  CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt datetime2(7) NOT NULL,
  CreatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id),
  UpdatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id), RowVersion rowversion NOT NULL,
  CONSTRAINT UQ_Course_Owner_Id UNIQUE(OwnerId,Id),
  CONSTRAINT FK_Course_Resource FOREIGN KEY(OwnerId,Id) REFERENCES [platform].[Resource](OwnerId,Id),
  CONSTRAINT FK_Course_TrashBatch FOREIGN KEY(OwnerId,TrashBatchId) REFERENCES [operations].[TrashBatch](OwnerId,Id),
  CONSTRAINT CK_Course_Manual CHECK((ProgressMode='Milestones' AND ManualProgress IS NULL) OR
    (ProgressMode='ManualPercent' AND (ManualProgress IS NULL OR (ManualProgress>=0 AND ManualProgress<=1 AND FirstProgressAt IS NOT NULL)))),
  CONSTRAINT CK_Course_Dates CHECK(StartedOn IS NULL OR CompletedOn IS NULL OR CompletedOn>=StartedOn),
  CONSTRAINT CK_Course_Completion CHECK(
    ((Status='Completed' OR COALESCE(PreArchiveState,'')='Completed' OR COALESCE(PreTrashState,'')='Completed') AND CompletedOn IS NOT NULL) OR
    (Status<>'Completed' AND COALESCE(PreArchiveState,'')<>'Completed' AND COALESCE(PreTrashState,'')<>'Completed' AND CompletedOn IS NULL)),
  CONSTRAINT CK_Course_Archive CHECK(
    (Status='Archived' AND PreArchiveState IS NOT NULL AND PreArchiveState IN ('Planned','InProgress','Completed','Abandoned')) OR
    (Status='Trash' AND ((PreTrashState='Archived' AND PreArchiveState IS NOT NULL AND PreArchiveState IN ('Planned','InProgress','Completed','Abandoned')) OR
      (PreTrashState IN ('Planned','InProgress','Completed','Abandoned') AND PreArchiveState IS NULL))) OR
    (Status IN ('Planned','InProgress','Completed','Abandoned') AND PreArchiveState IS NULL)),
  CONSTRAINT CK_Course_Trash CHECK((Status='Trash' AND TrashBatchId IS NOT NULL AND TrashMilestoneFingerprint IS NOT NULL AND
    PreTrashState IS NOT NULL AND PreTrashState IN ('Planned','InProgress','Completed','Abandoned','Archived')) OR
    (Status<>'Trash' AND TrashBatchId IS NULL AND PreTrashState IS NULL AND TrashMilestoneFingerprint IS NULL)));
 CREATE UNIQUE CLUSTERED INDEX CX_Course_Owner_Created_Id ON [learning].[Course](OwnerId,CreatedAt,Id);
 CREATE INDEX IX_Course_Page ON [learning].[Course](OwnerId,Status,UpdatedAt DESC,Id DESC);
END;
IF OBJECT_ID('[learning].[CourseMilestone]', 'U') IS NULL
BEGIN
 CREATE TABLE [learning].[CourseMilestone](
  Id uniqueidentifier NOT NULL CONSTRAINT PK_CourseMilestone PRIMARY KEY NONCLUSTERED,
  OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id), CourseId uniqueidentifier NOT NULL,
  Title nvarchar(200) NOT NULL CHECK(LEN(LTRIM(RTRIM(Title)))>0), Position int NOT NULL CHECK(Position>=0),
  Completed bit NOT NULL, CompletedAt datetime2(7) NULL,
  CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt datetime2(7) NOT NULL,
  CreatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id),
  UpdatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id), RowVersion rowversion NOT NULL,
  CONSTRAINT UQ_CourseMilestone_Owner_Id UNIQUE(OwnerId,Id),
  CONSTRAINT UQ_CourseMilestone_Position UNIQUE(OwnerId,CourseId,Position),
  CONSTRAINT FK_CourseMilestone_Course FOREIGN KEY(OwnerId,CourseId) REFERENCES [learning].[Course](OwnerId,Id),
  CONSTRAINT CK_CourseMilestone_Completion CHECK((Completed=0 AND CompletedAt IS NULL) OR (Completed=1 AND CompletedAt IS NOT NULL)));
 CREATE UNIQUE CLUSTERED INDEX CX_CourseMilestone_Owner_Created_Id ON [learning].[CourseMilestone](OwnerId,CreatedAt,Id);
END;
IF OBJECT_ID('[learning].[CourseCompletionHistory]', 'U') IS NULL
 CREATE TABLE [learning].[CourseCompletionHistory](
  Id uniqueidentifier NOT NULL PRIMARY KEY, OwnerId uniqueidentifier NOT NULL, CourseId uniqueidentifier NOT NULL,
  CompletedOn date NOT NULL, RecordedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
  ActorUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id),
  CONSTRAINT FK_CourseHistory_Course FOREIGN KEY(OwnerId,CourseId) REFERENCES [learning].[Course](OwnerId,Id));
IF NOT EXISTS(SELECT 1 FROM [platform].[ResourceType] WHERE ModuleId=(SELECT Id FROM [platform].[Module] WHERE Code='FX40') AND Code='Course')
 INSERT [platform].[ResourceType](Id,ModuleId,Code,ContractVersion,CapabilitiesJson)
 SELECT NEWID(),Id,'Course','courses-v1',N'{"schemaVersion":1,"trash":true,"share":false,"support":false,"search":false}'
 FROM [platform].[Module] WHERE Code='FX40';
INSERT [platform].[Permission](ActionKey,EffectiveStatus)
SELECT s.ActionKey,'Resolved' FROM (VALUES
 ('learning.course.read'),('learning.course.create'),('learning.course.update'),('learning.course.progress'),('learning.course.milestone'),
 ('learning.course.complete'),('learning.course.abandon'),('learning.course.archive'),('learning.course.unarchive'),
 ('learning.course.trash'),('learning.course.restore'),('learning.course.purge'))s(ActionKey)
WHERE NOT EXISTS(SELECT 1 FROM [platform].[Permission] WITH(UPDLOCK,HOLDLOCK) WHERE ActionKey=s.ActionKey);
UPDATE [platform].[Module] SET Name=N'Learning — Skills and Courses only',PolicyRevision=PolicyRevision+1,UpdatedAt=SYSUTCDATETIME()
WHERE Code='FX40' AND State='Ready' AND Name=N'Learning — Skills only';
COMMIT;
