using Nexora.Application.Identity;

namespace Nexora.Application.Settings;

public sealed record PreferenceRecord(
    Guid Id,
    string PreferenceKey,
    int SchemaVersion,
    string ValueJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string ETag);

public sealed record PreferencePage(IReadOnlyList<PreferenceRecord> Items, string? NextCursor);

public sealed record PreferenceUpdateCommand(
    string PreferenceKey,
    int SchemaVersion,
    string ValueJson,
    string? IfMatch);

public interface ISettingsService
{
    IdentityOperationResult<PreferencePage> ListPreferences(IdentityPrincipal actor);
    IdentityOperationResult<PreferenceRecord> UpdatePreference(IdentityPrincipal actor, PreferenceUpdateCommand command,
        string? idempotencyKey = null, string? traceId = null);
}
