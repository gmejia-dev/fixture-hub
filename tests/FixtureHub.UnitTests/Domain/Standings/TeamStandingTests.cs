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

    [Fact]
    public void RecordResult_SeveralMatches_AccumulatesEverything()
    {
        var standing = TeamStanding.Create(TeamId);

        standing.RecordResult(3, 1);
        standing.RecordResult(2, 2);
        standing.RecordResult(0, 1);

        Assert.Equal(3, standing.Played);
        Assert.Equal((1, 1, 1), (standing.Won, standing.Drawn, standing.Lost));
        Assert.Equal((5, 4, 1), (standing.GoalsFor, standing.GoalsAgainst, standing.GoalDifference));
        Assert.Equal(4, standing.Points);
    }

    [Fact]
    public void RevertResult_RecordedResult_LeavesTheStandingAsBefore()
    {
        var standing = TeamStanding.Create(TeamId);
        standing.RecordResult(2, 1);
        standing.RecordResult(1, 1);
        standing.RecordResult(0, 2);

        standing.RevertResult(0, 2);

        Assert.Equal(2, standing.Played);
        Assert.Equal((1, 1, 0), (standing.Won, standing.Drawn, standing.Lost));
        Assert.Equal((3, 2, 1), (standing.GoalsFor, standing.GoalsAgainst, standing.GoalDifference));
        Assert.Equal(4, standing.Points);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    public void RecordResult_WithNegativeGoals_ThrowsArgumentOutOfRangeException(int goalsFor, int goalsAgainst)
    {
        var standing = TeamStanding.Create(TeamId);

        Assert.Throws<ArgumentOutOfRangeException>(() => standing.RecordResult(goalsFor, goalsAgainst));
        Assert.Equal(0, standing.Played);
    }

    [Fact]
    public void RevertResult_OutcomeNeverRecorded_ThrowsInvalidOperationException()
    {
        var standing = TeamStanding.Create(TeamId);
        standing.RecordResult(2, 0);

        Assert.Throws<InvalidOperationException>(() => standing.RevertResult(1, 1));
        Assert.Equal(1, standing.Played);
    }
}
