using System.Net;
using System.Net.Http.Json;
using FixtureHub.Application.Pagination;
using FixtureHub.Application.TopScorers.GetTopScorers;

namespace FixtureHub.IntegrationTests.Api;

[Collection(nameof(SqlServerCollection))]
public sealed class TopScorerQueryTests(SqlServerFixture database)
{
    private readonly HttpClient _client = database.CreateClient();

    public static TheoryData<string, string> EverySortField()
    {
        var data = new TheoryData<string, string>();
        foreach (var field in GetTopScorersQuery.SortFields)
        {
            data.Add(field, Paging.Ascending);
            data.Add(field, Paging.Descending);
        }

        return data;
    }

    [Fact]
    public async Task GetTopScorers_CountsValidGoalsOfLiveAndFinishedMatchesOnly()
    {
        var home = await _client.CreateTeamAsync(TestData.UniqueName("Los Halcones"));
        var away = await _client.CreateTeamAsync(TestData.UniqueName("Los Pumas"));
        var striker = await _client.AddPlayerAsync(home, "Ana Pérez", 9);
        var defender = await _client.AddPlayerAsync(away, "Luis Ramos", 4);

        var finished = await _client.StartMatchAsync(home, away, ApiScenarios.KickOff);
        await _client.ScoreAsync(finished, striker, 10);
        await _client.ScoreAsync(finished, striker, 20);
        var annulled = await _client.ScoreAsync(finished, striker, 30);
        await _client.ExpectNoContentAsync($"/api/matches/{finished}/goals/{annulled}/annul");
        await _client.ScoreAsync(finished, defender, 40, isOwnGoal: true);
        await _client.ExpectNoContentAsync($"/api/matches/{finished}/finish");

        var cancelled = await _client.StartMatchAsync(home, away, ApiScenarios.KickOff.AddDays(1));
        await _client.ScoreAsync(cancelled, striker, 5);
        await _client.ExpectNoContentAsync($"/api/matches/{cancelled}/cancel");

        var live = await _client.StartMatchAsync(home, away, ApiScenarios.KickOff.AddDays(2));
        await _client.ScoreAsync(live, striker, 15);

        var homeScorers = await GetTopScorersAsync($"teamId={home}");
        var awayScorers = await GetTopScorersAsync($"teamId={away}");

        var scorer = Assert.Single(homeScorers.Data);
        Assert.Equal((striker, "Ana Pérez", 3), (scorer.PlayerId, scorer.PlayerName, scorer.Goals));
        Assert.Empty(awayScorers.Data);
    }

    [Fact]
    public async Task GetTopScorers_TiedPlayers_ShareThePositionAndTheNextOneSkipsIt()
    {
        var team = await _client.CreateTeamAsync(TestData.UniqueName("Los Halcones"));
        var rival = await _client.CreateTeamAsync(TestData.UniqueName("Los Pumas"));
        var ana = await _client.AddPlayerAsync(team, "Ana", 9);
        var beto = await _client.AddPlayerAsync(team, "Beto", 10);
        var carla = await _client.AddPlayerAsync(team, "Carla", 11);
        var matchId = await _client.StartMatchAsync(team, rival, ApiScenarios.KickOff);
        foreach (var (player, minute) in new[] { (beto, 1), (ana, 2), (beto, 3), (ana, 4), (carla, 5) })
            await _client.ScoreAsync(matchId, player, minute);

        var scorers = (await GetTopScorersAsync($"teamId={team}")).Data;

        Assert.Equal([ana, beto, carla], scorers.Select(scorer => scorer.PlayerId));
        Assert.Equal(scorers[0].Position, scorers[1].Position);
        Assert.True(scorers[2].Position >= scorers[0].Position + 2, "RANK salta las posiciones de los empatados.");
    }

    [Fact]
    public async Task GetTopScorers_PlayerRemovedFromTheTeam_KeepsTheGoals()
    {
        var team = await _client.CreateTeamAsync(TestData.UniqueName("Los Halcones"));
        var rival = await _client.CreateTeamAsync(TestData.UniqueName("Los Pumas"));
        var player = await _client.AddPlayerAsync(team, "Ana", 9);
        var matchId = await _client.StartMatchAsync(team, rival, ApiScenarios.KickOff);
        await _client.ScoreAsync(matchId, player, 10);
        await _client.ExpectNoContentAsync($"/api/matches/{matchId}/finish");

        await _client.DeleteAsync($"/api/teams/{team}/players/{player}");

        Assert.Equal([player], (await GetTopScorersAsync($"teamId={team}")).Data.Select(scorer => scorer.PlayerId));
    }

    [Fact]
    public async Task GetTopScorers_DeletedTeam_LeavesTheList()
    {
        var team = await _client.CreateTeamAsync(TestData.UniqueName("Los Halcones"));
        var rival = await _client.CreateTeamAsync(TestData.UniqueName("Los Pumas"));
        var player = await _client.AddPlayerAsync(team, "Ana", 9);
        var matchId = await _client.StartMatchAsync(team, rival, ApiScenarios.KickOff);
        await _client.ScoreAsync(matchId, player, 10);
        await _client.ExpectNoContentAsync($"/api/matches/{matchId}/finish");

        await _client.DeleteAsync($"/api/teams/{team}");

        Assert.Empty((await GetTopScorersAsync($"teamId={team}")).Data);
    }

    [Theory]
    [MemberData(nameof(EverySortField))]
    public async Task GetTopScorers_EveryWhitelistedSortField_RunsInBothDirections(string sortBy, string sortDirection)
    {
        var response = await _client.GetAsync($"/api/top-scorers?sortBy={sortBy}&sortDirection={sortDirection}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<PagedResult<TopScorerItem>> GetTopScorersAsync(string query) =>
        (await _client.GetFromJsonAsync<PagedResult<TopScorerItem>>($"/api/top-scorers?{query}"))!;
}
