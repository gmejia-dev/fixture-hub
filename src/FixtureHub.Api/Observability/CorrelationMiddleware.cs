using System.Security.Claims;
using FixtureHub.Api.Authentication;

namespace FixtureHub.Api.Observability;

internal sealed class CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
{
    public const string TraceIdHeader = "X-Trace-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = RequestTrace.IdOf(context);
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[TraceIdHeader] = traceId;
            return Task.CompletedTask;
        });

        var userId = context.User.FindFirstValue(JwtClaims.Subject);
        if (userId is null)
        {
            await next(context);
            return;
        }

        using (logger.BeginScope(new Dictionary<string, object> { [LogProperties.UserId] = userId }))
            await next(context);
    }
}
