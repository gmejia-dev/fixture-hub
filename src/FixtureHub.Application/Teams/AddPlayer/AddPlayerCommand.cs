using FixtureHub.Application.Abstractions.Messaging;

namespace FixtureHub.Application.Teams.AddPlayer;

public sealed record AddPlayerCommand(Guid TeamId, string Name, int ShirtNumber) : ICommand<Guid>;
