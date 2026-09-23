using Nexora.Domain.Common;

namespace Nexora.Domain.Identity;

public static class ProfilePatchPolicy
{
    private static readonly HashSet<string> AllowedFields = new(StringComparer.Ordinal)
    {
        "displayName",
        "timeZoneId",
        "locale"
    };

    public static PolicyDecision ValidatePatchFields(IEnumerable<string> fields)
    {
        var fieldList = fields.ToArray();
        if (fieldList.Length == 0)
        {
            return PolicyDecision.Deny("ValidationFailed", "Profile patch must contain at least one editable field.");
        }

        var unknown = fieldList.FirstOrDefault(field => !AllowedFields.Contains(field));
        return unknown is null
            ? PolicyDecision.Allow("ProfilePatchAccepted", "Profile patch only touches editable fields.")
            : PolicyDecision.Deny("UnknownField", $"Field '{unknown}' is not editable through the M01 profile endpoint.");
    }
}
