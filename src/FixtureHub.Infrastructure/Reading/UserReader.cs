using Dapper;
using FixtureHub.Application.Abstractions.Reading;
using FixtureHub.Application.Authentication.Login;

namespace FixtureHub.Infrastructure.Reading;

internal sealed class UserReader(SqlConnectionFactory connections) : IUserReader
{
    private const string UserByUsername = """
        SELECT [Id] AS [UserId], [Username], [PasswordHash], [Role]
        FROM [Users]
        WHERE [Username] = @Username
        """;

    public async Task<UserCredentials?> FindByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<UserCredentials>(
            new CommandDefinition(UserByUsername, new { Username = username }, cancellationToken: cancellationToken));
    }
}
