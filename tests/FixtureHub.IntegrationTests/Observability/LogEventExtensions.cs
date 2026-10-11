using Serilog.Events;

namespace FixtureHub.IntegrationTests.Observability;

internal static class LogEventExtensions
{
    public const string RequestLoggingSource = "Serilog.AspNetCore.RequestLoggingMiddleware";

    public static object? Scalar(this LogEvent logEvent, string property) =>
        logEvent.Properties.TryGetValue(property, out var value) && value is ScalarValue scalar ? scalar.Value : null;

    public static bool IsRequestLog(this LogEvent logEvent, string path) =>
        Equals(logEvent.Scalar("SourceContext"), RequestLoggingSource) && Equals(logEvent.Scalar("RequestPath"), path);

    public static bool BelongsTo(this LogEvent logEvent, string traceId) =>
        logEvent.TraceId?.ToHexString() == traceId;
}
