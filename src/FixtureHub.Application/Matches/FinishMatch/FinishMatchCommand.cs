using FixtureHub.Application.Abstractions.Messaging;

namespace FixtureHub.Application.Matches.FinishMatch;

public sealed record FinishMatchCommand(Guid MatchId) : ICommand;
