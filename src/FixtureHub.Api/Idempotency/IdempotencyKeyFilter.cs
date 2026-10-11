using System.Security.Claims;
using FixtureHub.Api.Authentication;
using FixtureHub.Api.Http;
using FixtureHub.Application.Abstractions.Idempotency;
using FixtureHub.Application.Errors;

namespace FixtureHub.Api.Idempotency;

internal sealed class IdempotencyKeyFilter(IdempotencyContext idempotency) : IEndpointFilter
{
    public const string HeaderName = "Idempotency-Key";
    public const string ReplayedHeaderName = "Idempotent-Replayed";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.HttpContext.Request;
        if (!HttpMethods.IsPost(request.Method))
            return await next(context);

        var values = request.Headers[HeaderName];
        if (values.Count != 1 || string.IsNullOrWhiteSpace(values[0]))
            return IdempotencyErrors.KeyRequired.ToProblem();

        var key = values[0]!;
        if (key.Length > IdempotencyRecord.KeyMaxLength)
            return IdempotencyErrors.KeyTooLong.ToProblem();

        idempotency.Key = key;
        idempotency.Scope = Guid.TryParse(context.HttpContext.User.FindFirstValue(JwtClaims.Subject), out var userId)
            ? userId.ToString()
            : null;

        var result = await next(context);

        if (idempotency.Replayed)
            context.HttpContext.Response.Headers[ReplayedHeaderName] = "true";

        return result;
    }
}
