SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF SCHEMA_ID(N'discovery') IS NULL EXEC(N'CREATE SCHEMA [discovery]');
IF OBJECT_ID(N'discovery.RecentItem',N'U') IS NULL
BEGIN
    CREATE TABLE discovery.RecentItem(
        Id uniqueidentifier NOT NULL CONSTRAINT PK_RecentItem PRIMARY KEY,
        OwnerId uniqueidentifier NOT NULL CONSTRAINT FK_RecentItem_Owner REFERENCES platform.PersonalSpace(Id),
        ResourceType varchar(32) NOT NULL,
        ResourceId uniqueidentifier NOT NULL,
        LastOpenedAt datetime2(7) NOT NULL,
        CreatedAt datetime2(7) NOT NULL CONSTRAINT DF_RecentItem_Created DEFAULT SYSUTCDATETIME(),
        CreatedByUserId uniqueidentifier NOT NULL CONSTRAINT FK_RecentItem_CreatedBy REFERENCES [identity].[User](Id),
        UpdatedAt datetime2(7) NOT NULL,
        UpdatedByUserId uniqueidentifier NOT NULL CONSTRAINT FK_RecentItem_UpdatedBy REFERENCES [identity].[User](Id),
        RowVersion rowversion NOT NULL,
        CONSTRAINT UQ_RecentItem_Owner_Id UNIQUE(OwnerId,Id),
        CONSTRAINT UQ_RecentItem_Source UNIQUE(OwnerId,ResourceType,ResourceId),
        CONSTRAINT CK_RecentItem_Type CHECK(ResourceType IN('Project','Task','Event','Document','Bookmark','Snippet','Goal')),
        CONSTRAINT CK_RecentItem_Source CHECK(ResourceId<>'00000000-0000-0000-0000-000000000000')
    );
    CREATE INDEX IX_RecentItem_Order ON discovery.RecentItem(OwnerId,LastOpenedAt DESC,RowVersion DESC,Id DESC);
END;
INSERT platform.Permission(ActionKey,EffectiveStatus)
SELECT ActionKey,'Resolved' FROM(VALUES(N'discovery.recent.read'),(N'discovery.recent.clear')) v(ActionKey)
WHERE NOT EXISTS(SELECT 1 FROM platform.Permission p WITH(UPDLOCK,HOLDLOCK) WHERE p.ActionKey=v.ActionKey);
COMMIT TRANSACTION;
GO
