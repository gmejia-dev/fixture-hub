using FixtureHub.Application.Abstractions.Reading;
using FixtureHub.Application.Pagination;
using FixtureHub.Application.Standings.GetStandings;

namespace FixtureHub.UnitTests.Application.Fakes;

internal sealed class FakeStandingsReader : IStandingsReader
{
    public Task<PagedResult<StandingItem>> GetStandingsAsync(GetStandingsQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(new PagedResult<StandingItem>([], query.PageNumber, query.PageSize, 0));
}
