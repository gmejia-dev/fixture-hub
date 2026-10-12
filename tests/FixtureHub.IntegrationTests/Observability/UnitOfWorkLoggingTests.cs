using System.Net;
using System.Net.Http.Json;
using FixtureHub.IntegrationTests.Api;
using Serilog.Events;

namespace FixtureHub.IntegrationTests.Observability;

[Collection(nameof(SqlServerCollection))]
public sealed class UnitOfWorkLoggingTests(SqlServerFixture database)
{
    private const string UnitOfWorkSource = "FixtureHub.Infrastructure.Persistence.UnitOfWork";

    private static readonly Dictionary<string, string> DebugLogs = new()
    {
        ["Serilog:MinimumLevel:Override:FixtureHub"] = "Debug"
    };

    [Fact]
    public async Task SuccessfulCommand_LogsAtDebugTheTransactionOpenedAndCommittedWithItsChanges()
    {
        await using var api = new ObservedApi(database, settings: DebugLogs);
        var client = await api.CreateAdminClientAsync();
        var traceParent = TraceParent.CreateRandom();

        var response = await client.SendAsync(CreateTeamRequest(TestData.UniqueName("Los Halcones"), traceParent));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var logs = await TransactionLogsAsync(api, traceParent);
        Assert.Equal(["Transacción abierta", "Transacción confirmada; cambios guardados: {Changes}"], logs.Select(Template));
        Assert.All(logs, log => Assert.Equal(LogEventLevel.Debug, log.Level));
        Assert.Equal(4, logs[1].Scalar("Changes"));
    }

    [Fact]
    public async Task RejectedCommand_LogsAtDebugTheTransactionOpenedAndRolledBack()
    {
        await using var api = new ObservedApi(database, settings: DebugLogs);
        var client = await api.CreateAdminClientAsync();
        var name = TestData.UniqueName("Los Halcones");
        await client.CreateAsync("/api/teams", new { name, country = "El Salvador" });
        var traceParent = TraceParent.CreateRandom();

        var response = await client.SendAsync(CreateTeamRequest(name, traceParent));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var logs = await TransactionLogsAsync(api, traceParent);
        Assert.Equal(["Transacción abierta", "Transacción revertida"], logs.Select(Template));
        Assert.All(logs, log => Assert.Equal(LogEventLevel.Debug, log.Level));
    }

    [Fact]
    public async Task WithTheDefaultLevels_TheTransactionIsNotLogged()
    {
        await using var api = new ObservedApi(database);
        var client = await api.CreateAdminClientAsync();
        var traceParent = TraceParent.CreateRandom();

        var response = await client.SendAsync(CreateTeamRequest(TestData.UniqueName("Los Halcones"), traceParent));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Empty(await TransactionLogsAsync(api, traceParent));
        Assert.Contains(api.Logs.Events, log => log.BelongsTo(traceParent.TraceId) && IsFromTheUnitOfWork(log));
    }

    private static async Task<List<LogEvent>> TransactionLogsAsync(ObservedApi api, TraceParent traceParent)
    {
        await api.Logs.WaitForAsync(log => log.IsRequestLog("/api/teams") && log.BelongsTo(traceParent.TraceId));

        return api.Logs.Events
            .Where(log => log.BelongsTo(traceParent.TraceId) && IsFromTheUnitOfWork(log))
            .Where(log => Template(log).StartsWith("Transacción", StringComparison.Ordinal))
            .ToList();
    }

    private static bool IsFromTheUnitOfWork(LogEvent log) => Equals(log.Scalar("SourceContext"), UnitOfWorkSource);

    private static string Template(LogEvent log) => log.MessageTemplate.Text;

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
}
