using FixtureHub.Api.Endpoints;
using FixtureHub.Domain.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FixtureHub.Api.Http;

internal static class ResultExtensions
{
    public static Results<NoContent, ProblemHttpResult> ToNoContent(this Result result) =>
        result.IsSuccess ? TypedResults.NoContent() : result.Error.ToProblem();

    public static Results<Created<CreatedResponse>, ProblemHttpResult> ToCreated(
        this Result<Guid> result,
        Func<Guid, string> location) =>
        result.IsSuccess
            ? TypedResults.Created(location(result.Value), new CreatedResponse(result.Value))
            : result.Error.ToProblem();

    public static Results<Ok<TValue>, ProblemHttpResult> ToOk<TValue>(this Result<TValue> result) =>
        result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
}
