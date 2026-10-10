using FixtureHub.Application.Standings;
using FixtureHub.Domain.Teams;
using FixtureHub.UnitTests.Application.Fakes;

namespace FixtureHub.UnitTests.Application.Standings;

public class TeamCreatedHandlerTests
{
    private readonly FakeTeamStandingRepository _standings = new();

    [Fact]
    public async Task HandleAsync_NewTeam_AddsItsRowWithZeroPoints()
    {
        var teamId = Guid.NewGuid();

        await new TeamCreatedHandler(_standings).HandleAsync(new TeamCreated(teamId, "Los Halcones"), CancellationToken.None);

        var standing = Assert.Single(_standings.All);
        Assert.Equal(teamId, standing.TeamId);
        Assert.Equal(0, standing.Played);
        Assert.Equal(0, standing.Points);
    }
}
