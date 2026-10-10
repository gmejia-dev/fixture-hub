using System.Net;

namespace FixtureHub.IntegrationTests.Api;

internal static class ApiScenarios
{
    public static readonly DateTimeOffset KickOff = new(2026, 11, 7, 15, 0, 0, TimeSpan.FromHours(-6));

    public static async Task<(Guid TeamId, Guid PlayerId)> CreateTeamWithPlayerAsync(this HttpClient client, string prefix)
    {
        var teamId = await client.CreateAsync("/api/teams", new { name = TestData.UniqueName(prefix), country = "El Salvador" });
        var playerId = await client.CreateAsync($"/api/teams/{teamId}/players", new { name = $"Goleador de {prefix}", shirtNumber = 9 });

        return (teamId, playerId);
    }

    public static async Task<ScheduledMatch> ScheduleMatchAsync(this HttpClient client)
    {
        var (homeTeamId, homePlayerId) = await client.CreateTeamWithPlayerAsync("Los Halcones");
        var (awayTeamId, awayPlayerId) = await client.CreateTeamWithPlayerAsync("Los Pumas");
        var matchId = await client.CreateAsync("/api/matches", new { homeTeamId, awayTeamId, scheduledAt = KickOff });

        return new ScheduledMatch(matchId, homeTeamId, homePlayerId, awayTeamId, awayPlayerId);
    }

    public static async Task<ScheduledMatch> StartMatchAsync(this HttpClient client)
    {
        var match = await client.ScheduleMatchAsync();
        await client.ExpectNoContentAsync($"/api/matches/{match.MatchId}/start");

        return match;
    }

    public static async Task<Guid> CreateTeamAsync(this HttpClient client, string name) =>
        await client.CreateAsync("/api/teams", new { name, country = "El Salvador" });

    public static async Task<Guid> AddPlayerAsync(this HttpClient client, Guid teamId, string name, int shirtNumber) =>
        await client.CreateAsync($"/api/teams/{teamId}/players", new { name, shirtNumber });

    public static async Task<Guid> StartMatchAsync(this HttpClient client, Guid homeTeamId, Guid awayTeamId, DateTimeOffset scheduledAt)
    {
        var matchId = await client.CreateAsync("/api/matches", new { homeTeamId, awayTeamId, scheduledAt });
        await client.ExpectNoContentAsync($"/api/matches/{matchId}/start");

        return matchId;
    }

    public static async Task<Guid> ScoreAsync(this HttpClient client, Guid matchId, Guid playerId, int minute, bool isOwnGoal = false) =>
        await client.CreateAsync($"/api/matches/{matchId}/goals", new { playerId, minute, isOwnGoal });

    public static async Task PlayAsync(
        this HttpClient client,
        (Guid TeamId, Guid PlayerId) home,
        (Guid TeamId, Guid PlayerId) away,
        int homeGoals,
        int awayGoals,
        DateTimeOffset scheduledAt)
    {
        var matchId = await client.StartMatchAsync(home.TeamId, away.TeamId, scheduledAt);

        for (var minute = 1; minute <= homeGoals; minute++)
            await client.ScoreAsync(matchId, home.PlayerId, minute);

        for (var minute = 1; minute <= awayGoals; minute++)
            await client.ScoreAsync(matchId, away.PlayerId, 45 + minute);

        await client.ExpectNoContentAsync($"/api/matches/{matchId}/finish");
    }

    public static async Task ExpectNoContentAsync(this HttpClient client, string url)
    {
        var response = await client.PostJsonAsync(url);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
