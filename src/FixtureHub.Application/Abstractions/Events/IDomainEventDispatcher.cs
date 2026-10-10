using FixtureHub.Domain.Common;

namespace FixtureHub.Application.Abstractions.Events;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);
}
