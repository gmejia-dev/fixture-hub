using FixtureHub.Application.Abstractions.Messaging;

namespace FixtureHub.Application.Matches.RescheduleMatch;

public sealed record RescheduleMatchCommand(Guid MatchId, DateTimeOffset ScheduledAt) : ICommand;
