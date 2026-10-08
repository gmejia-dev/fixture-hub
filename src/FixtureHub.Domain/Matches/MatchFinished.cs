using FixtureHub.Domain.Common;

namespace FixtureHub.Domain.Matches;

public sealed record MatchFinished(
    Guid MatchId,
    Guid HomeTeamId,
    Guid AwayTeamId,
    int HomeScore,
    int AwayScore) : IDomainEvent;
