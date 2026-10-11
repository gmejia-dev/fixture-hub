using FixtureHub.Application.Abstractions.Reading;
using FixtureHub.Application.Authentication.Login;

namespace FixtureHub.UnitTests.Application.Fakes;

internal sealed class FakeUserReader : IUserReader
{
    public List<UserCredentials> Users { get; } = [];

    public Task<UserCredentials?> FindByUsernameAsync(string username, CancellationToken cancellationToken) =>
        Task.FromResult(Users.FirstOrDefault(user => user.Username == username));
}
