using FixtureHub.Application.Pagination;
using FixtureHub.Application.TopScorers.GetTopScorers;

namespace FixtureHub.Application.Abstractions.Reading;

public interface ITopScorerReader
{
    Task<PagedResult<TopScorerItem>> GetTopScorersAsync(GetTopScorersQuery query, CancellationToken cancellationToken);
}
