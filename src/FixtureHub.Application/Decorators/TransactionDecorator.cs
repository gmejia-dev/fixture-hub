using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Abstractions.Persistence;
using FixtureHub.Domain.Common;

namespace FixtureHub.Application.Decorators;

internal static class TransactionDecorator
{
    internal sealed class CommandHandler<TCommand>(ICommandHandler<TCommand> inner, IUnitOfWork unitOfWork)
        : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        public Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken) =>
            RunAsync(unitOfWork, () => inner.HandleAsync(command, cancellationToken), Result.Failure, cancellationToken);
    }

    internal sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> inner,
        IUnitOfWork unitOfWork)
        : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        public Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken) =>
            RunAsync(
                unitOfWork,
                () => inner.HandleAsync(command, cancellationToken),
                Result.Failure<TResponse>,
                cancellationToken);
    }

    private static async Task<TResult> RunAsync<TResult>(
        IUnitOfWork unitOfWork,
        Func<Task<TResult>> handle,
        Func<Error, TResult> failure,
        CancellationToken cancellationToken)
        where TResult : Result
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);

        TResult result;
        try
        {
            result = await handle();

            if (result.IsSuccess)
            {
                var commit = await unitOfWork.CommitAsync(cancellationToken);

                if (commit.IsSuccess)
                    return result;

                result = failure(commit.Error);
            }
        }
        catch
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }

        await unitOfWork.RollbackAsync(CancellationToken.None);
        return result;
    }
}
