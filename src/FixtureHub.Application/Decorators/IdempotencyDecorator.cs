using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FixtureHub.Application.Abstractions.Idempotency;
using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Errors;
using FixtureHub.Domain.Common;

namespace FixtureHub.Application.Decorators;

internal static class IdempotencyDecorator
{
    public static readonly TimeSpan Retention = TimeSpan.FromHours(24);

    internal sealed class CommandHandler<TCommand>(
        ICommandHandler<TCommand> inner,
        IdempotencyContext context,
        IIdempotencyStore store,
        TimeProvider timeProvider)
        : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        public Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken) =>
            RunAsync(
                new Request<TCommand>(context.Key, command, store, timeProvider),
                () => inner.HandleAsync(command, cancellationToken),
                serialize: _ => null,
                replay: _ => Result.Success(),
                Result.Failure,
                cancellationToken);
    }

    internal sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> inner,
        IdempotencyContext context,
        IIdempotencyStore store,
        TimeProvider timeProvider)
        : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        public Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken) =>
            RunAsync(
                new Request<TCommand>(context.Key, command, store, timeProvider),
                () => inner.HandleAsync(command, cancellationToken),
                serialize: result => JsonSerializer.Serialize(result.Value),
                replay: response => Result.Success(JsonSerializer.Deserialize<TResponse>(response!)!),
                Result.Failure<TResponse>,
                cancellationToken);
    }

    private static async Task<TResult> RunAsync<TCommand, TResult>(
        Request<TCommand> request,
        Func<Task<TResult>> handle,
        Func<TResult, string?> serialize,
        Func<string?, TResult> replay,
        Func<Error, TResult> failure,
        CancellationToken cancellationToken)
        where TResult : Result
    {
        if (request.Key is not { } key)
            return await handle();

        if (!await request.Store.TryLockAsync(key, cancellationToken))
            return failure(IdempotencyErrors.RequestInProgress(key));

        var fingerprint = Fingerprint(request.Command);
        var now = request.TimeProvider.GetUtcNow();
        var record = await request.Store.FindAsync(key, cancellationToken);

        if (record is not null && !record.IsExpired(now))
            return record.Fingerprint == fingerprint ? replay(record.Response) : failure(IdempotencyErrors.KeyReused(key));

        if (record is not null)
            request.Store.Remove(record);

        var result = await handle();

        if (result.IsSuccess)
            request.Store.Add(new IdempotencyRecord(key, fingerprint, serialize(result), now, now + Retention));

        return result;
    }

    private static string Fingerprint<TCommand>(TCommand command)
    {
        var content = $"{typeof(TCommand).FullName}:{JsonSerializer.Serialize(command)}";

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
    }

    private sealed record Request<TCommand>(
        string? Key,
        TCommand Command,
        IIdempotencyStore Store,
        TimeProvider TimeProvider);
}
