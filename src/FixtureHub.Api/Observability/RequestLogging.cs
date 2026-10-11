using System.Security.Claims;
using FixtureHub.Api.Authentication;
using Serilog;

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
        });

    private static void Enrich(IDiagnosticContext diagnostics, HttpContext context)
    {
        if (context.User.FindFirstValue(JwtClaims.Subject) is { } userId)
            diagnostics.Set(LogProperties.UserId, userId);

        if (context.GetEndpoint() is RouteEndpoint { RoutePattern.RawText: { } route })
            diagnostics.Set(LogProperties.Route, route);
    }
}
