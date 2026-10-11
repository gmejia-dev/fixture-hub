using System.Diagnostics;

namespace FixtureHub.IntegrationTests.Observability;

public sealed record TraceParent(string TraceId, string SpanId)
{
    public const string Header = "traceparent";

    public string Value => $"00-{TraceId}-{SpanId}-01";

    public static TraceParent CreateRandom() =>
        new(ActivityTraceId.CreateRandom().ToHexString(), ActivitySpanId.CreateRandom().ToHexString());
}
