using System.Net;
using System.Text;
using FullRes.Api.Models;
using FullRes.Api.Options;
using FullRes.Api.Providers;
using FullRes.Api.Providers.SerpApi;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FullRes.Tests.Providers;

public class SerpApiGoogleLensProviderTests
{
    private const string UploadJson = """{"image_id":"abc123"}""";

    private static readonly string SearchJson = """
    {
      "search_metadata": { "status": "Success", "id": "test-search-id" },
      "visual_matches": [
        {
          "position": 1,
          "title": "Big version",
          "link": "https://www.example.com/page",
          "source": "Example",
          "image": "https://cdn.example.com/big.jpg",
          "image_width": 3840,
          "image_height": 2160,
          "thumbnail": "https://cdn.example.com/big_thumb.jpg"
        },
        {
          "position": 2,
          "title": "No image url",
          "link": "https://example.org/page"
        }
      ]
    }
    """;

    private static PreparedImage Image() => new()
    {
        Content = [ 0xFF, 0xD8, 0xFF, 0xDB, 0x00 ],
        ContentType = "image/jpeg",
        FileName = "upload.jpg",
        Width = 100,
        Height = 100,
        OriginalSizeBytes = 4,
        OriginalFormat = "JPG"
    };

    private static SerpApiGoogleLensProvider Create(
        StubHandler handler,
        string? apiKey = "test-key",
        Action<FullResOptions>? configure = null,
        ProviderCircuitBreaker? breaker = null)
    {
        var options = new FullResOptions();
        options.SerpApi.ApiKey = apiKey;
        configure?.Invoke(options);
        var factory = new StubHttpClientFactory(handler, TimeSpan.FromSeconds(options.ProviderTimeoutSeconds));
        return new SerpApiGoogleLensProvider(
            factory, Options.Create(options), breaker ?? new ProviderCircuitBreaker(options), NullLogger<SerpApiGoogleLensProvider>.Instance);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string json) => new(status)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    [Fact]
    public async Task MapsVisualMatches()
    {
        var handler = new StubHandler(request =>
            request.Method == HttpMethod.Post
                ? StubHandler.Json(UploadJson)
                : StubHandler.Json(SearchJson));

        var matches = await Create(handler).SearchAsync(Image(), CancellationToken.None);

        Assert.Single(matches);
        var match = matches[0];
        Assert.Equal("https://cdn.example.com/big.jpg", match.ImageUrl);
        Assert.Equal("https://www.example.com/page", match.SourceUrl);
        Assert.Equal("Big version", match.Title);
        Assert.Equal("https://cdn.example.com/big_thumb.jpg", match.ThumbnailUrl);
        Assert.Equal(3840, match.ReportedWidth);
        Assert.Equal(2160, match.ReportedHeight);
        Assert.Null(match.SimilarityScore);
        Assert.Equal("Google Lens (SerpApi)", match.ProviderName);
    }

    [Fact]
    public async Task SendsCorrectSearchParameters()
    {
        Uri? searchUri = null;
        var handler = new StubHandler(request =>
        {
            if (request.Method == HttpMethod.Post)
                return StubHandler.Json(UploadJson);
            searchUri = request.RequestUri;
            return StubHandler.Json(SearchJson);
        });

        await Create(handler).SearchAsync(Image(), CancellationToken.None);

        Assert.NotNull(searchUri);
        var query = searchUri!.Query;
        Assert.Contains("engine=google_lens", query);
        Assert.Contains("type=visual_matches", query);
        Assert.Contains("image_id=abc123", query);
        Assert.Contains("api_key=test-key", query);
    }

    [Fact]
    public async Task UploadsImageAsMultipart()
    {
        string? body = null;
        var handler = new StubHandler(async request =>
        {
            if (request.Method == HttpMethod.Post && request.Content is not null)
                body = await request.Content.ReadAsStringAsync();
            return request.Method == HttpMethod.Post ? StubHandler.Json(UploadJson) : StubHandler.Json(SearchJson);
        });

        await Create(handler).SearchAsync(Image(), CancellationToken.None);

        Assert.NotNull(body);
        Assert.Contains("name=image", body);
        Assert.Contains("api_key", body);
    }

    [Fact]
    public async Task ThrowsNotConfiguredWhenKeyMissing()
    {
        var handler = new StubHandler(_ => StubHandler.Json(UploadJson));
        var ex = await Assert.ThrowsAsync<SearchProviderException>(
            () => Create(handler, apiKey: null).SearchAsync(Image(), CancellationToken.None));
        Assert.Equal(ProviderErrorKind.NotConfigured, ex.Kind);
    }

    [Fact]
    public async Task MapsProviderErrorFieldOnUpload()
    {
        var handler = new StubHandler(_ => StubHandler.Json("""{"error":"Invalid API key. The API key is invalid."}"""));
        var ex = await Assert.ThrowsAsync<SearchProviderException>(
            () => Create(handler).SearchAsync(Image(), CancellationToken.None));
        Assert.Equal(ProviderErrorKind.Unauthorized, ex.Kind);
    }

    [Fact]
    public async Task MapsRateLimitStatus()
    {
        var handler = new StubHandler(request =>
            request.Method == HttpMethod.Post
                ? StubHandler.Json(UploadJson)
                : Json(HttpStatusCode.TooManyRequests, """{"error":"Hourly throughput limit exceeded."}"""));

        var ex = await Assert.ThrowsAsync<SearchProviderException>(
            () => Create(handler).SearchAsync(Image(), CancellationToken.None));
        Assert.Equal(ProviderErrorKind.RateLimited, ex.Kind);
    }

    [Fact]
    public async Task MapsQuotaExhaustedOn429()
    {
        var handler = new StubHandler(request =>
            request.Method == HttpMethod.Post
                ? StubHandler.Json(UploadJson)
                : Json(HttpStatusCode.TooManyRequests, """{"error":"Your account has run out of searches."}"""));

        var ex = await Assert.ThrowsAsync<SearchProviderException>(
            () => Create(handler).SearchAsync(Image(), CancellationToken.None));
        Assert.Equal(ProviderErrorKind.QuotaExceeded, ex.Kind);
    }

    [Fact]
    public async Task ReadsRetryAfterHeaderOn429()
    {
        var handler = new StubHandler(request =>
        {
            if (request.Method == HttpMethod.Post)
                return StubHandler.Json(UploadJson);
            var response = Json(HttpStatusCode.TooManyRequests, """{"error":"Our servers are overloaded."}""");
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(42));
            return response;
        });

        var ex = await Assert.ThrowsAsync<SearchProviderException>(
            () => Create(handler).SearchAsync(Image(), CancellationToken.None));
        Assert.Equal(ProviderErrorKind.RateLimited, ex.Kind);
        Assert.Equal(42, ex.RetryAfterSeconds);
    }

    [Fact]
    public async Task MapsForbiddenToUnauthorized()
    {
        var handler = new StubHandler(request =>
            request.Method == HttpMethod.Post
                ? StubHandler.Json(UploadJson)
                : Json(HttpStatusCode.Forbidden, """{"error":"Account disabled."}"""));

        var ex = await Assert.ThrowsAsync<SearchProviderException>(
            () => Create(handler).SearchAsync(Image(), CancellationToken.None));
        Assert.Equal(ProviderErrorKind.Unauthorized, ex.Kind);
    }

    [Fact]
    public async Task MapsBadRequest()
    {
        var handler = new StubHandler(request =>
            request.Method == HttpMethod.Post
                ? StubHandler.Json(UploadJson)
                : Json(HttpStatusCode.BadRequest, """{"error":"Missing image_id parameter."}"""));

        var ex = await Assert.ThrowsAsync<SearchProviderException>(
            () => Create(handler).SearchAsync(Image(), CancellationToken.None));
        Assert.Equal(ProviderErrorKind.BadRequest, ex.Kind);
    }

    [Fact]
    public async Task MapsMetadataErrorStatusOn200()
    {
        var handler = new StubHandler(request =>
            request.Method == HttpMethod.Post
                ? StubHandler.Json(UploadJson)
                : StubHandler.Json("""{"search_metadata":{"status":"Error","id":"abc"},"error":"We couldn't get valid results for this search."}"""));

        var ex = await Assert.ThrowsAsync<SearchProviderException>(
            () => Create(handler).SearchAsync(Image(), CancellationToken.None));
        Assert.Equal(ProviderErrorKind.Unavailable, ex.Kind);
    }

    [Fact]
    public async Task MapsServerErrorsToUnavailable()
    {
        foreach (var status in new[] { HttpStatusCode.InternalServerError, HttpStatusCode.ServiceUnavailable })
        {
            var handler = new StubHandler(request =>
                request.Method == HttpMethod.Post
                    ? StubHandler.Json(UploadJson)
                    : Json(status, """{"error":"Something went wrong on SerpApi's end."}"""));

            var ex = await Assert.ThrowsAsync<SearchProviderException>(
                () => Create(handler, configure: o => o.Resilience.MaxRetries = 0).SearchAsync(Image(), CancellationToken.None));
            Assert.Equal(ProviderErrorKind.Unavailable, ex.Kind);
        }
    }

    [Fact]
    public async Task RetriesServerErrorsUpToConfiguredLimit()
    {
        var searchAttempts = 0;
        var handler = new StubHandler(request =>
        {
            if (request.Method == HttpMethod.Post)
                return StubHandler.Json(UploadJson);
            searchAttempts++;
            return searchAttempts < 3
                ? Json(HttpStatusCode.ServiceUnavailable, """{"error":"temporary"}""")
                : StubHandler.Json(SearchJson);
        });

        var matches = await Create(handler, configure: o =>
        {
            o.Resilience.MaxRetries = 2;
            o.Resilience.RetryBaseDelayMs = 10;
        }).SearchAsync(Image(), CancellationToken.None);

        Assert.Equal(3, searchAttempts);
        Assert.Single(matches);
    }

    [Fact]
    public async Task DoesNotRetryRateLimit()
    {
        var searchAttempts = 0;
        var handler = new StubHandler(request =>
        {
            if (request.Method == HttpMethod.Post)
                return StubHandler.Json(UploadJson);
            searchAttempts++;
            return Json(HttpStatusCode.TooManyRequests, """{"error":"rate limit"}""");
        });

        await Assert.ThrowsAsync<SearchProviderException>(
            () => Create(handler, configure: o => o.Resilience.RetryBaseDelayMs = 10).SearchAsync(Image(), CancellationToken.None));
        Assert.Equal(1, searchAttempts);
    }

    [Fact]
    public async Task ThrowsInvalidResponseWhenImageIdMissing()
    {
        var handler = new StubHandler(_ => StubHandler.Json("""{"upload_status":"ok"}"""));
        var ex = await Assert.ThrowsAsync<SearchProviderException>(
            () => Create(handler).SearchAsync(Image(), CancellationToken.None));
        Assert.Equal(ProviderErrorKind.InvalidResponse, ex.Kind);
    }

    [Fact]
    public async Task ThrowsInvalidResponseOnNonJsonBody()
    {
        var handler = new StubHandler(_ => StubHandler.Json("<html>error</html>"));
        var ex = await Assert.ThrowsAsync<SearchProviderException>(
            () => Create(handler).SearchAsync(Image(), CancellationToken.None));
        Assert.Equal(ProviderErrorKind.InvalidResponse, ex.Kind);
    }

    [Fact]
    public async Task ReturnsEmptyWhenNoVisualMatches()
    {
        var handler = new StubHandler(request =>
            request.Method == HttpMethod.Post
                ? StubHandler.Json(UploadJson)
                : StubHandler.Json("""{"search_metadata":{"status":"Success"}}"""));

        var matches = await Create(handler).SearchAsync(Image(), CancellationToken.None);
        Assert.Empty(matches);
    }

    [Fact]
    public async Task SuccessWithErrorMessageIsTreatedAsEmptyResults()
    {
        // SerpApi may include an informational "error" string on a successful search that
        // found nothing. That must not be reported as a failure.
        var handler = new StubHandler(request =>
            request.Method == HttpMethod.Post
                ? StubHandler.Json(UploadJson)
                : StubHandler.Json("""{"search_metadata":{"status":"Success"},"error":"No results found for this image."}"""));

        var matches = await Create(handler).SearchAsync(Image(), CancellationToken.None);
        Assert.Empty(matches);
    }

    [Fact]
    public async Task ThrowsTimeoutWhenProviderDoesNotRespond()
    {
        var handler = new StubHandler(async (request, cancellationToken) =>
        {
            if (request.Method == HttpMethod.Post)
                return StubHandler.Json(UploadJson);
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return StubHandler.Json(SearchJson);
        });

        var ex = await Assert.ThrowsAsync<SearchProviderException>(() =>
            Create(handler, configure: o =>
            {
                o.ProviderTimeoutSeconds = 1;
                o.Resilience.MaxRetries = 0;
            }).SearchAsync(Image(), CancellationToken.None));
        Assert.Equal(ProviderErrorKind.Timeout, ex.Kind);
    }

    [Fact]
    public async Task PropagatesClientCancellation()
    {
        using var cts = new CancellationTokenSource();
        var handler = new StubHandler(async (request, cancellationToken) =>
        {
            if (request.Method == HttpMethod.Post)
                return StubHandler.Json(UploadJson);
            cts.Cancel();
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return StubHandler.Json(SearchJson);
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Create(handler).SearchAsync(Image(), cts.Token));
    }

    [Fact]
    public async Task OpensCircuitAfterRepeatedTransientFailures()
    {
        var attempts = 0;
        var handler = new StubHandler(request =>
        {
            if (request.Method == HttpMethod.Post)
                return StubHandler.Json(UploadJson);
            attempts++;
            return Json(HttpStatusCode.ServiceUnavailable, """{"error":"down"}""");
        });

        var options = new FullResOptions();
        options.SerpApi.ApiKey = "test-key";
        options.Resilience.MaxRetries = 0;
        options.Resilience.RetryBaseDelayMs = 1;
        options.Resilience.CircuitBreakerFailureThreshold = 2;
        options.Resilience.CircuitBreakerOpenSeconds = 60;
        var breaker = new ProviderCircuitBreaker(options);
        var provider = new SerpApiGoogleLensProvider(
            new StubHttpClientFactory(handler, TimeSpan.FromSeconds(options.ProviderTimeoutSeconds)),
            Options.Create(options),
            breaker,
            NullLogger<SerpApiGoogleLensProvider>.Instance);

        await Assert.ThrowsAsync<SearchProviderException>(() => provider.SearchAsync(Image(), CancellationToken.None));
        await Assert.ThrowsAsync<SearchProviderException>(() => provider.SearchAsync(Image(), CancellationToken.None));

        Assert.Equal(ProviderCircuitState.Open, breaker.State);

        var ex = await Assert.ThrowsAsync<SearchProviderException>(() => provider.SearchAsync(Image(), CancellationToken.None));
        Assert.Equal(ProviderErrorKind.Unavailable, ex.Kind);
        Assert.Equal(2, attempts);
    }
}

internal sealed class StubHttpClientFactory : IHttpClientFactory
{
    private readonly HttpClient _client;

    public StubHttpClientFactory(HttpMessageHandler handler, TimeSpan? timeout = null)
        => _client = new HttpClient(handler) { Timeout = timeout ?? TimeSpan.FromSeconds(100) };

    public HttpClient CreateClient(string name) => _client;
}

internal sealed class StubHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

    public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        => _handler = (request, _) => Task.FromResult(handler(request));

    public StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        => _handler = (request, _) => handler(request);

    public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        => _handler = handler;

    public static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => _handler(request, cancellationToken);
}
