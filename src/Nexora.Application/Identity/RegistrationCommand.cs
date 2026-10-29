using Nexora.Domain.Common;
using Nexora.Domain.Identity;

namespace Nexora.Application.Identity;

public sealed record RegistrationCommand(
    string Email,
    string Password,
    string TimeZoneId,
    string? DisplayName,
    string? Locale);

public sealed record RegistrationDraft(
    string OriginalEmail,
    string NormalizedEmail,
    string DisplayName,
    string TimeZoneId,
    string Locale);

public static class RegistrationCommandPolicy
{
    public static RegistrationCommandResult Validate(RegistrationCommand command)
    {
        var issues = new List<PolicyDecision>();
        string normalizedEmail;

        try
        {
            normalizedEmail = EmailNormalizer.Normalize(command.Email);
        }
        catch (Exception ex) when (ex is ArgumentNullException or FormatException)
        {
            normalizedEmail = string.Empty;
            issues.Add(PolicyDecision.Deny("EmailInvalid", "Email format is invalid."));
        }

        var passwordDecision = PasswordPolicy.Validate(command.Password);
        if (!passwordDecision.Allowed)
        {
            issues.Add(passwordDecision);
        }

        var timeZoneDecision = RegistrationPolicy.ValidateTimeZoneId(command.TimeZoneId);
        if (!timeZoneDecision.Allowed)
        {
            issues.Add(timeZoneDecision);
        }

        var locale = string.IsNullOrWhiteSpace(command.Locale) ? RegistrationPolicy.DefaultLocale : command.Locale.Trim();
        var localeDecision = RegistrationPolicy.ValidateLocale(locale);
        if (!localeDecision.Allowed)
        {
            issues.Add(localeDecision);
        }

        if (issues.Count > 0)
        {
            return RegistrationCommandResult.Invalid(issues);
        }

        var displayName = string.IsNullOrWhiteSpace(command.DisplayName)
            ? RegistrationPolicy.BuildDefaultDisplayName(command.Email)
            : command.DisplayName.Trim();

        if (displayName.Length > RegistrationPolicy.DisplayNameMaxLength)
        {
            displayName = displayName[..RegistrationPolicy.DisplayNameMaxLength];
        }

        return RegistrationCommandResult.Valid(new RegistrationDraft(
            command.Email.Trim(),
            normalizedEmail,
            displayName,
            command.TimeZoneId.Trim(),
            locale));
    }
}

public sealed record RegistrationCommandResult(bool IsValid, RegistrationDraft? Draft, IReadOnlyList<PolicyDecision> Issues)
{
    public static RegistrationCommandResult Valid(RegistrationDraft draft) => new(true, draft, Array.Empty<PolicyDecision>());
    public static RegistrationCommandResult Invalid(IReadOnlyList<PolicyDecision> issues) => new(false, null, issues);
}
