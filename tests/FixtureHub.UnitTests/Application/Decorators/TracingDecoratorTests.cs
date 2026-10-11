using System.Collections.Concurrent;
using System.Diagnostics;
using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Decorators;
using FixtureHub.Application.Diagnostics;
using FixtureHub.Domain.Common;

namespace FixtureHub.UnitTests.Application.Decorators;

public sealed class TracingDecoratorTests : IDisposable
{
    private readonly ConcurrentQueue<Activity> _stopped = new();
    private readonly ActivityListener _listener;
    private readonly Activity _test = new Activity(nameof(TracingDecoratorTests)).Start();

    public TracingDecoratorTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ApplicationTracing.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = _stopped.Enqueue
        };
        ActivitySource.AddActivityListener(_listener);
    }

    [Fact]
    public async Task HandleAsync_CommandSucceeds_RecordsASpanNamedAfterTheCommandMarkedAsSuccess()
    {
        var decorator = new TracingDecorator.CommandHandler<RenameThing>(new StubCommandHandler(Result.Success()));

        var result = await decorator.HandleAsync(new RenameThing("Nuevo nombre"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var span = SingleSpan();
        Assert.Equal(nameof(RenameThing), span.DisplayName);
        Assert.Equal(_test.SpanId, span.ParentSpanId);
        Assert.Equal(ApplicationTracing.Command, span.GetTagItem(ApplicationTracing.OperationTag));
        Assert.Equal(ApplicationTracing.Success, span.GetTagItem(ApplicationTracing.ResultTag));
        Assert.Null(span.GetTagItem(ApplicationTracing.ErrorCodeTag));
        Assert.Equal(ActivityStatusCode.Unset, span.Status);
    }

    [Fact]
    public async Task HandleAsync_CommandRejected_RecordsTheErrorCodeWithoutMarkingTheSpanAsFailed()
    {
        var error = Error.Conflict("Thing.Taken", "Ya existe.");
        var decorator = new TracingDecorator.CommandHandler<RenameThing>(new StubCommandHandler(error));

        var result = await decorator.HandleAsync(new RenameThing("Nuevo nombre"), CancellationToken.None);

        Assert.Same(error, result.Error);
        var span = SingleSpan();
        Assert.Equal(ApplicationTracing.Rejected, span.GetTagItem(ApplicationTracing.ResultTag));
        Assert.Equal("Thing.Taken", span.GetTagItem(ApplicationTracing.ErrorCodeTag));
        Assert.Equal(ActivityStatusCode.Unset, span.Status);
    }

    [Fact]
    public async Task HandleAsync_HandlerThrows_MarksTheSpanAsFailedWithTheExceptionAndRethrowsIt()
    {
        var decorator = new TracingDecorator.CommandHandler<RenameThing>(new ThrowingCommandHandler());

        await Assert.ThrowsAsync<TimeoutException>(
            () => decorator.HandleAsync(new RenameThing("Nuevo nombre"), CancellationToken.None));

        var span = SingleSpan();
        Assert.Equal(ActivityStatusCode.Error, span.Status);
        var exception = Assert.Single(span.Events, activityEvent => activityEvent.Name == "exception");
        Assert.Contains(exception.Tags, tag => tag.Key == "exception.type" && Equals(tag.Value, typeof(TimeoutException).FullName));
    }

    [Fact]
    public async Task HandleAsync_Query_RecordsASpanNamedAfterTheQueryWithTheQueryOperation()
    {
        var decorator = new TracingDecorator.QueryHandler<FindThing, string>(new StubQueryHandler());

        var result = await decorator.HandleAsync(new FindThing(7), CancellationToken.None);

        Assert.Equal("cosa 7", result.Value);
        var span = SingleSpan();
        Assert.Equal(nameof(FindThing), span.DisplayName);
        Assert.Equal(ApplicationTracing.Query, span.GetTagItem(ApplicationTracing.OperationTag));
        Assert.Equal(ApplicationTracing.Success, span.GetTagItem(ApplicationTracing.ResultTag));
    }

    public void Dispose()
    {
        _listener.Dispose();
        _test.Dispose();
    }

    private Activity SingleSpan() => Assert.Single(_stopped, activity => activity.TraceId == _test.TraceId);

    private sealed record RenameThing(string Name) : ICommand;

    private sealed record FindThing(int Id) : IQuery<string>;

    private sealed class StubCommandHandler(Result result) : ICommandHandler<RenameThing>
    {
        public Task<Result> HandleAsync(RenameThing command, CancellationToken cancellationToken) => Task.FromResult(result);
    }

    private sealed class ThrowingCommandHandler : ICommandHandler<RenameThing>
    {
        public Task<Result> HandleAsync(RenameThing command, CancellationToken cancellationToken) =>
            throw new TimeoutException("La base de datos no respondió.");
    }

    private sealed class StubQueryHandler : IQueryHandler<FindThing, string>
    {
        public Task<Result<string>> HandleAsync(FindThing query, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success($"cosa {query.Id}"));
    }
}
