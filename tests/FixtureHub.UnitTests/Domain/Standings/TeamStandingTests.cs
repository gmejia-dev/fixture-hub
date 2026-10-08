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

    [Theory]
    [InlineData(2, 1, 1, 0, 0)]
    [InlineData(1, 1, 0, 1, 0)]
    [InlineData(0, 2, 0, 0, 1)]
    public void RecordResult_CountsTheOutcome(int goalsFor, int goalsAgainst, int won, int drawn, int lost)
    {
        var standing = TeamStanding.Create(TeamId);

        standing.RecordResult(goalsFor, goalsAgainst);

        Assert.Equal(won, standing.Won);
        Assert.Equal(drawn, standing.Drawn);
        Assert.Equal(lost, standing.Lost);
    }

    [Theory]
    [InlineData(3, 1, 2)]
    [InlineData(0, 0, 0)]
    [InlineData(1, 4, -3)]
    public void RecordResult_AddsTheGoalsAndTheGoalDifference(int goalsFor, int goalsAgainst, int expectedDifference)
    {
        var standing = TeamStanding.Create(TeamId);

        standing.RecordResult(goalsFor, goalsAgainst);

        Assert.Equal(goalsFor, standing.GoalsFor);
        Assert.Equal(goalsAgainst, standing.GoalsAgainst);
        Assert.Equal(expectedDifference, standing.GoalDifference);
    }
}
