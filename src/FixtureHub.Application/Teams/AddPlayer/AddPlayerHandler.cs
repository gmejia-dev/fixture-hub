using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Domain.Common;
using FixtureHub.Domain.Teams;

namespace FixtureHub.Application.Teams.AddPlayer;

internal sealed class AddPlayerHandler(ITeamRepository teams) : ICommandHandler<AddPlayerCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(AddPlayerCommand command, CancellationToken cancellationToken)
    {
        var team = await teams.GetByIdAsync(command.TeamId, cancellationToken);
        if (team is null)
            return TeamErrors.NotFound(command.TeamId);

        var added = team.AddPlayer(command.Name, command.ShirtNumber);
        if (added.IsFailure)
            return added.Error;

        return added.Value.Id;
    }
}
