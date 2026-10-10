using System.Net;
using System.Net.Http.Json;
using FixtureHub.Application.Matches.GetMatchById;
using FixtureHub.Application.Matches.GetMatches;
using FixtureHub.Application.Pagination;

namespace FixtureHub.IntegrationTests.Api;

[Collection(nameof(SqlServerCollection))]
public sealed class MatchQueryTests(SqlServerFixture database)
{
    private readonly HttpClient _client = database.CreateClient();

    public static TheoryData<string, string> EverySortField()
    {
        var data = new TheoryData<string, string>();
        foreach (var field in GetMatchesQuery.SortFields)
        {
            data.Add(field, Paging.Ascending);
            data.Add(field, Paging.Descending);
        }

        return data;
    }

    [Fact]
    public async Task GetMatches_FilteredByTeam_ReturnsItsMatchesAsHomeOrAwayOrderedByDate()
    {
        var (teamId, _) = await _client.CreateTeamWithPlayerAsync("Los Halcones");
        var (rivalA, _) = await _client.CreateTeamWithPlayerAsync("Los Pumas");
        var (rivalB, _) = await _client.CreateTeamWithPlayerAsync("Los Jaguares");
        var later = await ScheduleAsync(rivalB, teamId, ApiScenarios.KickOff.AddDays(7));
        var sooner = await ScheduleAsync(teamId, rivalA, ApiScenarios.KickOff);
        await ScheduleAsync(rivalA, rivalB, ApiScenarios.KickOff.AddDays(1));

        var page = await GetMatchesAsync($"teamId={teamId}");

        Assert.Equal([sooner, later], page.Items.Select(match => match.Id));
        Assert.Equal(teamId, page.Items[1].AwayTeamId);
    }

    [Fact]
    public async Task GetMatches_FilteredByStatusInAnyCase_ReturnsOnlyThatStatus()
    {
        var (teamId, _) = await _client.CreateTeamWithPlayerAsync("Los Halcones");
        var (rivalId, _) = await _client.CreateTeamWithPlayerAsync("Los Pumas");
        var finished = await ScheduleAsync(teamId, rivalId, ApiScenarios.KickOff);
        await ScheduleAsync(rivalId, teamId, ApiScenarios.KickOff.AddDays(7));
        await _client.ExpectNoContentAsync($"/api/matches/{finished}/start");
        await _client.ExpectNoContentAsync($"/api/matches/{finished}/finish");

        var page = await GetMatchesAsync($"teamId={teamId}&status=finished");

        var match = Assert.Single(page.Items);
        Assert.Equal((finished, "Finished"), (match.Id, match.Status));
    }

    [Fact]
    public async Task GetMatches_FilteredByDateRange_IncludesBothLimits()
    {
        var (teamId, _) = await _client.CreateTeamWithPlayerAsync("Los Halcones");
        var (rivalId, _) = await _client.CreateTeamWithPlayerAsync("Los Pumas");
        await ScheduleAsync(teamId, rivalId, ApiScenarios.KickOff.AddDays(-1));
        var first = await ScheduleAsync(rivalId, teamId, ApiScenarios.KickOff);
        var second = await ScheduleAsync(teamId, rivalId, ApiScenarios.KickOff.AddDays(1));
        await ScheduleAsync(rivalId, teamId, ApiScenarios.KickOff.AddDays(2));

        var from = Uri.EscapeDataString(ApiScenarios.KickOff.ToString("O"));
        var to = Uri.EscapeDataString(ApiScenarios.KickOff.AddDays(1).ToUniversalTime().ToString("O"));
        var page = await GetMatchesAsync($"teamId={teamId}&from={from}&to={to}");

        Assert.Equal([first, second], page.Items.Select(match => match.Id));
    }

    [Fact]
    public async Task GetMatches_InvalidStatusAndDateRange_Returns400WithACodePerParameter()
    {
        var response = await _client.GetAsync("/api/matches?status=Jugando&from=2026-11-08T00:00:00Z&to=2026-11-07T00:00:00Z");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.ReadProblemAsync()).Metadata().GetProperty("errors");
        Assert.Equal("Query.InvalidStatus", errors.GetProperty("status")[0].GetString());
        Assert.Equal("Query.InvalidDateRange", errors.GetProperty("to")[0].GetString());
    }

    [Theory]
    [MemberData(nameof(EverySortField))]
    public async Task GetMatches_EveryWhitelistedSortField_RunsInBothDirections(string sortBy, string sortDirection)
    {
        var response = await _client.GetAsync($"/api/matches?sortBy={sortBy}&sortDirection={sortDirection}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMatches_TeamDeletedAfterPlaying_KeepsItsMatchesInTheHistory()
    {
        var match = await _client.StartMatchAsync();
        await _client.ExpectNoContentAsync($"/api/matches/{match.MatchId}/finish");
        var deleted = await _client.DeleteAsync($"/api/teams/{match.HomeTeamId}");

        var page = await GetMatchesAsync($"teamId={match.HomeTeamId}");

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.StartsWith("Los Halcones", Assert.Single(page.Items).HomeTeamName, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetMatch_ReturnsTheGoalsWithTheirPlayersAndTheCorrectionsInOrder()
    {
        var match = await _client.StartMatchAsync();
        var late = await _client.CreateAsync($"/api/matches/{match.MatchId}/goals", new { playerId = match.HomePlayerId, minute = 70, isOwnGoal = false });
        var early = await _client.CreateAsync($"/api/matches/{match.MatchId}/goals", new { playerId = match.AwayPlayerId, minute = 5, isOwnGoal = true });
        await _client.ExpectNoContentAsync($"/api/matches/{match.MatchId}/goals/{late}/annul");
        await _client.ExpectNoContentAsync($"/api/matches/{match.MatchId}/finish");
        await CorrectAsync(match, homeGoals: 2, "Primera corrección");
        await CorrectAsync(match, homeGoals: 3, "Segunda corrección");

        var details = await _client.GetFromJsonAsync<MatchDetails>($"/api/matches/{match.MatchId}");

        Assert.Equal(("Finished", 3, 0), (details!.Status, details.HomeScore, details.AwayScore));
        Assert.Equal(7, details.Goals.Count);
        Assert.Equal(3, details.Goals.Count(goal => !goal.IsAnnulled));
        Assert.Equal(details.Goals.Select(goal => goal.Minute).Order(), details.Goals.Select(goal => goal.Minute));
        var ownGoal = details.Goals.Single(goal => goal.Id == early);
        Assert.Equal((5, true, true), (ownGoal.Minute, ownGoal.IsOwnGoal, ownGoal.IsAnnulled));
        Assert.Equal((match.HomeTeamId, "Goleador de Los Pumas"), (ownGoal.ScoringTeamId, ownGoal.PlayerName));
        Assert.True(details.Goals.Single(goal => goal.Id == late).IsAnnulled);
        Assert.Equal(
            [("Primera corrección", 1, 2), ("Segunda corrección", 2, 3)],
            details.Corrections.Select(correction => (correction.Reason, correction.PreviousHomeScore, correction.NewHomeScore)));
    }

    [Fact]
    public async Task GetMatch_Deleted_Returns404()
    {
        var match = await _client.ScheduleMatchAsync();
        await _client.DeleteAsync($"/api/matches/{match.MatchId}");

        var response = await _client.GetAsync($"/api/matches/{match.MatchId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Match.NotFound", (await response.ReadProblemAsync()).ErrorCode());
    }

    private Task<Guid> ScheduleAsync(Guid homeTeamId, Guid awayTeamId, DateTimeOffset scheduledAt) =>
        _client.CreateAsync("/api/matches", new { homeTeamId, awayTeamId, scheduledAt });

    private async Task CorrectAsync(ScheduledMatch match, int homeGoals, string reason)
    {
        var goals = Enumerable.Range(1, homeGoals)
            .Select(minute => new { playerId = match.HomePlayerId, minute, isOwnGoal = false });

        var response = await _client.PutAsJsonAsync($"/api/matches/{match.MatchId}/result", new { goals, reason });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private async Task<PagedResult<MatchSummary>> GetMatchesAsync(string query) =>
        (await _client.GetFromJsonAsync<PagedResult<MatchSummary>>($"/api/matches?{query}"))!;
}
