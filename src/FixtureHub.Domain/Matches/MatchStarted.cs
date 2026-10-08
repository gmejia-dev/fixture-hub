using FixtureHub.Domain.Common;

namespace FixtureHub.Domain.Matches;

public sealed record MatchStarted(Guid MatchId, Guid HomeTeamId, Guid AwayTeamId) : IDomainEvent;
