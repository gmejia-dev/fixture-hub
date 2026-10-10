using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Domain.Common;
using FixtureHub.Domain.Teams;

namespace FixtureHub.Application.Teams.UpdatePlayer;

internal sealed class UpdatePlayerHandler(ITeamRepository teams) : ICommandHandler<UpdatePlayerCommand>
{
    public async Task<Result> HandleAsync(UpdatePlayerCommand command, CancellationToken cancellationToken)
    {
        var team = await teams.GetByIdAsync(command.TeamId, cancellationToken);
        if (team is null)
            return TeamErrors.NotFound(command.TeamId);

        return team.UpdatePlayer(command.PlayerId, command.Name, command.ShirtNumber);
    }
}
