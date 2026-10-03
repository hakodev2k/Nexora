using Nexora.Application.Identity;

namespace Nexora.Application.Career;

public sealed record CompanyMetadata(string Title, string? Url = null, string? Industry = null,
    string? Location = null, string? Notes = null);
public sealed record CareerCompany(Guid Id, CompanyMetadata Metadata, Guid? MergedIntoId,
    int? LinkedJobCount, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string ETag);
public sealed record CompanyPage(IReadOnlyList<CareerCompany> Items, Guid? NextCursor);
public sealed record CompanyChange(CompanyMetadata Metadata);
public sealed record JobMetadata(string Title, Guid? CompanyId = null, string? Url = null,
    string? Location = null, string? WorkMode = null, string? EmploymentType = null,
    string? SalaryText = null, string? SalaryMin = null, string? SalaryMax = null,
    string? Currency = null, string? Description = null, string? Notes = null,
    string? Source = null, DateOnly? DiscoveredOn = null, DateOnly? AppliedOn = null);
public sealed record CareerJob(Guid Id, JobMetadata Metadata, string Stage, DateTimeOffset StageChangedAt,
    bool IsTrash, string? CompanyLabel, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string ETag);
public sealed record JobPage(IReadOnlyList<CareerJob> Items, Guid? NextCursor);
public sealed record JobCreate(JobMetadata Metadata, string Stage);
public sealed record JobChange(JobMetadata Metadata);
public sealed record JobStageChange(string Stage, bool Confirm, bool ConfirmReturnToProgress = false, string? Reason = null);
public sealed record CareerConfirmation(bool Confirm);
public sealed record CareerAcknowledgement(Guid ItemId, string? ETag);
public sealed record JobPreview(Guid ItemId, string ETag, string Operation, int ReferenceCount);
public sealed record JobSnapshotFields(JobMetadata Metadata, string Stage, DateTimeOffset StageChangedAt,
    string? PreTrashStage, string? CompanyLabelSnapshot);
public sealed record CareerProvenance(string Source);
public sealed record JobSnapshot(int SchemaVersion, string ResourceType, JobSnapshotFields Fields,
    IReadOnlyList<object> Relations, IReadOnlyList<Guid> EvidenceFileIds,
    IReadOnlyDictionary<string, CareerProvenance> Provenance);
public sealed record JobEvent(Guid Id, long VersionNumber, string ActionKey, string? FromStage,
    string ToStage, DateTimeOffset OccurredAt, Guid? CreatedByUserId, string? Note, JobSnapshot Snapshot);
public sealed record JobHistoryPage(IReadOnlyList<JobEvent> Items, Guid? NextCursor);
public sealed record MergeImpact(Guid Id, string ETag, string? Title);
public sealed record CompanyMergePreview(Guid SourceId, Guid TargetId, string SourceETag, string TargetETag,
    CompanyMetadata? SourceMetadata, CompanyMetadata? TargetMetadata, IReadOnlyList<MergeImpact> Jobs,
    IReadOnlyList<Guid> RetainedReferences, DateTimeOffset ExpiresAt, string PreviewToken);
public sealed record CompanyMergeRequest(Guid TargetId, string SourceETag, string TargetETag,
    string PreviewToken, bool Confirm, bool KeepTargetMetadata);

public interface ICareerService
{
    IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor);
    IdentityOperationResult<CompanyPage> Companies(IdentityPrincipal actor, Guid? cursor, string? query, bool? eligibleOnly);
    IdentityOperationResult<CareerCompany> Company(IdentityPrincipal actor, Guid id);
    IdentityOperationResult<CareerAcknowledgement> CreateCompany(IdentityPrincipal actor, CompanyChange body, string key, string? trace);
    IdentityOperationResult<CareerAcknowledgement> UpdateCompany(IdentityPrincipal actor, Guid id, string? etag, CompanyChange body, string key, string? trace);
    IdentityOperationResult<CompanyMergePreview> PreviewMerge(IdentityPrincipal actor, Guid sourceId, Guid targetId);
    IdentityOperationResult<CareerAcknowledgement> Merge(IdentityPrincipal actor, Guid sourceId, CompanyMergeRequest body, string key, string? trace);
    IdentityOperationResult<JobPage> Jobs(IdentityPrincipal actor, Guid? cursor, string? state, string? stage,
        Guid? companyId, string? query, string? location, string? dateField, DateOnly? from, DateOnly? to);
    IdentityOperationResult<CareerJob> Job(IdentityPrincipal actor, Guid id);
    IdentityOperationResult<CareerAcknowledgement> CreateJob(IdentityPrincipal actor, JobCreate body, string key, string? trace);
    IdentityOperationResult<CareerAcknowledgement> UpdateJob(IdentityPrincipal actor, Guid id, string? etag, JobChange body, string key, string? trace);
    IdentityOperationResult<CareerAcknowledgement> ChangeStage(IdentityPrincipal actor, Guid id, string? etag, JobStageChange body, string key, string? trace);
    IdentityOperationResult<JobHistoryPage> History(IdentityPrincipal actor, Guid id, Guid? cursor, string? action, string? from, string? to);
    IdentityOperationResult<JobPreview> PreviewJob(IdentityPrincipal actor, Guid id, string operation);
    IdentityOperationResult<CareerAcknowledgement> Lifecycle(IdentityPrincipal actor, Guid id, string? etag, string operation, CareerConfirmation body, string key, string? trace);
}
