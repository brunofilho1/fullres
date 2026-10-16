using Microsoft.AspNetCore.Mvc;

namespace FullRes.Api.Errors;

public static class ApiProblem
{
    public static ObjectResult Fail(HttpContext httpContext, int statusCode, string code, string title, string? detail = null)
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
        return new ObjectResult(problem)
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" }
        };
    }
}
