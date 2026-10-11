using FixtureHub.Application.Abstractions.Reading;
using FixtureHub.Application.Pagination;
using FixtureHub.Application.Standings.GetStandings;

namespace FixtureHub.IntegrationTests.Observability;

public sealed class ThrowingStandingsReader : IStandingsReader
{
    public const string Message = "Falla simulada del lector de posiciones.";

    public Task<PagedResult<StandingItem>> GetStandingsAsync(GetStandingsQuery query, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(Message);
}
