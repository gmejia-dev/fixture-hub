using FixtureHub.Application.Abstractions.Idempotency;
using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Decorators;
using FixtureHub.Domain.Common;
using FixtureHub.UnitTests.Application.Fakes;

namespace FixtureHub.UnitTests.Application.Decorators;

public class IdempotencyDecoratorTests
{
    private const string Key = "c1a7e5e2-clave";

    private static readonly DateTimeOffset Now = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

    private readonly IdempotencyContext _context = new() { Key = Key };
    private readonly FakeIdempotencyStore _store = new();
    private readonly FakeTimeProvider _time = new(Now);

    [Fact]
    public async Task HandleAsync_WithoutKey_CallsTheHandlerWithoutLockingOrStoring()
    {
        _context.Key = null;
        var handler = new StubHandler(Guid.NewGuid());

        await Decorate(handler).HandleAsync(new CreateThing("Copa"), CancellationToken.None);

        Assert.Equal(1, handler.Calls);
        Assert.Empty(_store.Locks);
        Assert.Empty(_store.All);
    }

    [Fact]
    public async Task HandleAsync_NewKey_LocksCallsTheHandlerAndStoresTheValueFor24Hours()
    {
        var id = Guid.NewGuid();

        var result = await Decorate(new StubHandler(id)).HandleAsync(new CreateThing("Copa"), CancellationToken.None);

        Assert.Equal(id, result.Value);
        Assert.Equal([Key], _store.Locks);
        var record = Assert.Single(_store.All);
        Assert.Equal(Key, record.Key);
        Assert.Equal($"\"{id}\"", record.Response);
        Assert.Equal(Now.AddHours(24), record.ExpiresAt);
        Assert.Equal(64, record.Fingerprint.Length);
    }

    [Fact]
    public async Task HandleAsync_SameKeyAndSameCommand_ReturnsTheStoredValueWithoutCallingTheHandlerAgain()
    {
        var id = Guid.NewGuid();
        var handler = new StubHandler(id);
        await Decorate(handler).HandleAsync(new CreateThing("Copa"), CancellationToken.None);

        var repeated = await Decorate(handler).HandleAsync(new CreateThing("Copa"), CancellationToken.None);

        Assert.Equal(id, repeated.Value);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task HandleAsync_SameKeyAndDifferentCommand_ReturnsKeyReused()
    {
        var handler = new StubHandler(Guid.NewGuid());
        await Decorate(handler).HandleAsync(new CreateThing("Copa"), CancellationToken.None);

        var result = await Decorate(handler).HandleAsync(new CreateThing("Liga"), CancellationToken.None);

        Assert.Equal("Idempotency.KeyReused", result.Error.Code);
        Assert.Equal(Key, result.Error.Metadata?["key"]);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task HandleAsync_ExpiredKey_RunsTheCommandAgainAndReplacesTheRecord()
    {
        var handler = new StubHandler(Guid.NewGuid());
        await Decorate(handler).HandleAsync(new CreateThing("Copa"), CancellationToken.None);
        _time.Now = Now.AddHours(24);

        await Decorate(handler).HandleAsync(new CreateThing("Liga"), CancellationToken.None);

        Assert.Equal(2, handler.Calls);
        Assert.Equal(Now.AddHours(48), Assert.Single(_store.All).ExpiresAt);
    }

    [Fact]
    public async Task HandleAsync_HandlerFails_StoresNothing()
    {
        var error = Error.Conflict("Thing.Taken", "Ya existe.");

        var result = await Decorate(new StubHandler(error)).HandleAsync(new CreateThing("Copa"), CancellationToken.None);

        Assert.Same(error, result.Error);
        Assert.Empty(_store.All);
    }

    [Fact]
    public async Task HandleAsync_LockNotAcquired_ReturnsRequestInProgressWithoutCallingTheHandler()
    {
        _store.LockAvailable = false;
        var handler = new StubHandler(Guid.NewGuid());

        var result = await Decorate(handler).HandleAsync(new CreateThing("Copa"), CancellationToken.None);

        Assert.Equal("Idempotency.RequestInProgress", result.Error.Code);
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task HandleAsync_CommandWithoutResponseRepeated_ReturnsSuccessWithoutCallingTheHandlerAgain()
    {
        var handler = new DeleteThingHandler();
        var decorator = new IdempotencyDecorator.CommandHandler<DeleteThing>(handler, _context, _store, _time);
        await decorator.HandleAsync(new DeleteThing(Guid.Empty), CancellationToken.None);

        var repeated = await decorator.HandleAsync(new DeleteThing(Guid.Empty), CancellationToken.None);

        Assert.True(repeated.IsSuccess);
        Assert.Equal(1, handler.Calls);
        Assert.Null(Assert.Single(_store.All).Response);
    }

    private IdempotencyDecorator.CommandHandler<CreateThing, Guid> Decorate(ICommandHandler<CreateThing, Guid> handler) =>
        new(handler, _context, _store, _time);

    private sealed record CreateThing(string Name) : ICommand<Guid>;

    private sealed record DeleteThing(Guid Id) : ICommand;

    private sealed class StubHandler(Result<Guid> result) : ICommandHandler<CreateThing, Guid>
    {
        public int Calls { get; private set; }

        public Task<Result<Guid>> HandleAsync(CreateThing command, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(result);
        }
    }

    private sealed class DeleteThingHandler : ICommandHandler<DeleteThing>
    {
        public int Calls { get; private set; }

        public Task<Result> HandleAsync(DeleteThing command, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(Result.Success());
        }
    }
}
