using Nexora.Domain.Identity;

namespace Nexora.Application.Identity;

/// <summary>
/// Result returned by application use cases.  HTTP concerns stay at the API
/// boundary while status/code values remain available for a stable problem
/// contract.
/// </summary>
public sealed record IdentityOperationResult<T>(
    bool Succeeded,
    T? Value,
    string Code,
    int StatusCode,
    string Title)
{
    public static IdentityOperationResult<T> Success(T value, int statusCode = 200, string code = "Ok") =>
        new(true, value, code, statusCode, code);

    public static IdentityOperationResult<T> NoContent(string code = "NoContent") =>
        new(true, default, code, 204, code);

    public static IdentityOperationResult<T> Failure(string code, int statusCode, string title) =>
        new(false, default, code, statusCode, title);
}

public sealed record IdentityAccepted(string Status, string MessageCode);

public sealed record IdentityModuleProjection(string Code, bool Enabled, string? UnavailableReason);

public sealed record IdentityProfile(
    Guid Id,
    string Email,
    string DisplayName,
    string TimeZoneId,
    string Locale,
    string State,
    Guid? PersonalSpaceId,
    IReadOnlyList<IdentityModuleProjection> Modules,
    string Role = "User");

public sealed record IdentityVerification(string Status, string MessageCode, IdentityProfile Profile);

public sealed record IdentityProfileRead(IdentityProfile Profile, string ETag);

public sealed record IdentityLoginIssue(IdentityProfile Profile, string RawSessionHandle, DateTimeOffset ExpiresAt);

public sealed record IdentitySession(
    Guid Id,
    string DeviceLabel,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastSeenAt,
    DateTimeOffset ExpiresAt,
    bool IsCurrent);

public sealed record IdentitySessionPage(IReadOnlyList<IdentitySession> Items, string? NextCursor);

/// <summary>
/// Trusted identity context used by other feature modules. OwnerId is the
/// PersonalSpace identifier; it must never be accepted from request input.
/// User and SuperAdmin own-resource access follows the baseline. Admin SELF
/// actions require an explicit Allow and have no implicit access from the
/// role alone; OwnerId remains the PersonalSpace identifier.
/// </summary>
public sealed record IdentityPrincipal(
    Guid UserId,
    Guid OwnerId,
    string Role,
    DateTimeOffset RecentAuthenticatedAt);

public sealed record BootstrapSuperAdminCommand(
    string Email,
    string Password,
    string TimeZoneId,
    string? DisplayName,
    string? Locale = null);

public sealed record BootstrapSuperAdminResult(Guid UserId, Guid PersonalSpaceId);

public interface IIdentityService
{
    IdentityOperationResult<IdentityAccepted> Register(RegistrationCommand command, string? idempotencyKey = null, string? traceId = null, string? anonymousSessionBinding = null);
    IdentityOperationResult<IdentityVerification> Verify(string token, string? idempotencyKey = null, string? traceId = null, string? anonymousSessionBinding = null);
    IdentityOperationResult<IdentityAccepted> ResendVerification(string email, string? idempotencyKey = null, string? traceId = null, string? anonymousSessionBinding = null);
    IdentityOperationResult<IdentityLoginIssue> Login(string email, string password, string? deviceLabel = null, string? idempotencyKey = null, string? traceId = null, string? anonymousSessionBinding = null);
    IdentityOperationResult<object?> Logout(string? rawSessionHandle, string? idempotencyKey = null, string? traceId = null, string? anonymousSessionBinding = null);
    IdentityOperationResult<object?> Reauthenticate(string? rawSessionHandle, string password, string? idempotencyKey = null, string? traceId = null, string? anonymousSessionBinding = null);
    IdentityOperationResult<IdentityAccepted> RequestPasswordReset(string email, string? idempotencyKey = null, string? traceId = null, string? anonymousSessionBinding = null);
    IdentityOperationResult<object?> ConfirmPasswordReset(string token, string newPassword, string? idempotencyKey = null, string? traceId = null, string? anonymousSessionBinding = null);
    IdentityOperationResult<IdentityProfileRead> GetMe(string? rawSessionHandle);
    IdentityOperationResult<IdentityProfileRead> UpdateMe(string? rawSessionHandle, string? ifMatch, ProfilePatchRequest command, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<IdentitySessionPage> ListSessions(string? rawSessionHandle);
    IdentityOperationResult<object?> RevokeSession(string? rawSessionHandle, Guid sessionId, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<object?> RevokeAll(string? rawSessionHandle, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<object?> SoftDelete(string? rawSessionHandle, string confirmation, string password, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<IdentityPrincipal> GetPrincipal(string? rawSessionHandle);
    Task<IdentityOperationResult<IdentityPrincipal>> AuthenticateAsync(string? rawSessionHandle, CancellationToken cancellationToken = default);
    IdentityOperationResult<BootstrapSuperAdminResult> BootstrapSuperAdmin(BootstrapSuperAdminCommand command, string? traceId = null);
}

public sealed record ProfilePatchRequest(string? DisplayName, string? TimeZoneId, string? Locale);

/// <summary>
/// Account messages are represented by a durable, pre-activation delivery
/// intent. The raw token is only transient in the command and the approved
/// local transport; SQL stores an authenticated short-lived envelope so a
/// bounded worker can recover after a restart. Production provider delivery
/// remains outside this local implementation scope.
/// </summary>
public sealed record LocalAccountMessage(
    Guid Id,
    Guid? UserId,
    string Purpose,
    string Email,
    string RawToken,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt);

public interface IAccountMessageSink
{
    void Publish(LocalAccountMessage message);
}

/// <summary>
/// Effect boundary for the approved local account-message transport. A worker
/// must present the SQL lease fence and a just-in-time authority check before
/// a capture becomes visible. The ordinary sink interface remains available
/// for source compatibility, but the durable worker uses this fenced contract.
/// </summary>
public interface IAccountMessageEffectSink
{
    LocalAccountMessagePublishOutcome Publish(
        LocalAccountMessage message,
        Guid effectFence,
        Func<bool> isCurrent);

    bool PromoteIfOwned(Guid messageId, Guid effectFence);

    void RemoveIfOwned(Guid messageId, Guid effectFence);

    void SweepExpired(DateTimeOffset now);

    int ReconcilePending(Func<Guid, Guid, LocalAccountMessagePendingDisposition> resolve);
}

public enum LocalAccountMessagePublishOutcome
{
    AuthorityLost = 0,
    Prepared = 1,
    AlreadyCaptured = 2
}

public enum LocalAccountMessagePendingDisposition
{
    Remove = 0,
    Keep = 1,
    Promote = 2
}
