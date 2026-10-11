using System.Security.Claims;
using FixtureHub.Api.Authentication;
using FixtureHub.Api.Endpoints.Health;
using Serilog;
using Serilog.Events;

namespace FixtureHub.Api.Observability;

internal static class RequestLogging
{
    public const string MessageTemplate = "HTTP {RequestMethod} {RequestPath} respondió {StatusCode} en {Elapsed:0.0} ms";

    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app) =>
        app.UseSerilogRequestLogging(options =>
        {
            options.Logger = app.ApplicationServices.GetRequiredService<Serilog.ILogger>();
            options.MessageTemplate = MessageTemplate;
            options.EnrichDiagnosticContext = Enrich;
            options.GetLevel = GetLevel;
        });

    private static LogEventLevel GetLevel(HttpContext context, double elapsedMilliseconds, Exception? exception)
    {
        if (exception is null && HealthEndpoints.IsHealthCheck(context.Request.Path))
            return LogEventLevel.Verbose;

        return exception is not null || context.Response.StatusCode >= StatusCodes.Status500InternalServerError
            ? LogEventLevel.Error
            : LogEventLevel.Information;
    }

    private static void Enrich(IDiagnosticContext diagnostics, HttpContext context)
    {
        if (context.User.FindFirstValue(JwtClaims.Subject) is { } userId)
            diagnostics.Set(LogProperties.UserId, userId);

        if (context.GetEndpoint() is RouteEndpoint { RoutePattern.RawText: { } route })
            diagnostics.Set(LogProperties.Route, route);
    }
}
