namespace Nexora.Application.Identity;

public enum BootstrapOutcome { Created, AlreadyBootstrapped }

// The application identity contract uses BootstrapSuperAdminCommand for its
// interactive raw-password flow. Keep the low-level SQL adapter on a distinct
// command so both local entry points can coexist safely.
// PasswordHash is derived before entering the transaction; never serialize this command.
public sealed class SqlBootstrapSuperAdminCommand
{
    public required string Email { get; init; }
    public required string DisplayName { get; init; }
    public required string TimeZoneId { get; init; }
    public required string PasswordHash { get; init; }
}

public interface IBootstrapSuperAdmin
{
    Task<BootstrapOutcome> ExecuteAsync(SqlBootstrapSuperAdminCommand command, CancellationToken cancellationToken = default);
}
