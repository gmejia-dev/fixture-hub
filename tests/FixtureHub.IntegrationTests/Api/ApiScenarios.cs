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

    public static async Task ExpectNoContentAsync(this HttpClient client, string url)
    {
        var response = await client.PostJsonAsync(url);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
