using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FixtureHub.Infrastructure.Persistence;

internal sealed class UnitOfWork(FixtureHubDbContext context) : IUnitOfWork
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
}
