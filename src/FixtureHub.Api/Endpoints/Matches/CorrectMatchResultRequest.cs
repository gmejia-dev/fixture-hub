using FixtureHub.Application.Matches.CorrectMatchResult;

namespace FixtureHub.Api.Endpoints.Matches;

internal sealed record CorrectMatchResultRequest(IReadOnlyList<CorrectedGoal>? Goals, string Reason);
