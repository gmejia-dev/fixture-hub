using FixtureHub.Application.Teams.GetPlayers;

namespace FixtureHub.Application.Teams.GetTeamById;

public sealed record TeamDetails(Guid Id, string Name, string Country, IReadOnlyList<PlayerItem> Players);
