using System.Data;
using Microsoft.Data.SqlClient;

namespace Nexora.Infrastructure.News;

/// <summary>Trusted onboarding participant; runs only inside the caller's existing SQL transaction.</summary>
internal static class SqlNewsOwnerInitializer
{
    internal static void Initialize(SqlConnection connection, SqlTransaction transaction, Guid ownerId, Guid userId, DateTime now)
    {
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = """
            IF NOT EXISTS(SELECT 1 FROM [platform].[PersonalSpace] WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Owner AND UserId=@User)
                THROW 51046,'Owner initialization mapping is unavailable.',1;
            IF NOT EXISTS(SELECT 1 FROM [news].[OwnerInitialization] WITH(UPDLOCK,HOLDLOCK) WHERE OwnerId=@Owner)
            BEGIN
                DECLARE @Type uniqueidentifier=(SELECT rt.Id FROM [platform].[ResourceType] rt JOIN [platform].[Module] m ON m.Id=rt.ModuleId
                    WHERE m.Code='FX29' AND rt.Code='Category' AND rt.ContractVersion='news-category-v1');
                IF @Type IS NULL THROW 51046,'News initialization contract is unavailable.',1;
                DECLARE @AI uniqueidentifier=NEWID(),@Tech uniqueidentifier=NEWID();
                INSERT [platform].[Resource](Id,OwnerId,ResourceTypeId,Availability,Revision,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                    VALUES(@AI,@Owner,@Type,'Active',1,@Now,@User,@User),(@Tech,@Owner,@Type,'Active',1,@Now,@User,@User);
                INSERT [news].[Category](Id,OwnerId,Name,CreatedAt,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                    VALUES(@AI,@Owner,N'AI News',@Now,@Now,@User,@User),(@Tech,@Owner,N'Tech News',@Now,@Now,@User,@User);
                INSERT [news].[OwnerInitialization](OwnerId,DefaultCategoryVersion,InitializedAt) VALUES(@Owner,1,@Now);
            END;
            """;
        command.Parameters.Add("@Owner", SqlDbType.UniqueIdentifier).Value = ownerId;
        command.Parameters.Add("@User", SqlDbType.UniqueIdentifier).Value = userId;
        command.Parameters.Add("@Now", SqlDbType.DateTime2).Value = now;
        command.ExecuteNonQuery();
    }
}
