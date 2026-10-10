using FixtureHub.Application.Abstractions.Messaging;

namespace FixtureHub.Application.Matches.CancelMatch;

public sealed record CancelMatchCommand(Guid MatchId) : ICommand;
