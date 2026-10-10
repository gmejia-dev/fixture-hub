using FixtureHub.Application.Pagination;

namespace FixtureHub.Application.TopScorers.GetTopScorers;

internal sealed class GetTopScorersValidator() : PagedQueryValidator<GetTopScorersQuery>(GetTopScorersQuery.SortFields);
