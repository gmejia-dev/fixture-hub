namespace FixtureHub.Application.Abstractions.Idempotency;

public interface IIdempotencyStore
{
    Task<bool> TryLockAsync(string key, CancellationToken cancellationToken);

    Task<IdempotencyRecord?> FindAsync(string key, CancellationToken cancellationToken);

    void Add(IdempotencyRecord record);

    void Remove(IdempotencyRecord record);
}
