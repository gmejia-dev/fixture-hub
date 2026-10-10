using FixtureHub.Application.Pagination;

namespace FixtureHub.Application.Teams.GetTeams;

internal sealed class GetTeamsValidator() : PagedQueryValidator<GetTeamsQuery>(GetTeamsQuery.SortFields);
