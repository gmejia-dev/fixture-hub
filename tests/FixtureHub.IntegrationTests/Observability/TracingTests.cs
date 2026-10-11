using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using FixtureHub.Application.Abstractions.Reading;
using FixtureHub.Application.Diagnostics;
using FixtureHub.IntegrationTests.Api;
using Microsoft.Extensions.DependencyInjection;

namespace FixtureHub.IntegrationTests.Observability;

[Collection(nameof(SqlServerCollection))]
public sealed class TracingTests(SqlServerFixture database) : IAsyncLifetime
{
    private readonly ObservedApi _api = new(database);

    [Fact]
    public async Task Write_ProducesAServerSpanThatContainsTheCommandSpanWithItsSqlAndItsDomainEvent()
    {
        var client = await _api.CreateAdminClientAsync();
        var traceParent = TraceParent.CreateRandom();

        var response = await client.SendAsync(CreateTeamRequest(TestData.UniqueName("Los Halcones"), traceParent));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var server = await _api.Spans.WaitForAsync(traceParent.TraceId, span => span.Kind == ActivityKind.Server);
        Assert.Equal(traceParent.SpanId, server.ParentSpanId.ToHexString());
        Assert.Equal("POST /api/teams/", server.DisplayName);
        var command = Assert.Single(_api.Spans.Of(traceParent.TraceId), span => span.Source.Name == ApplicationTracing.SourceName);
        Assert.Equal("CreateTeamCommand", command.DisplayName);
        Assert.Equal(server.SpanId, command.ParentSpanId);
        Assert.Equal(ApplicationTracing.Command, command.GetTagItem(ApplicationTracing.OperationTag));
        Assert.Equal(ApplicationTracing.Success, command.GetTagItem(ApplicationTracing.ResultTag));
        var domainEvent = Assert.Single(command.Events, activityEvent => activityEvent.Name == "TeamCreated");
        Assert.Contains(domainEvent.Tags, tag => tag.Key == ApplicationTracing.EventIdTag);
        Assert.Contains(
            _api.Spans.Of(traceParent.TraceId),
            span => span.Kind == ActivityKind.Client && span.ParentSpanId == command.SpanId);
    }

    [Fact]
    public async Task RejectedCommand_RecordsItsErrorCodeWithoutMarkingAnySpanAsFailed()
    {
        var client = await _api.CreateAdminClientAsync();
        var traceParent = TraceParent.CreateRandom();

        var response = await client.SendAsync(CreateTeamRequest(string.Empty, traceParent));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await _api.Spans.WaitForAsync(traceParent.TraceId, span => span.Kind == ActivityKind.Server);
        var command = Assert.Single(_api.Spans.Of(traceParent.TraceId), span => span.Source.Name == ApplicationTracing.SourceName);
        Assert.Equal(ApplicationTracing.Rejected, command.GetTagItem(ApplicationTracing.ResultTag));
        Assert.Equal("Validation.Failed", command.GetTagItem(ApplicationTracing.ErrorCodeTag));
        Assert.All(_api.Spans.Of(traceParent.TraceId), span => Assert.NotEqual(ActivityStatusCode.Error, span.Status));
    }

    [Fact]
    public async Task Query_ProducesASpanWithTheSqlThatReadsTheData()
    {
        var traceParent = TraceParent.CreateRandom();

        var response = await _api.CreateAnonymousClient().SendAsync(StandingsRequest(traceParent));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await _api.Spans.WaitForAsync(traceParent.TraceId, span => span.Kind == ActivityKind.Server);
        var query = Assert.Single(_api.Spans.Of(traceParent.TraceId), span => span.Source.Name == ApplicationTracing.SourceName);
        Assert.Equal("GetStandingsQuery", query.DisplayName);
        Assert.Equal(ApplicationTracing.Query, query.GetTagItem(ApplicationTracing.OperationTag));
        Assert.Contains(
            _api.Spans.Of(traceParent.TraceId),
            span => span.Kind == ActivityKind.Client && span.ParentSpanId == query.SpanId);
    }

    [Fact]
    public async Task ServerError_MarksTheQuerySpanAndTheServerSpanAsFailed()
    {
        await using var api = new ObservedApi(
            database,
            services => services.AddScoped<IStandingsReader, ThrowingStandingsReader>());
        var traceParent = TraceParent.CreateRandom();

        var response = await api.CreateAnonymousClient().SendAsync(StandingsRequest(traceParent));

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var server = await api.Spans.WaitForAsync(traceParent.TraceId, span => span.Kind == ActivityKind.Server);
        Assert.Equal(ActivityStatusCode.Error, server.Status);
        var query = Assert.Single(api.Spans.Of(traceParent.TraceId), span => span.Source.Name == ApplicationTracing.SourceName);
        Assert.Equal(ActivityStatusCode.Error, query.Status);
        Assert.Contains(query.Events, activityEvent => activityEvent.Name == "exception");
    }

    [Fact]
    public async Task HealthCheckRequest_IsNotTraced()
    {
        var client = _api.CreateAnonymousClient();
        var health = TraceParent.CreateRandom();
        var standings = TraceParent.CreateRandom();
        using var healthRequest = new HttpRequestMessage(HttpMethod.Get, "/health/ready");
        healthRequest.Headers.Add(TraceParent.Header, health.Value);

        var response = await client.SendAsync(healthRequest);
        await client.SendAsync(StandingsRequest(standings));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await _api.Spans.WaitForAsync(standings.TraceId, span => span.Kind == ActivityKind.Server);
        Assert.Empty(_api.Spans.Of(health.TraceId));
    }

    private static HttpRequestMessage CreateTeamRequest(string name, TraceParent traceParent)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/teams")
        {
            Content = JsonContent.Create(new { name, country = "El Salvador" })
        };
        request.Headers.Add(HttpClientExtensions.IdempotencyKeyHeader, Guid.NewGuid().ToString());
        request.Headers.Add(TraceParent.Header, traceParent.Value);

        return request;
    }

    private static HttpRequestMessage StandingsRequest(TraceParent traceParent)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/standings");
        request.Headers.Add(TraceParent.Header, traceParent.Value);

        return request;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => _api.DisposeAsync().AsTask();
}
