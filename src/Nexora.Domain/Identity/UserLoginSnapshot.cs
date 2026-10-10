namespace Nexora.Domain.Identity;

public sealed record UserLoginSnapshot(
    Guid UserId,
    UserState State,
    bool EmailConfirmed,
    bool IsDeleted,
    bool IsDisabled,
    bool HasEnabledMfa,
    string SecurityStamp);
