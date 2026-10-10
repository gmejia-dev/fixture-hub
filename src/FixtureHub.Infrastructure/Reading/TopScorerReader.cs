using FixtureHub.Application.Abstractions.Reading;
using FixtureHub.Application.Pagination;
using FixtureHub.Application.TopScorers.GetTopScorers;
using FixtureHub.Domain.Matches;

namespace FixtureHub.Infrastructure.Reading;

internal sealed class TopScorerReader(SqlConnectionFactory connections) : ITopScorerReader
{
    public static readonly IReadOnlyDictionary<string, string> SortColumns =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["position"] = "[Position]",
            ["goals"] = "[Goals]",
            ["playerName"] = "[PlayerName]",
            ["teamName"] = "[TeamName]"
        };

    private const string Columns = "[Position], [PlayerId], [PlayerName], [TeamId], [TeamName], [Goals]";

    // Cuentan los goles no anulados que no son autogol, de partidos en curso o finalizados.
    // RANK: los empatados comparten posición y la siguiente se salta (1, 1, 3).
    private const string Scorers = """
        SELECT
            CAST(RANK() OVER (ORDER BY COUNT(*) DESC) AS int) AS [Position],
            p.[Id] AS [PlayerId],
            p.[Name] AS [PlayerName],
            t.[Id] AS [TeamId],
            t.[Name] AS [TeamName],
            COUNT(*) AS [Goals]
        FROM [Goals] g
        JOIN [Matches] m ON m.[Id] = g.[MatchId]
        JOIN [Players] p ON p.[Id] = g.[PlayerId]
        JOIN [Teams] t ON t.[Id] = p.[TeamId]
        WHERE g.[IsAnnulled] = 0
            AND g.[IsOwnGoal] = 0
            AND m.[IsDeleted] = 0
            AND m.[Status] IN (@InProgress, @Finished)
            AND t.[IsDeleted] = 0
        GROUP BY p.[Id], p.[Name], t.[Id], t.[Name]
        """;

    public async Task<PagedResult<TopScorerItem>> GetTopScorersAsync(GetTopScorersQuery query, CancellationToken cancellationToken)
    {
        var sql = new PagedSql(Scorers, Columns, SortColumns, "position", "[PlayerName]", "[PlayerId]")
            .Parameter("InProgress", nameof(MatchStatus.InProgress))
            .Parameter("Finished", nameof(MatchStatus.Finished));

        if (query.TeamId is { } teamId)
            sql.Where("[TeamId] = @TeamId", "TeamId", teamId);

        if (!string.IsNullOrWhiteSpace(query.PlayerName))
            sql.Where("[PlayerName] LIKE @PlayerName", "PlayerName", SqlLike.Contains(query.PlayerName));

        await using var connection = await connections.OpenAsync(cancellationToken);

        return await sql.QueryAsync<TopScorerItem>(connection, query, cancellationToken);
    }
}
