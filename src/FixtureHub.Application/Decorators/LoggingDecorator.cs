using System.Diagnostics;
using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Domain.Common;
using Microsoft.Extensions.Logging;

namespace FixtureHub.Application.Decorators;

internal static partial class LoggingDecorator
{
    internal sealed class CommandHandler<TCommand>(ICommandHandler<TCommand> inner, ILogger<TCommand> logger)
        : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        public Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken) =>
            RunAsync(logger, typeof(TCommand).Name, () => inner.HandleAsync(command, cancellationToken));
    }

    internal sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> inner,
        ILogger<TCommand> logger)
        : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        public Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken) =>
            RunAsync(logger, typeof(TCommand).Name, () => inner.HandleAsync(command, cancellationToken));
    }

    private static async Task<TResult> RunAsync<TResult>(ILogger logger, string command, Func<Task<TResult>> handle)
        where TResult : Result
    {
        LogStarted(logger, command);
        var startedAt = Stopwatch.GetTimestamp();

        var result = await handle();

        var elapsedMilliseconds = (long)Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;

        if (result.IsSuccess)
            LogSucceeded(logger, command, elapsedMilliseconds);
        else
            LogRejected(logger, command, result.Error.Code, result.Error.Type, elapsedMilliseconds);

        return result;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Ejecutando el comando {Command}")]
    private static partial void LogStarted(ILogger logger, string command);

    [LoggerMessage(Level = LogLevel.Information, Message = "Comando {Command} completado en {ElapsedMilliseconds} ms")]
    private static partial void LogSucceeded(ILogger logger, string command, long elapsedMilliseconds);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Comando {Command} rechazado con {ErrorCode} ({ErrorType}) en {ElapsedMilliseconds} ms")]
    private static partial void LogRejected(
        ILogger logger,
        string command,
        string errorCode,
        ErrorType errorType,
        long elapsedMilliseconds);
}
