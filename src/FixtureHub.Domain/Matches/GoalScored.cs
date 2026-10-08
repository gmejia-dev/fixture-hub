using FixtureHub.Domain.Common;

namespace FixtureHub.Domain.Matches;

public sealed record GoalScored(
    Guid MatchId,
    Guid GoalId,
    Guid PlayerId,
    Guid ScoringTeamId,
    int Minute,
    bool IsOwnGoal,
    int HomeScore,
    int AwayScore) : IDomainEvent;
