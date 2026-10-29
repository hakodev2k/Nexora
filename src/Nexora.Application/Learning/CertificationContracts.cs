using Nexora.Application.Identity;

namespace Nexora.Application.Learning;

public sealed record Certification(Guid Id, string Title, string Issuer, DateOnly? IssuedOn, DateOnly? ExpiresOn,
    string? VerificationUrl, string Status, string EffectiveStatus, string VersionNumber,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string ETag);
public sealed record CertificationPage(IReadOnlyList<Certification> Items, string? NextCursor);
public sealed record CertificationCreate(string Title, string Issuer, DateOnly? IssuedOn = null,
    DateOnly? ExpiresOn = null, string? VerificationUrl = null);
public sealed record CertificationMetadata(string Title, string Issuer, string? VerificationUrl = null);
public sealed record CertificationRenewal(DateOnly? IssuedOn, DateOnly? ExpiresOn, string? Reason = null);
public sealed record CertificationVersion(Guid Id, string VersionNumber, string Operation,
    CertificationSnapshot Snapshot, string? Reason, DateTimeOffset CreatedAt);
public sealed record CertificationSnapshot(int SchemaVersion, string Title, string Issuer,
    DateOnly? IssuedOn, DateOnly? ExpiresOn, string? VerificationUrl, string Status);
public sealed record CertificationHistoryPage(IReadOnlyList<CertificationVersion> Items, string? NextCursor);
public sealed record CertificationPreview(Guid Id, string ETag, string Operation, string VersionNumber,
    DateTimeOffset ExpiresAt, string PreviewToken);
public sealed record CertificationTransition(bool Confirm, DateTimeOffset ExpiresAt, string PreviewToken);

public static class CertificationRules
{
    public static bool ValidDates(DateOnly? issued, DateOnly? expiry) => issued is null || expiry is null || expiry >= issued;
    public static string EffectiveStatus(string state, DateOnly? expiry, DateOnly ownerToday) =>
        state == "Archived" ? "Archived" : expiry is { } day && day < ownerToday ? "Expired" : "Active";
    public static bool ValidMetadata(string? title, string? issuer, string? verificationUrl)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 200 ||
            string.IsNullOrWhiteSpace(issuer) || issuer.Trim().Length > 200) return false;
        if (string.IsNullOrWhiteSpace(verificationUrl)) return true;
        var url = verificationUrl.Trim();
        return url.Length <= 2048 && !url.Any(char.IsControl) && Uri.TryCreate(url, UriKind.Absolute, out var parsed)
            && parsed.Scheme is "https" or "http" && parsed.UserInfo.Length == 0;
    }
}

public interface ICertificationService
{
    IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor);
    IdentityOperationResult<CertificationPage> List(IdentityPrincipal actor, DateOnly ownerToday, string? status, string? query, string? cursor);
    IdentityOperationResult<Certification> Get(IdentityPrincipal actor, DateOnly ownerToday, Guid id);
    IdentityOperationResult<CertificationHistoryPage> History(IdentityPrincipal actor, Guid id, string? cursor);
    IdentityOperationResult<object?> Create(IdentityPrincipal actor, CertificationCreate body, string key, string? trace);
    IdentityOperationResult<object?> Update(IdentityPrincipal actor, Guid id, string? etag, CertificationMetadata body, string key, string? trace);
    IdentityOperationResult<object?> Renew(IdentityPrincipal actor, Guid id, string? etag, CertificationRenewal body, string key, string? trace);
    IdentityOperationResult<CertificationPreview> Preview(IdentityPrincipal actor, Guid id, string operation);
    IdentityOperationResult<object?> Transition(IdentityPrincipal actor, Guid id, string? etag, string operation, CertificationTransition body, string key, string? trace);
}
