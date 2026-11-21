using System.Security.Cryptography;

namespace FamilyFirst.Application.Common.Security;

/// <summary>
/// Salted PBKDF2-SHA256 hashing for PINs and passwords (ProjectOverview §2.4 rule 10).
/// Format: <c>v1.{base64(16-byte salt)}.{base64(32-byte hash)}</c>, 100,000 iterations, constant-time verify.
/// </summary>
public static class PinHasher
{
    private const int Iterations = 100_000;
    private const int SaltBytes = 16;
    private const int HashBytes = 32;

    public static string Hash(string secret)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(secret, salt, Iterations, HashAlgorithmName.SHA256, HashBytes);
        return $"v1.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string secret, string storedHash)
    {
        var parts = storedHash.Split('.');
        if (parts.Length != 3 || parts[0] != "v1")
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[1]);
        var expectedHash = Convert.FromBase64String(parts[2]);
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(secret, salt, Iterations, HashAlgorithmName.SHA256, expectedHash.Length);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
