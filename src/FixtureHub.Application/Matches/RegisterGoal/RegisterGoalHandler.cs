using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Domain.Common;
using FixtureHub.Domain.Matches;

namespace FixtureHub.Application.Matches.RegisterGoal;

internal sealed class RegisterGoalHandler(IMatchRepository matches, ITeamRepository teams)
    : ICommandHandler<RegisterGoalCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(RegisterGoalCommand command, CancellationToken cancellationToken)
    {
        var match = await matches.GetByIdAsync(command.MatchId, cancellationToken);
        if (match is null)
            return MatchErrors.NotFound(command.MatchId);

        var roster = await MatchRoster.LoadAsync(teams, match, cancellationToken);
        var scorer = roster.FindPlayer(command.PlayerId);
        if (scorer is null)
            return MatchErrors.PlayerNotInMatch(command.PlayerId);

        var goal = match.AddGoal(scorer, command.Minute, command.IsOwnGoal);
        if (goal.IsFailure)
            return goal.Error;

        return goal.Value.Id;
    }
}
