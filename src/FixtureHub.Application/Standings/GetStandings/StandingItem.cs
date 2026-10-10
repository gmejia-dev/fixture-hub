namespace FixtureHub.Application.Standings.GetStandings;

public sealed record StandingItem(
    int Position,
    Guid TeamId,
    string TeamName,
    int Played,
    int Won,
    int Drawn,
    int Lost,
    int GoalsFor,
    int GoalsAgainst,
    int GoalDifference,
    int Points);
