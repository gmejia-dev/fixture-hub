namespace FixtureHub.Api.Endpoints.Matches;

internal sealed record RegisterGoalRequest(Guid PlayerId, int Minute, bool IsOwnGoal);
