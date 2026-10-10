using FixtureHub.Application.Abstractions.Messaging;

namespace FixtureHub.Application.Matches.CorrectMatchResult;

public sealed record CorrectMatchResultCommand(Guid MatchId, IReadOnlyList<CorrectedGoal> Goals, string Reason)
    : ICommand;
