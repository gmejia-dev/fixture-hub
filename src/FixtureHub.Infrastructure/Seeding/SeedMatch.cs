using FixtureHub.Domain.Matches;

namespace FixtureHub.Infrastructure.Seeding;

internal sealed record SeedMatch(int Round, TimeOnly KickOff, int Home, int Away, MatchStatus Status, params IReadOnlyList<SeedGoal> Goals);
