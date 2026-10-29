SET XACT_ABORT ON;
GO
BEGIN TRANSACTION;
GO
IF OBJECT_ID(N'organization.Template',N'U') IS NOT NULL AND NOT EXISTS(
 SELECT 1 FROM sys.extended_properties WHERE major_id=OBJECT_ID(N'organization.Template') AND minor_id=0
 AND name=N'Nexora.Template.Contract' AND CONVERT(nvarchar(100),value)=N'bookmark-template-v1')
 THROW 51930,'Unexpected existing Template schema requires explicit reconciliation.',1;
GO
IF OBJECT_ID(N'organization.Template',N'U') IS NULL
BEGIN
 CREATE TABLE organization.Template (
  Id uniqueidentifier NOT NULL,
  OwnerId uniqueidentifier NOT NULL,
  Name nvarchar(100) NOT NULL,
  ModuleCode varchar(16) COLLATE Latin1_General_100_BIN2 NOT NULL,
  ResourceType varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL,
  SchemaVersion int NOT NULL,
  SeedJson nvarchar(max) NOT NULL,
  Status varchar(16) COLLATE Latin1_General_100_BIN2 NOT NULL,
  PreTrashStatus varchar(16) COLLATE Latin1_General_100_BIN2 NULL,
  DeletionBatchId uniqueidentifier NULL,
  CreatedByUserId uniqueidentifier NOT NULL,
  UpdatedByUserId uniqueidentifier NOT NULL,
  CreatedAt datetime2(7) NOT NULL CONSTRAINT DF_Template_Created DEFAULT SYSUTCDATETIME(),
  UpdatedAt datetime2(7) NOT NULL CONSTRAINT DF_Template_Updated DEFAULT SYSUTCDATETIME(),
  RowVersion rowversion NOT NULL,
  CONSTRAINT PK_Template PRIMARY KEY(Id),
  CONSTRAINT UQ_Template_Owner UNIQUE(OwnerId,Id),
  CONSTRAINT FK_Template_Owner FOREIGN KEY(OwnerId) REFERENCES platform.PersonalSpace(Id),
  CONSTRAINT FK_Template_Creator FOREIGN KEY(CreatedByUserId) REFERENCES [identity].[User](Id),
  CONSTRAINT FK_Template_Updater FOREIGN KEY(UpdatedByUserId) REFERENCES [identity].[User](Id),
  CONSTRAINT CK_Template_Name CHECK(LEN(LTRIM(RTRIM(Name))) BETWEEN 1 AND 100),
  CONSTRAINT CK_Template_Provider CHECK(ModuleCode='FX21' AND ResourceType='Bookmark' AND SchemaVersion=1),
  CONSTRAINT CK_Template_Seed CHECK(ISJSON(SeedJson)=1 AND DATALENGTH(SeedJson)<=131072),
  CONSTRAINT CK_Template_State CHECK((Status IN('Active','Archived') AND PreTrashStatus IS NULL AND DeletionBatchId IS NULL)
    OR(Status='Trash' AND PreTrashStatus IS NOT NULL AND PreTrashStatus IN('Active','Archived') AND DeletionBatchId IS NOT NULL))
 );
 CREATE INDEX IX_Template_Browse ON organization.Template(OwnerId,Status,Name,Id);
 EXEC sys.sp_addextendedproperty @name=N'Nexora.Template.Contract',@value=N'bookmark-template-v1',
  @level0type=N'SCHEMA',@level0name=N'organization',@level1type=N'TABLE',@level1name=N'Template';
END;
GO
INSERT platform.Permission(ActionKey,EffectiveStatus)
SELECT source.ActionKey,'Resolved' FROM (VALUES(N'organization.template.read'),(N'organization.template.create'),
 (N'organization.template.update'),(N'organization.template.archive'),(N'organization.template.unarchive'),
 (N'organization.template.trash'),(N'organization.template.restore'),(N'organization.template.purge'),
 (N'organization.template.instantiate')) source(ActionKey)
WHERE NOT EXISTS(SELECT 1 FROM platform.Permission existing WITH(UPDLOCK,HOLDLOCK) WHERE existing.ActionKey=source.ActionKey);
GO
COMMIT TRANSACTION;
GO
