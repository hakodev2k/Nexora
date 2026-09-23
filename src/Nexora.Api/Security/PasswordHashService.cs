using Microsoft.AspNetCore.Identity;
using Nexora.Infrastructure.Identity;

namespace Nexora.Api.Security;

public sealed class PasswordHashService
{
    private readonly Pbkdf2PasswordHasher _inner = new();

    public string Hash(string password) => _inner.Hash(password);

    public bool Verify(string password, string encodedHash) =>
        _inner.VerifyDetailed(password, encodedHash) != PasswordVerificationResult.Failed;
}
