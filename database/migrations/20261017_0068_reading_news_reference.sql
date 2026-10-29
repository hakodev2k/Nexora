SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'knowledge.ReadingItem') AND name=N'CK_ReadingItem_SourceType')
    ALTER TABLE [knowledge].[ReadingItem] DROP CONSTRAINT [CK_ReadingItem_SourceType];
ALTER TABLE [knowledge].[ReadingItem] WITH CHECK ADD CONSTRAINT [CK_ReadingItem_SourceType] CHECK(SourceType IN('Bookmark','NewsArticle'));
IF NOT EXISTS(SELECT 1 FROM sys.key_constraints WHERE parent_object_id=OBJECT_ID(N'knowledge.ReadingItem') AND name=N'UQ_ReadingItem_TypedSource')
    ALTER TABLE [knowledge].[ReadingItem] ADD CONSTRAINT UQ_ReadingItem_TypedSource UNIQUE(OwnerId,Id,SourceId,SourceType);
IF OBJECT_ID(N'knowledge.NewsReadingReference',N'U') IS NULL
BEGIN
    CREATE TABLE [knowledge].[NewsReadingReference](
        OwnerId uniqueidentifier NOT NULL, ReadingItemId uniqueidentifier NOT NULL, FeedId uniqueidentifier NOT NULL, ArticleId uniqueidentifier NOT NULL,
        SourceType varchar(32) NOT NULL DEFAULT 'NewsArticle' CONSTRAINT CK_NewsReadingReference_Type CHECK(SourceType='NewsArticle'),
        CONSTRAINT PK_NewsReadingReference PRIMARY KEY(OwnerId,ReadingItemId),
        CONSTRAINT UQ_NewsReadingReference_Article UNIQUE(OwnerId,ArticleId),
        CONSTRAINT FK_NewsReadingReference_Queue FOREIGN KEY(OwnerId,ReadingItemId,ArticleId,SourceType) REFERENCES [knowledge].[ReadingItem](OwnerId,Id,SourceId,SourceType),
        CONSTRAINT FK_NewsReadingReference_Subscription FOREIGN KEY(OwnerId,FeedId) REFERENCES [news].[FeedSubscription](OwnerId,FeedId),
        CONSTRAINT FK_NewsReadingReference_Article FOREIGN KEY(FeedId,ArticleId) REFERENCES [news].[Article](FeedId,Id)
    );
END;
EXEC(N'CREATE OR ALTER VIEW [news].[ReadingSourceProjectionV1] AS
    SELECT s.OwnerId,a.Id ArticleId,a.FeedId,st.ReadAt,COALESCE(st.RowVersion,CONVERT(binary(8),0x0000000000000000)) StateRowVersion
    FROM [news].[Article] a JOIN [news].[Feed] f ON f.Id=a.FeedId
    JOIN [news].[FeedSubscription] s ON s.FeedId=a.FeedId
    LEFT JOIN [news].[ArticleState] st ON st.OwnerId=s.OwnerId AND st.ArticleId=a.Id AND st.FeedId=a.FeedId
    WHERE s.State IN(''Followed'',''Paused'') AND f.State<>''Disabled'' AND a.BodyFormat=''PlainText'';');
INSERT [platform].[Permission](ActionKey,EffectiveStatus)
SELECT N'news.article.save','Resolved'
WHERE NOT EXISTS(SELECT 1 FROM [platform].[Permission] WITH(UPDLOCK,HOLDLOCK) WHERE ActionKey=N'news.article.save');
COMMIT TRANSACTION;
GO
