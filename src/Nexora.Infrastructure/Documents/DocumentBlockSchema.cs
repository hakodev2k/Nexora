using System.Text.Json;

namespace Nexora.Infrastructure.Documents;

/// <summary>Versioned text-block grammar. Every string remains plain text at render time.</summary>
internal static class DocumentBlockSchema
{
    internal static bool IsValid(string? body)
    {
        if (string.IsNullOrEmpty(body)) return true;
        try
        {
            using var json=JsonDocument.Parse(body,new JsonDocumentOptions{MaxDepth=12});var root=json.RootElement;
            if(!Fields(root,"schemaVersion","blocks")||!root.TryGetProperty("schemaVersion",out var version)||!version.TryGetInt32(out var v)||v!=1
                ||!root.TryGetProperty("blocks",out var blocks)||blocks.ValueKind!=JsonValueKind.Array||blocks.GetArrayLength()>1000)return false;
            var ids=new HashSet<Guid>();
            foreach(var block in blocks.EnumerateArray())
            {
                if(block.ValueKind!=JsonValueKind.Object||!block.TryGetProperty("id",out var id)||id.ValueKind!=JsonValueKind.String
                    ||!Guid.TryParseExact(id.GetString(),"D",out var uuid)||uuid==Guid.Empty||!ids.Add(uuid)||!Text(block,"type",out var type))return false;
                var valid=type switch
                {
                    "paragraph" or "quote"=>Fields(block,"id","type","text")&&Text(block,"text",out _),
                    "heading"=>Fields(block,"id","type","text","level")&&Text(block,"text",out _)&&block.TryGetProperty("level",out var level)&&level.TryGetInt32(out var n)&&n is >=1 and <=3,
                    "divider"=>Fields(block,"id","type"),
                    "code"=>Fields(block,"id","type","text","language")&&Text(block,"text",out _)&&(!block.TryGetProperty("language",out _)||(Text(block,"language",out var language)&&language.Length<=64)),
                    "link"=>Fields(block,"id","type","text","url")&&Text(block,"text",out _)&&Text(block,"url",out var url)&&SafeUrl(url),
                    "list"=>Fields(block,"id","type","ordered","items")&&block.TryGetProperty("ordered",out var ordered)&&ordered.ValueKind is JsonValueKind.True or JsonValueKind.False&&Items(block,false),
                    "checklist"=>Fields(block,"id","type","items")&&Items(block,true),
                    "table"=>Fields(block,"id","type","rows")&&Table(block),
                    _=>false
                };
                if(!valid)return false;
            }
            return true;
        }
        catch(JsonException){return false;}
        catch(InvalidOperationException){return false;}
    }
    internal static bool IsLegacyText(string body)=>!body.AsSpan().TrimStart().StartsWith("{")&&!body.AsSpan().TrimStart().StartsWith("[");
    private static bool Fields(JsonElement element,params string[] names)
    {
        if(element.ValueKind!=JsonValueKind.Object)return false;var seen=new HashSet<string>(StringComparer.Ordinal);
        foreach(var property in element.EnumerateObject())if(!names.Contains(property.Name,StringComparer.Ordinal)||!seen.Add(property.Name))return false;
        return true;
    }
    private static bool Text(JsonElement element,string name,out string value)
    {value=string.Empty;if(!element.TryGetProperty(name,out var field)||field.ValueKind!=JsonValueKind.String)return false;value=field.GetString()!;return !value.Contains('\0');}
    private static bool SafeUrl(string value)=>value.Length<=2048&&!value.Any(char.IsControl)&&Uri.TryCreate(value,UriKind.Absolute,out var uri)
        &&uri.Scheme is "https" or "http"&&string.IsNullOrEmpty(uri.UserInfo)&&!string.IsNullOrEmpty(uri.Host);
    private static bool Items(JsonElement block,bool checklist)
    {
        if(!block.TryGetProperty("items",out var items)||items.ValueKind!=JsonValueKind.Array||items.GetArrayLength()>1000)return false;
        foreach(var item in items.EnumerateArray())
            if(checklist? !Fields(item,"text","checked")||!Text(item,"text",out _)||!item.TryGetProperty("checked",out var check)||check.ValueKind is not(JsonValueKind.True or JsonValueKind.False)
                :item.ValueKind!=JsonValueKind.String||item.GetString()!.Contains('\0'))return false;
        return true;
    }
    private static bool Table(JsonElement block)
    {
        if(!block.TryGetProperty("rows",out var rows)||rows.ValueKind!=JsonValueKind.Array||rows.GetArrayLength() is <1 or >100)return false;int? columns=null;
        foreach(var row in rows.EnumerateArray())
        {
            if(row.ValueKind!=JsonValueKind.Array||row.GetArrayLength() is <1 or >20)return false;columns??=row.GetArrayLength();if(row.GetArrayLength()!=columns)return false;
            foreach(var cell in row.EnumerateArray())if(cell.ValueKind!=JsonValueKind.String||cell.GetString()!.Contains('\0'))return false;
        }
        return true;
    }
}
