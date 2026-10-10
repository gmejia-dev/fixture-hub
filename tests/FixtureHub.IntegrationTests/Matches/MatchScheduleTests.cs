using FixtureHub.Application.Matches.CancelMatch;
using FixtureHub.Application.Matches.DeleteMatch;
using FixtureHub.Application.Matches.ScheduleMatch;
using FixtureHub.Application.Teams.DeleteTeam;
using FixtureHub.Domain.Common;
using FixtureHub.Domain.Teams;

namespace FixtureHub.IntegrationTests.Matches;

[Collection(nameof(SqlServerCollection))]
public sealed class MatchScheduleTests(SqlServerFixture database)
{
    private static readonly DateTimeOffset KickOff = new(2026, 10, 24, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ScheduleMatch_TeamAlreadyPlaysAtThatTime_ReturnsScheduleConflict()
    {
        var (first, second, third) = await SaveTeamsAsync();
        await ScheduleAsync(first, second, KickOff);

        var result = await ScheduleAsync(third, second, KickOff.ToOffset(TimeSpan.FromHours(-6)));

        Assert.Equal("Match.ScheduleConflict", result.Error.Code);
    }

    [Fact]
    public async Task ScheduleMatch_OnlyACancelledMatchAtThatTime_SchedulesTheMatch()
    {
        var (first, second, third) = await SaveTeamsAsync();
        var cancelled = (await ScheduleAsync(first, second, KickOff)).Value;
        await database.SendAsync(new CancelMatchCommand(cancelled));

        var result = await ScheduleAsync(third, second, KickOff);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteTeam_TeamWithAScheduledMatch_ReturnsHasActiveMatches()
    {
        var (first, second, _) = await SaveTeamsAsync();
        await ScheduleAsync(first, second, KickOff);

        var result = await database.SendAsync(new DeleteTeamCommand(first.Id));

        Assert.Equal("Team.HasActiveMatches", result.Error.Code);
        Assert.False((await database.FindTeamAsync(first.Id))!.IsDeleted);
    }

    [Fact]
    public async Task DeleteTeam_TeamWhoseMatchWasDeleted_DeletesTheTeam()
    {
        var (first, second, _) = await SaveTeamsAsync();
        var matchId = (await ScheduleAsync(first, second, KickOff)).Value;
        await database.SendAsync(new DeleteMatchCommand(matchId));

        var result = await database.SendAsync(new DeleteTeamCommand(first.Id));

        Assert.True(result.IsSuccess);
        Assert.True((await database.FindMatchAsync(matchId))!.IsDeleted);
        Assert.True((await database.FindTeamAsync(first.Id))!.IsDeleted);
    }

    private Task<Result<Guid>> ScheduleAsync(Team home, Team away, DateTimeOffset at) =>
        database.SendAsync<ScheduleMatchCommand, Guid>(new ScheduleMatchCommand(home.Id, away.Id, at));

    private async Task<(Team First, Team Second, Team Third)> SaveTeamsAsync()
    {
        var teams = new[] { TestData.NewTeam(), TestData.NewTeam(), TestData.NewTeam() };

        foreach (var team in teams)
            await database.SaveAsync(team);

        return (teams[0], teams[1], teams[2]);
    }
}
