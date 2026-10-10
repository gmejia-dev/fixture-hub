using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Abstractions.Reading;
using FixtureHub.Application.Pagination;
using FixtureHub.Domain.Common;

namespace FixtureHub.Application.Standings.GetStandings;

internal sealed class GetStandingsHandler(IStandingsReader standings)
    : IQueryHandler<GetStandingsQuery, PagedResult<StandingItem>>
{
    public async Task<Result<PagedResult<StandingItem>>> HandleAsync(GetStandingsQuery query, CancellationToken cancellationToken) =>
        await standings.GetStandingsAsync(query, cancellationToken);
}
