using FixtureHub.Application.Abstractions.Messaging;

namespace FixtureHub.Application.Teams.UpdatePlayer;

public sealed record UpdatePlayerCommand(Guid TeamId, Guid PlayerId, string Name, int ShirtNumber) : ICommand;
