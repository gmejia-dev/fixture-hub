using System.Net;
using System.Net.Http.Json;

namespace FixtureHub.IntegrationTests.Api;

[Collection(nameof(SqlServerCollection))]
public sealed class TeamEndpointsTests(SqlServerFixture database)
{
    private readonly HttpClient _client = database.CreateClient();

    [Fact]
    public async Task CreateTeam_Returns201WithTheLocationAndTheId()
    {
        var name = TestData.UniqueName("Los Halcones");

        var response = await _client.PostJsonAsync("/api/teams", new { name, country = "El Salvador" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var teamId = await response.IdAsync();
        Assert.Equal($"/api/teams/{teamId}", response.Headers.Location?.ToString());
        var team = await database.FindTeamAsync(teamId);
        Assert.Equal(name, team?.Name);
    }

    [Fact]
    public async Task UpdateTeam_Returns204AndSavesTheChanges()
    {
        var teamId = await CreateTeamAsync();
        var newName = TestData.UniqueName("Los Pumas");

        var response = await _client.PutAsJsonAsync($"/api/teams/{teamId}", new { name = newName, country = "Guatemala" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var team = await database.FindTeamAsync(teamId);
        Assert.Equal(newName, team?.Name);
        Assert.Equal("Guatemala", team?.Country);
    }

    [Fact]
    public async Task DeleteTeam_Twice_Returns204BothTimes()
    {
        var teamId = await CreateTeamAsync();

        var first = await _client.DeleteAsync($"/api/teams/{teamId}");
        var second = await _client.DeleteAsync($"/api/teams/{teamId}");

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
        Assert.True((await database.FindTeamAsync(teamId))?.IsDeleted);
    }

    [Fact]
    public async Task AddPlayer_Returns201WithTheLocationOfItsTeam()
    {
        var teamId = await CreateTeamAsync();

        var response = await _client.PostJsonAsync($"/api/teams/{teamId}/players", new { name = "Ana Pérez", shirtNumber = 10 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/teams/{teamId}", response.Headers.Location?.ToString());
        var playerId = await response.IdAsync();
        var player = Assert.Single((await database.FindTeamAsync(teamId))!.Players);
        Assert.Equal((playerId, "Ana Pérez", 10), (player.Id, player.Name, player.ShirtNumber));
    }

    [Fact]
    public async Task AddPlayer_ShirtNumberTaken_Returns409WithTheNumberInMetadata()
    {
        var teamId = await CreateTeamAsync();
        await _client.CreateAsync($"/api/teams/{teamId}/players", new { name = "Ana Pérez", shirtNumber = 10 });

        var response = await _client.PostJsonAsync($"/api/teams/{teamId}/players", new { name = "Luis Ramos", shirtNumber = 10 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.ReadProblemAsync();
        Assert.Equal("Player.ShirtNumberTaken", problem.ErrorCode());
        Assert.Equal(10, problem.Metadata().GetProperty("shirtNumber").GetInt32());
    }

    [Fact]
    public async Task UpdatePlayer_Returns204AndSavesTheChanges()
    {
        var teamId = await CreateTeamAsync();
        var playerId = await _client.CreateAsync($"/api/teams/{teamId}/players", new { name = "Ana Pérez", shirtNumber = 10 });

        var response = await _client.PutAsJsonAsync(
            $"/api/teams/{teamId}/players/{playerId}",
            new { name = "Ana María Pérez", shirtNumber = 9 });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var player = Assert.Single((await database.FindTeamAsync(teamId))!.Players);
        Assert.Equal(("Ana María Pérez", 9), (player.Name, player.ShirtNumber));
    }

    [Fact]
    public async Task RemovePlayer_Returns204AndTheTeamNoLongerListsThePlayer()
    {
        var teamId = await CreateTeamAsync();
        var playerId = await _client.CreateAsync($"/api/teams/{teamId}/players", new { name = "Ana Pérez", shirtNumber = 10 });

        var response = await _client.DeleteAsync($"/api/teams/{teamId}/players/{playerId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty((await database.FindTeamAsync(teamId))!.Players);
    }

    private Task<Guid> CreateTeamAsync() =>
        _client.CreateAsync("/api/teams", new { name = TestData.UniqueName("Los Halcones"), country = "El Salvador" });
}
