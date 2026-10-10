using System.Net;
using System.Net.Http.Json;
using FixtureHub.Domain.Matches;
using FixtureHub.Domain.Standings;
using Microsoft.EntityFrameworkCore;

namespace FixtureHub.IntegrationTests.Api;

[Collection(nameof(SqlServerCollection))]
public sealed class MatchEndpointsTests(SqlServerFixture database)
{
    private readonly HttpClient _client = database.CreateClient();

    [Fact]
    public async Task ScheduleMatch_Returns201WithTheLocationOfTheMatch()
    {
        var (homeTeamId, _) = await _client.CreateTeamWithPlayerAsync("Los Halcones");
        var (awayTeamId, _) = await _client.CreateTeamWithPlayerAsync("Los Pumas");

        var response = await _client.PostJsonAsync("/api/matches", new { homeTeamId, awayTeamId, scheduledAt = ApiScenarios.KickOff });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var matchId = await response.IdAsync();
        Assert.Equal($"/api/matches/{matchId}", response.Headers.Location?.ToString());
        var match = await database.FindMatchAsync(matchId);
        Assert.Equal(MatchStatus.Scheduled, match?.Status);
        Assert.Equal(ApiScenarios.KickOff, match?.ScheduledAt);
    }

    [Fact]
    public async Task ScheduleMatch_AgainstItself_Returns400SameTeams()
    {
        var (teamId, _) = await _client.CreateTeamWithPlayerAsync("Los Halcones");

        var response = await _client.PostJsonAsync("/api/matches", new { homeTeamId = teamId, awayTeamId = teamId, scheduledAt = ApiScenarios.KickOff });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Match.SameTeams", (await response.ReadProblemAsync()).ErrorCode());
    }

    [Fact]
    public async Task ScheduleMatch_TeamAlreadyPlaysAtThatTime_Returns409ScheduleConflict()
    {
        var match = await _client.ScheduleMatchAsync();
        var (otherTeamId, _) = await _client.CreateTeamWithPlayerAsync("Los Jaguares");

        var response = await _client.PostJsonAsync(
            "/api/matches",
            new { homeTeamId = otherTeamId, awayTeamId = match.AwayTeamId, scheduledAt = ApiScenarios.KickOff.ToUniversalTime() });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.ReadProblemAsync();
        Assert.Equal("Match.ScheduleConflict", problem.ErrorCode());
        Assert.Equal(ApiScenarios.KickOff, problem.Metadata().GetProperty("scheduledAt").GetDateTimeOffset());
    }

    [Fact]
    public async Task RescheduleMatch_Returns204AndSavesTheNewDate()
    {
        var match = await _client.ScheduleMatchAsync();
        var newDate = ApiScenarios.KickOff.AddDays(1);

        var response = await _client.PatchAsJsonAsync($"/api/matches/{match.MatchId}", new { scheduledAt = newDate });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(newDate, (await database.FindMatchAsync(match.MatchId))?.ScheduledAt);
    }

    [Fact]
    public async Task LiveMatch_StartGoalAndFinish_UpdatesTheScoreAndTheStandings()
    {
        var match = await _client.ScheduleMatchAsync();

        await _client.ExpectNoContentAsync($"/api/matches/{match.MatchId}/start");
        var goal = await _client.PostJsonAsync($"/api/matches/{match.MatchId}/goals", new { playerId = match.HomePlayerId, minute = 30, isOwnGoal = false });
        await _client.ExpectNoContentAsync($"/api/matches/{match.MatchId}/finish");

        Assert.Equal(HttpStatusCode.Created, goal.StatusCode);
        Assert.Equal($"/api/matches/{match.MatchId}", goal.Headers.Location?.ToString());
        var saved = await database.FindMatchAsync(match.MatchId);
        Assert.Equal((MatchStatus.Finished, 1, 0), (saved?.Status, saved?.HomeScore, saved?.AwayScore));
        Assert.Equal(await goal.IdAsync(), Assert.Single(saved!.Goals).Id);
        Assert.Equal(3, (await StandingOf(match.HomeTeamId)).Points);
        Assert.Equal(0, (await StandingOf(match.AwayTeamId)).Points);
    }

    [Fact]
    public async Task RegisterGoal_RetriedWithTheSameKey_RegistersOneGoal()
    {
        var match = await _client.StartMatchAsync();
        var key = Guid.NewGuid().ToString();
        var body = new { playerId = match.HomePlayerId, minute = 30, isOwnGoal = false };

        var first = await _client.PostJsonAsync($"/api/matches/{match.MatchId}/goals", body, key);
        var retried = await _client.PostJsonAsync($"/api/matches/{match.MatchId}/goals", body, key);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, retried.StatusCode);
        Assert.Equal(await first.IdAsync(), await retried.IdAsync());
        Assert.True(retried.Headers.Contains("Idempotent-Replayed"));
        var saved = await database.FindMatchAsync(match.MatchId);
        Assert.Single(saved!.Goals);
        Assert.Equal(1, saved.HomeScore);
    }

    [Fact]
    public async Task FinishMatch_RetriedWithTheSameKey_Returns204ButANewAttemptReturns409()
    {
        var match = await _client.StartMatchAsync();
        var key = Guid.NewGuid().ToString();

        var first = await _client.PostJsonAsync($"/api/matches/{match.MatchId}/finish", idempotencyKey: key);
        var retried = await _client.PostJsonAsync($"/api/matches/{match.MatchId}/finish", idempotencyKey: key);
        var newAttempt = await _client.PostJsonAsync($"/api/matches/{match.MatchId}/finish");

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, retried.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, newAttempt.StatusCode);
        var problem = await newAttempt.ReadProblemAsync();
        Assert.Equal("Match.InvalidStatusTransition", problem.ErrorCode());
        Assert.Equal("Finished", problem.Metadata().GetProperty("currentStatus").GetString());
        var home = await StandingOf(match.HomeTeamId);
        Assert.Equal((1, 1, 1), (home.Played, home.Drawn, home.Points));
    }

    [Fact]
    public async Task AnnulGoal_Returns204AndTheScoreGoesBack()
    {
        var match = await _client.StartMatchAsync();
        var goalId = await _client.CreateAsync($"/api/matches/{match.MatchId}/goals", new { playerId = match.AwayPlayerId, minute = 12, isOwnGoal = false });

        await _client.ExpectNoContentAsync($"/api/matches/{match.MatchId}/goals/{goalId}/annul");

        var saved = await database.FindMatchAsync(match.MatchId);
        Assert.Equal(0, saved?.AwayScore);
        Assert.True(Assert.Single(saved!.Goals).IsAnnulled);
    }

    [Fact]
    public async Task CorrectMatchResult_Returns204AndRecalculatesTheStandings()
    {
        var match = await _client.StartMatchAsync();
        await _client.CreateAsync($"/api/matches/{match.MatchId}/goals", new { playerId = match.HomePlayerId, minute = 10, isOwnGoal = false });
        await _client.ExpectNoContentAsync($"/api/matches/{match.MatchId}/finish");

        var response = await _client.PutAsJsonAsync(
            $"/api/matches/{match.MatchId}/result",
            new
            {
                goals = new[]
                {
                    new { playerId = match.AwayPlayerId, minute = 10, isOwnGoal = false },
                    new { playerId = match.AwayPlayerId, minute = 80, isOwnGoal = false }
                },
                reason = "El primer gol fue del equipo visitante según el acta."
            });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var saved = await database.FindMatchAsync(match.MatchId);
        Assert.Equal((0, 2), (saved?.HomeScore, saved?.AwayScore));
        Assert.Equal(0, (await StandingOf(match.HomeTeamId)).Points);
        Assert.Equal(3, (await StandingOf(match.AwayTeamId)).Points);
    }

    [Fact]
    public async Task CorrectMatchResult_InvalidGoal_Returns400WithThePositionOfTheGoal()
    {
        var match = await _client.ScheduleMatchAsync();

        var response = await _client.PutAsJsonAsync(
            $"/api/matches/{match.MatchId}/result",
            new
            {
                goals = new[]
                {
                    new { playerId = match.HomePlayerId, minute = 10, isOwnGoal = false },
                    new { playerId = match.HomePlayerId, minute = 121, isOwnGoal = false }
                },
                reason = "Corrección del acta."
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.ReadProblemAsync()).Metadata().GetProperty("errors");
        Assert.Equal("Goal.InvalidMinute", errors.GetProperty("goals[1].minute")[0].GetString());
    }

    [Fact]
    public async Task CancelMatch_Returns204()
    {
        var match = await _client.ScheduleMatchAsync();

        await _client.ExpectNoContentAsync($"/api/matches/{match.MatchId}/cancel");

        Assert.Equal(MatchStatus.Cancelled, (await database.FindMatchAsync(match.MatchId))?.Status);
    }

    [Fact]
    public async Task DeleteMatch_Scheduled_Returns204()
    {
        var match = await _client.ScheduleMatchAsync();

        var response = await _client.DeleteAsync($"/api/matches/{match.MatchId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True((await database.FindMatchAsync(match.MatchId))?.IsDeleted);
    }

    [Fact]
    public async Task DeleteMatch_InProgress_Returns409CannotDelete()
    {
        var match = await _client.StartMatchAsync();

        var response = await _client.DeleteAsync($"/api/matches/{match.MatchId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Match.CannotDelete", (await response.ReadProblemAsync()).ErrorCode());
    }

    private Task<TeamStanding> StandingOf(Guid teamId) =>
        database.QueryAsync(context => context.TeamStandings.AsNoTracking().SingleAsync(standing => standing.TeamId == teamId));
}
