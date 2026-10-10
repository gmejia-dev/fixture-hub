using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Domain.Common;
using FixtureHub.Domain.Matches;

namespace FixtureHub.Application.Matches.CancelMatch;

internal sealed class CancelMatchHandler(IMatchRepository matches) : ICommandHandler<CancelMatchCommand>
{
    public async Task<Result> HandleAsync(CancelMatchCommand command, CancellationToken cancellationToken)
    {
        var match = await matches.GetByIdAsync(command.MatchId, cancellationToken);
        if (match is null)
            return MatchErrors.NotFound(command.MatchId);

        return match.Cancel();
    }
}
