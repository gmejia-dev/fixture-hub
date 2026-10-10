using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Domain.Common;
using FixtureHub.Domain.Matches;

namespace FixtureHub.Application.Matches.StartMatch;

internal sealed class StartMatchHandler(IMatchRepository matches) : ICommandHandler<StartMatchCommand>
{
    public async Task<Result> HandleAsync(StartMatchCommand command, CancellationToken cancellationToken)
    {
        var match = await matches.GetByIdAsync(command.MatchId, cancellationToken);
        if (match is null)
            return MatchErrors.NotFound(command.MatchId);

        return match.Start();
    }
}
