using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Domain.Common;
using FixtureHub.Domain.Matches;

namespace FixtureHub.Application.Matches.DeleteMatch;

internal sealed class DeleteMatchHandler(IMatchRepository matches) : ICommandHandler<DeleteMatchCommand>
{
    public async Task<Result> HandleAsync(DeleteMatchCommand command, CancellationToken cancellationToken)
    {
        var match = await matches.GetByIdIncludingDeletedAsync(command.MatchId, cancellationToken);
        if (match is null)
            return MatchErrors.NotFound(command.MatchId);

        return match.Delete();
    }
}
