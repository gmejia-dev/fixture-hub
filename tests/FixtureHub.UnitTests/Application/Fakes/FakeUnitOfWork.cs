using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Domain.Common;

namespace FixtureHub.UnitTests.Application.Fakes;

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public List<string> Calls { get; } = [];

    public Result CommitResult { get; set; } = Result.Success();

    public Exception? CommitException { get; set; }

    public Task BeginTransactionAsync(CancellationToken cancellationToken)
    {
        Calls.Add("Begin");
        return Task.CompletedTask;
    }

    public Task<Result> CommitAsync(CancellationToken cancellationToken)
    {
        Calls.Add("Commit");

        return CommitException is null
            ? Task.FromResult(CommitResult)
            : Task.FromException<Result>(CommitException);
    }

    public Task RollbackAsync(CancellationToken cancellationToken)
    {
        Calls.Add("Rollback");
        return Task.CompletedTask;
    }
}
