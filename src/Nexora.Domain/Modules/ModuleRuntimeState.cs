namespace Nexora.Domain.Modules;

public enum ModuleRuntimeState
{
    Ready = 0,
    Disabled = 1,
    Paused = 2,
    Uninstalled = 3,
    MigrationFailed = 4,
    Blocked = 5
}
