using Nexora.Domain.Common;

namespace Nexora.Domain.Identity;

public static class RegistrationPolicy
{
    public const int DisplayNameMaxLength = 100;
    public const string DefaultLocale = "vi";

    public static string BuildDefaultDisplayName(string email)
    {
        var trimmed = email.Trim();
        var at = trimmed.IndexOf('@');
        var localPart = at > 0 ? trimmed[..at] : trimmed;
        localPart = string.IsNullOrWhiteSpace(localPart) ? "User" : localPart;
        return localPart.Length <= DisplayNameMaxLength ? localPart : localPart[..DisplayNameMaxLength];
    }

    public static PolicyDecision ValidateLocale(string locale)
    {
        return locale is "vi" or "en"
            ? PolicyDecision.Allow("LocaleAccepted", "Locale is supported.")
            : PolicyDecision.Deny("LocaleUnsupported", "Locale must be vi or en.");
    }

    public static PolicyDecision ValidateTimeZoneId(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return PolicyDecision.Deny("TimeZoneRequired", "IANA timeZoneId is required.");
        }

        if (timeZoneId.Length > 128 || timeZoneId.Contains('\0'))
        {
            return PolicyDecision.Deny("TimeZoneInvalid", "timeZoneId is malformed.");
        }

        if (!TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId.Trim(), out _))
        {
            return PolicyDecision.Deny("TimeZoneInvalid", "timeZoneId must be a recognized IANA timezone.");
        }

        return PolicyDecision.Allow("TimeZoneAccepted", "timeZoneId is a recognized IANA timezone.");
    }
}
