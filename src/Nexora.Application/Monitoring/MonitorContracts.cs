using System.Text.Json.Serialization;
using Nexora.Application.Identity;

namespace Nexora.Application.Monitoring;

public sealed record MonitorMetadata([property: JsonRequired] int SchemaVersion, string Title, string Kind, string Target,
    [property: JsonRequired] int IntervalSeconds, [property: JsonRequired] int? ExpectedStatus);
public sealed record MonitorCreate([property: JsonRequired] MonitorMetadata Metadata, [property: JsonRequired] bool Enabled);
public sealed record MonitorUpdate([property: JsonRequired] MonitorMetadata Metadata);
public sealed record MonitorConfirmation([property: JsonRequired] bool Confirmed);
public sealed record MonitorRecord(Guid Id, MonitorMetadata Metadata, bool Enabled, string State,
    DateTimeOffset? LastObservedAt, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string ETag);
public sealed record MonitorPage(IReadOnlyList<MonitorRecord> Items, Guid? NextCursor);
public sealed record MonitorAcknowledgement(Guid Id, bool Enabled, string State, string ETag);
public interface IMonitoringService
{
    IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor);
    IdentityOperationResult<MonitorPage> List(IdentityPrincipal actor, Guid? cursor, string? query, string? state);
    IdentityOperationResult<MonitorRecord> Get(IdentityPrincipal actor, Guid id);
    IdentityOperationResult<MonitorAcknowledgement> Create(IdentityPrincipal actor, MonitorCreate body, string key, string? trace);
    IdentityOperationResult<MonitorAcknowledgement> Update(IdentityPrincipal actor, Guid id, string? etag, MonitorUpdate body, string key, string? trace);
    IdentityOperationResult<MonitorAcknowledgement> SetEnabled(IdentityPrincipal actor, Guid id, string? etag, bool enabled, MonitorConfirmation body, string key, string? trace);
}
