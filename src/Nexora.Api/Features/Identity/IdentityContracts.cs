namespace Nexora.Api.Features.Identity;

public sealed record AcceptedResponse(string Status, string MessageCode);

public sealed record CsrfResponse(string RequestToken, string TokenType, int ExpiresInSeconds);

public sealed record RegistrationRequest(
    string Email,
    string Password,
    string TimeZoneId,
    string? DisplayName);

public sealed record EmailRequest(string Email);

public sealed record TokenProofRequest(string Token);

public sealed record CredentialsRequest(string Email, string Password);

public sealed record PasswordProofRequest(string Password);

public sealed record ResetProofRequest(string Token, string NewPassword);

public sealed record ProfilePatchRequest(string? DisplayName, string? TimeZoneId, string? Locale);

public sealed record VerificationResponse(string Status, string MessageCode, ProfileResponse Profile);

public sealed record LoginResponse(ProfileResponse Profile, DateTimeOffset ExpiresAt);

public sealed record ProfileResponse(
    Guid Id,
    string Email,
    string DisplayName,
    string TimeZoneId,
    string Locale,
    string State,
    Guid? PersonalSpaceId,
    IReadOnlyList<ModuleProjection> Modules);

public sealed record ModuleProjection(string Code, bool Enabled, string? UnavailableReason);

public sealed record ProfileReadResult(ProfileResponse Profile, string ETag);

public sealed record LoginIssue(ProfileResponse Profile, string RawSessionHandle, DateTimeOffset ExpiresAt);

public sealed record SessionProjection(
    Guid Id,
    string DeviceLabel,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastSeenAt,
    DateTimeOffset ExpiresAt,
    bool IsCurrent);

public sealed record SessionPage(IReadOnlyList<SessionProjection> Items, string? NextCursor);

public sealed record DevAccountMessage(
    Guid Id,
    string Purpose,
    string Email,
    string Token,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt);
