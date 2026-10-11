using System.Security.Cryptography;
using FixtureHub.Application.Abstractions.Authentication;

namespace FixtureHub.Infrastructure.Authentication;

internal sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    public const string Algorithm = "PBKDF2-SHA256";
    public const int Iterations = 600_000;
    public const int SaltSize = 16;
    public const int HashSize = 32;

    private const char Separator = '$';

    private static readonly HashAlgorithmName Prf = HashAlgorithmName.SHA256;
    private static readonly byte[] DummySalt = RandomNumberGenerator.GetBytes(SaltSize);

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Prf, HashSize);

        return string.Join(Separator, Algorithm, Iterations, Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }

    public bool Verify(string password, string? passwordHash)
    {
        if (!TryParse(passwordHash, out var iterations, out var salt, out var expected))
        {
            Rfc2898DeriveBytes.Pbkdf2(password, DummySalt, Iterations, Prf, HashSize);
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Prf, expected.Length);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static bool TryParse(string? passwordHash, out int iterations, out byte[] salt, out byte[] hash)
    {
        iterations = 0;
        salt = [];
        hash = [];

        var parts = passwordHash?.Split(Separator) ?? [];

        return parts.Length == 4
            && parts[0] == Algorithm
            && int.TryParse(parts[1], out iterations)
            && iterations > 0
            && TryFromBase64(parts[2], out salt)
            && TryFromBase64(parts[3], out hash)
            && salt.Length > 0
            && hash.Length > 0;
    }

    private static bool TryFromBase64(string value, out byte[] bytes)
    {
        var buffer = new byte[value.Length];
        if (!Convert.TryFromBase64String(value, buffer, out var written))
        {
            bytes = [];
            return false;
        }

        bytes = buffer[..written];
        return true;
    }
}
