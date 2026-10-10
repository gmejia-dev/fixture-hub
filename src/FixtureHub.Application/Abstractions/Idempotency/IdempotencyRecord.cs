namespace FixtureHub.Application.Abstractions.Idempotency;

public sealed class IdempotencyRecord
{
    public const int KeyMaxLength = 100;
    public const int FingerprintLength = 64;

    public IdempotencyRecord(
        string key,
        string fingerprint,
        string? response,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        Key = key;
        Fingerprint = fingerprint;
        Response = response;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public string Key { get; private init; }

    public string Fingerprint { get; private init; }

    public string? Response { get; private init; }

    public DateTimeOffset CreatedAt { get; private init; }

    public DateTimeOffset ExpiresAt { get; private init; }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;
}
