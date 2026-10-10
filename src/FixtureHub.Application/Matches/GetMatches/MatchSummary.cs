namespace FixtureHub.Application.Matches.GetMatches;

public sealed record MatchSummary(
    Guid Id,
    Guid HomeTeamId,
    string HomeTeamName,
    Guid AwayTeamId,
    string AwayTeamName,
    DateTimeOffset ScheduledAt,
    string Status,
    int HomeScore,
    int AwayScore);
