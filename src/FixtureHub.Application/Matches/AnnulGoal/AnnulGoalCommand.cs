using FixtureHub.Application.Abstractions.Messaging;

namespace FixtureHub.Application.Matches.AnnulGoal;

public sealed record AnnulGoalCommand(Guid MatchId, Guid GoalId) : ICommand;
