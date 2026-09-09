using Nexora.Domain.Common;

namespace Nexora.Domain.Identity;

public static class PasswordPolicy
{
    public const int MinCodePoints = 15;
    public const int MaxCodePoints = 128;

    private static readonly HashSet<string> CommonPasswords = new(StringComparer.Ordinal)
    {
        "passwordpassword",
        "passwordpasswordpassword",
        "123456789012345",
        "qwertyqwertyqwerty",
        "letmeinletmeinletmein"
    };

    public static PolicyDecision Validate(string password)
    {
        if (password is null)
        {
            return PolicyDecision.Deny("PasswordRequired", "Password is required.");
        }

        var codePointCount = password.EnumerateRunes().Count();
        if (codePointCount < MinCodePoints)
        {
            return PolicyDecision.Deny("PasswordTooShort", "Password must contain at least 15 Unicode code points.");
        }

        if (codePointCount > MaxCodePoints)
        {
            return PolicyDecision.Deny("PasswordTooLong", "Password must not exceed 128 Unicode code points.");
        }

        if (CommonPasswords.Contains(password))
        {
            return PolicyDecision.Deny("PasswordCommon", "Password is present in the local M01 common-password blocklist.");
        }

        return PolicyDecision.Allow("PasswordAccepted", "Password satisfies the M01 length and local blocklist policy.");
    }
}
