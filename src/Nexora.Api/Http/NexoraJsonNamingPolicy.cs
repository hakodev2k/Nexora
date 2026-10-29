using System.Text.Json;

namespace Nexora.Api.Http;

/// <summary>
/// Keeps the API's conventional lower-camel JSON shape while normalizing the
/// ETag acronym to the stable <c>etag</c> wire field expected by clients.
/// </summary>
public sealed class NexoraJsonNamingPolicy : JsonNamingPolicy
{
    public static NexoraJsonNamingPolicy Instance { get; } = new();

    public override string ConvertName(string name) =>
        string.Equals(name, "ETag", StringComparison.Ordinal) ? "etag" : JsonNamingPolicy.CamelCase.ConvertName(name);
}
