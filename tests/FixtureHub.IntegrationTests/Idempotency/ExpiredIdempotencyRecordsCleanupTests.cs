using FixtureHub.Application.Abstractions.Idempotency;
using FixtureHub.Infrastructure;
using FixtureHub.Infrastructure.Idempotency;
using FixtureHub.IntegrationTests.Observability;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog.Events;

namespace FixtureHub.IntegrationTests.Idempotency;

[Collection(nameof(SqlServerCollection))]
public sealed class ExpiredIdempotencyRecordsCleanupTests(SqlServerFixture database)
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public async Task RunOnce_DeletesTheKeysExpiredUpToNowInBatchesAndKeepsTheValidOnes()
    {
        var expired = Enumerable.Range(1, 4).Select(hours => NewRecord(expiresAt: Now.AddHours(-hours))).Append(NewRecord(expiresAt: Now));
        var valid = NewRecord(expiresAt: Now.AddTicks(1));
        List<IdempotencyRecord> records = [.. expired, valid];
        await AddAsync(records);
        await using var scope = database.CreateScope();
        var cleanup = new ExpiredIdempotencyRecordsCleanup(
            scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
            new FixedTimeProvider(Now),
            NullLogger<ExpiredIdempotencyRecordsCleanup>.Instance)
        {
            BatchSize = 2
        };

        await cleanup.RunOnceAsync(CancellationToken.None);

        Assert.Equal([valid.Key], await ExistingKeysAsync(records));
    }

    [Fact]
    public async Task RunOnce_WhenTheDatabaseFails_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(database.ConnectionStringFor($"NoExiste_{Guid.NewGuid():N}"));
        await using var provider = services.BuildServiceProvider();
        var cleanup = Assert.Single(provider.GetServices<IHostedService>().OfType<ExpiredIdempotencyRecordsCleanup>());

        var exception = await Record.ExceptionAsync(() => cleanup.RunOnceAsync(CancellationToken.None));

        Assert.Null(exception);
    }

    [Fact]
    public async Task Api_WhenItStarts_DeletesTheExpiredKeysAndLogsHowMany()
    {
        var expired = NewRecord(expiresAt: DateTimeOffset.UtcNow.AddHours(-1));
        await AddAsync([expired]);

        await using var api = new ObservedApi(database);
        api.CreateAnonymousClient().Dispose();

        var log = await api.Logs.WaitForAsync(logEvent =>
            Equals(logEvent.Scalar("SourceContext"), typeof(ExpiredIdempotencyRecordsCleanup).FullName));
        Assert.Equal(LogEventLevel.Information, log.Level);
        Assert.True(log.Scalar("Count") is int count && count >= 1);
        Assert.Empty(await ExistingKeysAsync([expired]));
    }

    private static IdempotencyRecord NewRecord(DateTimeOffset expiresAt) =>
        new($"limpieza:{Guid.NewGuid()}", new string('0', IdempotencyRecord.FingerprintLength), null, expiresAt.AddHours(-24), expiresAt);

    private Task<int> AddAsync(IEnumerable<IdempotencyRecord> records) =>
        database.QueryAsync(context =>
        {
            context.IdempotencyRecords.AddRange(records);
            return context.SaveChangesAsync();
        });

    private Task<List<string>> ExistingKeysAsync(IReadOnlyCollection<IdempotencyRecord> records)
    {
        var keys = records.Select(record => record.Key).ToList();

        return database.QueryAsync(context => context.IdempotencyRecords
            .Where(record => keys.Contains(record.Key))
            .Select(record => record.Key)
            .ToListAsync());
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
