using FixtureHub.Application.Matches.AnnulGoal;
using FixtureHub.Domain.Matches;

namespace FixtureHub.UnitTests.Application.Matches.AnnulGoal;

public class AnnulGoalHandlerTests
{
    private readonly MatchScenario _scenario = new();
    private readonly AnnulGoalHandler _handler;

    public AnnulGoalHandlerTests() => _handler = new AnnulGoalHandler(_scenario.Matches);

    [Fact]
    public async Task HandleAsync_GoalOfAMatchInProgress_AnnulsItAndRecalculatesTheScore()
    {
        var match = _scenario.AddMatch(MatchStatus.InProgress);
        var goal = match.AddGoal(_scenario.HomePlayer, 30, isOwnGoal: false).Value;

        var result = await _handler.HandleAsync(new AnnulGoalCommand(match.Id, goal.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(goal.IsAnnulled);
        Assert.Equal(0, match.HomeScore);
    }

    [Fact]
    public async Task HandleAsync_UnknownGoal_ReturnsGoalNotFound()
    {
        var match = _scenario.AddMatch(MatchStatus.InProgress);

        var result = await _handler.HandleAsync(new AnnulGoalCommand(match.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.Equal("Goal.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_UnknownMatch_ReturnsMatchNotFound()
    {
        var result = await _handler.HandleAsync(
            new AnnulGoalCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.Equal("Match.NotFound", result.Error.Code);
    }
}
