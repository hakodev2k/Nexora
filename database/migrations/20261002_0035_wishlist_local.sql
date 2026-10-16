SET XACT_ABORT ON;
BEGIN TRANSACTION;
-- Main's identity-only resource directory; private content remains in typed domain tables.
IF OBJECT_ID('[platform].[ResourceType]', 'U') IS NULL
BEGIN
 CREATE TABLE [platform].[ResourceType](
  Id uniqueidentifier NOT NULL PRIMARY KEY,
  ModuleId uniqueidentifier NOT NULL REFERENCES [platform].[Module](Id),
  Code nvarchar(100) NOT NULL, ContractVersion nvarchar(50) NOT NULL,
  CapabilitiesJson nvarchar(max) NOT NULL CHECK(ISJSON(CapabilitiesJson)=1),
  CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
  UpdatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
  CreatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id),
  UpdatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id),
  RowVersion rowversion NOT NULL,
  CONSTRAINT UQ_ResourceType_Module_Code UNIQUE(ModuleId,Code));
END;
IF OBJECT_ID('[platform].[Resource]', 'U') IS NULL
BEGIN
 CREATE TABLE [platform].[Resource](
  Id uniqueidentifier NOT NULL CONSTRAINT PK_Resource PRIMARY KEY NONCLUSTERED,
  OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id),
  ResourceTypeId uniqueidentifier NOT NULL REFERENCES [platform].[ResourceType](Id),
  Availability varchar(64) NOT NULL CHECK(Availability IN ('Active','Archived','Trash','Purged')),
  Revision bigint NOT NULL CHECK(Revision>0), PurgedAt datetime2(7) NULL,
  CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt datetime2(7) NOT NULL,
  CreatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id),
  UpdatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id), RowVersion rowversion NOT NULL,
  CONSTRAINT UQ_Resource_Owner_Id UNIQUE(OwnerId,Id),
  CONSTRAINT CK_Resource_Purged CHECK((Availability='Purged' AND PurgedAt IS NOT NULL) OR
                                    (Availability<>'Purged' AND PurgedAt IS NULL)));
 CREATE UNIQUE CLUSTERED INDEX CX_Resource_Owner_Created_Id ON [platform].[Resource](OwnerId,CreatedAt,Id);
 CREATE INDEX IX_Resource_Owner_Type_Availability ON [platform].[Resource](OwnerId,ResourceTypeId,Availability,Id);
END;
IF SCHEMA_ID('shopping') IS NULL EXEC('CREATE SCHEMA [shopping]');
IF OBJECT_ID('[platform].[ResourceLink]', 'U') IS NULL
BEGIN
 CREATE TABLE [platform].[ResourceLink](
  Id uniqueidentifier NOT NULL CONSTRAINT PK_ResourceLink PRIMARY KEY NONCLUSTERED,
  OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id),
  SourceResourceId uniqueidentifier NOT NULL, TargetResourceId uniqueidentifier NOT NULL,
  RelationType nvarchar(100) NOT NULL, TargetVersion bigint NULL,
  State varchar(64) NOT NULL CHECK(State IN ('Active','Unavailable','Detached')),
  CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt datetime2(7) NOT NULL,
  CreatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id),
  UpdatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id), RowVersion rowversion NOT NULL,
  CONSTRAINT UQ_ResourceLink_Owner_Id UNIQUE(OwnerId,Id),
  CONSTRAINT FK_ResourceLink_Source FOREIGN KEY(OwnerId,SourceResourceId) REFERENCES [platform].[Resource](OwnerId,Id),
  CONSTRAINT FK_ResourceLink_Target FOREIGN KEY(OwnerId,TargetResourceId) REFERENCES [platform].[Resource](OwnerId,Id));
 CREATE UNIQUE CLUSTERED INDEX CX_ResourceLink_Owner_Created_Id ON [platform].[ResourceLink](OwnerId,CreatedAt,Id);
 CREATE UNIQUE INDEX UX_ResourceLink_Unversioned ON [platform].[ResourceLink](OwnerId,SourceResourceId,TargetResourceId,RelationType) WHERE TargetVersion IS NULL;
 CREATE UNIQUE INDEX UX_ResourceLink_Versioned ON [platform].[ResourceLink](OwnerId,SourceResourceId,TargetResourceId,RelationType,TargetVersion) WHERE TargetVersion IS NOT NULL;
 CREATE INDEX IX_ResourceLink_Owner_Target_State ON [platform].[ResourceLink](OwnerId,TargetResourceId,State);
END;
IF SCHEMA_ID('operations') IS NULL EXEC('CREATE SCHEMA [operations]');
IF OBJECT_ID('[operations].[TrashBatch]', 'U') IS NULL
BEGIN
 CREATE TABLE [operations].[TrashBatch](
  Id uniqueidentifier NOT NULL CONSTRAINT PK_TrashBatch PRIMARY KEY NONCLUSTERED,
  OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id),
  RootResourceId uniqueidentifier NOT NULL, DeletedAt datetime2(7) NOT NULL, RestoredAt datetime2(7) NULL,
  State varchar(64) NOT NULL CHECK(State IN ('Trashed','Restored','PartiallyPurged','Purged')),
  CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt datetime2(7) NOT NULL,
  CreatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id),
  UpdatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id), RowVersion rowversion NOT NULL,
  CONSTRAINT UQ_TrashBatch_Owner_Id UNIQUE(OwnerId,Id),
  CONSTRAINT FK_TrashBatch_Root FOREIGN KEY(OwnerId,RootResourceId) REFERENCES [platform].[Resource](OwnerId,Id),
  CONSTRAINT CK_TrashBatch_Restored CHECK((State='Restored' AND RestoredAt IS NOT NULL) OR (State<>'Restored' AND RestoredAt IS NULL)));
 CREATE UNIQUE CLUSTERED INDEX CX_TrashBatch_Owner_Created_Id ON [operations].[TrashBatch](OwnerId,CreatedAt,Id);
 CREATE INDEX IX_TrashBatch_Owner_State_Deleted ON [operations].[TrashBatch](OwnerId,State,DeletedAt);
END;
IF OBJECT_ID('[operations].[TrashMember]', 'U') IS NULL
BEGIN
 CREATE TABLE [operations].[TrashMember](
  Id uniqueidentifier NOT NULL CONSTRAINT PK_TrashMember PRIMARY KEY NONCLUSTERED,
  OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id),
  BatchId uniqueidentifier NOT NULL, ResourceId uniqueidentifier NOT NULL, ParentResourceId uniqueidentifier NULL,
  PreviousLifecycle varchar(64) NOT NULL, Depth int NOT NULL CHECK(Depth>=0), PurgedAt datetime2(7) NULL,
  CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt datetime2(7) NOT NULL,
  CreatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id),
  UpdatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id), RowVersion rowversion NOT NULL,
  CONSTRAINT UQ_TrashMember_Owner_Id UNIQUE(OwnerId,Id),
  CONSTRAINT UQ_TrashMember_Cohort UNIQUE(OwnerId,BatchId,ResourceId),
  CONSTRAINT FK_TrashMember_Batch FOREIGN KEY(OwnerId,BatchId) REFERENCES [operations].[TrashBatch](OwnerId,Id),
  CONSTRAINT FK_TrashMember_Resource FOREIGN KEY(OwnerId,ResourceId) REFERENCES [platform].[Resource](OwnerId,Id),
  CONSTRAINT FK_TrashMember_Parent FOREIGN KEY(OwnerId,ParentResourceId) REFERENCES [platform].[Resource](OwnerId,Id));
 CREATE UNIQUE CLUSTERED INDEX CX_TrashMember_Owner_Created_Id ON [operations].[TrashMember](OwnerId,CreatedAt,Id);
END;
IF OBJECT_ID('[shopping].[WishlistItem]', 'U') IS NULL
BEGIN
 CREATE TABLE [shopping].[WishlistItem](
  Id uniqueidentifier NOT NULL CONSTRAINT PK_WishlistItem PRIMARY KEY NONCLUSTERED,
  OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id),
  Title nvarchar(200) NOT NULL CHECK(LEN(LTRIM(RTRIM(Title)))>0), Url nvarchar(2048) NULL,
  Quantity decimal(28,8) NOT NULL CHECK(Quantity>0), TargetAmount decimal(28,8) NULL,
  Currency char(3) NULL, Notes nvarchar(max) NULL,
  Status varchar(64) NOT NULL CHECK(Status IN ('Wanted','Purchased','Archived','Trash')),
  PreArchiveState varchar(64) NULL, PreTrashState varchar(64) NULL, TrashBatchId uniqueidentifier NULL,
  CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt datetime2(7) NOT NULL,
  CreatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id),
  UpdatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id), RowVersion rowversion NOT NULL,
  CONSTRAINT UQ_WishlistItem_Owner_Id UNIQUE(OwnerId,Id),
  CONSTRAINT FK_WishlistItem_Resource FOREIGN KEY(OwnerId,Id) REFERENCES [platform].[Resource](OwnerId,Id),
  CONSTRAINT FK_WishlistItem_TrashBatch FOREIGN KEY(OwnerId,TrashBatchId) REFERENCES [operations].[TrashBatch](OwnerId,Id),
  CONSTRAINT CK_WishlistItem_Price CHECK((TargetAmount IS NULL AND Currency IS NULL) OR
      (TargetAmount IS NOT NULL AND TargetAmount>=0 AND Currency IS NOT NULL AND Currency COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^A-Z]%')),
  CONSTRAINT CK_WishlistItem_Archive CHECK((Status='Archived' AND PreArchiveState IS NOT NULL AND PreArchiveState IN ('Wanted','Purchased')) OR
      (Status='Trash' AND ((PreTrashState='Archived' AND PreArchiveState IS NOT NULL AND PreArchiveState IN ('Wanted','Purchased')) OR
          (PreTrashState IN ('Wanted','Purchased') AND PreArchiveState IS NULL))) OR
      (Status IN ('Wanted','Purchased') AND PreArchiveState IS NULL)),
  CONSTRAINT CK_WishlistItem_Trash CHECK((Status='Trash' AND TrashBatchId IS NOT NULL AND PreTrashState IS NOT NULL AND PreTrashState IN ('Wanted','Purchased','Archived')) OR
      (Status<>'Trash' AND TrashBatchId IS NULL AND PreTrashState IS NULL)));
 CREATE UNIQUE CLUSTERED INDEX CX_WishlistItem_Owner_Created_Id ON [shopping].[WishlistItem](OwnerId,CreatedAt,Id);
 CREATE INDEX IX_WishlistItem_Page ON [shopping].[WishlistItem](OwnerId,Status,UpdatedAt DESC,Id DESC);
END;
IF NOT EXISTS(SELECT 1 FROM [platform].[ResourceType] WHERE ModuleId=(SELECT Id FROM [platform].[Module] WHERE Code='FX31') AND Code='WishlistItem')
 INSERT [platform].[ResourceType](Id,ModuleId,Code,ContractVersion,CapabilitiesJson)
 SELECT NEWID(),Id,'WishlistItem','wishlist-v1',N'{"schemaVersion":1,"trash":true,"share":false,"support":false,"search":false}'
 FROM [platform].[Module] WHERE Code='FX31';
INSERT [platform].[Permission](ActionKey,EffectiveStatus)
SELECT s.ActionKey,'Resolved' FROM (VALUES
 ('shopping.wishlist.read'),('shopping.wishlist.create'),('shopping.wishlist.update'),
 ('shopping.wishlist.mark_purchased'),('shopping.wishlist.archive'),('shopping.wishlist.unarchive'),
 ('shopping.wishlist.trash'),('shopping.wishlist.restore'),('shopping.wishlist.purge'))s(ActionKey)
WHERE NOT EXISTS(SELECT 1 FROM [platform].[Permission] WITH(UPDLOCK,HOLDLOCK) WHERE ActionKey=s.ActionKey);
-- Installed subset only. Existing grants are preserved; other Shopping actions have no handlers.
UPDATE [platform].[Module] SET Name=N'Shopping — Wishlist only',State='Ready',
 PolicyRevision=PolicyRevision+1,UpdatedAt=SYSUTCDATETIME()
WHERE Code='FX31' AND State='Blocked';
COMMIT;
