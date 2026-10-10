using FixtureHub.Application.Pagination;
using FixtureHub.Application.Standings.GetStandings;

namespace FixtureHub.Application.Abstractions.Reading;

public interface IStandingsReader
{
    Task<PagedResult<StandingItem>> GetStandingsAsync(GetStandingsQuery query, CancellationToken cancellationToken);
}
