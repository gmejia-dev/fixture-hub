using FixtureHub.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FixtureHub.Api.Http;

internal sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, error) = Classify(exception);

        if (statusCode >= StatusCodes.Status500InternalServerError)
            LogUnexpected(logger, exception, httpContext.Request.Method, httpContext.Request.Path);
        else
            LogMalformed(logger, httpContext.Request.Method, httpContext.Request.Path, exception.Message);

        var problem = new ProblemDetails { Status = statusCode, Detail = error.Message };
        foreach (var (key, value) in ProblemMapping.ExtensionsFor(error))
            problem.Extensions[key] = value;

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    private static (int StatusCode, Error Error) Classify(Exception exception) => exception switch
    {
        BadHttpRequestException badRequest => (badRequest.StatusCode, ApiErrors.MalformedRequest),
        _ => (StatusCodes.Status500InternalServerError, ApiErrors.Unexpected)
    };

    [LoggerMessage(Level = LogLevel.Error, Message = "Error inesperado en {Method} {Path}")]
    private static partial void LogUnexpected(ILogger logger, Exception exception, string method, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Solicitud mal formada en {Method} {Path}: {Reason}")]
    private static partial void LogMalformed(ILogger logger, string method, string path, string reason);
}
