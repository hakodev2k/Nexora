using System.Text.Json.Serialization;
using Nexora.Application.Identity;
namespace Nexora.Application.Integrations;

public sealed record ConnectorManifest(string ProviderKey, int Version, string Name, string ImplementationState,
    bool RealOperationsAvailable, string[] AuthModes, string[] InboundCommands, string[] OutboundEvents, string[] BlockingDecisions);
public static class ConnectorRegistry
{
    // Internal configuration contract version; not a claim about any n8n public API version.
    public static ConnectorManifest[] Describe() => [new("n8n", 1, "n8n", "ConfigurationOnly", false, [], [], [], ["D04", "D05", "D06", "D07", "D09", "G3"])];
    public static bool SupportsConfiguration(string? key, int version) => key == "n8n" && version == 1;
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record IntegrationConnectionCommand([property: JsonRequired] int SchemaVersion, string ProviderKey,
    [property: JsonRequired] int ConnectorVersion, string Title, string Endpoint);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record IntegrationDisableCommand([property: JsonRequired] bool Confirmed);
public sealed record IntegrationConnectionRecord(Guid Id, string ProviderKey, int ConnectorVersion, string Title,
    string Endpoint, string State, DateTimeOffset UpdatedAt, DateTimeOffset? LastTestAt, string ETag,
    string AuthMode = "Unconfigured", bool RealOperationsAvailable = false);
public sealed record IntegrationConnectionAcknowledgement(Guid Id);
public sealed record IntegrationConnectionPage(IReadOnlyList<IntegrationConnectionRecord> Items, string? NextCursor);
public interface IIntegrationConnectionService
{
    IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor);
    IdentityOperationResult<IReadOnlyList<ConnectorManifest>> Connectors(IdentityPrincipal actor);
    IdentityOperationResult<IntegrationConnectionPage> List(IdentityPrincipal actor, string? cursor, string? query, string? provider, string? state);
    IdentityOperationResult<IntegrationConnectionRecord> Get(IdentityPrincipal actor, Guid id);
    IdentityOperationResult<IntegrationConnectionAcknowledgement> Create(IdentityPrincipal actor, IntegrationConnectionCommand body, string key, string? trace);
    IdentityOperationResult<IntegrationConnectionAcknowledgement> Update(IdentityPrincipal actor, Guid id, string? etag, IntegrationConnectionCommand body, string key, string? trace);
    IdentityOperationResult<IntegrationConnectionAcknowledgement> Disable(IdentityPrincipal actor, Guid id, string? etag, IntegrationDisableCommand body, string key, string? trace);
}
