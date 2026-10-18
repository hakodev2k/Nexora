SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'news.Feed',N'U') IS NULL
BEGIN
    CREATE TABLE [news].[Feed](
        Id uniqueidentifier NOT NULL CONSTRAINT PK_NewsFeed PRIMARY KEY,
        Url nvarchar(2048) COLLATE Latin1_General_100_BIN2 NOT NULL, UrlDigest binary(32) NOT NULL,
        Title nvarchar(200) NOT NULL, ApprovedPublicAt datetime2(7) NOT NULL,
        State varchar(64) NOT NULL CHECK(State IN('Active','Stale','Failed','Disabled')),
        LastAttemptAt datetime2(7) NULL, LastSuccessAt datetime2(7) NULL, LastErrorCode nvarchar(100) NULL,
        Etag nvarchar(500) NULL, LastModified nvarchar(200) NULL,
        CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id), UpdatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id),
        RowVersion rowversion NOT NULL, CONSTRAINT UQ_NewsFeed_UrlDigest UNIQUE(UrlDigest)
    );
END;
IF OBJECT_ID(N'news.FeedSubscription',N'U') IS NULL
BEGIN
    CREATE TABLE [news].[FeedSubscription](
        Id uniqueidentifier NOT NULL CONSTRAINT PK_NewsSubscription PRIMARY KEY,
        OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id),
        FeedId uniqueidentifier NOT NULL REFERENCES [news].[Feed](Id), CategoryId uniqueidentifier NULL,
        TitleOverride nvarchar(200) NULL, State varchar(32) NOT NULL CHECK(State IN('Followed','Paused','Unfollowed')),
        CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id), UpdatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id),
        RowVersion rowversion NOT NULL, CONSTRAINT UQ_NewsSubscription_Owner_Id UNIQUE(OwnerId,Id),
        CONSTRAINT UQ_NewsSubscription_Feed UNIQUE(OwnerId,FeedId),
        CONSTRAINT FK_NewsSubscription_Category FOREIGN KEY(OwnerId,CategoryId) REFERENCES [news].[Category](OwnerId,Id)
    );
    CREATE INDEX IX_NewsSubscription_Owner ON [news].[FeedSubscription](OwnerId,State,Id);
END;
IF OBJECT_ID(N'news.Article',N'U') IS NULL
BEGIN
    CREATE TABLE [news].[Article](
        Id uniqueidentifier NOT NULL CONSTRAINT PK_NewsArticle PRIMARY KEY,
        FeedId uniqueidentifier NOT NULL REFERENCES [news].[Feed](Id),
        ExternalKey nvarchar(2048) COLLATE Latin1_General_100_BIN2 NOT NULL, ExternalKeyDigest binary(32) NOT NULL,
        Title nvarchar(500) NOT NULL, Url nvarchar(2048) NOT NULL,
        PublishedAt datetime2(7) NULL, FetchedAt datetime2(7) NOT NULL,
        BodyFormat varchar(32) NOT NULL CHECK(BodyFormat='PlainText'), IsExcerpt bit NOT NULL,
        SanitizedBody nvarchar(max) NULL CHECK(DATALENGTH(SanitizedBody)<=524288), ContentDigest binary(32) NULL,
        CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id), UpdatedByUserId uniqueidentifier NULL REFERENCES [identity].[User](Id),
        RowVersion rowversion NOT NULL,
        CONSTRAINT UQ_NewsArticle_Key UNIQUE(FeedId,ExternalKeyDigest), CONSTRAINT UQ_NewsArticle_Feed_Id UNIQUE(FeedId,Id)
    );
    CREATE INDEX IX_NewsArticle_Feed_Published ON [news].[Article](FeedId,PublishedAt DESC,FetchedAt DESC,Id DESC);
END;
IF OBJECT_ID(N'news.ArticleState',N'U') IS NULL
BEGIN
    CREATE TABLE [news].[ArticleState](
        Id uniqueidentifier NOT NULL CONSTRAINT PK_NewsArticleState PRIMARY KEY,
        OwnerId uniqueidentifier NOT NULL, FeedId uniqueidentifier NOT NULL, ArticleId uniqueidentifier NOT NULL,
        ReadAt datetime2(7) NULL, LastOpenedAt datetime2(7) NULL,
        CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(), UpdatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id), UpdatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id),
        RowVersion rowversion NOT NULL, CONSTRAINT UQ_NewsArticleState_Article UNIQUE(OwnerId,ArticleId),
        CONSTRAINT FK_NewsArticleState_Subscription FOREIGN KEY(OwnerId,FeedId) REFERENCES [news].[FeedSubscription](OwnerId,FeedId),
        CONSTRAINT FK_NewsArticleState_Article FOREIGN KEY(FeedId,ArticleId) REFERENCES [news].[Article](FeedId,Id)
    );
END;
INSERT [platform].[Permission](ActionKey,EffectiveStatus)
SELECT ActionKey,'Resolved' FROM (VALUES(N'news.source.read'),(N'news.source.follow'),(N'news.source.update'),(N'news.source.pause'),(N'news.source.resume'),(N'news.source.unfollow'),
    (N'news.article.read'),(N'news.article.mark_read'),(N'news.article.mark_unread')) v(ActionKey)
WHERE NOT EXISTS(SELECT 1 FROM [platform].[Permission] p WITH(UPDLOCK,HOLDLOCK) WHERE p.ActionKey=v.ActionKey);
COMMIT TRANSACTION;
GO
