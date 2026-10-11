namespace FixtureHub.Application.Abstractions.Idempotency;

public sealed class IdempotencyContext
{
    public string? Key { get; set; }

    public string? Scope { get; set; }

    public bool Replayed { get; set; }

    public string? StorageKey => Key is null || Scope is null ? Key : $"{Scope}:{Key}";
}
