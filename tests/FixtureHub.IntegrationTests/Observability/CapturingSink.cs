using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;

namespace FixtureHub.IntegrationTests.Observability;

public sealed class CapturingSink : ILogEventSink
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly ConcurrentQueue<LogEvent> _events = new();

    public void Emit(LogEvent logEvent) => _events.Enqueue(logEvent);

    public IReadOnlyList<LogEvent> Events => [.. _events];

    public async Task<LogEvent> WaitForAsync(Func<LogEvent, bool> predicate)
    {
        var deadline = DateTime.UtcNow + Timeout;

        while (DateTime.UtcNow < deadline)
        {
            if (Events.FirstOrDefault(predicate) is { } logEvent)
                return logEvent;

            await Task.Delay(20);
        }

        throw new TimeoutException(
            "No llegó el evento de log esperado. Llegaron: " + string.Join(" | ", Events.Select(logEvent => logEvent.MessageTemplate.Text)));
    }
}
