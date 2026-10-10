using FixtureHub.Application.Abstractions.Reading;
using FixtureHub.Application.Pagination;
using FixtureHub.Application.TopScorers.GetTopScorers;

namespace FixtureHub.UnitTests.Application.Fakes;

internal sealed class FakeTopScorerReader : ITopScorerReader
{
    public Task<PagedResult<TopScorerItem>> GetTopScorersAsync(GetTopScorersQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(new PagedResult<TopScorerItem>([], query.PageNumber, query.PageSize, 0));
}
