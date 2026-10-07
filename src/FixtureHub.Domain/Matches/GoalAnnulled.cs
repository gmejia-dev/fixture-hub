using FixtureHub.Domain.Common;

namespace FixtureHub.Domain.Matches;

public sealed record GoalAnnulled(Guid MatchId, Guid GoalId, int HomeScore, int AwayScore) : IDomainEvent;