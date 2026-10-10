using FixtureHub.Application.Abstractions.Idempotency;

namespace FixtureHub.UnitTests.Application.Fakes;

internal sealed class FakeIdempotencyStore : IIdempotencyStore
{
    private readonly Dictionary<string, IdempotencyRecord> _records = [];

    public bool LockAvailable { get; set; } = true;

    public List<string> Locks { get; } = [];

    public IReadOnlyCollection<IdempotencyRecord> All => _records.Values;

    public Task<bool> TryLockAsync(string key, CancellationToken cancellationToken)
    {
        Locks.Add(key);
        return Task.FromResult(LockAvailable);
    }

    public Task<IdempotencyRecord?> FindAsync(string key, CancellationToken cancellationToken) =>
        Task.FromResult(_records.GetValueOrDefault(key));

    public void Add(IdempotencyRecord record) => _records.Add(record.Key, record);

    public void Remove(IdempotencyRecord record) => _records.Remove(record.Key);
}
