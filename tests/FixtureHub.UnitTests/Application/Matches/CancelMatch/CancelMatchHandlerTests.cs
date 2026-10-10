using FixtureHub.Application.Matches.CancelMatch;
using FixtureHub.Domain.Matches;

namespace FixtureHub.UnitTests.Application.Matches.CancelMatch;

public class CancelMatchHandlerTests
{
    private readonly MatchScenario _scenario = new();
    private readonly CancelMatchHandler _handler;

    public CancelMatchHandlerTests() => _handler = new CancelMatchHandler(_scenario.Matches);

    [Fact]
    public async Task HandleAsync_ScheduledMatch_CancelsIt()
    {
        var match = _scenario.AddMatch();

        var result = await _handler.HandleAsync(new CancelMatchCommand(match.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(MatchStatus.Cancelled, match.Status);
    }

    [Fact]
    public async Task HandleAsync_FinishedMatch_ReturnsInvalidStatusTransition()
    {
        var match = _scenario.AddMatch(MatchStatus.Finished);

        var result = await _handler.HandleAsync(new CancelMatchCommand(match.Id), CancellationToken.None);

        Assert.Equal("Match.InvalidStatusTransition", result.Error.Code);
    }

    [Fact]
    public async Task HandleAsync_UnknownMatch_ReturnsMatchNotFound()
    {
        var result = await _handler.HandleAsync(new CancelMatchCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal("Match.NotFound", result.Error.Code);
    }
}
