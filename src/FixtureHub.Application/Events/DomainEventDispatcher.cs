using System.Collections.Concurrent;
using System.Reflection;
using FixtureHub.Application.Abstractions.Events;
using FixtureHub.Domain.Common;
using Microsoft.Extensions.DependencyInjection;

namespace FixtureHub.Application.Events;

internal sealed class DomainEventDispatcher(IServiceProvider services) : IDomainEventDispatcher
{
    private static readonly MethodInfo InvokeHandlersMethod =
        typeof(DomainEventDispatcher).GetMethod(nameof(InvokeHandlersAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, IDomainEvent, CancellationToken, Task>>
        Invokers = new();

    public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken) =>
        Invokers.GetOrAdd(domainEvent.GetType(), CreateInvoker)(services, domainEvent, cancellationToken);

    private static Func<IServiceProvider, IDomainEvent, CancellationToken, Task> CreateInvoker(Type eventType) =>
        InvokeHandlersMethod
            .MakeGenericMethod(eventType)
            .CreateDelegate<Func<IServiceProvider, IDomainEvent, CancellationToken, Task>>();

    private static async Task InvokeHandlersAsync<TEvent>(
        IServiceProvider services,
        IDomainEvent domainEvent,
        CancellationToken cancellationToken)
        where TEvent : IDomainEvent
    {
        foreach (var handler in services.GetServices<IDomainEventHandler<TEvent>>())
            await handler.HandleAsync((TEvent)domainEvent, cancellationToken);
    }
}
