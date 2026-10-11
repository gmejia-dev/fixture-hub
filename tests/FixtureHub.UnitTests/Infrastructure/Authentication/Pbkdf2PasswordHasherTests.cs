using System.Security.Cryptography;
using FixtureHub.Infrastructure.Authentication;

namespace FixtureHub.UnitTests.Infrastructure.Authentication;

public class Pbkdf2PasswordHasherTests
{
    private const string Password = "contraseña-de-prueba";

    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Verify_WithTheSamePassword_ReturnsTrue()
    {
        var hash = _hasher.Hash(Password);

        Assert.True(_hasher.Verify(Password, hash));
    }

    [Fact]
    public void Verify_WithAnotherPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash(Password);

        Assert.False(_hasher.Verify("Contraseña-de-prueba", hash));
    }

    [Fact]
    public void Hash_HasTheAlgorithmTheIterationsTheSaltAndTheHashWithoutThePassword()
    {
        var parts = _hasher.Hash(Password).Split('$');

        Assert.Equal(4, parts.Length);
        Assert.Equal("PBKDF2-SHA256", parts[0]);
        Assert.Equal("600000", parts[1]);
        Assert.Equal(16, Convert.FromBase64String(parts[2]).Length);
        Assert.Equal(32, Convert.FromBase64String(parts[3]).Length);
    }

    [Fact]
    public void Hash_TheSamePasswordTwice_UsesADifferentSalt()
    {
        Assert.NotEqual(_hasher.Hash(Password), _hasher.Hash(Password));
    }

    [Fact]
    public void Verify_AHashWithFewerIterations_StillVerifies()
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(Password, salt, 1_000, HashAlgorithmName.SHA256, 32);
        var stored = $"PBKDF2-SHA256$1000${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";

        Assert.True(_hasher.Verify(Password, stored));
        Assert.False(_hasher.Verify("otra-contraseña", stored));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("contraseña-de-prueba")]
    [InlineData("PBKDF2-SHA256$600000$sal")]
    [InlineData("PBKDF2-SHA256$muchas$c2Fs$aGFzaA==")]
    [InlineData("PBKDF2-SHA256$0$c2Fs$aGFzaA==")]
    [InlineData("PBKDF2-SHA256$1000$no-es-base64$aGFzaA==")]
    [InlineData("MD5$1000$c2Fs$aGFzaA==")]
    public void Verify_WithoutAValidHash_ReturnsFalse(string? stored)
    {
        Assert.False(_hasher.Verify(Password, stored));
    }
}
