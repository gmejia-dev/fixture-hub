using FixtureHub.Domain.Teams;

namespace FixtureHub.Domain.Matches;

public sealed record GoalDetails(Player Scorer, int Minute, bool IsOwnGoal);
