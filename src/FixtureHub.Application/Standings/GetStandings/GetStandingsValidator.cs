using FixtureHub.Application.Pagination;

namespace FixtureHub.Application.Standings.GetStandings;

internal sealed class GetStandingsValidator() : PagedQueryValidator<GetStandingsQuery>(GetStandingsQuery.SortFields);
