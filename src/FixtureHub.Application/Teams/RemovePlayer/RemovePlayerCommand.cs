using FixtureHub.Application.Abstractions.Messaging;

namespace FixtureHub.Application.Teams.RemovePlayer;

public sealed record RemovePlayerCommand(Guid TeamId, Guid PlayerId) : ICommand;
