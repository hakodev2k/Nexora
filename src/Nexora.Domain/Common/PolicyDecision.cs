namespace Nexora.Domain.Common;

public sealed record PolicyDecision(bool Allowed, string Code, string Message)
{
    public static PolicyDecision Allow(string code = "Allowed", string message = "Allowed") =>
        new(true, code, message);

    public static PolicyDecision Deny(string code, string message) =>
        new(false, code, message);
}
