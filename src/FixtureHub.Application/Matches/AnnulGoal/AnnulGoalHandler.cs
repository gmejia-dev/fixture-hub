using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Domain.Common;
using FixtureHub.Domain.Matches;

namespace FixtureHub.Application.Matches.AnnulGoal;

internal sealed class AnnulGoalHandler(IMatchRepository matches) : ICommandHandler<AnnulGoalCommand>
{
    public async Task<Result> HandleAsync(AnnulGoalCommand command, CancellationToken cancellationToken)
    {
        var match = await matches.GetByIdAsync(command.MatchId, cancellationToken);
        if (match is null)
            return MatchErrors.NotFound(command.MatchId);

        return match.AnnulGoal(command.GoalId);
    }
}
