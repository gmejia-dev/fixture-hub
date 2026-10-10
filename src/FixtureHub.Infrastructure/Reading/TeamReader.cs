using Dapper;
using FixtureHub.Application.Abstractions.Reading;
using FixtureHub.Application.Pagination;
using FixtureHub.Application.Teams.GetPlayers;
using FixtureHub.Application.Teams.GetTeamById;
using FixtureHub.Application.Teams.GetTeams;

namespace FixtureHub.Infrastructure.Reading;

internal sealed class TeamReader(SqlConnectionFactory connections) : ITeamReader
{
    public static readonly IReadOnlyDictionary<string, string> TeamSortColumns =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = "[Name]",
            ["country"] = "[Country]",
            ["playerCount"] = "[PlayerCount]"
        };

    public static readonly IReadOnlyDictionary<string, string> PlayerSortColumns =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["shirtNumber"] = "[ShirtNumber]",
            ["name"] = "[Name]"
        };

    private const string Teams = """
        SELECT
            t.[Id],
            t.[Name],
            t.[Country],
            (SELECT COUNT(*) FROM [Players] p WHERE p.[TeamId] = t.[Id] AND p.[IsDeleted] = 0) AS [PlayerCount]
        FROM [Teams] t
        WHERE t.[IsDeleted] = 0
        """;

    private const string Players = """
        SELECT p.[Id], p.[Name], p.[ShirtNumber]
        FROM [Players] p
        WHERE p.[TeamId] = @TeamId AND p.[IsDeleted] = 0
        """;

    private const string TeamWithPlayers = """
        SELECT [Id], [Name], [Country] FROM [Teams] WHERE [Id] = @TeamId AND [IsDeleted] = 0;

        SELECT [Id], [Name], [ShirtNumber]
        FROM [Players]
        WHERE [TeamId] = @TeamId AND [IsDeleted] = 0
        ORDER BY [ShirtNumber];
        """;

    private const string TeamExists = """
        SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM [Teams] WHERE [Id] = @TeamId AND [IsDeleted] = 0) THEN 1 ELSE 0 END AS bit)
        """;

    public async Task<PagedResult<TeamSummary>> GetTeamsAsync(GetTeamsQuery query, CancellationToken cancellationToken)
    {
        var sql = new PagedSql(Teams, "[Id], [Name], [Country], [PlayerCount]", TeamSortColumns, "name", "[Id]");

        if (!string.IsNullOrWhiteSpace(query.Name))
            sql.Where("[Name] LIKE @Name", "Name", SqlLike.Contains(query.Name));

        if (!string.IsNullOrWhiteSpace(query.Country))
            sql.Where("[Country] LIKE @Country", "Country", SqlLike.Contains(query.Country));

        await using var connection = await connections.OpenAsync(cancellationToken);

        return await sql.QueryAsync<TeamSummary>(connection, query, cancellationToken);
    }

    public async Task<TeamDetails?> GetTeamAsync(Guid teamId, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var results = await connection.QueryMultipleAsync(
            new CommandDefinition(TeamWithPlayers, new { TeamId = teamId }, cancellationToken: cancellationToken));

        var team = await results.ReadSingleOrDefaultAsync<TeamRow>();
        if (team is null)
            return null;

        var players = (await results.ReadAsync<PlayerItem>()).AsList();

        return new TeamDetails(team.Id, team.Name, team.Country, players);
    }

    public async Task<PagedResult<PlayerItem>?> GetPlayersAsync(GetPlayersQuery query, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);

        var exists = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(TeamExists, new { query.TeamId }, cancellationToken: cancellationToken));
        if (!exists)
            return null;

        var sql = new PagedSql(Players, "[Id], [Name], [ShirtNumber]", PlayerSortColumns, "shirtNumber", "[Id]")
            .Parameter("TeamId", query.TeamId);

        if (!string.IsNullOrWhiteSpace(query.Name))
            sql.Where("[Name] LIKE @Name", "Name", SqlLike.Contains(query.Name));

        return await sql.QueryAsync<PlayerItem>(connection, query, cancellationToken);
    }

    private sealed record TeamRow(Guid Id, string Name, string Country);
}
