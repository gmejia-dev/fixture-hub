using FixtureHub.Application.Authentication.Login;

namespace FixtureHub.Application.Abstractions.Reading;

public interface IUserReader
{
    Task<UserCredentials?> FindByUsernameAsync(string username, CancellationToken cancellationToken);
}
