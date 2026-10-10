using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Domain.Teams;
using Microsoft.Extensions.DependencyInjection;

namespace FixtureHub.IntegrationTests.Persistence;

[Collection(nameof(SqlServerCollection))]
public sealed class TeamRepositoryTests(SqlServerFixture database)
{
    [Fact]
    public async Task GetByIdAsync_TeamWithPlayers_LoadsTheWholeAggregate()
    {
        var team = TestData.NewTeam();
        team.AddPlayer("Ana", 10);
        var removed = team.AddPlayer("Beto", 7).Value;
        team.RemovePlayer(removed.Id);
        await database.SaveAsync(team);

        var loaded = await GetByIdAsync(team.Id);

        Assert.NotNull(loaded);
        Assert.Equal("Ana", Assert.Single(loaded.Players).Name);
        Assert.True(loaded.RemovePlayer(removed.Id).IsSuccess);
    }

    [Fact]
    public async Task GetByIdAsync_DeletedTeam_ReturnsNull()
    {
        var team = TestData.NewTeam();
        team.Delete();
        await database.SaveAsync(team);

        Assert.Null(await GetByIdAsync(team.Id));
    }

    [Fact]
    public async Task GetByIdIncludingDeletedAsync_DeletedTeam_ReturnsTheTeam()
    {
        var team = TestData.NewTeam();
        team.Delete();
        await database.SaveAsync(team);
        await using var scope = database.CreateScope();

        var loaded = await scope.ServiceProvider.GetRequiredService<ITeamRepository>()
            .GetByIdIncludingDeletedAsync(team.Id, CancellationToken.None);

        Assert.True(loaded?.IsDeleted);
    }

    [Fact]
    public async Task IsNameTakenAsync_SameNameWithDifferentCase_ReturnsTrue()
    {
        var team = TestData.NewTeam();
        await database.SaveAsync(team);

        Assert.True(await IsNameTakenAsync(team.Name.ToUpperInvariant(), exceptTeamId: null));
    }

    [Fact]
    public async Task IsNameTakenAsync_NameOfTheSameTeam_ReturnsFalse()
    {
        var team = TestData.NewTeam();
        await database.SaveAsync(team);

        Assert.False(await IsNameTakenAsync(team.Name, exceptTeamId: team.Id));
    }

    [Fact]
    public async Task IsNameTakenAsync_NameOfADeletedTeam_ReturnsFalse()
    {
        var team = TestData.NewTeam();
        team.Delete();
        await database.SaveAsync(team);

        Assert.False(await IsNameTakenAsync(team.Name, exceptTeamId: null));
    }

    [Fact]
    public async Task Add_NameOfADeletedTeam_CanBeSaved()
    {
        var deleted = TestData.NewTeam();
        deleted.Delete();
        await database.SaveAsync(deleted);
        var team = TestData.NewTeam(deleted.Name);

        await database.SaveAsync(team);

        Assert.NotNull(await database.FindTeamAsync(team.Id));
    }

    private async Task<Team?> GetByIdAsync(Guid teamId)
    {
        await using var scope = database.CreateScope();

        return await scope.ServiceProvider.GetRequiredService<ITeamRepository>().GetByIdAsync(teamId, CancellationToken.None);
    }

    private async Task<bool> IsNameTakenAsync(string name, Guid? exceptTeamId)
    {
        await using var scope = database.CreateScope();

        return await scope.ServiceProvider.GetRequiredService<ITeamRepository>()
            .IsNameTakenAsync(name, exceptTeamId, CancellationToken.None);
    }
}
