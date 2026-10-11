using FixtureHub.Application.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace FixtureHub.Api.Authentication;

internal static class WriteAuthorization
{
    private static readonly AuthorizeAttribute AdminOnly = new() { Roles = Roles.Admin };

    public static TBuilder RequireAdminForWrites<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.Add(endpoint =>
        {
            if (!IsReadOnly(endpoint))
                endpoint.Metadata.Add(AdminOnly);
        });

        return builder;
    }

    private static bool IsReadOnly(EndpointBuilder endpoint) =>
        endpoint.Metadata.OfType<IHttpMethodMetadata>().LastOrDefault()?.HttpMethods is { Count: > 0 } methods
        && methods.All(method => HttpMethods.IsGet(method) || HttpMethods.IsHead(method));
}
