namespace FixtureHub.Application.Matches.GetMatchById;

public sealed record CorrectionItem(
    Guid Id,
    int PreviousHomeScore,
    int PreviousAwayScore,
    int NewHomeScore,
    int NewAwayScore,
    string Reason);
