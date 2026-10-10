using System.Net;
using System.Net.Http.Json;
using FixtureHub.Application.Pagination;
using FixtureHub.Application.Standings.GetStandings;

namespace FixtureHub.IntegrationTests.Api;

[Collection(nameof(SqlServerCollection))]
public sealed class StandingsQueryTests(SqlServerFixture database)
{
    private readonly HttpClient _client = database.CreateClient();

    public static TheoryData<string, string> EverySortField()
    {
        var data = new TheoryData<string, string>();
        foreach (var field in GetStandingsQuery.SortFields)
        {
            data.Add(field, Paging.Ascending);
            data.Add(field, Paging.Descending);
        }

        return data;
    }

    [Fact]
    public async Task GetStandings_OrdersByPointsThenGoalDifferenceThenGoalsForThenName()
    {
        var prefix = TestData.UniqueName("Liga");
        var alfa = await TeamAsync($"{prefix} Alfa");
        var beta = await TeamAsync($"{prefix} Beta");
        var gamma = await TeamAsync($"{prefix} Gamma");
        var delta = await TeamAsync($"{prefix} Delta");
        var epsilon = await TeamAsync($"{prefix} Epsilon");
        await TeamAsync($"{prefix} Zeta");
        await TeamAsync($"{prefix} Eta");

        await _client.PlayAsync(alfa, gamma, 2, 0, ApiScenarios.KickOff);
        await _client.PlayAsync(beta, gamma, 1, 0, ApiScenarios.KickOff.AddDays(1));
        await _client.PlayAsync(delta, epsilon, 3, 1, ApiScenarios.KickOff);

        var table = await GetStandingsAsync($"teamName={Uri.EscapeDataString(prefix)}&pageSize=100");

        Assert.Equal(
            ["Delta", "Alfa", "Beta", "Eta", "Zeta", "Epsilon", "Gamma"],
            table.Items.Select(row => row.TeamName[(prefix.Length + 1)..]));
        Assert.Equal(table.Items.Select(row => row.Position).Order(), table.Items.Select(row => row.Position));
        var leader = table.Items[0];
        Assert.Equal(
            (1, 1, 0, 0, 3, 1, 2, 3),
            (leader.Played, leader.Won, leader.Drawn, leader.Lost, leader.GoalsFor, leader.GoalsAgainst, leader.GoalDifference, leader.Points));
    }

    [Fact]
    public async Task GetStandings_FilteredByName_KeepsThePositionOfTheWholeTable()
    {
        var prefix = TestData.UniqueName("Liga");
        var winner = await TeamAsync($"{prefix} Ganador");
        var loser = await TeamAsync($"{prefix} Perdedor");
        await _client.PlayAsync(winner, loser, 1, 0, ApiScenarios.KickOff);

        var both = await GetStandingsAsync($"teamName={Uri.EscapeDataString(prefix)}");
        var onlyTheLoser = await GetStandingsAsync($"teamName={Uri.EscapeDataString($"{prefix} Perdedor")}");

        var position = Assert.Single(onlyTheLoser.Items).Position;
        Assert.Equal(both.Items.Single(row => row.TeamId == loser.TeamId).Position, position);
        Assert.True(position > 1, "Filtrar no debe renumerar la tabla.");
    }

    [Fact]
    public async Task GetStandings_DeletedTeam_LeavesTheTable()
    {
        var prefix = TestData.UniqueName("Liga");
        var kept = await TeamAsync($"{prefix} Activo");
        var deleted = await TeamAsync($"{prefix} Borrado");
        await _client.DeleteAsync($"/api/teams/{deleted.TeamId}");

        var table = await GetStandingsAsync($"teamName={Uri.EscapeDataString(prefix)}");

        Assert.Equal([kept.TeamId], table.Items.Select(row => row.TeamId));
    }

    [Fact]
    public async Task GetStandings_ResultCorrected_ShowsTheNewResult()
    {
        var prefix = TestData.UniqueName("Liga");
        var home = await TeamAsync($"{prefix} Local");
        var away = await TeamAsync($"{prefix} Visitante");
        var matchId = await _client.StartMatchAsync(home.TeamId, away.TeamId, ApiScenarios.KickOff);
        await _client.ScoreAsync(matchId, home.PlayerId, 10);
        await _client.ExpectNoContentAsync($"/api/matches/{matchId}/finish");

        await _client.PutAsJsonAsync(
            $"/api/matches/{matchId}/result",
            new { goals = new[] { new { playerId = away.PlayerId, minute = 10, isOwnGoal = false } }, reason = "Gol mal asignado." });

        var table = await GetStandingsAsync($"teamName={Uri.EscapeDataString(prefix)}");
        Assert.Equal(["Visitante", "Local"], table.Items.Select(row => row.TeamName[(prefix.Length + 1)..]));
        Assert.Equal([3, 0], table.Items.Select(row => row.Points));
    }

    [Theory]
    [MemberData(nameof(EverySortField))]
    public async Task GetStandings_EveryWhitelistedSortField_RunsInBothDirections(string sortBy, string sortDirection)
    {
        var response = await _client.GetAsync($"/api/standings?sortBy={sortBy}&sortDirection={sortDirection}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<(Guid TeamId, Guid PlayerId)> TeamAsync(string name)
    {
        var teamId = await _client.CreateTeamAsync(name);

        return (teamId, await _client.AddPlayerAsync(teamId, $"Capitán de {name}", 10));
    }

    private async Task<PagedResult<StandingItem>> GetStandingsAsync(string query) =>
        (await _client.GetFromJsonAsync<PagedResult<StandingItem>>($"/api/standings?{query}"))!;
}
