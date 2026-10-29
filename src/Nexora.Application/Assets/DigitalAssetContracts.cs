using Nexora.Application.Identity;
using System.Text.Json.Serialization;

namespace Nexora.Application.Assets;

public sealed record DigitalDomain(string Name, string? Registrar = null, bool? AutoRenewRecorded = null,
    DateOnly? RegisteredOn = null, string? NameserverNotes = null);
public sealed record DigitalHosting(string? Plan = null, string? Region = null, string? ControlPanelUrl = null,
    string? StorageLimitBytes = null, string? DomainName = null);
public sealed record DigitalVps(string? HostName = null, string? IpAddress = null, int? CpuCount = null,
    string? MemoryMiB = null, string? OperatingSystem = null, string? Plan = null, string? Region = null);
public sealed record DigitalCertificate(string Subject, string? Issuer = null, string? Fingerprint = null,
    string? NotBefore = null, string? NotAfter = null, string? HostName = null,
    IReadOnlyList<string>? SubjectAlternativeNames = null);
public sealed record DigitalLicense(string Product, int? Seats = null, DateOnly? PurchasedOn = null,
    string? Vendor = null, string? Edition = null);
public sealed record DigitalOnlineService(string? ServiceUrl = null, string? Plan = null);
public sealed record DigitalAssetDetails(DigitalDomain? Domain = null, DigitalHosting? Hosting = null,
    DigitalVps? Vps = null, DigitalCertificate? Certificate = null, DigitalLicense? License = null,
    DigitalOnlineService? OnlineService = null);
public sealed record DigitalAssetMetadata(string Title, string Kind, DigitalAssetDetails Details,
    string? Provider = null, DateOnly? ExpiresOn = null, string? Cost = null, string? Currency = null,
    string? RenewalCycle = null, string? Notes = null);
public sealed record DigitalAssetCreate(DigitalAssetMetadata Metadata, string State);
public sealed record DigitalAssetUpdate(DigitalAssetMetadata Metadata, bool ConfirmTypeChange = false);
public sealed record DigitalAssetRenewal([property: JsonRequired] DateOnly RenewedOn, [property: JsonRequired] DateOnly NewExpiry, bool Confirm,
    string? Amount = null, string? Currency = null, string? Notes = null);
public sealed record DigitalAssetConfirmation(bool Confirm);
public sealed record DigitalAssetAcknowledgement(Guid ItemId, string? ETag);
public sealed record DigitalAssetPreview(Guid ItemId, string ETag, string Operation, int ReferenceCount);
public sealed record DigitalAsset(Guid Id, DigitalAssetMetadata Metadata, string StoredState, string State,
    string ExpirationBasis, string TimeZoneId, string? AsciiName, string? UnicodeName,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string ETag);
public sealed record DigitalAssetPage(IReadOnlyList<DigitalAsset> Items, Guid? NextCursor);
public sealed record DigitalAssetSnapshotFields(DigitalAssetMetadata Metadata, string StoredState,
    string? PreArchiveState, string? PreTrashState, string? AsciiName, string? UnicodeName);
public sealed record DigitalAssetProvenance(string Source);
public sealed record DigitalAssetSnapshot(int SchemaVersion, string ResourceType, DigitalAssetSnapshotFields Fields,
    IReadOnlyList<object> Relations, IReadOnlyList<Guid> EvidenceFileIds,
    IReadOnlyDictionary<string, DigitalAssetProvenance> Provenance);
public sealed record DigitalAssetVersion(Guid Id, long VersionNumber, string ActionKey, DateTimeOffset CreatedAt,
    Guid? CreatedByUserId, string? Reason, DigitalAssetSnapshot Snapshot);
public sealed record DigitalAssetHistoryPage(IReadOnlyList<DigitalAssetVersion> Items, Guid? NextCursor);
public sealed record DigitalRenewalRecord(Guid Id, DateOnly RenewedOn, DateOnly? PreviousExpiry, DateOnly NewExpiry,
    string? Amount, string? Currency, string? Notes, DateTimeOffset CreatedAt);
public sealed record DigitalRenewalPage(IReadOnlyList<DigitalRenewalRecord> Items, Guid? NextCursor);

public interface IDigitalAssetService
{
    IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor);
    IdentityOperationResult<DigitalAssetPage> List(IdentityPrincipal actor, Guid? cursor, string? state,
        string? kind, string? query, string? expiry, DateOnly? from, DateOnly? to);
    IdentityOperationResult<DigitalAsset> Get(IdentityPrincipal actor, Guid id);
    IdentityOperationResult<DigitalAssetHistoryPage> History(IdentityPrincipal actor, Guid id, Guid? cursor,
        string? action, string? from, string? to, string? version);
    IdentityOperationResult<DigitalRenewalPage> Renewals(IdentityPrincipal actor, Guid id, Guid? cursor);
    IdentityOperationResult<DigitalAssetAcknowledgement> Create(IdentityPrincipal actor, DigitalAssetCreate body, string key, string? trace);
    IdentityOperationResult<DigitalAssetAcknowledgement> Update(IdentityPrincipal actor, Guid id, string? etag, DigitalAssetUpdate body, string key, string? trace);
    IdentityOperationResult<DigitalAssetAcknowledgement> RecordRenewal(IdentityPrincipal actor, Guid id, string? etag, DigitalAssetRenewal body, string key, string? trace);
    IdentityOperationResult<DigitalAssetPreview> Preview(IdentityPrincipal actor, Guid id, string operation);
    IdentityOperationResult<DigitalAssetAcknowledgement> Transition(IdentityPrincipal actor, Guid id, string? etag,
        string operation, DigitalAssetConfirmation body, string key, string? trace);
}
