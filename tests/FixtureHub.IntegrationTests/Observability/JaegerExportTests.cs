using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace FixtureHub.IntegrationTests.Observability;

[Collection(nameof(SqlServerCollection))]
public sealed class JaegerExportTests(SqlServerFixture database) : IAsyncLifetime
{
    private const string JaegerImage = "jaegertracing/jaeger:2.22.0";
    private const int OtlpGrpcPort = 4317;
    private const int QueryPort = 16686;

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    private readonly IContainer _jaeger = new ContainerBuilder(JaegerImage)
        .WithPortBinding(OtlpGrpcPort, assignRandomHostPort: true)
        .WithPortBinding(QueryPort, assignRandomHostPort: true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPort(QueryPort).ForPath("/")))
        .Build();

    [Fact]
    public async Task Request_IsExportedToJaegerByOtlpWithTheServiceNameAndItsSpans()
    {
        var traceParent = TraceParent.CreateRandom();
        var settings = new Dictionary<string, string> { ["OTEL_EXPORTER_OTLP_ENDPOINT"] = Endpoint(OtlpGrpcPort) };

        await using (var api = new ObservedApi(database, settings: settings))
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/standings");
            request.Headers.Add(TraceParent.Header, traceParent.Value);

            var response = await api.CreateAnonymousClient().SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            await api.Spans.WaitForAsync(traceParent.TraceId, span => span.Kind == ActivityKind.Server);
        }

        var trace = await FetchTraceAsync(traceParent.TraceId);
        var operations = trace.GetProperty("spans").EnumerateArray()
            .Select(span => span.GetProperty("operationName").GetString())
            .ToList();
        Assert.Contains("GET /api/standings", operations);
        Assert.Contains("GetStandingsQuery", operations);
        Assert.Contains(
            "fixturehub-api",
            trace.GetProperty("processes").EnumerateObject().Select(process => process.Value.GetProperty("serviceName").GetString()));
    }

    private async Task<JsonElement> FetchTraceAsync(string traceId)
    {
        using var client = new HttpClient { BaseAddress = new Uri(Endpoint(QueryPort)) };
        var deadline = DateTime.UtcNow + Timeout;

        while (true)
        {
            var response = await client.GetAsync($"/api/traces/{traceId}");
            if (response.IsSuccessStatusCode)
                return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data")[0];

            if (DateTime.UtcNow > deadline)
                throw new TimeoutException($"Jaeger no tiene la traza {traceId} (status {(int)response.StatusCode}).");

            await Task.Delay(250);
        }
    }

    private string Endpoint(int port) => $"http://{_jaeger.Hostname}:{_jaeger.GetMappedPublicPort(port)}";

    public Task InitializeAsync() => _jaeger.StartAsync();

    public Task DisposeAsync() => _jaeger.DisposeAsync().AsTask();
}
