SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'news.ArticleState') AND name=N'IX_NewsArticleState_History')
    CREATE INDEX IX_NewsArticleState_History ON [news].[ArticleState](OwnerId,LastOpenedAt DESC,ArticleId DESC) WHERE LastOpenedAt IS NOT NULL;
INSERT [platform].[Permission](ActionKey,EffectiveStatus)
SELECT ActionKey,'Resolved' FROM (VALUES(N'news.article.mark_all_read'),(N'news.history.read'),(N'news.history.clear')) v(ActionKey)
WHERE NOT EXISTS(SELECT 1 FROM [platform].[Permission] p WITH(UPDLOCK,HOLDLOCK) WHERE p.ActionKey=v.ActionKey);
COMMIT TRANSACTION;
GO
