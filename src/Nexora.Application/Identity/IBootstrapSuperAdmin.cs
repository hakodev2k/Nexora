namespace Nexora.Application.Identity;

public enum BootstrapOutcome { Created, AlreadyBootstrapped }

// PasswordHash is derived before entering the transaction; never serialize this command.
public sealed class BootstrapSuperAdminCommand
{
    public required string Email { get; init; }
    public required string DisplayName { get; init; }
    public required string TimeZoneId { get; init; }
    public required string PasswordHash { get; init; }
}

public interface IBootstrapSuperAdmin
{
    Task<BootstrapOutcome> ExecuteAsync(BootstrapSuperAdminCommand command, CancellationToken cancellationToken = default);
}
