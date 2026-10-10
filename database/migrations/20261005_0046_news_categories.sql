SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF EXISTS(SELECT 1 FROM [platform].[Module] WITH(UPDLOCK,HOLDLOCK) WHERE Code='FX29' AND State='Blocked' AND SystemEnabled=1)
 THROW 51046,'News installation requires explicit policy activation.',1;
IF SCHEMA_ID('news') IS NULL EXEC('CREATE SCHEMA [news]');
IF OBJECT_ID('[news].[Category]','U') IS NULL
BEGIN
 CREATE TABLE [news].[Category](
  Id uniqueidentifier NOT NULL CONSTRAINT PK_NewsCategory PRIMARY KEY NONCLUSTERED,
  OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id),
  Name nvarchar(100) COLLATE Latin1_General_100_BIN2 NOT NULL CHECK(LEN(LTRIM(RTRIM(Name)))>0),
  CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt datetime2(7) NOT NULL,
  CreatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id), UpdatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id),
  RowVersion rowversion NOT NULL,
  CONSTRAINT UQ_NewsCategory_Owner_Id UNIQUE(OwnerId,Id),
  CONSTRAINT FK_NewsCategory_Resource FOREIGN KEY(OwnerId,Id) REFERENCES [platform].[Resource](OwnerId,Id));
 CREATE UNIQUE CLUSTERED INDEX CX_NewsCategory_Owner_Created_Id ON [news].[Category](OwnerId,CreatedAt,Id);
 CREATE INDEX IX_NewsCategory_Page ON [news].[Category](OwnerId,Name,Id);
END;
IF OBJECT_ID('[news].[OwnerInitialization]','U') IS NULL
 CREATE TABLE [news].[OwnerInitialization](OwnerId uniqueidentifier NOT NULL PRIMARY KEY REFERENCES [platform].[PersonalSpace](Id),
  DefaultCategoryVersion int NOT NULL CHECK(DefaultCategoryVersion=1), InitializedAt datetime2(7) NOT NULL);
IF NOT EXISTS(SELECT 1 FROM [platform].[ResourceType] WHERE ModuleId=(SELECT Id FROM [platform].[Module] WHERE Code='FX29') AND Code='Category')
 INSERT [platform].[ResourceType](Id,ModuleId,Code,ContractVersion,CapabilitiesJson)
 SELECT NEWID(),Id,'Category','news-category-v1',N'{"schemaVersion":1,"trash":false,"share":false,"support":false,"search":false}'
 FROM [platform].[Module] WHERE Code='FX29';
DECLARE @Type uniqueidentifier=(SELECT rt.Id FROM [platform].[ResourceType] rt JOIN [platform].[Module] m ON m.Id=rt.ModuleId
 WHERE m.Code='FX29' AND rt.Code='Category' AND rt.ContractVersion='news-category-v1');
IF @Type IS NULL THROW 51046,'News initialization contract is unavailable.',1;
DECLARE @Owner uniqueidentifier,@LockedOwner uniqueidentifier,@AI uniqueidentifier,@Tech uniqueidentifier,@Now datetime2(7)=SYSUTCDATETIME();
DECLARE owners CURSOR LOCAL FAST_FORWARD FOR SELECT Id FROM [platform].[PersonalSpace] WITH(UPDLOCK,HOLDLOCK) ORDER BY Id;
OPEN owners; FETCH NEXT FROM owners INTO @Owner;
WHILE @@FETCH_STATUS=0
BEGIN
 SELECT @LockedOwner=Id FROM [platform].[PersonalSpace] WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Owner;
 IF NOT EXISTS(SELECT 1 FROM [news].[OwnerInitialization] WITH(UPDLOCK,HOLDLOCK) WHERE OwnerId=@Owner)
 BEGIN
  SET @AI=NEWID(); SET @Tech=NEWID();
  INSERT [platform].[Resource](Id,OwnerId,ResourceTypeId,Availability,Revision,UpdatedAt)
   VALUES(@AI,@Owner,@Type,'Active',1,@Now),(@Tech,@Owner,@Type,'Active',1,@Now);
  INSERT [news].[Category](Id,OwnerId,Name,CreatedAt,UpdatedAt)
   VALUES(@AI,@Owner,N'AI News',@Now,@Now),(@Tech,@Owner,N'Tech News',@Now,@Now);
  INSERT [news].[OwnerInitialization](OwnerId,DefaultCategoryVersion,InitializedAt) VALUES(@Owner,1,@Now);
 END;
 FETCH NEXT FROM owners INTO @Owner;
END;
CLOSE owners; DEALLOCATE owners;
INSERT [platform].[Permission](ActionKey,EffectiveStatus)
 SELECT s.ActionKey,'Resolved' FROM (VALUES('news.category.read'),('news.category.create'),('news.category.update'))s(ActionKey)
 WHERE NOT EXISTS(SELECT 1 FROM [platform].[Permission] WITH(UPDLOCK,HOLDLOCK) WHERE ActionKey=s.ActionKey);
UPDATE [platform].[Module] SET Name=N'News — Private Categories only',State='Ready',PolicyRevision=PolicyRevision+1,UpdatedAt=SYSUTCDATETIME()
 WHERE Code='FX29' AND State='Blocked';
COMMIT;
