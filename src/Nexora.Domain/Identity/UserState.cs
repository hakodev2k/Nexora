namespace Nexora.Domain.Identity;

public enum UserState
{
    PendingVerification = 0,
    Active = 1,
    Disabled = 2,
    Deleted = 3
}
