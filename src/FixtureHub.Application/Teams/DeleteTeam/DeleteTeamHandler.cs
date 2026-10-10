using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Domain.Common;
using FixtureHub.Domain.Teams;

namespace FixtureHub.Application.Teams.DeleteTeam;

internal sealed class DeleteTeamHandler(ITeamRepository teams, IMatchRepository matches)
    : ICommandHandler<DeleteTeamCommand>
{
    public async Task<Result> HandleAsync(DeleteTeamCommand command, CancellationToken cancellationToken)
    {
        var team = await teams.GetByIdIncludingDeletedAsync(command.TeamId, cancellationToken);
        if (team is null)
            return TeamErrors.NotFound(command.TeamId);

        if (team.IsDeleted)
            return Result.Success();

        if (await matches.HasActiveMatchesAsync(team.Id, cancellationToken))
            return TeamErrors.HasActiveMatches(team.Id);

        team.Delete();

        return Result.Success();
    }
}
