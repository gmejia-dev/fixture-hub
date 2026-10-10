using FixtureHub.Application.Abstractions.Reading;
using FixtureHub.Application.Pagination;
using FixtureHub.Application.Teams.GetPlayers;
using FixtureHub.Application.Teams.GetTeamById;
using FixtureHub.Application.Teams.GetTeams;

namespace FixtureHub.UnitTests.Application.Fakes;

internal sealed class FakeTeamReader : ITeamReader
{
    public List<object> Queries { get; } = [];

    public TeamDetails? Team { get; set; }

    public PagedResult<PlayerItem>? Players { get; set; }

    public Task<PagedResult<TeamSummary>> GetTeamsAsync(GetTeamsQuery query, CancellationToken cancellationToken)
    {
        Queries.Add(query);
        return Task.FromResult(new PagedResult<TeamSummary>([], query.PageNumber, query.PageSize, 0));
    }

    public Task<TeamDetails?> GetTeamAsync(Guid teamId, CancellationToken cancellationToken)
    {
        Queries.Add(teamId);
        return Task.FromResult(Team);
    }

    public Task<PagedResult<PlayerItem>?> GetPlayersAsync(GetPlayersQuery query, CancellationToken cancellationToken)
    {
        Queries.Add(query);
        return Task.FromResult(Players);
    }
}
