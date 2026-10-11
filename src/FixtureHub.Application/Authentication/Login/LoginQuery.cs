using FixtureHub.Application.Abstractions.Messaging;

namespace FixtureHub.Application.Authentication.Login;

public sealed record LoginQuery(string Username, string Password) : IQuery<AuthToken>
{
    public override string ToString() => $"{nameof(LoginQuery)} {{ {nameof(Username)} = {Username} }}";
}
