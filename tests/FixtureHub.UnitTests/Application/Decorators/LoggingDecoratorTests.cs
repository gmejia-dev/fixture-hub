using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Decorators;
using FixtureHub.Domain.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace FixtureHub.UnitTests.Application.Decorators;

public class LoggingDecoratorTests
{
    private const string SensitiveValue = "dato-que-no-debe-aparecer";

    private readonly FakeLogger<SignIn> _logger = new();

    [Fact]
    public async Task HandleAsync_HandlerSucceeds_LogsTheStartAndTheCompletionWithTheCommandName()
    {
        var decorator = Decorate(Result.Success());

        var result = await decorator.HandleAsync(new SignIn(SensitiveValue), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, _logger.Collector.Count);
        var completed = _logger.LatestRecord;
        Assert.Equal(LogLevel.Information, completed.Level);
        Assert.Contains("SignIn", completed.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_HandlerFails_LogsAWarningWithTheErrorCode()
    {
        var error = Error.Conflict("Thing.Taken", "Ya existe.");
        var decorator = Decorate(error);

        var result = await decorator.HandleAsync(new SignIn(SensitiveValue), CancellationToken.None);

        Assert.Same(error, result.Error);
        Assert.Equal(2, _logger.Collector.Count);
        var rejected = _logger.LatestRecord;
        Assert.Equal(LogLevel.Warning, rejected.Level);
        Assert.Contains("Thing.Taken", rejected.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_AnyResult_NeverLogsTheContentOfTheCommand()
    {
        var decorator = Decorate(Error.Validation("Thing.Invalid", "Inválido."));

        await decorator.HandleAsync(new SignIn(SensitiveValue), CancellationToken.None);

        Assert.DoesNotContain(
            _logger.Collector.GetSnapshot(),
            record => record.Message.Contains(SensitiveValue, StringComparison.Ordinal));
    }

    private LoggingDecorator.CommandHandler<SignIn> Decorate(Result result) =>
        new(new StubHandler(result), _logger);

    private sealed record SignIn(string Password) : ICommand;

    private sealed class StubHandler(Result result) : ICommandHandler<SignIn>
    {
        public Task<Result> HandleAsync(SignIn command, CancellationToken cancellationToken) =>
            Task.FromResult(result);
    }
}
