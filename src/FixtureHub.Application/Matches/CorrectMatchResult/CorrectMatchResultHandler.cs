using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Domain.Common;
using FixtureHub.Domain.Matches;

namespace FixtureHub.Application.Matches.CorrectMatchResult;

internal sealed class CorrectMatchResultHandler(IMatchRepository matches, ITeamRepository teams)
    : ICommandHandler<CorrectMatchResultCommand>
{
    public async Task<Result> HandleAsync(CorrectMatchResultCommand command, CancellationToken cancellationToken)
    {
        var match = await matches.GetByIdAsync(command.MatchId, cancellationToken);
        if (match is null)
            return MatchErrors.NotFound(command.MatchId);

        var roster = await MatchRoster.LoadAsync(teams, match, cancellationToken);
        var goals = new List<GoalDetails>();

        foreach (var goal in command.Goals)
        {
            var scorer = roster.FindPlayer(goal.PlayerId);
            if (scorer is null)
                return MatchErrors.PlayerNotInMatch(goal.PlayerId);

            goals.Add(new GoalDetails(scorer, goal.Minute, goal.IsOwnGoal));
        }

        return match.CorrectResult(goals, command.Reason);
    }
}
