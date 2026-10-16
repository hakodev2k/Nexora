SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF SCHEMA_ID('learning') IS NULL EXEC('CREATE SCHEMA [learning]');
IF OBJECT_ID('[learning].[Skill]', 'U') IS NULL
BEGIN
 CREATE TABLE [learning].[Skill](
  Id uniqueidentifier NOT NULL CONSTRAINT PK_Skill PRIMARY KEY NONCLUSTERED,
  OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id),
  Title nvarchar(200) NOT NULL CHECK(LEN(LTRIM(RTRIM(Title)))>0),
  NormalizedTitle nvarchar(200) COLLATE Latin1_General_100_BIN2 NOT NULL CHECK(LEN(NormalizedTitle)>0),
  Level varchar(64) NOT NULL CHECK(Level IN ('Beginner','Intermediate','Advanced','Expert')),
  Description nvarchar(max) NULL, Category nvarchar(200) NULL, LastUsed date NULL,
  Status varchar(64) NOT NULL CHECK(Status IN ('Active','Archived','Trash')),
  PreArchiveState varchar(64) NULL, PreTrashState varchar(64) NULL, TrashBatchId uniqueidentifier NULL,
  MergedIntoId uniqueidentifier NULL CONSTRAINT CK_Skill_MergeUnavailable CHECK(MergedIntoId IS NULL),
  CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt datetime2(7) NOT NULL,
  CreatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id),
  UpdatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id), RowVersion rowversion NOT NULL,
  CONSTRAINT UQ_Skill_Owner_Id UNIQUE(OwnerId,Id),
  CONSTRAINT UQ_Skill_NormalizedTitle UNIQUE(OwnerId,NormalizedTitle),
  CONSTRAINT FK_Skill_Resource FOREIGN KEY(OwnerId,Id) REFERENCES [platform].[Resource](OwnerId,Id),
  CONSTRAINT FK_Skill_TrashBatch FOREIGN KEY(OwnerId,TrashBatchId) REFERENCES [operations].[TrashBatch](OwnerId,Id),
  CONSTRAINT FK_Skill_MergedInto FOREIGN KEY(OwnerId,MergedIntoId) REFERENCES [learning].[Skill](OwnerId,Id),
  CONSTRAINT CK_Skill_Archive CHECK((Status='Archived' AND PreArchiveState IS NOT NULL AND PreArchiveState='Active') OR
      (Status='Trash' AND ((PreTrashState='Archived' AND PreArchiveState IS NOT NULL AND PreArchiveState='Active') OR
          (PreTrashState='Active' AND PreArchiveState IS NULL))) OR
      (Status='Active' AND PreArchiveState IS NULL)),
  CONSTRAINT CK_Skill_Trash CHECK((Status='Trash' AND TrashBatchId IS NOT NULL AND PreTrashState IS NOT NULL AND PreTrashState IN ('Active','Archived')) OR
      (Status<>'Trash' AND TrashBatchId IS NULL AND PreTrashState IS NULL)));
 CREATE UNIQUE CLUSTERED INDEX CX_Skill_Owner_Created_Id ON [learning].[Skill](OwnerId,CreatedAt,Id);
 CREATE INDEX IX_Skill_Page ON [learning].[Skill](OwnerId,Status,UpdatedAt DESC,Id DESC);
END;
IF NOT EXISTS(SELECT 1 FROM [platform].[ResourceType] WHERE ModuleId=(SELECT Id FROM [platform].[Module] WHERE Code='FX40') AND Code='Skill')
 INSERT [platform].[ResourceType](Id,ModuleId,Code,ContractVersion,CapabilitiesJson)
 SELECT NEWID(),Id,'Skill','skills-v1',N'{"schemaVersion":1,"trash":true,"share":false,"support":false,"search":false}'
 FROM [platform].[Module] WHERE Code='FX40';
INSERT [platform].[Permission](ActionKey,EffectiveStatus)
SELECT s.ActionKey,'Resolved' FROM (VALUES
 ('learning.skill.read'),('learning.skill.create'),('learning.skill.update'),('learning.skill.proficiency'),
 ('learning.skill.archive'),('learning.skill.unarchive'),('learning.skill.trash'),('learning.skill.restore'),('learning.skill.purge'))s(ActionKey)
WHERE NOT EXISTS(SELECT 1 FROM [platform].[Permission] WITH(UPDLOCK,HOLDLOCK) WHERE ActionKey=s.ActionKey);
UPDATE [platform].[Module] SET Name=N'Learning — Skills only',State='Ready',PolicyRevision=PolicyRevision+1,UpdatedAt=SYSUTCDATETIME()
WHERE Code='FX40' AND State='Blocked';
COMMIT;
