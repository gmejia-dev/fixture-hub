using System.Diagnostics;
using FixtureHub.Application.Abstractions.Events;
using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Domain.Common;
using FixtureHub.Infrastructure.Persistence.DomainEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace FixtureHub.Infrastructure.Persistence;

internal sealed partial class UnitOfWork(
    FixtureHubDbContext context,
    IDomainEventDispatcher dispatcher,
    TimeProvider timeProvider,
    ILogger<UnitOfWork> logger) : IUnitOfWork
{
    private IDbContextTransaction? _transaction;

    public async Task BeginTransactionAsync(CancellationToken cancellationToken)
    {
        _transaction = await context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task<Result> CommitAsync(CancellationToken cancellationToken)
    {
        if (_transaction is null)
            throw new InvalidOperationException("No hay una transacción abierta para confirmar.");

        await DispatchDomainEventsAsync(cancellationToken);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (UniqueIndexViolation.ToError(exception) is { } error)
        {
            return error;
        }

        await _transaction.CommitAsync(cancellationToken);

        await _transaction.DisposeAsync();
        _transaction = null;

        return Result.Success();
    }

    public async Task RollbackAsync(CancellationToken cancellationToken)
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        while (TakePendingEvents() is { Count: > 0 } domainEvents)
        {
            foreach (var domainEvent in domainEvents)
            {
                var record = DomainEventRecord.From(
                    domainEvent, timeProvider.GetUtcNow(), Activity.Current?.TraceId.ToString());

                context.DomainEvents.Add(record);
                LogDomainEvent(logger, record.Type, record.Id);

                await dispatcher.DispatchAsync(domainEvent, cancellationToken);
            }
        }
    }

    private List<IDomainEvent> TakePendingEvents()
    {
        var aggregates = context.ChangeTracker.Entries<AggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        var domainEvents = aggregates.SelectMany(aggregate => aggregate.DomainEvents).ToList();
        aggregates.ForEach(aggregate => aggregate.ClearDomainEvents());

        return domainEvents;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Evento de dominio {EventType} registrado con Id {EventId}")]
    private static partial void LogDomainEvent(ILogger logger, string eventType, Guid eventId);
}
