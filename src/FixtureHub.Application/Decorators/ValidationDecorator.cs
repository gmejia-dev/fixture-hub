using System.Text.Json;
using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Errors;
using FixtureHub.Domain.Common;
using FluentValidation;

namespace FixtureHub.Application.Decorators;

internal static class ValidationDecorator
{
    internal sealed class CommandHandler<TCommand>(
        ICommandHandler<TCommand> inner,
        IEnumerable<IValidator<TCommand>> validators)
        : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        public async Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken)
        {
            var error = await ValidateAsync(command, validators, cancellationToken);

            return error is null
                ? await inner.HandleAsync(command, cancellationToken)
                : Result.Failure(error);
        }
    }

    internal sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> inner,
        IEnumerable<IValidator<TCommand>> validators)
        : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        public async Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken)
        {
            var error = await ValidateAsync(command, validators, cancellationToken);

            return error is null
                ? await inner.HandleAsync(command, cancellationToken)
                : Result.Failure<TResponse>(error);
        }
    }

    private static async Task<Error?> ValidateAsync<TCommand>(
        TCommand command,
        IEnumerable<IValidator<TCommand>> validators,
        CancellationToken cancellationToken)
    {
        var failures = new List<FluentValidation.Results.ValidationFailure>();

        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(command, cancellationToken);
            failures.AddRange(result.Errors);
        }

        if (failures.Count == 0)
            return null;

        var errorsByField = failures
            .GroupBy(failure => ToCamelCase(failure.PropertyName))
            .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorCode).Distinct().ToArray());

        return ValidationErrors.Failed(errorsByField);
    }

    private static string ToCamelCase(string propertyPath) =>
        string.Join('.', propertyPath.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
}
