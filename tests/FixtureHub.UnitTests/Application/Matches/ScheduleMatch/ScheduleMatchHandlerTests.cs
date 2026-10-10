using FixtureHub.Application.Matches.ScheduleMatch;
using FixtureHub.Domain.Common;
using FixtureHub.Domain.Matches;

namespace FixtureHub.UnitTests.Application.Matches.ScheduleMatch;

public class ScheduleMatchHandlerTests
{
    private readonly MatchScenario _scenario = new();
    private readonly ScheduleMatchHandler _handler;

    public ScheduleMatchHandlerTests() => _handler = new ScheduleMatchHandler(_scenario.Teams, _scenario.Matches);

    [Fact]
    public async Task HandleAsync_ValidCommand_SchedulesTheMatchAndReturnsItsId()
    {
        var result = await HandleAsync(_scenario.Home.Id, _scenario.Away.Id, MatchScenario.KickOff);

        var match = Assert.Single(_scenario.Matches.All);
        Assert.Equal(match.Id, result.Value);
        Assert.Equal(MatchStatus.Scheduled, match.Status);
        Assert.Equal(MatchScenario.KickOff, match.ScheduledAt);
    }

    [Fact]
    public async Task HandleAsync_SameTeams_ReturnsSameTeams()
    {
        var result = await HandleAsync(_scenario.Home.Id, _scenario.Home.Id, MatchScenario.KickOff);

        Assert.Equal("Match.SameTeams", result.Error.Code);
        Assert.Empty(_scenario.Matches.All);
    }

    [Fact]
    public async Task HandleAsync_UnknownAwayTeam_ReturnsTeamNotFound()
    {
        var awayTeamId = Guid.NewGuid();

        var result = await HandleAsync(_scenario.Home.Id, awayTeamId, MatchScenario.KickOff);

        Assert.Equal("Team.NotFound", result.Error.Code);
        Assert.Equal(awayTeamId, result.Error.Metadata?["teamId"]);
    }

    [Fact]
    public async Task HandleAsync_TeamAlreadyPlaysAtThatTime_ReturnsScheduleConflict()
    {
        _scenario.AddMatch();
        var rival = _scenario.AddTeam("Los Toros");

        var result = await HandleAsync(rival.Id, _scenario.Away.Id, MatchScenario.KickOff);

        Assert.Equal("Match.ScheduleConflict", result.Error.Code);
        Assert.Single(_scenario.Matches.All);
    }

    [Fact]
    public async Task HandleAsync_CancelledMatchAtThatTime_SchedulesTheMatch()
    {
        _scenario.AddMatch(MatchStatus.Cancelled);

        var result = await HandleAsync(_scenario.Home.Id, _scenario.Away.Id, MatchScenario.KickOff);

        Assert.True(result.IsSuccess);
    }

    private Task<Result<Guid>> HandleAsync(Guid homeTeamId, Guid awayTeamId, DateTimeOffset at) =>
        _handler.HandleAsync(new ScheduleMatchCommand(homeTeamId, awayTeamId, at), CancellationToken.None);
}
