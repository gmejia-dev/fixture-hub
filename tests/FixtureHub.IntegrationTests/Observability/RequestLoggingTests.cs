using System.Net;
using System.Net.Http.Json;
using FixtureHub.Application.Abstractions.Reading;
using FixtureHub.Application.Teams.CreateTeam;
using FixtureHub.IntegrationTests.Api;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog.Events;

namespace FixtureHub.IntegrationTests.Observability;

[Collection(nameof(SqlServerCollection))]
public sealed class RequestLoggingTests(SqlServerFixture database) : IAsyncLifetime
{
    private const string UnitOfWorkSource = "FixtureHub.Infrastructure.Persistence.UnitOfWork";

    private readonly ObservedApi _api = new(database);

    [Fact]
    public async Task Request_IsLoggedOnceWithItsMethodPathStatusAndDuration()
    {
        var response = await _api.CreateAnonymousClient().GetAsync("/api/standings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var log = await _api.Logs.WaitForAsync(logEvent => logEvent.IsRequestLog("/api/standings"));
        Assert.Equal(LogEventLevel.Information, log.Level);
        Assert.Equal("GET", log.Scalar("RequestMethod"));
        Assert.Equal(200, log.Scalar("StatusCode"));
        Assert.True(log.Scalar("Elapsed") is double elapsed && elapsed > 0);
        Assert.Contains("respondió", log.MessageTemplate.Text, StringComparison.Ordinal);
        Assert.Single(_api.Logs.Events, logEvent => logEvent.IsRequestLog("/api/standings"));
    }

    [Fact]
    public async Task ClientError_IsLoggedAsInformationWithItsStatus()
    {
        var path = $"/api/teams/{Guid.NewGuid()}";

        var response = await _api.CreateAnonymousClient().GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var log = await _api.Logs.WaitForAsync(logEvent => logEvent.IsRequestLog(path));
        Assert.Equal(LogEventLevel.Information, log.Level);
        Assert.Equal(404, log.Scalar("StatusCode"));
    }

    [Fact]
    public async Task ServerError_IsLoggedAsErrorTogetherWithTheExceptionUnderTheSameTraceId()
    {
        await using var api = new ObservedApi(
            database,
            services => services.AddScoped<IStandingsReader, ThrowingStandingsReader>());

        var response = await api.CreateAnonymousClient().GetAsync("/api/standings");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var traceId = (await response.ReadProblemAsync()).GetProperty("traceId").GetString()!;
        var requestLog = await api.Logs.WaitForAsync(logEvent => logEvent.IsRequestLog("/api/standings"));
        Assert.Equal(LogEventLevel.Error, requestLog.Level);
        Assert.Equal(500, requestLog.Scalar("StatusCode"));
        Assert.True(requestLog.BelongsTo(traceId));
        var exceptionLog = Assert.Single(api.Logs.Events, logEvent => logEvent.Exception is not null);
        Assert.Equal(ThrowingStandingsReader.Message, exceptionLog.Exception!.Message);
        Assert.True(exceptionLog.BelongsTo(traceId));
    }

    [Fact]
    public async Task HealthCheckRequest_IsNotLogged()
    {
        var client = _api.CreateAnonymousClient();

        var health = await client.GetAsync("/health/ready");
        await client.GetAsync("/api/standings");

        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        await _api.Logs.WaitForAsync(logEvent => logEvent.IsRequestLog("/api/standings"));
        Assert.DoesNotContain(_api.Logs.Events, logEvent => logEvent.IsRequestLog("/health/ready"));
    }

    [Fact]
    public async Task FailingHealthCheck_IsLoggedAsErrorWithTheNameOfTheCheck()
    {
        await using var api = new ObservedApi(
            database,
            services => services.AddHealthChecks().AddCheck("falla", () => HealthCheckResult.Unhealthy("SQL Server no responde")));

        var response = await api.CreateAnonymousClient().GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var log = await api.Logs.WaitForAsync(logEvent => Equals(logEvent.Scalar("HealthCheckName"), "falla"));
        Assert.Equal(LogEventLevel.Error, log.Level);
        Assert.DoesNotContain(api.Logs.Events, logEvent => logEvent.IsRequestLog("/health/ready"));
    }

    [Fact]
    public async Task LogsOfARequest_AllCarryTheW3CTraceIdItArrivedWith()
    {
        var client = await _api.CreateAdminClientAsync();
        var traceParent = TraceParent.CreateRandom();

        var response = await client.SendAsync(CreateTeamRequest(traceParent));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await _api.Logs.WaitForAsync(logEvent => logEvent.IsRequestLog("/api/teams") && logEvent.BelongsTo(traceParent.TraceId));
        var sources = _api.Logs.Events
            .Where(logEvent => logEvent.BelongsTo(traceParent.TraceId))
            .Select(logEvent => logEvent.Scalar("SourceContext"))
            .ToHashSet();
        Assert.Contains(typeof(CreateTeamCommand).FullName, sources);
        Assert.Contains(UnitOfWorkSource, sources);
        Assert.Contains(LogEventExtensions.RequestLoggingSource, sources);
    }

    [Fact]
    public async Task DomainEventLog_CarriesTheTypeAndTheIdOfTheStoredEvent()
    {
        var client = await _api.CreateAdminClientAsync();
        var traceParent = TraceParent.CreateRandom();

        var response = await client.SendAsync(CreateTeamRequest(traceParent));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var log = await _api.Logs.WaitForAsync(logEvent =>
            logEvent.BelongsTo(traceParent.TraceId) && Equals(logEvent.Scalar("SourceContext"), UnitOfWorkSource));
        var storedId = await database.QueryAsync(context => context.DomainEvents
            .Where(record => record.TraceId == traceParent.TraceId)
            .Select(record => record.Id)
            .SingleAsync());
        Assert.Equal("TeamCreated", log.Scalar("DomainEventType"));
        Assert.Equal(storedId, log.Scalar("DomainEventId"));
    }

    private static HttpRequestMessage CreateTeamRequest(TraceParent traceParent)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/teams")
        {
            Content = JsonContent.Create(new { name = TestData.UniqueName("Los Halcones"), country = "El Salvador" })
        };
        request.Headers.Add(HttpClientExtensions.IdempotencyKeyHeader, Guid.NewGuid().ToString());
        request.Headers.Add(TraceParent.Header, traceParent.Value);

        return request;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => _api.DisposeAsync().AsTask();
}
