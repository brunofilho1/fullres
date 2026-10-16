using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FullRes.Api.Models;
using FullRes.Api.Providers;
using FullRes.Tests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace FullRes.Tests.Integration;

internal sealed class FakeProvider : IReverseImageSearchProvider
{
    private readonly IReadOnlyList<SearchCandidate> _matches;

    public FakeProvider(IReadOnlyList<SearchCandidate>? matches = null) => _matches = matches ?? DefaultMatches();

    public string Name => "Fake Provider";
    public bool IsConfigured => true;

    public Task<IReadOnlyList<SearchCandidate>> SearchAsync(PreparedImage image, CancellationToken cancellationToken)
        => Task.FromResult(_matches);

    public static IReadOnlyList<SearchCandidate> DefaultMatches() =>
    [
        new()
        {
            ImageUrl = "https://cdn.example.com/small.jpg",
            SourceUrl = "https://www.example.com/small",
            Title = "Small version",
            ThumbnailUrl = "https://cdn.example.com/small_t.jpg",
            ReportedWidth = 800,
            ReportedHeight = 600,
            ProviderPosition = 1,
            ProviderName = "Fake Provider"
        },
        new()
        {
            ImageUrl = "https://cdn.example.com/big.jpg",
            SourceUrl = "https://example.org/big",
            Title = "Big version",
            ReportedWidth = 3840,
            ReportedHeight = 2160,
            ProviderPosition = 2,
            ProviderName = "Fake Provider"
        },
        new()
        {
            ImageUrl = "https://cdn.example.com/small.jpg",
            SourceUrl = "https://example.net/duplicate",
            Title = "Duplicate",
            ReportedWidth = 800,
            ReportedHeight = 600,
            ProviderPosition = 3,
            ProviderName = "Fake Provider"
        }
    ];
}

internal sealed class ThrowingProvider : IReverseImageSearchProvider
{
    public const string SecretPayload = "secret-provider-payload";

    private readonly SearchProviderException _exception;

    public ThrowingProvider(ProviderErrorKind kind, int? retryAfterSeconds = null)
        => _exception = new SearchProviderException(
            kind,
            "The search provider failed.",
            providerDetail: SecretPayload,
            retryAfterSeconds: retryAfterSeconds);

    public string Name => "Throwing Provider";
    public bool IsConfigured => true;

    public Task<IReadOnlyList<SearchCandidate>> SearchAsync(PreparedImage image, CancellationToken cancellationToken)
        => Task.FromException<IReadOnlyList<SearchCandidate>>(_exception);
}

internal sealed class EmptyProvider : IReverseImageSearchProvider
{
    public string Name => "Empty Provider";
    public bool IsConfigured => true;

    public Task<IReadOnlyList<SearchCandidate>> SearchAsync(PreparedImage image, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<SearchCandidate>>(Array.Empty<SearchCandidate>());
}

internal sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly IReverseImageSearchProvider? _provider;
    private readonly string _apiKey;
    private readonly Dictionary<string, string?> _settings;

    public ApiFactory(
        IReverseImageSearchProvider? provider = null,
        string? apiKey = "test-key",
        Dictionary<string, string?>? settings = null)
    {
        _provider = provider;
        _apiKey = apiKey ?? string.Empty;
        _settings = settings ?? new Dictionary<string, string?>();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.UseSetting("FullRes:Probe:Enabled", "false");
        builder.UseSetting("FullRes:RateLimit:PermitLimit", "1000");
        builder.UseSetting("FullRes:SerpApi:ApiKey", _apiKey);

        foreach (var (key, value) in _settings)
            builder.UseSetting(key, value);

        if (_provider is not null)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IReverseImageSearchProvider>();
                services.AddSingleton(_provider);
            });
        }
    }
}

public class ImagesSearchEndpointTests
{
    private static MultipartFormDataContent Form(byte[]? bytes, Dictionary<string, string>? fields = null)
    {
        var form = new MultipartFormDataContent();
        if (bytes is not null)
        {
            var content = new ByteArrayContent(bytes);
            content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            form.Add(content, "image", "upload.jpg");
        }

        if (fields is not null)
        {
            foreach (var (key, value) in fields)
                form.Add(new StringContent(value), key);
        }

        return form;
    }

    [Fact]
    public async Task Search_ReturnsSortedDeduplicatedResults()
    {
        using var factory = new ApiFactory(new FakeProvider());
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/images/search", Form(ImageFixtures.RealJpeg(64, 64)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;

        Assert.Equal(64, root.GetProperty("query").GetProperty("width").GetInt32());
        Assert.Equal("JPG", root.GetProperty("query").GetProperty("format").GetString());
        Assert.Equal("Fake Provider", root.GetProperty("provider").GetProperty("name").GetString());

        var results = root.GetProperty("results").EnumerateArray().ToList();
        Assert.Equal(2, results.Count);
        Assert.Equal("https://cdn.example.com/big.jpg", results[0].GetProperty("imageUrl").GetString());
        Assert.Equal(3_840L * 2160, results[0].GetProperty("totalPixels").GetInt64());
        Assert.Equal("example.com", results[1].GetProperty("domain").GetString());
        Assert.True(results[1].GetProperty("dimensionsVerified").GetBoolean() == false);

        var warnings = root.GetProperty("provider").GetProperty("warnings").EnumerateArray().Select(w => w.GetString()).ToList();
        Assert.Contains(warnings, w => w!.Contains("duplicate"));
    }

    [Fact]
    public async Task Search_NoMatches_Returns200WithEmptyList()
    {
        using var factory = new ApiFactory(new EmptyProvider());
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/images/search", Form(ImageFixtures.RealJpeg(64, 64)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Empty(json.RootElement.GetProperty("results").EnumerateArray());
    }

    [Fact]
    public async Task Search_RespectsResolutionFilter()
    {
        using var factory = new ApiFactory(new FakeProvider());
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/images/search", Form(ImageFixtures.RealJpeg(64, 64), new Dictionary<string, string>
        {
            ["minWidth"] = "1920",
            ["minHeight"] = "1080"
        }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var results = json.RootElement.GetProperty("results").EnumerateArray().ToList();
        Assert.Single(results);
        Assert.Equal("https://cdn.example.com/big.jpg", results[0].GetProperty("imageUrl").GetString());
    }

    [Fact]
    public async Task Search_MissingFile_Returns400FileMissing()
    {
        using var factory = new ApiFactory(new FakeProvider());
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/images/search", Form(bytes: null, new Dictionary<string, string>
        {
            ["sortBy"] = "resolution"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("file_missing", json.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrEmpty(json.RootElement.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task Search_UnsupportedFormat_Returns415()
    {
        using var factory = new ApiFactory(new FakeProvider());
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/images/search", Form(ImageFixtures.GifHeader(10, 10)));

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("format_unsupported", json.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Search_InvalidSortBy_Returns400()
    {
        using var factory = new ApiFactory(new FakeProvider());
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/images/search", Form(
            ImageFixtures.RealJpeg(64, 64),
            new Dictionary<string, string> { ["sortBy"] = "random" }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("invalid_request", json.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Search_UploadExceedsLimit_Returns413FileTooLarge()
    {
        using var factory = new ApiFactory(new FakeProvider(), settings: new Dictionary<string, string?>
        {
            ["FullRes:MaxUploadBytes"] = "4096"
        });
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/images/search", Form(ImageFixtures.RealJpeg(256, 256, noise: 100)));

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("file_too_large", json.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Search_WithoutApiKey_Returns503ProviderNotConfigured()
    {
        using var factory = new ApiFactory(provider: null, apiKey: "");
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/images/search", Form(ImageFixtures.RealJpeg(64, 64)));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("provider_not_configured", json.RootElement.GetProperty("code").GetString());
    }

    [Theory]
    [InlineData(ProviderErrorKind.QuotaExceeded, HttpStatusCode.ServiceUnavailable, "provider_quota_exceeded")]
    [InlineData(ProviderErrorKind.Unavailable, HttpStatusCode.ServiceUnavailable, "provider_unavailable")]
    [InlineData(ProviderErrorKind.RateLimited, HttpStatusCode.ServiceUnavailable, "provider_rate_limited")]
    [InlineData(ProviderErrorKind.Unauthorized, HttpStatusCode.ServiceUnavailable, "provider_credentials_invalid")]
    [InlineData(ProviderErrorKind.NotConfigured, HttpStatusCode.ServiceUnavailable, "provider_not_configured")]
    [InlineData(ProviderErrorKind.Timeout, HttpStatusCode.GatewayTimeout, "provider_timeout")]
    [InlineData(ProviderErrorKind.InvalidResponse, HttpStatusCode.BadGateway, "provider_invalid_response")]
    [InlineData(ProviderErrorKind.BadRequest, HttpStatusCode.BadGateway, "provider_error")]
    [InlineData(ProviderErrorKind.RejectedImage, HttpStatusCode.UnprocessableEntity, "provider_rejected_image")]
    public async Task Search_ProviderFailure_MapsToStableContract(
        ProviderErrorKind kind, HttpStatusCode expectedStatus, string expectedCode)
    {
        using var factory = new ApiFactory(new ThrowingProvider(kind));
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/images/search", Form(ImageFixtures.RealJpeg(64, 64)));

        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(expectedCode, json.RootElement.GetProperty("code").GetString());
        Assert.Equal((int)expectedStatus, json.RootElement.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrEmpty(json.RootElement.GetProperty("traceId").GetString()));
        // Raw provider payloads must never reach the client.
        var serialized = json.RootElement.GetRawText();
        Assert.DoesNotContain(ThrowingProvider.SecretPayload, serialized);
    }

    [Fact]
    public async Task Search_ProviderRateLimit_ForwardsRetryAfter()
    {
        using var factory = new ApiFactory(new ThrowingProvider(ProviderErrorKind.RateLimited, retryAfterSeconds: 30));
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/images/search", Form(ImageFixtures.RealJpeg(64, 64)));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(30, response.Headers.RetryAfter!.Delta!.Value.TotalSeconds);
    }

    [Fact]
    public async Task Search_ExceedsRateLimit_Returns429RateLimited()
    {
        using var factory = new ApiFactory(new FakeProvider(), settings: new Dictionary<string, string?>
        {
            ["FullRes:RateLimit:PermitLimit"] = "1"
        });
        using var client = factory.CreateClient();

        var first = await client.PostAsync("/api/images/search", Form(ImageFixtures.RealJpeg(64, 64)));
        var second = await client.PostAsync("/api/images/search", Form(ImageFixtures.RealJpeg(64, 64)));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
        using var json = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        Assert.Equal("rate_limited", json.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrEmpty(json.RootElement.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task Health_ReturnsOkWithProviderState()
    {
        using var factory = new ApiFactory(new FakeProvider());
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("ok", json.RootElement.GetProperty("status").GetString());
        Assert.True(json.RootElement.GetProperty("providerConfigured").GetBoolean());
    }
}
