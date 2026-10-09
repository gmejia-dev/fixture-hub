using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Domain.Teams;

namespace FixtureHub.UnitTests.Application.Fakes;

internal sealed class FakeTeamRepository : ITeamRepository
{
    private readonly List<Team> _teams = [];

    public IReadOnlyList<Team> All => _teams;

    public Task<Team?> GetByIdAsync(Guid teamId, CancellationToken cancellationToken) =>
        Task.FromResult(_teams.FirstOrDefault(team => team.Id == teamId && !team.IsDeleted));

    public Task<Team?> GetByIdIncludingDeletedAsync(Guid teamId, CancellationToken cancellationToken) =>
        Task.FromResult(_teams.FirstOrDefault(team => team.Id == teamId));

    public Task<bool> IsNameTakenAsync(string name, Guid? exceptTeamId, CancellationToken cancellationToken) =>
        Task.FromResult(_teams.Any(team =>
            !team.IsDeleted
            && team.Id != exceptTeamId
            && string.Equals(team.Name, name, StringComparison.OrdinalIgnoreCase)));

    public void Add(Team team) => _teams.Add(team);
}
