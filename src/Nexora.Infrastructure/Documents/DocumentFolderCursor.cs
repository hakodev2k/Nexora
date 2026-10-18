using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Nexora.Infrastructure.Documents;

internal sealed class DocumentFolderCursor(string secret)
{
    private readonly byte[] key=SHA256.HashData(Encoding.UTF8.GetBytes("nexora/documents/folders/key/v1\0"+secret));
    private sealed record Position(string Title,Guid Id);
    private static byte[] Context(Guid owner,Guid? parent)=>JsonSerializer.SerializeToUtf8Bytes(new{version=1,owner,parent,limit=25});
    private static string EncodeBytes(byte[] value)=>Convert.ToBase64String(value).TrimEnd('=').Replace('+','-').Replace('/','_');
    public string Encode(Guid owner,Guid? parent,string title,Guid id)
    {
        var plain=JsonSerializer.SerializeToUtf8Bytes(new Position(title,id));var envelope=new byte[plain.Length+28];RandomNumberGenerator.Fill(envelope.AsSpan(0,12));
        using var cipher=new AesGcm(key,16);cipher.Encrypt(envelope.AsSpan(0,12),plain,envelope.AsSpan(28),envelope.AsSpan(12,16),Context(owner,parent));return "df1."+EncodeBytes(envelope);
    }
    public bool TryDecode(string token,Guid owner,Guid? parent,out string? title,out Guid? id)
    {
        title=null;id=null;if(token.Length is <44 or >2048||!token.StartsWith("df1.",StringComparison.Ordinal))return false;
        try
        {
            var encoded=token[4..];var envelope=Convert.FromBase64String(encoded.Replace('-','+').Replace('_','/')+new string('=',(4-encoded.Length%4)%4));
            if(envelope.Length<29||EncodeBytes(envelope)!=encoded)return false;var plain=new byte[envelope.Length-28];using var cipher=new AesGcm(key,16);
            cipher.Decrypt(envelope.AsSpan(0,12),envelope.AsSpan(28),envelope.AsSpan(12,16),plain,Context(owner,parent));var position=JsonSerializer.Deserialize<Position>(plain);
            if(position is null||position.Id==Guid.Empty||string.IsNullOrWhiteSpace(position.Title)||position.Title.Length>200)return false;
            title=position.Title;id=position.Id;return true;
        }
        catch(Exception error)when(error is FormatException or CryptographicException or JsonException){return false;}
    }
}
