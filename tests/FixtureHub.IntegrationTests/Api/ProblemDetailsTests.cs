using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace FixtureHub.IntegrationTests.Api;

[Collection(nameof(SqlServerCollection))]
public sealed class ProblemDetailsTests(SqlServerFixture database)
{
    private readonly HttpClient _client = database.CreateClient();

    [Fact]
    public async Task ValidationError_Returns400WithTheCodesPerFieldInMetadata()
    {
        var response = await _client.PostJsonAsync("/api/teams", new { name = "", country = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadProblemAsync();
        Assert.Equal(400, problem.GetProperty("status").GetInt32());
        Assert.Equal("Validation.Failed", problem.ErrorCode());
        Assert.Equal("La solicitud tiene datos inválidos.", problem.GetProperty("detail").GetString());
        Assert.Equal("/api/teams", problem.GetProperty("instance").GetString());
        var errors = problem.Metadata().GetProperty("errors");
        Assert.Equal("Team.NameRequired", errors.GetProperty("name")[0].GetString());
        Assert.Equal("Team.CountryRequired", errors.GetProperty("country")[0].GetString());
    }

    [Fact]
    public async Task NotFound_Returns404WithTheIdInMetadata()
    {
        var teamId = Guid.NewGuid();

        var response = await _client.DeleteAsync($"/api/teams/{teamId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.ReadProblemAsync();
        Assert.Equal("Team.NotFound", problem.ErrorCode());
        Assert.Equal(teamId, problem.Metadata().GetProperty("teamId").GetGuid());
    }

    [Fact]
    public async Task Conflict_Returns409WithTheValueInMetadata()
    {
        var name = TestData.UniqueName("Los Halcones");
        await _client.CreateAsync("/api/teams", new { name, country = "El Salvador" });

        var response = await _client.PostJsonAsync("/api/teams", new { name, country = "Guatemala" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.ReadProblemAsync();
        Assert.Equal("Team.NameTaken", problem.ErrorCode());
        Assert.Equal(name, problem.Metadata().GetProperty("name").GetString());
    }

    [Fact]
    public async Task ErrorWithoutMetadata_ReturnsAnEmptyMetadataObject()
    {
        var response = await _client.PostJsonWithoutKeyAsync("/api/teams", new { name = "Los Pumas", country = "Guatemala" });

        var problem = await response.ReadProblemAsync();
        Assert.Equal("Idempotency.KeyRequired", problem.ErrorCode());
        Assert.Empty(problem.Metadata().EnumerateObject());
    }

    [Fact]
    public async Task MalformedJson_Returns400RequestMalformed()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/teams")
        {
            Content = new StringContent("{ \"name\": ", Encoding.UTF8, "application/json")
        };
        request.Headers.Add(HttpClientExtensions.IdempotencyKeyHeader, Guid.NewGuid().ToString());

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Request.Malformed", (await response.ReadProblemAsync()).ErrorCode());
    }

    [Fact]
    public async Task FieldWithTheWrongType_Returns400RequestMalformed()
    {
        var teamId = await _client.CreateAsync("/api/teams", new { name = TestData.UniqueName("Los Halcones"), country = "El Salvador" });

        var response = await _client.PostJsonAsync($"/api/teams/{teamId}/players", new { name = "Ana", shirtNumber = "diez" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Request.Malformed", (await response.ReadProblemAsync()).ErrorCode());
    }

    [Fact]
    public async Task EveryProblem_CarriesTheTraceIdOfTheRequest()
    {
        const string traceId = "4bf92f3577b34da6a3ce929d0e0e4736";
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/teams/{Guid.NewGuid()}");
        request.Headers.Add("traceparent", $"00-{traceId}-00f067aa0ba902b7-01");

        var response = await _client.SendAsync(request);

        Assert.Equal(traceId, (await response.ReadProblemAsync()).GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task TheTraceIdOfTheRequest_IsStoredWithItsDomainEvents()
    {
        const string traceId = "0af7651916cd43dd8448eb211c80319c";
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/teams")
        {
            Content = JsonContent.Create(new { name = TestData.UniqueName("Los Halcones"), country = "El Salvador" })
        };
        request.Headers.Add(HttpClientExtensions.IdempotencyKeyHeader, Guid.NewGuid().ToString());
        request.Headers.Add("traceparent", $"00-{traceId}-b7ad6b7169203331-01");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var events = await database.QueryAsync(context =>
            context.DomainEvents.Where(record => record.TraceId == traceId).Select(record => record.Type).ToListAsync());
        Assert.Equal(["TeamCreated"], events);
    }

    [Fact]
    public async Task UnknownRoute_Returns404AsProblemDetails()
    {
        var response = await _client.GetAsync("/api/no-existe");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(404, (await response.ReadProblemAsync()).GetProperty("status").GetInt32());
    }
}
