namespace FixtureHub.Api.Endpoints.Matches;

internal sealed record ScheduleMatchRequest(Guid HomeTeamId, Guid AwayTeamId, DateTimeOffset ScheduledAt);
