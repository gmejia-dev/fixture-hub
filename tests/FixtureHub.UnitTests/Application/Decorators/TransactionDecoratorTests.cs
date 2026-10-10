using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Decorators;
using FixtureHub.Domain.Common;
using FixtureHub.UnitTests.Application.Fakes;

namespace FixtureHub.UnitTests.Application.Decorators;

public class TransactionDecoratorTests
{
    private static readonly Error BusinessError = Error.Conflict("Thing.Taken", "Ya existe.");

    private readonly FakeUnitOfWork _unitOfWork = new();

    [Fact]
    public async Task HandleAsync_HandlerSucceeds_CommitsAndReturnsTheValue()
    {
        var id = Guid.NewGuid();
        var decorator = Decorate(new StubHandler(id));

        var result = await decorator.HandleAsync(new CreateThing("Copa"), CancellationToken.None);

        Assert.Equal(id, result.Value);
        Assert.Equal(["Begin", "Commit"], _unitOfWork.Calls);
    }

    [Fact]
    public async Task HandleAsync_HandlerFails_RollsBackWithoutCommitting()
    {
        var decorator = Decorate(new StubHandler(BusinessError));

        var result = await decorator.HandleAsync(new CreateThing("Copa"), CancellationToken.None);

        Assert.Same(BusinessError, result.Error);
        Assert.Equal(["Begin", "Rollback"], _unitOfWork.Calls);
    }

    [Fact]
    public async Task HandleAsync_CommitFails_RollsBackAndReturnsTheCommitError()
    {
        var commitError = Error.Conflict("Team.NameTaken", "Ya existe un equipo con ese nombre.");
        _unitOfWork.CommitResult = commitError;
        var decorator = Decorate(new StubHandler(Guid.NewGuid()));

        var result = await decorator.HandleAsync(new CreateThing("Copa"), CancellationToken.None);

        Assert.Same(commitError, result.Error);
        Assert.Equal(["Begin", "Commit", "Rollback"], _unitOfWork.Calls);
    }

    [Fact]
    public async Task HandleAsync_HandlerThrows_RollsBackAndRethrows()
    {
        var decorator = Decorate(new ThrowingHandler());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => decorator.HandleAsync(new CreateThing("Copa"), CancellationToken.None));

        Assert.Equal(["Begin", "Rollback"], _unitOfWork.Calls);
    }

    [Fact]
    public async Task HandleAsync_CommitThrows_RollsBackAndRethrowsTheSameException()
    {
        var exception = new TimeoutException("La base de datos no respondió a tiempo.");
        _unitOfWork.CommitException = exception;
        var decorator = Decorate(new StubHandler(Guid.NewGuid()));

        var thrown = await Assert.ThrowsAsync<TimeoutException>(
            () => decorator.HandleAsync(new CreateThing("Copa"), CancellationToken.None));

        Assert.Same(exception, thrown);
        Assert.Equal(["Begin", "Commit", "Rollback"], _unitOfWork.Calls);
    }

    [Fact]
    public async Task HandleAsync_CommandWithoutValue_CommitsOnSuccess()
    {
        var decorator = new TransactionDecorator.CommandHandler<DeleteThing>(new DeleteThingHandler(), _unitOfWork);

        var result = await decorator.HandleAsync(new DeleteThing(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(["Begin", "Commit"], _unitOfWork.Calls);
    }

    private TransactionDecorator.CommandHandler<CreateThing, Guid> Decorate(
        ICommandHandler<CreateThing, Guid> handler) =>
        new(handler, _unitOfWork);

    private sealed record CreateThing(string Name) : ICommand<Guid>;

    private sealed record DeleteThing(Guid Id) : ICommand;

    private sealed class StubHandler(Result<Guid> result) : ICommandHandler<CreateThing, Guid>
    {
        public Task<Result<Guid>> HandleAsync(CreateThing command, CancellationToken cancellationToken) =>
            Task.FromResult(result);
    }

    private sealed class ThrowingHandler : ICommandHandler<CreateThing, Guid>
    {
        public Task<Result<Guid>> HandleAsync(CreateThing command, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("La base de datos no responde.");
    }

    private sealed class DeleteThingHandler : ICommandHandler<DeleteThing>
    {
        public Task<Result> HandleAsync(DeleteThing command, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success());
    }
}
