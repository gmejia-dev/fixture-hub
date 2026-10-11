using FixtureHub.Application.Abstractions.Authentication;

namespace FixtureHub.UnitTests.Application.Fakes;

internal sealed class FakePasswordHasher : IPasswordHasher
{
    public List<string?> VerifiedHashes { get; } = [];

    public string Hash(string password) => $"hash:{password}";

    public bool Verify(string password, string? passwordHash)
    {
        VerifiedHashes.Add(passwordHash);
        return passwordHash == Hash(password);
    }
}
