using FixtureHub.Domain.Teams;

namespace FixtureHub.Application.Abstractions.Persistence;

public interface ITeamRepository
{
    Task<Team?> GetByIdAsync(Guid teamId, CancellationToken cancellationToken);

    Task<Team?> GetByIdIncludingDeletedAsync(Guid teamId, CancellationToken cancellationToken);

    Task<bool> IsNameTakenAsync(string name, Guid? exceptTeamId, CancellationToken cancellationToken);

    void Add(Team team);
}
