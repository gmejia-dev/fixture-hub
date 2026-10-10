using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Abstractions.Reading;
using FixtureHub.Application.Pagination;
using FixtureHub.Domain.Common;
using FixtureHub.Domain.Teams;

namespace FixtureHub.Application.Teams.GetPlayers;

internal sealed class GetPlayersHandler(ITeamReader teams) : IQueryHandler<GetPlayersQuery, PagedResult<PlayerItem>>
{
    public async Task<Result<PagedResult<PlayerItem>>> HandleAsync(GetPlayersQuery query, CancellationToken cancellationToken)
    {
        var players = await teams.GetPlayersAsync(query, cancellationToken);
        if (players is null)
            return TeamErrors.NotFound(query.TeamId);

        return players;
    }
}
