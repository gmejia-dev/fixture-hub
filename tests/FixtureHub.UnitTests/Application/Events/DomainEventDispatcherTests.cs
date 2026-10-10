using FixtureHub.Application.Abstractions.Events;
using FixtureHub.Application.Events;
using FixtureHub.Domain.Common;
using FixtureHub.Domain.Matches;
using FixtureHub.Domain.Teams;
using Microsoft.Extensions.DependencyInjection;

namespace FixtureHub.UnitTests.Application.Events;

public class DomainEventDispatcherTests
{
    private readonly List<string> _calls = [];

    [Fact]
    public async Task DispatchAsync_EventWithTwoHandlers_CallsBothWithTheSameEvent()
    {
        var dispatcher = CreateDispatcher(services =>
        {
            services.AddSingleton<IDomainEventHandler<TeamCreated>>(new RecordingHandler<TeamCreated>("first", _calls));
            services.AddSingleton<IDomainEventHandler<TeamCreated>>(new RecordingHandler<TeamCreated>("second", _calls));
        });

        await dispatcher.DispatchAsync(new TeamCreated(Guid.NewGuid(), "Los Halcones"), CancellationToken.None);

        Assert.Equal(["first:TeamCreated", "second:TeamCreated"], _calls);
    }

    [Fact]
    public async Task DispatchAsync_EventTypedAsTheInterface_ReachesTheHandlerOfItsRealType()
    {
        var dispatcher = CreateDispatcher(services =>
            services.AddSingleton<IDomainEventHandler<MatchCancelled>>(new RecordingHandler<MatchCancelled>("handler", _calls)));
        IDomainEvent domainEvent = new MatchCancelled(Guid.NewGuid());

        await dispatcher.DispatchAsync(domainEvent, CancellationToken.None);

        Assert.Equal(["handler:MatchCancelled"], _calls);
    }

    [Fact]
    public async Task DispatchAsync_EventWithoutHandlers_DoesNothing()
    {
        var dispatcher = CreateDispatcher(_ => { });

        await dispatcher.DispatchAsync(new MatchCancelled(Guid.NewGuid()), CancellationToken.None);

        Assert.Empty(_calls);
    }

    private static DomainEventDispatcher CreateDispatcher(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);

        return new DomainEventDispatcher(services.BuildServiceProvider());
    }

    private sealed class RecordingHandler<TEvent>(string name, List<string> calls) : IDomainEventHandler<TEvent>
        where TEvent : IDomainEvent
    {
        public Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken)
        {
            calls.Add($"{name}:{domainEvent.GetType().Name}");
            return Task.CompletedTask;
        }
    }
}
