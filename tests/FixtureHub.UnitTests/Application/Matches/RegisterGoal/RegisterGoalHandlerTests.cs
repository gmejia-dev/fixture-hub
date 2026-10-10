using FixtureHub.Application.Matches.RegisterGoal;
using FixtureHub.Domain.Common;
using FixtureHub.Domain.Matches;

namespace FixtureHub.UnitTests.Application.Matches.RegisterGoal;

public class RegisterGoalHandlerTests
{
    private readonly MatchScenario _scenario = new();
    private readonly RegisterGoalHandler _handler;

    public RegisterGoalHandlerTests() => _handler = new RegisterGoalHandler(_scenario.Matches, _scenario.Teams);

    [Fact]
    public async Task HandleAsync_PlayerOfTheAwayTeam_AddsTheGoalAndReturnsItsId()
    {
        var match = _scenario.AddMatch(MatchStatus.InProgress);

        var result = await HandleAsync(match.Id, _scenario.AwayPlayer.Id, isOwnGoal: false);

        var goal = Assert.Single(match.Goals);
        Assert.Equal(goal.Id, result.Value);
        Assert.Equal(0, match.HomeScore);
        Assert.Equal(1, match.AwayScore);
    }

    [Fact]
    public async Task HandleAsync_OwnGoal_CountsForTheRival()
    {
        var match = _scenario.AddMatch(MatchStatus.InProgress);

        await HandleAsync(match.Id, _scenario.HomePlayer.Id, isOwnGoal: true);

        Assert.Equal(0, match.HomeScore);
        Assert.Equal(1, match.AwayScore);
    }

    [Fact]
    public async Task HandleAsync_PlayerOfAnotherTeam_ReturnsPlayerNotInMatch()
    {
        var match = _scenario.AddMatch(MatchStatus.InProgress);
        var outsider = _scenario.AddTeam("Los Toros").AddPlayer("Rodolfo Zelaya", 9).Value;

        var result = await HandleAsync(match.Id, outsider.Id, isOwnGoal: false);

        Assert.Equal("Match.PlayerNotInMatch", result.Error.Code);
        Assert.Empty(match.Goals);
    }

    [Fact]
    public async Task HandleAsync_ScheduledMatch_ReturnsNotInProgress()
    {
        var match = _scenario.AddMatch();

        var result = await HandleAsync(match.Id, _scenario.HomePlayer.Id, isOwnGoal: false);

        Assert.Equal("Match.NotInProgress", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_UnknownMatch_ReturnsMatchNotFound()
    {
        var result = await HandleAsync(Guid.NewGuid(), _scenario.HomePlayer.Id, isOwnGoal: false);

        Assert.Equal("Match.NotFound", result.Error.Code);
    }

    private Task<Result<Guid>> HandleAsync(Guid matchId, Guid playerId, bool isOwnGoal) =>
        _handler.HandleAsync(new RegisterGoalCommand(matchId, playerId, 30, isOwnGoal), CancellationToken.None);
}
