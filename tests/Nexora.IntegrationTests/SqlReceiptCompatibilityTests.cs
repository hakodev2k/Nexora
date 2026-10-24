using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Organization;
using Nexora.Infrastructure.Organization;
using Nexora.Infrastructure.Persistence;
using Xunit;

namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlReceiptCompatibilityTests(SqlApiFixture fixture)
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Compatibility_successes_must_prove_one_original_outcome(bool divergent, bool removed)
    {
        fixture.RequireAvailable();
        var session = await fixture.CreateActiveSessionAsync();
        var actor = new IdentityPrincipal(session.UserId, session.OwnerId, "User", DateTimeOffset.UtcNow, session.SessionId);
        const string secret = "synthetic-compatibility-receipt-key";
        var service = new SqlTagService(new SqlConnectionFactory(fixture.ConnectionString!), secret);
        var command = new TagCreateCommand("projects", "Original compatibility tag", null);
        var key = Guid.NewGuid().ToString();
        var created = service.Create(actor, command, key);
        Assert.True(created.Succeeded, created.Code);
        if (removed) Assert.True(service.Remove(actor, created.Value!.Id, created.Value.ETag, Guid.NewGuid().ToString()).Succeeded);
        byte[] Hash(string value) => HMACSHA256.HashData(SHA256.HashData(Encoding.UTF8.GetBytes(secret)), Encoding.UTF8.GetBytes(value));
        // Deliberate persisted-compatibility fault, not a user journey or seed receipt.
        await fixture.ExecuteAsync("""
            INSERT [identity].[RequestReceipt]
              (SubjectHash,OperationKey,KeyHash,RequestDigest,State,ResultCode,CreatedAt,ExpiresAt,ResultStatusCode,ResultJson)
            SELECT @Subject,OperationKey,KeyHash,RequestDigest,State,ResultCode,CreatedAt,ExpiresAt,ResultStatusCode,
                   CASE WHEN @Divergent=1 THEN JSON_MODIFY(ResultJson,'$.Id',CONVERT(nvarchar(36),@Other)) ELSE ResultJson END
            FROM [identity].[RequestReceipt] WHERE SubjectHash=@Original AND OperationKey='organization.tag.create' AND KeyHash=@Key;
            """,
            new SqlParameter("@Subject", SqlDbType.Binary, 32) { Value = Hash($"user:{session.UserId:N}") },
            new SqlParameter("@Original", SqlDbType.Binary, 32) { Value = Hash($"user:{session.UserId:N}:organization.tag.create") },
            new SqlParameter("@Key", SqlDbType.Binary, 32) { Value = Hash("key:" + key) },
            new SqlParameter("@Divergent", SqlDbType.Bit) { Value = divergent },
            new SqlParameter("@Other", SqlDbType.UniqueIdentifier) { Value = Guid.NewGuid() });
        var before = await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent]");
        var result = service.Create(actor, command, key);
        if (divergent)
        {
            Assert.False(result.Succeeded);
            Assert.Equal("IdempotencyConflict", result.Code);
            Assert.Null(result.Value); Assert.Null(result.Acknowledgement);
        }
        else { Assert.True(result.Succeeded, result.Code); Assert.Equal(created.Value!.Id, result.Value!.Id); }
        Assert.Equal(before, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent]"));
    }
}
