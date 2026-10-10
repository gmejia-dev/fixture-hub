using FixtureHub.Application.Abstractions.Messaging;

namespace FixtureHub.Application.Matches.DeleteMatch;

public sealed record DeleteMatchCommand(Guid MatchId) : ICommand;
