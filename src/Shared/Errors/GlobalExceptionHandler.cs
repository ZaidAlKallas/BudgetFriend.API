using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Serilog.Context;
using System.Diagnostics;

namespace BudgetFriend.API.Shared.Errors;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService,
    IWebHostEnvironment environment)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = MapException(exception);
        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;

        using (LogContext.PushProperty("TraceId", traceId))
        using (LogContext.PushProperty("SpanId", Activity.Current?.SpanId.ToString() ?? ""))
        {
            logger.LogError(
                exception,
                "Unhandled exception while processing {Method} {Path}",
                httpContext.Request.Method,
                httpContext.Request.Path);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = "about:blank"
        };
        problemDetails.Extensions["traceId"] = traceId;

        if (environment.IsDevelopment())
        {
            problemDetails.Detail = exception.Message;
        }

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails
        });
    }

    private static (int StatusCode, string Title) MapException(Exception exception) => exception switch
    {
        BadHttpRequestException badRequest => (
            badRequest.StatusCode,
            ReasonPhrases.GetReasonPhrase(badRequest.StatusCode) ?? "Bad Request"),
        _ => (
            StatusCodes.Status500InternalServerError,
            "An error occurred while processing your request.")
    };
}
