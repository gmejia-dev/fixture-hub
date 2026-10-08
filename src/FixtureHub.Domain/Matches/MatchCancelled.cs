using FixtureHub.Domain.Common;

namespace FixtureHub.Domain.Matches;

public sealed record MatchCancelled(Guid MatchId) : IDomainEvent;
