namespace FixtureHub.Application.Matches.GetMatchById;

public sealed record GoalItem(
    Guid Id,
    Guid PlayerId,
    string PlayerName,
    Guid ScoringTeamId,
    int Minute,
    bool IsOwnGoal,
    bool IsAnnulled);
