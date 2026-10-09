using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Domain.Common;
using FixtureHub.Domain.Teams;

namespace FixtureHub.Application.Teams.UpdateTeam;

internal sealed class UpdateTeamHandler(ITeamRepository teams) : ICommandHandler<UpdateTeamCommand>
{
    public async Task<Result> HandleAsync(UpdateTeamCommand command, CancellationToken cancellationToken)
    {
        var team = await teams.GetByIdAsync(command.TeamId, cancellationToken);
        if (team is null)
            return TeamErrors.NotFound(command.TeamId);

        var updated = team.Update(command.Name, command.Country);
        if (updated.IsFailure)
            return updated;

        if (await teams.IsNameTakenAsync(team.Name, exceptTeamId: team.Id, cancellationToken))
            return TeamErrors.NameTaken(team.Name);

        return Result.Success();
    }
}
