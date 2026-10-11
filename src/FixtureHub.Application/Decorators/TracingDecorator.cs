using System.Diagnostics;
using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Diagnostics;
using FixtureHub.Domain.Common;

namespace FixtureHub.Application.Decorators;

internal static class TracingDecorator
{
    internal sealed class CommandHandler<TCommand>(ICommandHandler<TCommand> inner) : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        public Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken) =>
            TraceAsync(typeof(TCommand).Name, ApplicationTracing.Command, () => inner.HandleAsync(command, cancellationToken));
    }

    internal sealed class CommandHandler<TCommand, TResponse>(ICommandHandler<TCommand, TResponse> inner)
        : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        public Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken) =>
            TraceAsync(typeof(TCommand).Name, ApplicationTracing.Command, () => inner.HandleAsync(command, cancellationToken));
    }

    internal sealed class QueryHandler<TQuery, TResponse>(IQueryHandler<TQuery, TResponse> inner)
        : IQueryHandler<TQuery, TResponse>
        where TQuery : IQuery<TResponse>
    {
        public Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken) =>
            TraceAsync(typeof(TQuery).Name, ApplicationTracing.Query, () => inner.HandleAsync(query, cancellationToken));
    }

    private static async Task<TResult> TraceAsync<TResult>(string name, string operation, Func<Task<TResult>> handle)
        where TResult : Result
    {
        using var activity = ApplicationTracing.Source.StartActivity(name);
        activity?.SetTag(ApplicationTracing.OperationTag, operation);

        try
        {
            var result = await handle();

            activity?.SetTag(ApplicationTracing.ResultTag, result.IsSuccess ? ApplicationTracing.Success : ApplicationTracing.Rejected);
            if (result.IsFailure)
                activity?.SetTag(ApplicationTracing.ErrorCodeTag, result.Error.Code);

            return result;
        }
        catch (Exception exception)
        {
            activity?.AddException(exception);
            activity?.SetStatus(ActivityStatusCode.Error);
            throw;
        }
    }
}
