using Nexora.Domain.Common;

namespace Nexora.Domain.Identity;

public static class AccountAccessPolicy
{
    public static PolicyDecision CanLogin(UserLoginSnapshot? user)
    {
        if (user is null)
        {
            return PolicyDecision.Deny("InvalidCredentials", "Authentication failed without revealing account existence.");
        }

        if (user.State == UserState.PendingVerification || !user.EmailConfirmed)
        {
            return PolicyDecision.Deny("EmailVerificationRequired", "Email verification is required before login.");
        }

        if (user.State == UserState.Disabled || user.IsDisabled)
        {
            return PolicyDecision.Deny("AccountUnavailable", "The account is not currently available.");
        }

        if (user.State == UserState.Deleted || user.IsDeleted)
        {
            return PolicyDecision.Deny("AccountUnavailable", "Deleted accounts cannot authenticate or be implicitly restored.");
        }

        if (user.HasEnabledMfa)
        {
            return PolicyDecision.Deny("MfaUnavailable", "MFA-enabled accounts fail closed until the MFA slice is implemented.");
        }

        return PolicyDecision.Allow("LoginAllowed", "Password-only M01 account may receive a session after credential validation.");
    }

    public static PolicyDecision CanConfirmPasswordReset(UserLoginSnapshot? user)
    {
        if (user is null)
        {
            return PolicyDecision.Deny("TokenUnavailable", "Reset token cannot be associated with an active account.");
        }

        if (user.State == UserState.Deleted || user.IsDeleted || user.State == UserState.Disabled || user.IsDisabled)
        {
            return PolicyDecision.Deny("AccountUnavailable", "Password reset must not undelete or enable an account.");
        }

        if (user.HasEnabledMfa)
        {
            return PolicyDecision.Deny("MfaRecoveryRequired", "Password reset must not remove or bypass MFA.");
        }

        return PolicyDecision.Allow("PasswordResetAllowed", "Password reset may update credentials and revoke sessions atomically.");
    }
}
