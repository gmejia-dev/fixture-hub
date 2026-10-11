using FixtureHub.Application.Authentication.Login;

namespace FixtureHub.Application.Abstractions.Authentication;

public interface ITokenIssuer
{
    AuthToken Issue(Guid userId, string username, string role);
}
