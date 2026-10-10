using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Domain.Teams;
using Microsoft.EntityFrameworkCore;

namespace FixtureHub.Infrastructure.Persistence.Repositories;

internal sealed class TeamRepository(FixtureHubDbContext context) : ITeamRepository
{
    public Task<Team?> GetByIdAsync(Guid teamId, CancellationToken cancellationToken) =>
        context.Teams
            .Include(team => team.Players)
            .FirstOrDefaultAsync(team => team.Id == teamId, cancellationToken);

    public Task<Team?> GetByIdIncludingDeletedAsync(Guid teamId, CancellationToken cancellationToken) =>
        context.Teams
            .IgnoreQueryFilters()
            .Include(team => team.Players)
            .FirstOrDefaultAsync(team => team.Id == teamId, cancellationToken);

    public Task<bool> IsNameTakenAsync(string name, Guid? exceptTeamId, CancellationToken cancellationToken) =>
        context.Teams.AnyAsync(
            team => team.Name == name && (exceptTeamId == null || team.Id != exceptTeamId),
            cancellationToken);

    public void Add(Team team) => context.Teams.Add(team);
}
