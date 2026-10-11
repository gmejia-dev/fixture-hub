using FixtureHub.Api.Authentication;
using FixtureHub.Api.Http;

namespace FixtureHub.Api;

internal static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddOpenApi();

        services.AddProblemDetails(options => options.CustomizeProblemDetails = ProblemDetailsEnricher.Enrich);
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);

        services.AddJwtAuthentication();

        return services;
    }
}
