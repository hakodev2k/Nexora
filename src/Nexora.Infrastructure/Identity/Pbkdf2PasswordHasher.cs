using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Nexora.Infrastructure.Identity;

/// <summary>
/// M01 password hashing boundary. New hashes and the normal verification path
/// use the versioned ASP.NET Identity PasswordHasher with the approved
/// PBKDF2-HMAC-SHA512 cost. The legacy parser exists only for bounded upgrade
/// compatibility with hashes written by the pre-review local implementation.
/// </summary>
public sealed class Pbkdf2PasswordHasher
{
    public const int ApprovedIterationCount = 220_000;
    private const string LegacyPrefix = "PBKDF2-HMAC-SHA512$";
    private const int LegacyMinimumIterations = 100_000;
    private const int LegacyMaximumIterations = 2_000_000;
    private const int LegacyMinimumSaltBytes = 8;
    private const int LegacyMaximumSaltBytes = 64;
    private const int LegacyMinimumHashBytes = 16;
    private const int LegacyMaximumHashBytes = 64;

    private static readonly object IdentityUser = new();
    private readonly PasswordHasher<object> _identityHasher;

    public Pbkdf2PasswordHasher()
    {
        _identityHasher = new PasswordHasher<object>(Options.Create(new PasswordHasherOptions
        {
            CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3,
            IterationCount = ApprovedIterationCount
        }));
    }

    public string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        return _identityHasher.HashPassword(IdentityUser, password);
    }

    public bool Verify(string password, string encodedHash) =>
        VerifyDetailed(password, encodedHash) != PasswordVerificationResult.Failed;

    public PasswordVerificationResult VerifyDetailed(string password, string encodedHash)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(encodedHash);

        if (encodedHash.StartsWith(LegacyPrefix, StringComparison.Ordinal))
        {
            return VerifyLegacy(password, encodedHash);
        }

        try
        {
            return _identityHasher.VerifyHashedPassword(IdentityUser, encodedHash, password);
        }
        catch (ArgumentException)
        {
            return PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return PasswordVerificationResult.Failed;
        }
    }

    private static PasswordVerificationResult VerifyLegacy(string password, string encodedHash)
    {
        var parts = encodedHash.Split('$');
        if (parts.Length != 4 || !int.TryParse(parts[1], System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out var iterations) ||
            iterations is < LegacyMinimumIterations or > LegacyMaximumIterations)
        {
            return PasswordVerificationResult.Failed;
        }

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return PasswordVerificationResult.Failed;
        }

        if (salt.Length is < LegacyMinimumSaltBytes or > LegacyMaximumSaltBytes ||
            expected.Length is < LegacyMinimumHashBytes or > LegacyMaximumHashBytes)
        {
            return PasswordVerificationResult.Failed;
        }

        try
        {
            var actual = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations,
                HashAlgorithmName.SHA512, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected)
                ? PasswordVerificationResult.SuccessRehashNeeded
                : PasswordVerificationResult.Failed;
        }
        catch (ArgumentException)
        {
            return PasswordVerificationResult.Failed;
        }
    }
}
