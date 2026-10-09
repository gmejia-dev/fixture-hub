using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Domain.Common;
using FixtureHub.Domain.Teams;

namespace FixtureHub.Application.Teams.RemovePlayer;

internal sealed class RemovePlayerHandler(ITeamRepository teams) : ICommandHandler<RemovePlayerCommand>
{
    public async Task<Result> HandleAsync(RemovePlayerCommand command, CancellationToken cancellationToken)
    {
        var team = await teams.GetByIdAsync(command.TeamId, cancellationToken);
        if (team is null)
            return TeamErrors.NotFound(command.TeamId);

        return team.RemovePlayer(command.PlayerId);
    }
}
