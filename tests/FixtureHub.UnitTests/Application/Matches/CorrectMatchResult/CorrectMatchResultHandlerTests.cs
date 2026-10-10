using FixtureHub.Application.Matches.CorrectMatchResult;
using FixtureHub.Domain.Common;
using FixtureHub.Domain.Matches;

namespace FixtureHub.UnitTests.Application.Matches.CorrectMatchResult;

public class CorrectMatchResultHandlerTests
{
    private readonly MatchScenario _scenario = new();
    private readonly CorrectMatchResultHandler _handler;

    public CorrectMatchResultHandlerTests() =>
        _handler = new CorrectMatchResultHandler(_scenario.Matches, _scenario.Teams);

    [Fact]
    public async Task HandleAsync_FinishedMatch_ReplacesTheGoalsAndKeepsTheHistory()
    {
        var match = _scenario.AddMatch(MatchStatus.Finished);

        var result = await HandleAsync(
            match.Id,
            new CorrectedGoal(_scenario.HomePlayer.Id, 10, false),
            new CorrectedGoal(_scenario.HomePlayer.Id, 80, false));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, match.HomeScore);
        Assert.Equal(0, match.AwayScore);
        var correction = Assert.Single(match.Corrections);
        Assert.Equal("Error de planilla", correction.Reason);
    }

    [Fact]
    public async Task HandleAsync_PlayerOfAnotherTeam_ReturnsPlayerNotInMatch()
    {
        var match = _scenario.AddMatch(MatchStatus.Finished);
        var outsider = _scenario.AddTeam("Los Toros").AddPlayer("Rodolfo Zelaya", 9).Value;

        var result = await HandleAsync(match.Id, new CorrectedGoal(outsider.Id, 10, false));

        Assert.Equal("Match.PlayerNotInMatch", result.Error.Code);
        Assert.Equal(outsider.Id, result.Error.Metadata?["playerId"]);
        Assert.Empty(match.Corrections);
    }

    [Fact]
    public async Task HandleAsync_MatchInProgress_ReturnsNotFinished()
    {
        var match = _scenario.AddMatch(MatchStatus.InProgress);

        var result = await HandleAsync(match.Id, new CorrectedGoal(_scenario.HomePlayer.Id, 10, false));

        Assert.Equal("Match.NotFinished", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_UnknownMatch_ReturnsMatchNotFound()
    {
        var result = await HandleAsync(Guid.NewGuid());

        Assert.Equal("Match.NotFound", result.Error.Code);
    }

    private Task<Result> HandleAsync(Guid matchId, params CorrectedGoal[] goals) =>
        _handler.HandleAsync(new CorrectMatchResultCommand(matchId, goals, "Error de planilla"), CancellationToken.None);
}
