namespace Nexora.Api.Features.Support;

public sealed record SupportGrantRequest(string ModuleCode, string DurationMode, DateTimeOffset? ExpiresAt);

public sealed record SupportGrantResponse(
    Guid Id,
    string ModuleCode,
    string DurationMode,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? RevokedAt,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string ETag);

public sealed record SupportGrantPageResponse(IReadOnlyList<SupportGrantResponse> Items, string? NextCursor);

public sealed record SupportSessionRequest(Guid TargetUserId, string ModuleCode);
public sealed record EmergencyAccessRequest(Guid TargetUserId, string ModuleCode, string Reason);

public sealed record SupportSessionResponse(
    Guid Id,
    Guid TargetUserId,
    string ModuleCode,
    string Mode,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? EndedAt,
    Guid? SupportGrantId,
    string? Reason,
    DateTimeOffset CreatedAt,
    string ETag);

public sealed record SupportSessionPageResponse(IReadOnlyList<SupportSessionResponse> Items, string? NextCursor);
