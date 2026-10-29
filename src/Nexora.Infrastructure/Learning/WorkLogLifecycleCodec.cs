using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Nexora.Infrastructure.Learning;

internal sealed class WorkLogLifecycleCodec(string secret)
{
    private readonly byte[] key=SHA256.HashData(Encoding.UTF8.GetBytes("nexora/worklog/lifecycle-preview/v1\0"+secret));
    internal sealed record Scope(Guid Actor,Guid Owner,Guid Id,string Operation);
    internal sealed record Plan(string ETag,string From,string To,Guid? Batch,string? Prior,long History,long References,string? Cohort);
    internal sealed record Payload(Plan Plan,DateTimeOffset ExpiresAt);
    internal string Encode(Scope scope,Payload payload)
    {
        var plain=JsonSerializer.SerializeToUtf8Bytes(payload);var bytes=new byte[28+plain.Length];RandomNumberGenerator.Fill(bytes.AsSpan(0,12));
        using var aes=new AesGcm(key,16);aes.Encrypt(bytes.AsSpan(0,12),plain,bytes.AsSpan(28),bytes.AsSpan(12,16),JsonSerializer.SerializeToUtf8Bytes(scope));
        return "wlp1."+Convert.ToBase64String(bytes).TrimEnd('=').Replace('+','-').Replace('/','_');
    }
    internal Payload? Decode(Scope scope,string? token)
    {
        if(token is null || token.Length is <43 or >4096 || !token.StartsWith("wlp1.",StringComparison.Ordinal))return null;
        try {
            var encoded=token[5..];var bytes=Convert.FromBase64String(encoded.Replace('-','+').Replace('_','/')+new string('=',(4-encoded.Length%4)%4));
            if(bytes.Length<29 || Convert.ToBase64String(bytes).TrimEnd('=').Replace('+','-').Replace('/','_')!=encoded)return null;
            var plain=new byte[bytes.Length-28];using var aes=new AesGcm(key,16);
            aes.Decrypt(bytes.AsSpan(0,12),bytes.AsSpan(28),bytes.AsSpan(12,16),plain,JsonSerializer.SerializeToUtf8Bytes(scope));
            return JsonSerializer.Deserialize<Payload>(plain);
        } catch(Exception error)when(error is FormatException or CryptographicException or JsonException){return null;}
    }
}
