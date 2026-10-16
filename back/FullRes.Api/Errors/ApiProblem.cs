using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FullRes.Api.Errors;

public static class ApiProblem
{
    public static ObjectResult Fail(
        HttpContext httpContext, int statusCode, string code, string title, string? detail = null)
    {
        var problem = new ProblemDetails
        {
            Type = "about:blank",
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = httpContext.Request.Path
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        return new ObjectResult(problem)
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" }
        };
    }

    public static ObjectResult FailWithRetryAfter(
        HttpContext httpContext, int statusCode, string code, string title, string? detail, int? retryAfterSeconds)
    {
        var result = Fail(httpContext, statusCode, code, title, detail);
        if (retryAfterSeconds is > 0)
            httpContext.Response.Headers.RetryAfter = retryAfterSeconds.Value.ToString();
        return result;
    }
}
