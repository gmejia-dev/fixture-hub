using FixtureHub.Domain.Common;

namespace FixtureHub.Domain.Matches;

public sealed record MatchResultCorrected(
    Guid MatchId,
    Guid HomeTeamId,
    Guid AwayTeamId,
    int PreviousHomeScore,
    int PreviousAwayScore,
    int NewHomeScore,
    int NewAwayScore) : IDomainEvent;