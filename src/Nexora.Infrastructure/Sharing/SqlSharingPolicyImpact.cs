using System.Data;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;

namespace Nexora.Infrastructure.Sharing;

internal sealed record SharingPolicyImpact(long LinkCount, string CohortDigest);

/// <summary>Only aggregate impact leaves Sharing; tokens and source content are never read.</summary>
internal static class SqlSharingPolicyImpact
{
    internal static SharingPolicyImpact Read(SqlConnection connection, SqlTransaction transaction, string moduleCode, bool disabling)
    {
        using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        if (!disabling || moduleCode is not ("FX04" or "FX11" or "FX20"))
            return new(0, Convert.ToHexString(digest.GetHashAndReset()));
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        // Match0043's effect, including expired and temporarily suspended links.
        command.CommandText = """
            SELECT Id,OwnerId,RowVersion FROM [security].[ShareLink] WITH(HOLDLOCK)
            WHERE IsDeleted=0 AND (@Module='FX04' OR (@Module='FX11' AND ResourceType='Project')
                OR (@Module='FX20' AND ResourceType='Document')) ORDER BY Id;
            """;
        command.Parameters.Add("@Module", SqlDbType.VarChar, 64).Value = moduleCode;
        long count = 0;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            // Fixed16+16+8 byte fields provide unambiguous constant-memory encoding.
            digest.AppendData(reader.GetGuid(0).ToByteArray());
            digest.AppendData(reader.GetGuid(1).ToByteArray());
            digest.AppendData(reader.GetFieldValue<byte[]>(2));
            count = checked(count + 1);
        }
        return new(count, Convert.ToHexString(digest.GetHashAndReset()));
    }
}
