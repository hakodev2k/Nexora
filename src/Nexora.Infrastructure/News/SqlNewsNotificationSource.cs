using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Notifications;

namespace Nexora.Infrastructure.News;

public sealed partial class SqlNewsReaderService
{
    // Read-only source participation: no reader-history or read-state mutation.
    internal NotificationSource? ReadNotificationSourceWithin(SqlConnection connection,
        SqlTransaction transaction, IdentityPrincipal actor, Guid articleId)
    {
        if (!Allowed(connection, transaction, actor, "news.article.read")) return null;
        using var command = Command(connection, transaction, actor.OwnerId,
            $"SELECT a.Title {ArticleJoins} WHERE a.Id=@Id AND {VisibleArticle};");
        Add(command, "@Id", SqlDbType.UniqueIdentifier, articleId);
        string? title;
        using (var reader = command.ExecuteReader()) title = reader.Read() ? reader.GetString(0) : null;
        if (title is null || !Allowed(connection, transaction, actor, "news.article.read")) return null;
        return new("NewsArticle", articleId, title, $"/news/articles/{articleId:D}");
    }
}
