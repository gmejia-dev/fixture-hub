namespace FixtureHub.Application.Matches.CorrectMatchResult;

public sealed record CorrectedGoal(Guid PlayerId, int Minute, bool IsOwnGoal);
