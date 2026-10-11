using FixtureHub.Application.Abstractions.Authentication;
using FixtureHub.Application.Authentication.Login;

namespace FixtureHub.UnitTests.Application.Fakes;

internal sealed class FakeTokenIssuer : ITokenIssuer
{
    public static readonly DateTimeOffset ExpiresAt = new(2026, 11, 7, 16, 0, 0, TimeSpan.Zero);

    public AuthToken Issue(Guid userId, string username, string role) =>
        new($"token:{userId}:{username}:{role}", AuthToken.BearerType, ExpiresAt);
}
