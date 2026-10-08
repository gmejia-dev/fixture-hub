using FixtureHub.Domain.Common;

namespace FixtureHub.Domain.Teams;

public sealed record TeamCreated(Guid TeamId, string Name) : IDomainEvent;
