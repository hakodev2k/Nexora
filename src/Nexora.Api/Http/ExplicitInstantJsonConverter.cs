using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Nexora.Api.Http;

/// <summary>Transport boundary for instants: never infer the server's local zone.</summary>
public sealed class ExplicitInstantJsonConverter : JsonConverter<DateTimeOffset>
{
    public static bool TryParse(string? value, out DateTimeOffset instant)
    {
        instant = default;
        return value is { Length: >= 20 and <= 40 } && value[10] == 'T'
            && (value.EndsWith('Z') || Regex.IsMatch(value, @"[+-]\d{2}:\d{2}$", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100)))
            && DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out instant);
    }
    public static bool QueryValid(HttpContext context, string name) => !context.Request.Query.ContainsKey(name)
        || (context.Request.Query[name].Count == 1 && TryParse(context.Request.Query[name].ToString(), out _));
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String && TryParse(reader.GetString(), out var result) ? result : throw new JsonException("An ISO 8601 instant with an explicit UTC offset is required.");
    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToUniversalTime());
}
