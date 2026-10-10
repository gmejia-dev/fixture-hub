namespace FixtureHub.Application.Abstractions.Idempotency;

public sealed class IdempotencyContext
{
    public string? Key { get; set; }
}
