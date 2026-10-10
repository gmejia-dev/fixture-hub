using FixtureHub.Application.Abstractions.Messaging;

namespace FixtureHub.Application.Matches.RegisterGoal;

public sealed record RegisterGoalCommand(Guid MatchId, Guid PlayerId, int Minute, bool IsOwnGoal) : ICommand<Guid>;
