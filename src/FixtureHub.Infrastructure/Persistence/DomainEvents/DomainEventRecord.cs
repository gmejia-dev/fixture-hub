using System.Text.Json;
using FixtureHub.Domain.Common;

namespace FixtureHub.Infrastructure.Persistence.DomainEvents;

internal sealed class DomainEventRecord
{
    public const int TypeMaxLength = 100;
    public const int TraceIdLength = 32;

    private DomainEventRecord(Guid id, string type, string payload, DateTimeOffset occurredAt, string? traceId)
    {
        Id = id;
        Type = type;
        Payload = payload;
        OccurredAt = occurredAt;
        TraceId = traceId;
    }

    public Guid Id { get; private init; }

    public string Type { get; private init; }

    public string Payload { get; private init; }

    public DateTimeOffset OccurredAt { get; private init; }

    public string? TraceId { get; private init; }

    public static DomainEventRecord From(IDomainEvent domainEvent, DateTimeOffset occurredAt, string? traceId) =>
        new(
            Guid.CreateVersion7(),
            domainEvent.GetType().Name,
            JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
            occurredAt,
            traceId);
}
