using FixtureHub.Application.Pagination;

namespace FixtureHub.Application.Teams.GetPlayers;

internal sealed class GetPlayersValidator() : PagedQueryValidator<GetPlayersQuery>(GetPlayersQuery.SortFields);
