using FixtureHub.Api.Http;
using FixtureHub.Application.Abstractions.Messaging;
using FixtureHub.Application.Authentication.Login;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FixtureHub.Api.Endpoints.Auth;

internal static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder api)
    {
        api.MapPost("/auth/login", Login)
            .WithTags("Auth")
            .WithName(nameof(Login))
            .WithSummary("Inicia sesión y devuelve un token JWT para las operaciones de escritura.");

        return api;
    }

    private static async Task<Results<Ok<AuthToken>, ProblemHttpResult>> Login(
        LoginRequest request,
        IQueryHandler<LoginQuery, AuthToken> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new LoginQuery(request.Username, request.Password), cancellationToken);

        return result.ToOk();
    }
}
