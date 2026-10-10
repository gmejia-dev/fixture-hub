using FixtureHub.Application.Abstractions.Reading;
using FixtureHub.Application.Pagination;
using FixtureHub.Application.Standings.GetStandings;

namespace FixtureHub.Infrastructure.Reading;

internal sealed class StandingsReader(SqlConnectionFactory connections) : IStandingsReader
{
    public static readonly IReadOnlyDictionary<string, string> SortColumns =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["position"] = "[Position]",
            ["teamName"] = "[TeamName]",
            ["played"] = "[Played]",
            ["won"] = "[Won]",
            ["drawn"] = "[Drawn]",
            ["lost"] = "[Lost]",
            ["goalsFor"] = "[GoalsFor]",
            ["goalsAgainst"] = "[GoalsAgainst]",
            ["goalDifference"] = "[GoalDifference]",
            ["points"] = "[Points]"
        };

    private const string Columns =
        "[Position], [TeamId], [TeamName], [Played], [Won], [Drawn], [Lost], [GoalsFor], [GoalsAgainst], [GoalDifference], [Points]";

    // La posición se calcula ANTES de filtrar: ROW_NUMBER vive dentro del CTE y el filtro por nombre
    // se aplica afuera, así que filtrar no renumera la tabla.
    private const string Standings = """
        SELECT
            CAST(ROW_NUMBER() OVER (
                ORDER BY s.[Points] DESC, s.[GoalDifference] DESC, s.[GoalsFor] DESC, t.[Name], t.[Id]) AS int) AS [Position],
            s.[TeamId],
            t.[Name] AS [TeamName],
            s.[Played],
            s.[Won],
            s.[Drawn],
            s.[Lost],
            s.[GoalsFor],
            s.[GoalsAgainst],
            s.[GoalDifference],
            s.[Points]
        FROM [TeamStandings] s
        JOIN [Teams] t ON t.[Id] = s.[TeamId]
        WHERE t.[IsDeleted] = 0
        """;

    public async Task<PagedResult<StandingItem>> GetStandingsAsync(GetStandingsQuery query, CancellationToken cancellationToken)
    {
        var sql = new PagedSql(Standings, Columns, SortColumns, "position", "[Position]");

        if (!string.IsNullOrWhiteSpace(query.TeamName))
            sql.Where("[TeamName] LIKE @TeamName", "TeamName", SqlLike.Contains(query.TeamName));

        await using var connection = await connections.OpenAsync(cancellationToken);

        return await sql.QueryAsync<StandingItem>(connection, query, cancellationToken);
    }
}
