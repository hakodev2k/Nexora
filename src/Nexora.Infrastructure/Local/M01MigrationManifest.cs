namespace Nexora.Infrastructure.Local;

/// <summary>
/// The only reviewed SQL migrations that a local M01 process may apply.
/// R1 migrations remain in the repository for their own future approved
/// slices, but must never be selected by a directory wildcard.
/// </summary>
public static class M01MigrationManifest
{
    public static IReadOnlyList<string> RequiredFileNames { get; } =
    [
        "20260909_0001_m01_identity_platform.sql",
        "20260909_0002_bootstrap_closure.sql",
        "20260913_0026_identity_local_delivery.sql",
        "20260922_0027_sanitize_session_device_labels.sql"
    ];
}
