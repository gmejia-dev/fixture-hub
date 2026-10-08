using FixtureHub.Domain.Standings;

namespace FixtureHub.UnitTests.Domain.Standings;

public class TeamStandingTests
{
    private static readonly Guid TeamId = Guid.CreateVersion7();

    [Fact]
    public void Create_NewTeam_StartsWithNoMatchesAndNoPoints()
    {
        var standing = TeamStanding.Create(TeamId);

        Assert.Equal(TeamId, standing.TeamId);
        Assert.Equal(0, standing.Played);
        Assert.Equal(0, standing.Points);
    }

    [Theory]
    [InlineData(2, 1, 3)]
    [InlineData(1, 1, 1)]
    [InlineData(0, 2, 0)]
    public void RecordResult_AddsOnePlayedAndThePointsOfTheOutcome(int goalsFor, int goalsAgainst, int expectedPoints)
    {
        var standing = TeamStanding.Create(TeamId);

        standing.RecordResult(goalsFor, goalsAgainst);

        Assert.Equal(1, standing.Played);
        Assert.Equal(expectedPoints, standing.Points);
    }
}
