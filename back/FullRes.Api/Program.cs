using System.Threading.RateLimiting;
using FullRes.Api.Options;
using FullRes.Api.Providers;
using FullRes.Api.Providers.SerpApi;
using FullRes.Api.Security;
using FullRes.Api.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

// dotnet run does not read .env on its own; load back/.env (walking up from the
// app directory) so FullRes__SerpApi__ApiKey reaches IConfiguration. Shell-exported
// variables always win: DotNetEnv does not clobber existing environment variables.
foreach (var dir in EnumerateUpwards(AppContext.BaseDirectory))
{
    var envPath = Path.Combine(dir, ".env");
    if (File.Exists(envPath))
    {
        DotNetEnv.Env.Load(envPath);
        break;
    }
}

var builder = WebApplication.CreateBuilder(args);

static IEnumerable<string> EnumerateUpwards(string start)
{
    var dir = new DirectoryInfo(start);
    while (dir is not null)
    {
        yield return dir.FullName;
        dir = dir.Parent;
    }
}

var fullResOptions = builder.Configuration.GetSection(FullResOptions.SectionName).Get<FullResOptions>()
    ?? new FullResOptions();
builder.Services.AddSingleton(fullResOptions);
builder.Services.AddSingleton<IOptions<FullResOptions>>(Options.Create(fullResOptions));

builder.Services.AddSingleton<UploadedImagePreparationService>();
builder.Services.AddSingleton<SearchResultProcessor>();
builder.Services.AddSingleton<ImageProbeService>();
builder.Services.AddSingleton<IReverseImageSearchProvider, SerpApiGoogleLensProvider>();

builder.Services.AddHttpClient("serpapi", (sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<FullResOptions>>().Value;
    client.Timeout = TimeSpan.FromSeconds(options.ProviderTimeoutSeconds);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("FullRes/1.0 (reverse image search)");
});

builder.Services.AddHttpClient("probe", (sp, client) =>
{
    client.Timeout = Timeout.InfiniteTimeSpan;
    client.DefaultRequestHeaders.UserAgent.ParseAdd("FullRes/1.0 (dimension check)");
    client.DefaultRequestHeaders.Accept.ParseAdd("image/*");
}).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    AllowAutoRedirect = false,
    ConnectCallback = SsrfGuard.ConnectAsync,
    ConnectTimeout = TimeSpan.FromSeconds(6),
    PooledConnectionLifetime = TimeSpan.FromMinutes(5)
});

builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problem = new ValidationProblemDetails(context.ModelState)
        {
            Type = "about:blank",
            Title = "The request is invalid.",
            Status = StatusCodes.Status400BadRequest,
            Instance = context.HttpContext.Request.Path
        };
        problem.Extensions["code"] = "invalid_request";
        return new BadRequestObjectResult(problem)
        {
            ContentTypes = { "application/problem+json" }
        };
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FullRes API",
        Version = "v1",
        Description = "Reverse image search: upload an image, get higher-resolution matches with verified dimensions."
    });
});

builder.Services.AddCors(options => options.AddPolicy("frontend", policy => policy
    .WithOrigins(fullResOptions.Cors.AllowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

var rateLimit = fullResOptions.RateLimit;
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        var problem = new ProblemDetails
        {
            Type = "about:blank",
            Title = "Too many requests.",
            Status = StatusCodes.Status429TooManyRequests,
            Detail = $"Rate limit exceeded: {rateLimit.PermitLimit} requests per {rateLimit.WindowSeconds} seconds. Try again shortly.",
            Instance = context.HttpContext.Request.Path
        };
        problem.Extensions["code"] = "rate_limited";
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
    };
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimit.PermitLimit,
                Window = TimeSpan.FromSeconds(rateLimit.WindowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.Configure<FormOptions>(options =>
    options.MultipartBodyLengthLimit = fullResOptions.MaxUploadBytes);
builder.WebHost.ConfigureKestrel(options =>
    options.Limits.MaxRequestBodySize = fullResOptions.MaxUploadBytes);

builder.Services.AddProblemDetails();

var app = builder.Build();

foreach (var issue in fullResOptions.Validate())
    app.Logger.LogWarning("Configuration: {Issue}", issue);

app.UseExceptionHandler(new ExceptionHandlerOptions
{
    ExceptionHandler = async httpContext =>
    {
        var error = httpContext.Features.Get<IExceptionHandlerFeature>()?.Error;
        var logger = httpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("FullRes.Api.Unhandled");

        if (error is not OperationCanceledException || !httpContext.RequestAborted.IsCancellationRequested)
            logger.LogError(error, "Unhandled exception while processing {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);

        if (httpContext.Response.HasStarted)
            return;

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";
        var problem = new ProblemDetails
        {
            Type = "about:blank",
            Title = "An unexpected error occurred.",
            Status = StatusCodes.Status500InternalServerError,
            Detail = "The server failed to process the request.",
            Instance = httpContext.Request.Path
        };
        problem.Extensions["code"] = "unexpected_error";
        await httpContext.Response.WriteAsJsonAsync(problem, httpContext.RequestAborted);
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("frontend");
app.UseRateLimiter();

app.MapControllers();
app.MapGet("/api/health", (IReverseImageSearchProvider provider) => Results.Ok(new
{
    status = "ok",
    provider = provider.Name,
    providerConfigured = provider.IsConfigured,
    utc = DateTime.UtcNow
}));

app.Run();

public partial class Program;
