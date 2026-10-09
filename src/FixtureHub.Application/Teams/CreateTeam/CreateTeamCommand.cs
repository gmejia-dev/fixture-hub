using FixtureHub.Application.Abstractions.Messaging;

namespace FixtureHub.Application.Teams.CreateTeam;

public sealed record CreateTeamCommand(string Name, string Country) : ICommand<Guid>;
