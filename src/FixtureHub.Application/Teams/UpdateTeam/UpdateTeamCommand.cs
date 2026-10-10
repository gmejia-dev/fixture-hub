using FixtureHub.Application.Abstractions.Messaging;

namespace FixtureHub.Application.Teams.UpdateTeam;

public sealed record UpdateTeamCommand(Guid TeamId, string Name, string Country) : ICommand;
