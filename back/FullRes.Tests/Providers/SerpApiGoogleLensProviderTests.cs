using System.Net;
using System.Text;
using FullRes.Api.Models;
using FullRes.Api.Options;
using FullRes.Api.Providers;
using FullRes.Api.Providers.SerpApi;
using Microsoft.Extensions.Logging.Abstractions;

namespace FullRes.Tests.Providers;

public class SerpApiGoogleLensProviderTests
{
    private const string UploadJson = """{"image_id":"abc123"}""";

    private static readonly string SearchJson = """
    {
      "search_metadata": { "status": "Success" },
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

    private static SerpApiGoogleLensProvider Create(StubHandler handler, string? apiKey = "test-key")
    {
        var options = new FullResOptions();
        options.SerpApi.ApiKey = apiKey;
        var factory = new StubHttpClientFactory(handler);
        return new SerpApiGoogleLensProvider(factory, Microsoft.Extensions.Options.Options.Create(options), NullLogger<SerpApiGoogleLensProvider>.Instance);
    }

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
                : new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Content = new StringContent("""{"error":"rate limit"}""", Encoding.UTF8, "application/json") });

        var ex = await Assert.ThrowsAsync<SearchProviderException>(
            () => Create(handler).SearchAsync(Image(), CancellationToken.None));
        Assert.Equal(ProviderErrorKind.RateLimited, ex.Kind);
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
}

internal sealed class StubHttpClientFactory : IHttpClientFactory
{
    private readonly HttpClient _client;

    public StubHttpClientFactory(HttpMessageHandler handler) => _client = new HttpClient(handler);

    public HttpClient CreateClient(string name) => _client;
}

internal sealed class StubHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

    public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        => _handler = request => Task.FromResult(handler(request));

    public StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) => _handler = handler;

    public static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => _handler(request);
}
