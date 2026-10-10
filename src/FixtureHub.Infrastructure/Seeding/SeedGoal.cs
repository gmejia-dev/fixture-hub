namespace FixtureHub.Infrastructure.Seeding;

internal sealed record SeedGoal(int Minute, int Team, int ShirtNumber, bool IsOwnGoal = false, bool IsAnnulled = false);
