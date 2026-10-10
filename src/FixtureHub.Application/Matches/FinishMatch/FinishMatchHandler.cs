using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Domain.Common;
using FixtureHub.Domain.Matches;

namespace FixtureHub.Application.Matches.FinishMatch;

internal sealed class FinishMatchHandler(IMatchRepository matches) : ICommandHandler<FinishMatchCommand>
{
    public async Task<Result> HandleAsync(FinishMatchCommand command, CancellationToken cancellationToken)
    {
        var match = await matches.GetByIdAsync(command.MatchId, cancellationToken);
        if (match is null)
            return MatchErrors.NotFound(command.MatchId);

        return match.Finish();
    }
}
