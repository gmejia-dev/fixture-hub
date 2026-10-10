using FixtureHub.Application.Abstractions.Messaging;

namespace FixtureHub.Application.Matches.ScheduleMatch;

public sealed record ScheduleMatchCommand(Guid HomeTeamId, Guid AwayTeamId, DateTimeOffset ScheduledAt) : ICommand<Guid>;
