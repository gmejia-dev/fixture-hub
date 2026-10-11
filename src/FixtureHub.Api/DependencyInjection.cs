using FixtureHub.Api.Authentication;
using FixtureHub.Api.Http;
using FixtureHub.Api.Observability;

namespace FixtureHub.Api;

internal static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddStructuredLogging(configuration);
        services.AddTracing(configuration);

        services.AddOpenApi();

        services.AddProblemDetails(options => options.CustomizeProblemDetails = ProblemDetailsEnricher.Enrich);
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);

        services.AddJwtAuthentication();

        return services;
    }
}
