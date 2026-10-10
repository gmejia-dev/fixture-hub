namespace FixtureHub.Application.TopScorers.GetTopScorers;

public sealed record TopScorerItem(
    int Position,
    Guid PlayerId,
    string PlayerName,
    Guid TeamId,
    string TeamName,
    int Goals);
