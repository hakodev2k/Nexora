using Nexora.Application.Identity;

namespace Nexora.Application.Support;

public sealed record SupportGrantRecord(
    Guid Id,
    string ModuleCode,
    string DurationMode,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? RevokedAt,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string ETag);

public sealed record SupportGrantPage(IReadOnlyList<SupportGrantRecord> Items, string? NextCursor);

public sealed record SupportGrantCommand(string ModuleCode, string DurationMode, DateTimeOffset? ExpiresAt);

public sealed record SupportSessionRecord(
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

public sealed record SupportSessionPage(IReadOnlyList<SupportSessionRecord> Items, string? NextCursor);

public sealed record SupportSessionOpenCommand(Guid TargetUserId, string ModuleCode);

public sealed record EmergencyOpenCommand(Guid TargetUserId, string ModuleCode, string Reason);

public interface ISupportService
{
    IdentityOperationResult<SupportGrantPage> ListGrants(IdentityPrincipal actor, int? limit = null);
    IdentityOperationResult<SupportGrantRecord> Grant(IdentityPrincipal actor, SupportGrantCommand command,
        string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<object?> RevokeGrant(IdentityPrincipal actor, Guid grantId, string? ifMatch,
        string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<SupportSessionPage> ListSessions(IdentityPrincipal actor, int? limit = null);
    IdentityOperationResult<SupportSessionRecord> OpenSupport(IdentityPrincipal actor, SupportSessionOpenCommand command,
        string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<SupportSessionRecord> OpenEmergency(IdentityPrincipal actor, EmergencyOpenCommand command,
        string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<object?> EndSession(IdentityPrincipal actor, Guid sessionId, string? ifMatch,
        string? idempotencyKey = null, string? traceId = null);
}
