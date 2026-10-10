namespace FixtureHub.IntegrationTests.Api;

internal sealed record ScheduledMatch(Guid MatchId, Guid HomeTeamId, Guid HomePlayerId, Guid AwayTeamId, Guid AwayPlayerId);
