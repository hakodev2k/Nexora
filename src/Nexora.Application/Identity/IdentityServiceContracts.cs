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
/// Role-specific administrative grants do not replace the enabled-module
/// own-resource baseline used by the Admin role.
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
    IdentityOperationResult<IdentityAccepted> Register(RegistrationCommand command, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<IdentityVerification> Verify(string token, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<IdentityAccepted> ResendVerification(string email, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<IdentityLoginIssue> Login(string email, string password, string? deviceLabel = null, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<object?> Logout(string? rawSessionHandle, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<object?> Reauthenticate(string? rawSessionHandle, string password, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<IdentityAccepted> RequestPasswordReset(string email, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<object?> ConfirmPasswordReset(string token, string newPassword, string? idempotencyKey = null, string? traceId = null);
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
/// Account messages are emitted after the SQL transaction commits. The raw
/// token is intentionally never persisted in SQL; production implementations
/// should send through an approved provider, while local development may use a
/// protected in-process capture for synthetic accounts.
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
