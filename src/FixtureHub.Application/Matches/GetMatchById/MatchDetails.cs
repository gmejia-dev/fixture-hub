namespace FixtureHub.Application.Matches.GetMatchById;

public sealed record MatchDetails(
    Guid Id,
    Guid HomeTeamId,
    string HomeTeamName,
    Guid AwayTeamId,
    string AwayTeamName,
    DateTimeOffset ScheduledAt,
    string Status,
    int HomeScore,
    int AwayScore,
    IReadOnlyList<GoalItem> Goals,
    IReadOnlyList<CorrectionItem> Corrections);
