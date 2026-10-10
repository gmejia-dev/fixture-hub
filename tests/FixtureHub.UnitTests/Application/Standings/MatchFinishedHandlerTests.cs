using FixtureHub.Application.Standings;
using FixtureHub.Domain.Matches;
using FixtureHub.Domain.Standings;
using FixtureHub.UnitTests.Application.Fakes;

namespace FixtureHub.UnitTests.Application.Standings;

public class MatchFinishedHandlerTests
{
    private readonly FakeTeamStandingRepository _standings = new();
    private readonly TeamStanding _home = TeamStanding.Create(Guid.NewGuid());
    private readonly TeamStanding _away = TeamStanding.Create(Guid.NewGuid());
    private readonly MatchFinishedHandler _handler;

    public MatchFinishedHandlerTests()
    {
        _standings.Add(_home);
        _standings.Add(_away);
        _handler = new MatchFinishedHandler(_standings);
    }

    [Fact]
    public async Task HandleAsync_HomeWin_GivesThreePointsToHomeAndNoneToAway()
    {
        await HandleAsync(homeScore: 2, awayScore: 1);

        Assert.Equal((1, 1, 3, 2, 1), (_home.Played, _home.Won, _home.Points, _home.GoalsFor, _home.GoalsAgainst));
        Assert.Equal((1, 1, 0, 1, 2), (_away.Played, _away.Lost, _away.Points, _away.GoalsFor, _away.GoalsAgainst));
    }

    [Fact]
    public async Task HandleAsync_Draw_GivesOnePointToEachTeam()
    {
        await HandleAsync(homeScore: 1, awayScore: 1);

        Assert.Equal(1, _home.Points);
        Assert.Equal(1, _away.Points);
        Assert.Equal(1, _home.Drawn);
        Assert.Equal(1, _away.Drawn);
    }

    [Fact]
    public async Task HandleAsync_TeamWithoutStanding_Throws()
    {
        var finished = new MatchFinished(Guid.NewGuid(), _home.TeamId, Guid.NewGuid(), 1, 0);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.HandleAsync(finished, CancellationToken.None));
    }

    private Task HandleAsync(int homeScore, int awayScore) =>
        _handler.HandleAsync(
            new MatchFinished(Guid.NewGuid(), _home.TeamId, _away.TeamId, homeScore, awayScore),
            CancellationToken.None);
}
