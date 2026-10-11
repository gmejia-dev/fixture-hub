using FixtureHub.Application.Abstractions.Authentication;
using FixtureHub.Application.Authentication.Login;

namespace FixtureHub.IntegrationTests.Authentication;

internal sealed class UnusedTokenIssuer : ITokenIssuer
{
    public AuthToken Issue(Guid userId, string username, string role) =>
        throw new NotSupportedException("Los tests de handlers no emiten tokens: el login se prueba por HTTP con la Api completa.");
}
