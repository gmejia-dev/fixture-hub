using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Teams.CreateTeam;
using FixtureHub.Domain.Common;
using Microsoft.Extensions.DependencyInjection;

namespace FixtureHub.IntegrationTests.Teams;

[Collection(nameof(SqlServerCollection))]
public sealed class CreateTeamTests(SqlServerFixture database)
{
    [Fact]
    public async Task HandleAsync_ValidCommand_SavesTheTeamInSqlServer()
    {
        var name = TestData.UniqueName("Los Halcones");

        var result = await HandleAsync(new CreateTeamCommand($"  {name}  ", "El Salvador"));

        Assert.True(result.IsSuccess);
        var saved = await database.FindTeamAsync(result.Value);
        Assert.Equal(name, saved?.Name);
    }

    [Fact]
    public async Task HandleAsync_NameTakenWithDifferentCase_ReturnsNameTaken()
    {
        var name = TestData.UniqueName("Los Halcones");
        await HandleAsync(new CreateTeamCommand(name, "El Salvador"));

        var result = await HandleAsync(new CreateTeamCommand(name.ToUpperInvariant(), "Honduras"));

        Assert.Equal("Team.NameTaken", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_SameNameInParallel_SavesOnlyOneTeam()
    {
        var name = TestData.UniqueName("Los Halcones");

        var results = await Task.WhenAll(
            HandleAsync(new CreateTeamCommand(name, "El Salvador")),
            HandleAsync(new CreateTeamCommand(name, "Honduras")));

        Assert.Single(results, result => result.IsSuccess);
        Assert.Single(results, result => result.IsFailure && result.Error.Code == "Team.NameTaken");
    }

    private async Task<Result<Guid>> HandleAsync(CreateTeamCommand command)
    {
        await using var scope = database.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateTeamCommand, Guid>>();

        return await handler.HandleAsync(command, CancellationToken.None);
    }
}
