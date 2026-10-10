using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Abstractions.Reading;
using FixtureHub.Domain.Common;
using FixtureHub.Domain.Teams;

namespace FixtureHub.Application.Teams.GetTeamById;

internal sealed class GetTeamByIdHandler(ITeamReader teams) : IQueryHandler<GetTeamByIdQuery, TeamDetails>
{
    public async Task<Result<TeamDetails>> HandleAsync(GetTeamByIdQuery query, CancellationToken cancellationToken)
    {
        var team = await teams.GetTeamAsync(query.TeamId, cancellationToken);
        if (team is null)
            return TeamErrors.NotFound(query.TeamId);

        return team;
    }
}
