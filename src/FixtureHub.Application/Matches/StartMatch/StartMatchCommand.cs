using FixtureHub.Application.Abstractions.Messaging;

namespace FixtureHub.Application.Matches.StartMatch;

public sealed record StartMatchCommand(Guid MatchId) : ICommand;
