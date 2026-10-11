using System.Net;
using System.Net.Http.Json;
using FixtureHub.Application.Abstractions.Reading;
using FixtureHub.Application.Teams.CreateTeam;
using FixtureHub.IntegrationTests.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;

namespace FixtureHub.IntegrationTests.Observability;

[Collection(nameof(SqlServerCollection))]
public sealed class CorrelationTests(SqlServerFixture database) : IAsyncLifetime
{
    private const string TraceIdHeader = "X-Trace-Id";

    private readonly ObservedApi _api = new(database);

    [Fact]
    public async Task Response_CarriesTheTraceIdTheRequestArrivedWithInTheXTraceIdHeader()
    {
        var traceParent = TraceParent.CreateRandom();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/standings");
        request.Headers.Add(TraceParent.Header, traceParent.Value);

        var response = await _api.CreateAnonymousClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(traceParent.TraceId, TraceIdOf(response));
    }

    [Fact]
    public async Task ErrorResponse_WithoutTraceparent_CarriesANewTraceIdEqualToTheOneInItsProblem()
    {
        var client = await _api.CreateAdminClientAsync();

        var response = await client.DeleteAsync($"/api/teams/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var traceId = TraceIdOf(response);
        Assert.Matches("^[0-9a-f]{32}$", traceId);
        Assert.Equal(traceId, (await response.ReadProblemAsync()).GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task UnauthenticatedWrite_CarriesTheHeaderEqualToTheTraceIdOfItsProblem()
    {
        var response = await _api.CreateAnonymousClient().PostJsonAsync("/api/teams", new { name = "Los Pumas", country = "Guatemala" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(TraceIdOf(response), (await response.ReadProblemAsync()).GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task ServerError_KeepsTheHeaderAfterTheExceptionHandlerClearsTheResponse()
    {
        await using var api = new ObservedApi(
            database,
            services => services.AddScoped<IStandingsReader, ThrowingStandingsReader>());

        var response = await api.CreateAnonymousClient().GetAsync("/api/standings");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(TraceIdOf(response), (await response.ReadProblemAsync()).GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task LogsOfAnAuthenticatedRequest_CarryTheIdOfTheUserFromTheToken()
    {
        var client = await _api.CreateAdminClientAsync();
        var userId = new JsonWebToken(client.DefaultRequestHeaders.Authorization!.Parameter).Subject;
        var traceParent = TraceParent.CreateRandom();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/teams")
        {
            Content = JsonContent.Create(new { name = TestData.UniqueName("Los Halcones"), country = "El Salvador" })
        };
        request.Headers.Add(HttpClientExtensions.IdempotencyKeyHeader, Guid.NewGuid().ToString());
        request.Headers.Add(TraceParent.Header, traceParent.Value);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var requestLog = await _api.Logs.WaitForAsync(logEvent => logEvent.IsRequestLog("/api/teams") && logEvent.BelongsTo(traceParent.TraceId));
        Assert.Equal(userId, requestLog.Scalar("UserId"));
        Assert.Equal("/api/teams/", requestLog.Scalar("Route"));
        var commandLogs = _api.Logs.Events
            .Where(logEvent => logEvent.BelongsTo(traceParent.TraceId)
                && Equals(logEvent.Scalar("SourceContext"), typeof(CreateTeamCommand).FullName))
            .ToList();
        Assert.NotEmpty(commandLogs);
        Assert.All(commandLogs, logEvent => Assert.Equal(userId, logEvent.Scalar("UserId")));
    }

    [Fact]
    public async Task LogsOfAnAnonymousRequest_HaveNoUserIdAndCarryTheRouteTemplate()
    {
        var teamId = Guid.NewGuid();

        await _api.CreateAnonymousClient().GetAsync($"/api/teams/{teamId}");

        var requestLog = await _api.Logs.WaitForAsync(logEvent => logEvent.IsRequestLog($"/api/teams/{teamId}"));
        Assert.Null(requestLog.Scalar("UserId"));
        Assert.Equal("/api/teams/{teamId:guid}", requestLog.Scalar("Route"));
    }

    private static string TraceIdOf(HttpResponseMessage response) =>
        Assert.Single(response.Headers.GetValues(TraceIdHeader));

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => _api.DisposeAsync().AsTask();
}
