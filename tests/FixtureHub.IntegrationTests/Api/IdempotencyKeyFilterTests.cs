using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FixtureHub.IntegrationTests.Api;

[Collection(nameof(SqlServerCollection))]
public sealed partial class IdempotencyKeyFilterTests(SqlServerFixture database)
{
    private readonly HttpClient _client = database.CreateClient();

    [Fact]
    public async Task Post_WithoutKey_Returns400KeyRequiredAndCreatesNothing()
    {
        var name = TestData.UniqueName("Los Halcones");

        var response = await _client.PostJsonWithoutKeyAsync("/api/teams", new { name, country = "El Salvador" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Idempotency.KeyRequired", (await response.ReadProblemAsync()).ErrorCode());
        Assert.Equal(0, await CountTeamsAsync(name));
    }

    [Fact]
    public async Task Post_WithAnEmptyKey_Returns400KeyRequired()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/teams")
        {
            Content = JsonContent.Create(new { name = TestData.UniqueName("Los Halcones"), country = "El Salvador" })
        };
        request.Headers.TryAddWithoutValidation(HttpClientExtensions.IdempotencyKeyHeader, string.Empty);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Idempotency.KeyRequired", (await response.ReadProblemAsync()).ErrorCode());
    }

    [Fact]
    public async Task Post_WithAKeyLongerThan100Characters_Returns400KeyTooLong()
    {
        var response = await _client.PostJsonAsync(
            "/api/teams",
            new { name = TestData.UniqueName("Los Halcones"), country = "El Salvador" },
            idempotencyKey: new string('k', 101));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadProblemAsync();
        Assert.Equal("Idempotency.KeyTooLong", problem.ErrorCode());
        Assert.Equal(100, problem.Metadata().GetProperty("maxLength").GetInt32());
    }

    [Fact]
    public async Task Post_RepeatedWithTheSameKey_ReturnsTheSameResponseMarkedAsReplayedAndCreatesOneTeam()
    {
        var key = Guid.NewGuid().ToString();
        var body = new { name = TestData.UniqueName("Los Halcones"), country = "El Salvador" };

        var first = await _client.PostJsonAsync("/api/teams", body, key);
        var repeated = await _client.PostJsonAsync("/api/teams", body, key);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, repeated.StatusCode);
        Assert.Equal(first.Headers.Location, repeated.Headers.Location);
        Assert.Equal(await first.IdAsync(), await repeated.IdAsync());
        Assert.False(first.Headers.Contains("Idempotent-Replayed"));
        Assert.Equal("true", Assert.Single(repeated.Headers.GetValues("Idempotent-Replayed")));
        Assert.Equal(1, await CountTeamsAsync(body.name));
    }

    [Fact]
    public async Task Post_SameKeyWithADifferentBody_Returns409KeyReused()
    {
        var key = Guid.NewGuid().ToString();
        await _client.PostJsonAsync("/api/teams", new { name = TestData.UniqueName("Los Halcones"), country = "El Salvador" }, key);

        var response = await _client.PostJsonAsync(
            "/api/teams",
            new { name = TestData.UniqueName("Los Pumas"), country = "Guatemala" },
            key);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.ReadProblemAsync();
        Assert.Equal("Idempotency.KeyReused", problem.ErrorCode());
        Assert.Equal(key, problem.Metadata().GetProperty("key").GetString());
    }

    [Fact]
    public async Task Put_WithoutKey_IsNotRejectedByTheFilter()
    {
        var response = await _client.PutAsJsonAsync($"/api/teams/{Guid.NewGuid()}", new { name = "Los Pumas", country = "Guatemala" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Team.NotFound", (await response.ReadProblemAsync()).ErrorCode());
    }

    [Fact]
    public async Task EveryPostEndpoint_WithoutKey_Returns400KeyRequired()
    {
        var routes = database.ApiServices.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Contains("POST") == true)
            .Select(endpoint => endpoint.RoutePattern.RawText!)
            .ToList();

        Assert.Contains("/api/teams/", routes);
        Assert.Contains("/api/teams/{teamId:guid}/players", routes);

        foreach (var route in routes)
        {
            var url = RouteParameter().Replace(route, _ => Guid.NewGuid().ToString());

            var response = await _client.PostJsonWithoutKeyAsync(url, new { });

            Assert.True(
                response.StatusCode == HttpStatusCode.BadRequest
                    && (await response.ReadProblemAsync()).ErrorCode() == "Idempotency.KeyRequired",
                $"POST {route} no exige el header Idempotency-Key (respondió {(int)response.StatusCode}).");
        }
    }

    private Task<int> CountTeamsAsync(string name) =>
        database.QueryAsync(context => context.Teams.CountAsync(team => team.Name == name));

    [GeneratedRegex(@"\{[^}]+\}")]
    private static partial Regex RouteParameter();
}
