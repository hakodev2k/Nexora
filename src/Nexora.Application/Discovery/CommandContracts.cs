using Nexora.Application.Identity;

namespace Nexora.Application.Discovery;

public sealed record DiscoveryCommand(string Key, string Title, string Group);
public sealed record DiscoveryCommandPage(IReadOnlyList<DiscoveryCommand> Items, bool CanSearch, bool CanReadRecent, bool CanExecute);
public sealed record CommandSelection(string Kind, string? Key, string? ResourceType, Guid? ResourceId);
public sealed record CommandHandoff(string Screen, string? ModuleCode = null, string? ResourceType = null, Guid? ResourceId = null, string? Mode = null);
public interface ICommandPaletteService
{
    IdentityOperationResult<DiscoveryCommandPage> List(IdentityPrincipal actor, string? query = null);
    IdentityOperationResult<CommandHandoff> Execute(IdentityPrincipal actor, CommandSelection selection);
}
