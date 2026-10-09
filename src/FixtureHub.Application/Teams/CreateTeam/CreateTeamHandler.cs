using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Domain.Common;
using FixtureHub.Domain.Teams;

namespace FixtureHub.Application.Teams.CreateTeam;

internal sealed class CreateTeamHandler(ITeamRepository teams) : ICommandHandler<CreateTeamCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateTeamCommand command, CancellationToken cancellationToken)
    {
        var created = Team.Create(command.Name, command.Country);
        if (created.IsFailure)
            return created.Error;

        var team = created.Value;
        if (await teams.IsNameTakenAsync(team.Name, exceptTeamId: null, cancellationToken))
            return TeamErrors.NameTaken(team.Name);

        teams.Add(team);

        return team.Id;
    }
}
