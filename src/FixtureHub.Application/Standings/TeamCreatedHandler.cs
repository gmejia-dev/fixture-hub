using FixtureHub.Application.Abstractions.Events;
using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Domain.Standings;
using FixtureHub.Domain.Teams;

namespace FixtureHub.Application.Standings;

internal sealed class TeamCreatedHandler(ITeamStandingRepository standings) : IDomainEventHandler<TeamCreated>
{
    public Task HandleAsync(TeamCreated domainEvent, CancellationToken cancellationToken)
    {
        standings.Add(TeamStanding.Create(domainEvent.TeamId));

        return Task.CompletedTask;
    }
}
