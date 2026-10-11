using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FixtureHub.Application.Pagination;
using FixtureHub.Application.Teams.GetPlayers;
using FixtureHub.Application.Teams.GetTeamById;
using FixtureHub.Application.Teams.GetTeams;
using Microsoft.EntityFrameworkCore;

namespace FixtureHub.IntegrationTests.Api;

[Collection(nameof(SqlServerCollection))]
public sealed class TeamQueryTests(SqlServerFixture database)
{
    private readonly HttpClient _client = database.CreateClient();

    public static TheoryData<string, string> EverySortField()
    {
        var data = new TheoryData<string, string>();
        foreach (var field in GetTeamsQuery.SortFields)
        {
            data.Add(field, Paging.Ascending);
            data.Add(field, Paging.Descending);
        }

        return data;
    }

    [Fact]
    public async Task GetTeams_FilteredByName_ReturnsTheMatchingTeamsPaginatedInSql()
    {
        var prefix = TestData.UniqueName("Copa");
        foreach (var name in new[] { "Alfa", "Beta", "Gamma" })
            await CreateTeamAsync($"{prefix} {name}");

        var firstPage = await GetTeamsAsync($"name={Uri.EscapeDataString(prefix)}&pageSize=2");
        var secondPage = await GetTeamsAsync($"name={Uri.EscapeDataString(prefix)}&pageSize=2&pageNumber=2");

        Assert.Equal([$"{prefix} Alfa", $"{prefix} Beta"], firstPage.Data.Select(team => team.Name));
        Assert.Equal([$"{prefix} Gamma"], secondPage.Data.Select(team => team.Name));
        Assert.Equal((3, 2), (firstPage.TotalRecords, firstPage.TotalPages));
    }

    [Fact]
    public async Task GetTeams_Response_HasThePaginationContractInCamelCase()
    {
        var prefix = TestData.UniqueName("Copa");
        await CreateTeamAsync($"{prefix} Alfa");

        var json = await _client.GetFromJsonAsync<JsonElement>($"/api/teams?name={Uri.EscapeDataString(prefix)}");

        Assert.Equal(
            ["data", "pageNumber", "pageSize", "totalRecords", "totalPages"],
            json.EnumerateObject().Select(property => property.Name));
        Assert.Equal(
            ["id", "name", "country", "playerCount"],
            json.GetProperty("data")[0].EnumerateObject().Select(property => property.Name));
    }

    [Fact]
    public async Task GetTeams_SortedByNameDescending_ReturnsTheReverseOrder()
    {
        var prefix = TestData.UniqueName("Copa");
        foreach (var name in new[] { "Alfa", "Beta", "Gamma" })
            await CreateTeamAsync($"{prefix} {name}");

        var page = await GetTeamsAsync($"name={Uri.EscapeDataString(prefix)}&sortBy=name&sortDirection=desc");

        Assert.Equal(["Gamma", "Beta", "Alfa"], page.Data.Select(team => team.Name[(prefix.Length + 1)..]));
    }

    [Fact]
    public async Task GetTeams_CountsOnlyTheActivePlayersAndSkipsDeletedTeams()
    {
        var prefix = TestData.UniqueName("Copa");
        var active = await CreateTeamAsync($"{prefix} Activo");
        var deleted = await CreateTeamAsync($"{prefix} Borrado");
        await _client.CreateAsync($"/api/teams/{active}/players", new { name = "Ana", shirtNumber = 1 });
        var removed = await _client.CreateAsync($"/api/teams/{active}/players", new { name = "Luis", shirtNumber = 2 });
        await _client.DeleteAsync($"/api/teams/{active}/players/{removed}");
        await _client.DeleteAsync($"/api/teams/{deleted}");

        var page = await GetTeamsAsync($"name={Uri.EscapeDataString(prefix)}");

        var team = Assert.Single(page.Data);
        Assert.Equal((active, 1), (team.Id, team.PlayerCount));
    }

    [Fact]
    public async Task GetTeams_NameWithWildcards_IsSearchedLiterally()
    {
        var prefix = TestData.UniqueName("Copa");
        await CreateTeamAsync($"{prefix} 50% [A]_1");
        await CreateTeamAsync($"{prefix} 500 A11");

        var page = await GetTeamsAsync($"name={Uri.EscapeDataString($"{prefix} 50% [A]_")}");

        Assert.Equal([$"{prefix} 50% [A]_1"], page.Data.Select(team => team.Name));
    }

    [Theory]
    [MemberData(nameof(EverySortField))]
    public async Task GetTeams_EveryWhitelistedSortField_RunsInBothDirections(string sortBy, string sortDirection)
    {
        var response = await _client.GetAsync($"/api/teams?sortBy={sortBy}&sortDirection={sortDirection}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTeams_SqlInjectionInSortBy_IsRejectedBeforeReachingTheDatabase()
    {
        await CreateTeamAsync(TestData.UniqueName("Copa"));

        var response = await _client.GetAsync($"/api/teams?sortBy={Uri.EscapeDataString("Name; DROP TABLE Teams--")}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.ReadProblemAsync()).Metadata().GetProperty("errors");
        Assert.Equal("Query.InvalidSortBy", errors.GetProperty("sortBy")[0].GetString());
        Assert.True(await database.QueryAsync(context => context.Teams.AnyAsync()));
    }

    [Fact]
    public async Task GetTeams_InvalidPagination_Returns400WithACodePerParameter()
    {
        var response = await _client.GetAsync("/api/teams?pageNumber=0&pageSize=101&sortDirection=up");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.ReadProblemAsync()).Metadata().GetProperty("errors");
        Assert.Equal("Query.InvalidPageNumber", errors.GetProperty("pageNumber")[0].GetString());
        Assert.Equal("Query.InvalidPageSize", errors.GetProperty("pageSize")[0].GetString());
        Assert.Equal("Query.InvalidSortDirection", errors.GetProperty("sortDirection")[0].GetString());
    }

    [Fact]
    public async Task GetTeams_PageNumberThatIsNotANumber_Returns400RequestMalformed()
    {
        var response = await _client.GetAsync("/api/teams?pageNumber=uno");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Request.Malformed", (await response.ReadProblemAsync()).ErrorCode());
    }

    [Fact]
    public async Task GetTeams_HugePageNumber_ReturnsAnEmptyPageInsteadOfFailing()
    {
        var page = await GetTeamsAsync($"pageNumber={int.MaxValue}&pageSize=100");

        Assert.Empty(page.Data);
        Assert.Equal(int.MaxValue, page.PageNumber);
    }

    [Fact]
    public async Task GetTeam_ReturnsTheActivePlayersOrderedByShirtNumber()
    {
        var teamId = await CreateTeamAsync(TestData.UniqueName("Los Halcones"));
        await _client.CreateAsync($"/api/teams/{teamId}/players", new { name = "Nueve", shirtNumber = 9 });
        await _client.CreateAsync($"/api/teams/{teamId}/players", new { name = "Uno", shirtNumber = 1 });
        var removed = await _client.CreateAsync($"/api/teams/{teamId}/players", new { name = "Cinco", shirtNumber = 5 });
        await _client.DeleteAsync($"/api/teams/{teamId}/players/{removed}");

        var team = await _client.GetFromJsonAsync<TeamDetails>($"/api/teams/{teamId}");

        Assert.Equal(teamId, team?.Id);
        Assert.Equal([1, 9], team!.Players.Select(player => player.ShirtNumber));
    }

    [Fact]
    public async Task GetTeam_Deleted_Returns404()
    {
        var teamId = await CreateTeamAsync(TestData.UniqueName("Los Halcones"));
        await _client.DeleteAsync($"/api/teams/{teamId}");

        var response = await _client.GetAsync($"/api/teams/{teamId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Team.NotFound", (await response.ReadProblemAsync()).ErrorCode());
    }

    [Fact]
    public async Task GetPlayers_SortedByNameAndPaginated_ReturnsTheRequestedPage()
    {
        var teamId = await CreateTeamAsync(TestData.UniqueName("Los Halcones"));
        await _client.CreateAsync($"/api/teams/{teamId}/players", new { name = "Carla", shirtNumber = 1 });
        await _client.CreateAsync($"/api/teams/{teamId}/players", new { name = "Ana", shirtNumber = 2 });
        await _client.CreateAsync($"/api/teams/{teamId}/players", new { name = "Beto", shirtNumber = 3 });

        var page = await _client.GetFromJsonAsync<PagedResult<PlayerItem>>(
            $"/api/teams/{teamId}/players?sortBy=name&pageSize=2&pageNumber=2");

        Assert.Equal(["Carla"], page!.Data.Select(player => player.Name));
        Assert.Equal(3, page.TotalRecords);
    }

    [Fact]
    public async Task GetPlayers_UnknownTeam_Returns404()
    {
        var response = await _client.GetAsync($"/api/teams/{Guid.NewGuid()}/players");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Team.NotFound", (await response.ReadProblemAsync()).ErrorCode());
    }

    private Task<Guid> CreateTeamAsync(string name) =>
        _client.CreateAsync("/api/teams", new { name, country = "El Salvador" });

    private async Task<PagedResult<TeamSummary>> GetTeamsAsync(string query) =>
        (await _client.GetFromJsonAsync<PagedResult<TeamSummary>>($"/api/teams?{query}"))!;
}
