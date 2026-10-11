using System.Collections.Concurrent;
using System.Diagnostics;
using OpenTelemetry;

namespace FixtureHub.IntegrationTests.Observability;

public sealed class SpanCollector : BaseProcessor<Activity>
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly ConcurrentQueue<Activity> _spans = new();

    public override void OnEnd(Activity data) => _spans.Enqueue(data);

    public IReadOnlyList<Activity> Of(string traceId) => [.. _spans.Where(span => span.TraceId.ToHexString() == traceId)];

    public async Task<Activity> WaitForAsync(string traceId, Func<Activity, bool> predicate)
    {
        var deadline = DateTime.UtcNow + Timeout;

        while (DateTime.UtcNow < deadline)
        {
            if (Of(traceId).FirstOrDefault(predicate) is { } span)
                return span;

            await Task.Delay(20);
        }

        throw new TimeoutException(
            "No llegó el span esperado. Llegaron: " + string.Join(" | ", Of(traceId).Select(span => span.DisplayName)));
    }
}
