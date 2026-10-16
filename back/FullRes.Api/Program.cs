using System.Net;
using System.Threading.RateLimiting;
using FullRes.Api.Options;
using FullRes.Api.Providers;
using FullRes.Api.Providers.SerpApi;
using FullRes.Api.Security;
using FullRes.Api.Services;
using FullRes.Api.Controllers;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
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
builder.Services.AddSingleton<ProviderCircuitBreaker>();
builder.Services.AddSingleton<SearchConcurrencyGate>();
builder.Services.AddSingleton<IReverseImageSearchProvider, SerpApiGoogleLensProvider>();

// Cap how much the provider client is willing to buffer: a runaway or malicious
// response cannot exhaust memory. PostAsync/GetAsync buffer the body, so this
// limit applies before JSON parsing.
builder.Services.AddHttpClient("serpapi", (sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<FullResOptions>>().Value;
    client.Timeout = TimeSpan.FromSeconds(options.ProviderTimeoutSeconds);
    client.MaxResponseContentBufferSize = 4 * 1024 * 1024;
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
        problem.Extensions["traceId"] = System.Diagnostics.Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
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
        problem.Extensions["traceId"] = System.Diagnostics.Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
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

// FullRes applies its own precise size check (MaxUploadBytes) and returns a friendly
// 413 file_too_large. The transport limits are set slightly higher so that check can run;
// anything above the hard ceiling is still rejected by Kestrel/multipart parsing and mapped
// to the same 413 contract by the exception handler below.
var hardUploadLimit = fullResOptions.MaxUploadBytes + (1024 * 1024);
builder.Services.Configure<FormOptions>(options =>
    options.MultipartBodyLengthLimit = hardUploadLimit);
builder.WebHost.ConfigureKestrel(options =>
    options.Limits.MaxRequestBodySize = hardUploadLimit);

builder.Services.AddProblemDetails();

var app = builder.Build();

foreach (var issue in fullResOptions.Validate())
    app.Logger.LogWarning("Configuration: {Issue}", issue);

// Trust forwarded headers only when explicitly enabled behind a reverse proxy/CDN the
// operator controls. The proxy address must be listed in FullRes:ForwardedHeaders:KnownProxies;
// otherwise only the ASP.NET Core defaults (loopback) are trusted. Enabling this without
// pinning the proxy would let clients spoof X-Forwarded-For and bypass the per-IP rate limit.
if (fullResOptions.ForwardedHeaders.Enabled)
{
    var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("FullRes.Api.Startup");
    var forwarded = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    };

    foreach (var proxy in fullResOptions.ForwardedHeaders.KnownProxies)
    {
        if (IPAddress.TryParse(proxy, out var address))
        {
            forwarded.KnownProxies.Add(address);
            startupLogger.LogInformation("Trusting forwarded headers from proxy {Proxy}.", proxy);
        }
        else
        {
            startupLogger.LogWarning("FullRes:ForwardedHeaders:KnownProxies entry '{Proxy}' is not a valid IP and was ignored.", proxy);
        }
    }

    if (forwarded.KnownProxies.Count == 0)
        startupLogger.LogWarning(
            "Forwarded headers are enabled but no FullRes:ForwardedHeaders:KnownProxies were configured; only loopback proxies are trusted.");

    app.UseForwardedHeaders(forwarded);
}

app.UseExceptionHandler(new ExceptionHandlerOptions
{
    ExceptionHandler = async httpContext =>
    {
        var error = httpContext.Features.Get<IExceptionHandlerFeature>()?.Error;
        var logger = httpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("FullRes.Api.Unhandled");

        var clientAborted = httpContext.RequestAborted.IsCancellationRequested;

        if (error is not OperationCanceledException || !clientAborted)
            logger.LogError(error, "Unhandled exception while processing {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);

        if (httpContext.Response.HasStarted || clientAborted)
            return;

        // Oversized request bodies surface as BadHttpRequestException (Kestrel) or
        // InvalidDataException (multipart form limit) before the endpoint validation runs.
        if (error is BadHttpRequestException { StatusCode: StatusCodes.Status413PayloadTooLarge }
            or InvalidDataException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            httpContext.Response.ContentType = "application/problem+json";
            await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Type = "about:blank",
                Title = "The uploaded file is too large.",
                Status = StatusCodes.Status413PayloadTooLarge,
                Detail = "The request body exceeds the configured upload limit.",
                Instance = httpContext.Request.Path,
                Extensions =
                {
                    ["code"] = "file_too_large",
                    ["traceId"] = System.Diagnostics.Activity.Current?.Id ?? httpContext.TraceIdentifier
                }
            }, httpContext.RequestAborted);
            return;
        }

        if (error is BadHttpRequestException { StatusCode: var status } && status is >= 400 and < 500)
        {
            httpContext.Response.StatusCode = status;
            httpContext.Response.ContentType = "application/problem+json";
            await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Type = "about:blank",
                Title = "The request is invalid.",
                Status = status,
                Detail = "The request could not be processed.",
                Instance = httpContext.Request.Path,
                Extensions =
                {
                    ["code"] = "invalid_request",
                    ["traceId"] = System.Diagnostics.Activity.Current?.Id ?? httpContext.TraceIdentifier
                }
            }, httpContext.RequestAborted);
            return;
        }

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";
        var internalProblem = new ProblemDetails
        {
            Type = "about:blank",
            Title = "An unexpected error occurred.",
            Status = StatusCodes.Status500InternalServerError,
            Detail = "The server failed to process the request.",
            Instance = httpContext.Request.Path
        };
        internalProblem.Extensions["code"] = "unexpected_error";
        internalProblem.Extensions["traceId"] = System.Diagnostics.Activity.Current?.Id ?? httpContext.TraceIdentifier;
        await httpContext.Response.WriteAsJsonAsync(internalProblem, httpContext.RequestAborted);
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
