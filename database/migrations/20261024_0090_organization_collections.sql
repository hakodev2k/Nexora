SET XACT_ABORT ON;
GO
BEGIN TRANSACTION;
GO
IF (OBJECT_ID(N'organization.Collection',N'U') IS NOT NULL AND NOT EXISTS(
 SELECT 1 FROM sys.extended_properties WHERE major_id=OBJECT_ID(N'organization.Collection') AND minor_id=0
 AND name=N'Nexora.Collection.Contract' AND CONVERT(nvarchar(100),value)=N'collections-v1'))
 OR (OBJECT_ID(N'organization.CollectionMember',N'U') IS NOT NULL AND NOT EXISTS(
 SELECT 1 FROM sys.extended_properties WHERE major_id=OBJECT_ID(N'organization.CollectionMember') AND minor_id=0
 AND name=N'Nexora.Collection.Contract' AND CONVERT(nvarchar(100),value)=N'collections-v1'))
 THROW 51900,'Unexpected existing Collection schema requires explicit reconciliation.',1;
GO
IF OBJECT_ID(N'organization.Collection', N'U') IS NULL
BEGIN
 CREATE TABLE organization.Collection (
  Id uniqueidentifier NOT NULL,
  OwnerId uniqueidentifier NOT NULL,
  Name nvarchar(100) NOT NULL,
  Description nvarchar(4000) NULL,
  CreatedByUserId uniqueidentifier NOT NULL,
  UpdatedByUserId uniqueidentifier NOT NULL,
  CreatedAt datetime2(7) NOT NULL CONSTRAINT DF_Collection_Created DEFAULT SYSUTCDATETIME(),
  UpdatedAt datetime2(7) NOT NULL CONSTRAINT DF_Collection_Updated DEFAULT SYSUTCDATETIME(),
  RowVersion rowversion NOT NULL,
  CONSTRAINT PK_Collection PRIMARY KEY(Id),
  CONSTRAINT UQ_Collection_Owner UNIQUE(OwnerId,Id),
  CONSTRAINT FK_Collection_Owner FOREIGN KEY(OwnerId) REFERENCES platform.PersonalSpace(Id),
  CONSTRAINT FK_Collection_Creator FOREIGN KEY(CreatedByUserId) REFERENCES [identity].[User](Id),
  CONSTRAINT FK_Collection_Updater FOREIGN KEY(UpdatedByUserId) REFERENCES [identity].[User](Id),
  CONSTRAINT CK_Collection_Name CHECK(LEN(LTRIM(RTRIM(Name))) BETWEEN 1 AND 100)
 );
 CREATE INDEX IX_Collection_Browse ON organization.Collection(OwnerId,Name,Id);
 EXEC sys.sp_addextendedproperty @name=N'Nexora.Collection.Contract',@value=N'collections-v1',
  @level0type=N'SCHEMA',@level0name=N'organization',@level1type=N'TABLE',@level1name=N'Collection';
END;
GO
IF OBJECT_ID(N'organization.CollectionMember', N'U') IS NULL
BEGIN
 CREATE TABLE organization.CollectionMember (
  Id uniqueidentifier NOT NULL,
  OwnerId uniqueidentifier NOT NULL,
  CollectionId uniqueidentifier NOT NULL,
  ResourceType varchar(32) COLLATE Latin1_General_100_BIN2 NOT NULL,
  ResourceId uniqueidentifier NOT NULL,
  Rank decimal(28,8) NOT NULL,
  CreatedByUserId uniqueidentifier NOT NULL,
  CreatedAt datetime2(7) NOT NULL CONSTRAINT DF_CollectionMember_Created DEFAULT SYSUTCDATETIME(),
  CONSTRAINT PK_CollectionMember PRIMARY KEY(Id),
  CONSTRAINT UQ_CollectionMember_Source UNIQUE(OwnerId,CollectionId,ResourceType,ResourceId),
  CONSTRAINT FK_CollectionMember_Collection FOREIGN KEY(OwnerId,CollectionId) REFERENCES organization.Collection(OwnerId,Id),
  CONSTRAINT FK_CollectionMember_Creator FOREIGN KEY(CreatedByUserId) REFERENCES [identity].[User](Id),
  CONSTRAINT CK_CollectionMember_Type CHECK(ResourceType IN('Project','Document','Bookmark','Snippet')),
  CONSTRAINT CK_CollectionMember_Rank CHECK(Rank >= 0)
 );
 CREATE INDEX IX_CollectionMember_Order ON organization.CollectionMember(OwnerId,CollectionId,Rank,Id);
 EXEC sys.sp_addextendedproperty @name=N'Nexora.Collection.Contract',@value=N'collections-v1',
  @level0type=N'SCHEMA',@level0name=N'organization',@level1type=N'TABLE',@level1name=N'CollectionMember';
END;
GO
INSERT platform.Permission(ActionKey,EffectiveStatus)
SELECT source.ActionKey,'Resolved' FROM (VALUES(N'organization.collection.read'),(N'organization.collection.create'),
 (N'organization.collection.update'),(N'organization.collection.delete'),
 (N'organization.collection.add'),(N'organization.collection.remove'),(N'organization.collection.reorder')) source(ActionKey)
WHERE NOT EXISTS(SELECT 1 FROM platform.Permission currentPermission WITH(UPDLOCK,HOLDLOCK)
 WHERE currentPermission.ActionKey=source.ActionKey);
GO
COMMIT TRANSACTION;
GO
