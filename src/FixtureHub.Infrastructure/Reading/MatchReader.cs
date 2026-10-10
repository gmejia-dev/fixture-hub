using Dapper;
using FixtureHub.Application.Abstractions.Reading;
using FixtureHub.Application.Matches.GetMatchById;
using FixtureHub.Application.Matches.GetMatches;
using FixtureHub.Application.Pagination;

namespace FixtureHub.Infrastructure.Reading;

internal sealed class MatchReader(SqlConnectionFactory connections) : IMatchReader
{
    public static readonly IReadOnlyDictionary<string, string> SortColumns =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["scheduledAt"] = "[ScheduledAt]",
            ["homeTeam"] = "[HomeTeamName]",
            ["awayTeam"] = "[AwayTeamName]",
            ["status"] = "[Status]"
        };

    private const string Columns =
        "[Id], [HomeTeamId], [HomeTeamName], [AwayTeamId], [AwayTeamName], [ScheduledAt], [Status], [HomeScore], [AwayScore]";

    private const string Matches = """
        SELECT
            m.[Id],
            m.[HomeTeamId],
            home.[Name] AS [HomeTeamName],
            m.[AwayTeamId],
            away.[Name] AS [AwayTeamName],
            m.[ScheduledAt],
            m.[Status],
            m.[HomeScore],
            m.[AwayScore]
        FROM [Matches] m
        JOIN [Teams] home ON home.[Id] = m.[HomeTeamId]
        JOIN [Teams] away ON away.[Id] = m.[AwayTeamId]
        WHERE m.[IsDeleted] = 0
        """;

    private const string MatchWithGoalsAndCorrections = $"""
        WITH [Rows] AS ({Matches})
        SELECT {Columns} FROM [Rows] WHERE [Id] = @MatchId;

        SELECT g.[Id], g.[PlayerId], p.[Name] AS [PlayerName], g.[ScoringTeamId], g.[Minute], g.[IsOwnGoal], g.[IsAnnulled]
        FROM [Goals] g
        JOIN [Players] p ON p.[Id] = g.[PlayerId]
        WHERE g.[MatchId] = @MatchId;

        SELECT [Id], [PreviousHomeScore], [PreviousAwayScore], [NewHomeScore], [NewAwayScore], [Reason]
        FROM [MatchResultCorrections]
        WHERE [MatchId] = @MatchId;
        """;

    public async Task<PagedResult<MatchSummary>> GetMatchesAsync(GetMatchesQuery query, CancellationToken cancellationToken)
    {
        var sql = new PagedSql(Matches, Columns, SortColumns, "scheduledAt", "[Id]");

        if (query.TeamId is { } teamId)
            sql.Where("([HomeTeamId] = @TeamId OR [AwayTeamId] = @TeamId)", "TeamId", teamId);

        if (!string.IsNullOrWhiteSpace(query.Status))
            sql.Where("[Status] = @Status", "Status", query.Status);

        if (query.From is { } from)
            sql.Where("[ScheduledAt] >= @From", "From", from);

        if (query.To is { } to)
            sql.Where("[ScheduledAt] <= @To", "To", to);

        await using var connection = await connections.OpenAsync(cancellationToken);

        return await sql.QueryAsync<MatchSummary>(connection, query, cancellationToken);
    }

    public async Task<MatchDetails?> GetMatchAsync(Guid matchId, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var results = await connection.QueryMultipleAsync(
            new CommandDefinition(MatchWithGoalsAndCorrections, new { MatchId = matchId }, cancellationToken: cancellationToken));

        var match = await results.ReadSingleOrDefaultAsync<MatchSummary>();
        if (match is null)
            return null;

        var goals = (await results.ReadAsync<GoalItem>())
            .OrderBy(goal => goal.Minute)
            .ThenBy(goal => goal.Id)
            .ToList();

        var corrections = (await results.ReadAsync<CorrectionItem>())
            .OrderBy(correction => correction.Id)
            .ToList();

        return new MatchDetails(
            match.Id,
            match.HomeTeamId,
            match.HomeTeamName,
            match.AwayTeamId,
            match.AwayTeamName,
            match.ScheduledAt,
            match.Status,
            match.HomeScore,
            match.AwayScore,
            goals,
            corrections);
    }
}
