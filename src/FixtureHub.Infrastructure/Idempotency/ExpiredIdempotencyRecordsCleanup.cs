using FixtureHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FixtureHub.Infrastructure.Idempotency;

internal sealed partial class ExpiredIdempotencyRecordsCleanup(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<ExpiredIdempotencyRecordsCleanup> logger)
    : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    public int BatchSize { get; init; } = 1000;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, timeProvider);

        do
        {
            await RunOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await DeleteExpiredAsync(cancellationToken);

            if (deleted > 0)
                LogDeleted(logger, deleted);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogFailed(logger, exception);
        }
    }

    private async Task<int> DeleteExpiredAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var records = scope.ServiceProvider.GetRequiredService<FixtureHubDbContext>().IdempotencyRecords;
        var now = timeProvider.GetUtcNow();
        var total = 0;
        int deleted;

        do
        {
            deleted = await records
                .Where(record => record.ExpiresAt <= now)
                .Take(BatchSize)
                .ExecuteDeleteAsync(cancellationToken);
            total += deleted;
        }
        while (deleted == BatchSize);

        return total;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Se borraron {Count} claves de idempotencia vencidas")]
    private static partial void LogDeleted(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "No se pudieron borrar las claves de idempotencia vencidas; se reintentará en el próximo ciclo")]
    private static partial void LogFailed(ILogger logger, Exception exception);
}
