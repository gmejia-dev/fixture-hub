using FixtureHub.Application.Pagination;
using FixtureHub.Application.Teams.GetPlayers;
using FixtureHub.Application.Teams.GetTeamById;
using FixtureHub.Application.Teams.GetTeams;

namespace FixtureHub.Application.Abstractions.Reading;

public interface ITeamReader
{
    Task<PagedResult<TeamSummary>> GetTeamsAsync(GetTeamsQuery query, CancellationToken cancellationToken);

    Task<TeamDetails?> GetTeamAsync(Guid teamId, CancellationToken cancellationToken);

    Task<PagedResult<PlayerItem>?> GetPlayersAsync(GetPlayersQuery query, CancellationToken cancellationToken);
}
