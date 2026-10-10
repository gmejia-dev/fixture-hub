using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FixtureHub.IntegrationTests.Persistence;

[Collection(nameof(SqlServerCollection))]
public sealed class UnitOfWorkTests(SqlServerFixture database)
{
    [Fact]
    public async Task CommitAsync_AfterBeginTransaction_SavesTheChanges()
    {
        var team = TestData.NewTeam();

        await using (var scope = database.CreateScope())
        {
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await unitOfWork.BeginTransactionAsync(CancellationToken.None);
            scope.ServiceProvider.GetRequiredService<ITeamRepository>().Add(team);

            var result = await unitOfWork.CommitAsync(CancellationToken.None);

            Assert.True(result.IsSuccess);
        }

        Assert.NotNull(await database.FindTeamAsync(team.Id));
    }

    [Fact]
    public async Task RollbackAsync_ChangesWrittenInsideTheTransaction_AreDiscarded()
    {
        var team = TestData.NewTeam();
        await using var scope = database.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var context = scope.ServiceProvider.GetRequiredService<FixtureHubDbContext>();
        await unitOfWork.BeginTransactionAsync(CancellationToken.None);
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        await unitOfWork.RollbackAsync(CancellationToken.None);

        Assert.False(await context.Teams.AnyAsync(saved => saved.Id == team.Id));
    }

    [Fact]
    public async Task CommitAsync_TeamNameTakenWithDifferentCase_ReturnsNameTakenAndAllowsTheRollback()
    {
        var name = TestData.UniqueName("Los Halcones");
        await database.SaveAsync(TestData.NewTeam(name));
        await using var scope = database.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await unitOfWork.BeginTransactionAsync(CancellationToken.None);
        scope.ServiceProvider.GetRequiredService<ITeamRepository>().Add(TestData.NewTeam(name.ToUpperInvariant()));

        var result = await unitOfWork.CommitAsync(CancellationToken.None);
        await unitOfWork.RollbackAsync(CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Team.NameTaken", result.Error.Code);
        Assert.Equal(name.ToUpperInvariant(), result.Error.Metadata?["name"]);
    }

    [Fact]
    public async Task CommitAsync_ShirtNumberTakenByAConcurrentRequest_ReturnsShirtNumberTaken()
    {
        var team = TestData.NewTeam();
        await database.SaveAsync(team);
        await using var first = database.CreateScope();
        await using var second = database.CreateScope();
        var firstUnitOfWork = first.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var secondUnitOfWork = second.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await firstUnitOfWork.BeginTransactionAsync(CancellationToken.None);
        await secondUnitOfWork.BeginTransactionAsync(CancellationToken.None);
        var firstCopy = await first.ServiceProvider.GetRequiredService<ITeamRepository>().GetByIdAsync(team.Id, CancellationToken.None);
        var secondCopy = await second.ServiceProvider.GetRequiredService<ITeamRepository>().GetByIdAsync(team.Id, CancellationToken.None);
        firstCopy!.AddPlayer("Ana", 10);
        secondCopy!.AddPlayer("Beto", 10);

        var firstResult = await firstUnitOfWork.CommitAsync(CancellationToken.None);
        var secondResult = await secondUnitOfWork.CommitAsync(CancellationToken.None);
        await secondUnitOfWork.RollbackAsync(CancellationToken.None);

        Assert.True(firstResult.IsSuccess);
        Assert.Equal("Player.ShirtNumberTaken", secondResult.Error.Code);
        Assert.Equal(10, secondResult.Error.Metadata?["shirtNumber"]);
    }
}
