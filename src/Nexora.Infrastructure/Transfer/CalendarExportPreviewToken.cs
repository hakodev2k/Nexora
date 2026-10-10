using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Nexora.Infrastructure.Transfer;

internal sealed record ExportPreviewClaim(Guid Owner, Guid User, Guid? Session, string FilterDigest, string CohortDigest, string Zone, DateTimeOffset ExpiresAt);
internal sealed class CalendarExportPreviewToken(string secret)
{
    internal const int MaxTokenLength = 2048;
    private readonly byte[] key = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes("Nexora/CalendarExportPreview/v1"));
    internal string Sign(ExportPreviewClaim claim)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(claim, SqlImportBatchStore.Json);
        return Convert.ToBase64String(bytes) + "." + Convert.ToBase64String(HMACSHA256.HashData(key, bytes));
    }
    internal ExportPreviewClaim? Read(string? token)
    {
        if (token is null || token.Length > MaxTokenLength || token.Any(c => c > 127)) return null;
        try
        {
            var parts = token.Split('.'); if (parts.Length != 2) return null;
            var bytes = Convert.FromBase64String(parts[0]); var signature = Convert.FromBase64String(parts[1]);
            if (!CryptographicOperations.FixedTimeEquals(signature, HMACSHA256.HashData(key, bytes))) return null;
            return JsonSerializer.Deserialize<ExportPreviewClaim>(bytes, SqlImportBatchStore.Json);
        }
        catch (Exception error) when (error is FormatException or JsonException) { return null; }
    }
}
