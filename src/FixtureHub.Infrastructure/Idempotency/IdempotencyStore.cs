using System.Data;
using FixtureHub.Application.Abstractions.Idempotency;
using FixtureHub.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FixtureHub.Infrastructure.Idempotency;

internal sealed class IdempotencyStore(FixtureHubDbContext context) : IIdempotencyStore
{
    public static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);

    public async Task<bool> TryLockAsync(string key, CancellationToken cancellationToken)
    {
        var result = new SqlParameter("@result", SqlDbType.Int) { Direction = ParameterDirection.Output };

        await context.Database.ExecuteSqlRawAsync(
            """
            EXEC @result = sp_getapplock
                @Resource = @resource,
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = @timeout
            """,
            [
                result,
                new SqlParameter("@resource", $"idempotency:{key}"),
                new SqlParameter("@timeout", (int)LockTimeout.TotalMilliseconds)
            ],
            cancellationToken);

        return (int)result.Value >= 0;
    }

    public Task<IdempotencyRecord?> FindAsync(string key, CancellationToken cancellationToken) =>
        context.IdempotencyRecords.FirstOrDefaultAsync(record => record.Key == key, cancellationToken);

    public void Add(IdempotencyRecord record) => context.IdempotencyRecords.Add(record);

    public void Remove(IdempotencyRecord record) => context.IdempotencyRecords.Remove(record);
}
