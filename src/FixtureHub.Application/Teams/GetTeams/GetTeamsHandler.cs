using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Abstractions.Reading;
using FixtureHub.Application.Pagination;
using FixtureHub.Domain.Common;

namespace FixtureHub.Application.Teams.GetTeams;

internal sealed class GetTeamsHandler(ITeamReader teams) : IQueryHandler<GetTeamsQuery, PagedResult<TeamSummary>>
{
    public async Task<Result<PagedResult<TeamSummary>>> HandleAsync(GetTeamsQuery query, CancellationToken cancellationToken) =>
        await teams.GetTeamsAsync(query, cancellationToken);
}
