using FixtureHub.Application.Abstractions.Messaging;

namespace FixtureHub.Application.Teams.DeleteTeam;

public sealed record DeleteTeamCommand(Guid TeamId) : ICommand;
