using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Abstractions.Reading;
using FixtureHub.Application.Pagination;
using FixtureHub.Domain.Common;

namespace FixtureHub.Application.TopScorers.GetTopScorers;

internal sealed class GetTopScorersHandler(ITopScorerReader scorers)
    : IQueryHandler<GetTopScorersQuery, PagedResult<TopScorerItem>>
{
    public async Task<Result<PagedResult<TopScorerItem>>> HandleAsync(GetTopScorersQuery query, CancellationToken cancellationToken) =>
        await scorers.GetTopScorersAsync(query, cancellationToken);
}
