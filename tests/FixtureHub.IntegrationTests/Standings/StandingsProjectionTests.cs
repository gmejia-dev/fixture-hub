using FixtureHub.Application.Matches.CorrectMatchResult;
using FixtureHub.Application.Matches.FinishMatch;
using FixtureHub.Application.Matches.RegisterGoal;
using FixtureHub.Application.Matches.ScheduleMatch;
using FixtureHub.Application.Matches.StartMatch;
using FixtureHub.Application.Teams.CreateTeam;
using FixtureHub.Domain.Standings;
using FixtureHub.Domain.Teams;
using Microsoft.EntityFrameworkCore;

namespace FixtureHub.IntegrationTests.Standings;

[Collection(nameof(SqlServerCollection))]
public sealed class StandingsProjectionTests(SqlServerFixture database)
{
    private static readonly DateTimeOffset KickOff = new(2026, 10, 31, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateTeam_NewTeam_GetsItsRowInTheStandingsWithZeroPoints()
    {
        var created = await database.SendAsync<CreateTeamCommand, Guid>(
            new CreateTeamCommand(TestData.UniqueName("Los Halcones"), "El Salvador"));

        var standing = await FindStandingAsync(created.Value);

        Assert.NotNull(standing);
        Assert.Equal(0, standing.Played);
        Assert.Equal(0, standing.Points);
    }

    [Fact]
    public async Task FinishMatch_HomeWin_UpdatesBothRowsInTheSameTransaction()
    {
        var (home, homePlayer, away, awayPlayer) = await SaveTeamsAsync();
        var matchId = await PlayAsync(home, away, homePlayer, homePlayer, awayPlayer);

        var finished = await database.SendAsync(new FinishMatchCommand(matchId));

        Assert.True(finished.IsSuccess);
        var homeStanding = await FindStandingAsync(home.Id);
        var awayStanding = await FindStandingAsync(away.Id);
        Assert.Equal((1, 1, 3, 2, 1, 1), (homeStanding!.Played, homeStanding.Won, homeStanding.Points,
            homeStanding.GoalsFor, homeStanding.GoalsAgainst, homeStanding.GoalDifference));
        Assert.Equal((1, 1, 0, 1, 2, -1), (awayStanding!.Played, awayStanding.Lost, awayStanding.Points,
            awayStanding.GoalsFor, awayStanding.GoalsAgainst, awayStanding.GoalDifference));
    }

    [Fact]
    public async Task CorrectMatchResult_WinCorrectedToDraw_RecalculatesBothRows()
    {
        var (home, homePlayer, away, awayPlayer) = await SaveTeamsAsync();
        var matchId = await PlayAsync(home, away, homePlayer, homePlayer, awayPlayer);
        await database.SendAsync(new FinishMatchCommand(matchId));

        var corrected = await database.SendAsync(new CorrectMatchResultCommand(
            matchId,
            [new CorrectedGoal(homePlayer.Id, 10, false), new CorrectedGoal(awayPlayer.Id, 60, false)],
            "El segundo gol local no cruzó la línea"));

        Assert.True(corrected.IsSuccess);
        var homeStanding = await FindStandingAsync(home.Id);
        var awayStanding = await FindStandingAsync(away.Id);
        Assert.Equal((1, 0, 1, 1), (homeStanding!.Played, homeStanding.Won, homeStanding.Drawn, homeStanding.Points));
        Assert.Equal((1, 0, 1, 1), (awayStanding!.Played, awayStanding.Lost, awayStanding.Drawn, awayStanding.Points));
    }

    [Fact]
    public async Task MatchCommands_EachDomainEvent_IsRecordedInTheDomainEventsTable()
    {
        var (home, homePlayer, away, awayPlayer) = await SaveTeamsAsync();
        var matchId = await PlayAsync(home, away, homePlayer, awayPlayer);
        await database.SendAsync(new FinishMatchCommand(matchId));

        var types = await database.QueryAsync(context => context.DomainEvents
            .Where(record => record.Payload.Contains(matchId.ToString()))
            .OrderBy(record => record.OccurredAt)
            .Select(record => record.Type)
            .ToListAsync());

        Assert.Equal(["MatchStarted", "GoalScored", "GoalScored", "MatchFinished"], types);
    }

    private Task<TeamStanding?> FindStandingAsync(Guid teamId) =>
        database.QueryAsync(context => context.TeamStandings.AsNoTracking()
            .FirstOrDefaultAsync(standing => standing.TeamId == teamId));

    private async Task<Guid> PlayAsync(Team home, Team away, params Player[] scorers)
    {
        var matchId = (await database.SendAsync<ScheduleMatchCommand, Guid>(
            new ScheduleMatchCommand(home.Id, away.Id, KickOff))).Value;
        await database.SendAsync(new StartMatchCommand(matchId));

        foreach (var (scorer, minute) in scorers.Select((scorer, index) => (scorer, 10 + (index * 20))))
            await database.SendAsync<RegisterGoalCommand, Guid>(new RegisterGoalCommand(matchId, scorer.Id, minute, false));

        return matchId;
    }

    private async Task<(Team Home, Player HomePlayer, Team Away, Player AwayPlayer)> SaveTeamsAsync()
    {
        var home = TestData.NewTeam();
        var homePlayer = home.AddPlayer("Jorge González", 10).Value;
        var away = TestData.NewTeam(TestData.UniqueName("Los Pumas"));
        var awayPlayer = away.AddPlayer("Carlos Ruiz", 20).Value;
        await database.SaveAsync(home);
        await database.SaveAsync(away);

        return (home, homePlayer, away, awayPlayer);
    }
}
