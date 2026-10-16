using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FullRes.Api.Models;
using FullRes.Api.Options;
using Microsoft.Extensions.Options;

namespace FullRes.Api.Providers.SerpApi;

public sealed class SerpApiGoogleLensProvider : IReverseImageSearchProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly FullResOptions _options;
    private readonly ILogger<SerpApiGoogleLensProvider> _logger;

    public SerpApiGoogleLensProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<FullResOptions> options,
        ILogger<SerpApiGoogleLensProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public string Name => "Google Lens (SerpApi)";

    public bool IsConfigured => _options.HasProviderKey;

    public async Task<IReadOnlyList<SearchCandidate>> SearchAsync(PreparedImage image, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
            throw new SearchProviderException(
                ProviderErrorKind.NotConfigured,
                "The search provider is not configured. Set FullRes:SerpApi:ApiKey to enable reverse image search.");

        var client = _httpClientFactory.CreateClient("serpapi");
        var imageId = await UploadImageAsync(client, image, cancellationToken);
        return await SearchByImageIdAsync(client, imageId, cancellationToken);
    }

    private async Task<string> UploadImageAsync(HttpClient client, PreparedImage image, CancellationToken cancellationToken)
    {
        using var form = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(image.Content);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);
        form.Add(imageContent, "image", image.FileName);
        form.Add(new StringContent(_options.SerpApi.ApiKey!), "api_key");

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync(_options.SerpApi.UploadEndpoint, form, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new SearchProviderException(ProviderErrorKind.Timeout, "The image upload to the search provider timed out.");
        }
        catch (HttpRequestException ex)
        {
            throw new SearchProviderException(ProviderErrorKind.Unavailable, "Could not reach the search provider.", inner: ex);
        }

        using (response)
        {
            var body = await ReadBodyAsync(response, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw ClassifyHttpError(response.StatusCode, body, "image upload");

            using var doc = ParseJsonOrThrow(body, "image upload");
            if (TryReadProviderError(doc, out var providerError, out var kind))
                throw new SearchProviderException(kind, "The search provider rejected the request.", providerError);

            if (!doc.RootElement.TryGetProperty("image_id", out var idElement) || idElement.ValueKind != JsonValueKind.String)
                throw new SearchProviderException(
                    ProviderErrorKind.InvalidResponse,
                    "The search provider returned an unexpected upload response.",
                    Truncate(body));

            return idElement.GetString()!;
        }
    }

    private async Task<IReadOnlyList<SearchCandidate>> SearchByImageIdAsync(HttpClient client, string imageId, CancellationToken cancellationToken)
    {
        var url = BuildSearchUrl(imageId);

        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(url, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new SearchProviderException(ProviderErrorKind.Timeout, "The search request to the provider timed out.");
        }
        catch (HttpRequestException ex)
        {
            throw new SearchProviderException(ProviderErrorKind.Unavailable, "Could not reach the search provider.", inner: ex);
        }

        using (response)
        {
            var body = await ReadBodyAsync(response, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw ClassifyHttpError(response.StatusCode, body, "search");

            using var doc = ParseJsonOrThrow(body, "search");
            if (TryReadProviderError(doc, out var providerError, out var kind))
                throw new SearchProviderException(kind, "The search provider rejected the search request.", providerError);

            var matches = new List<SearchCandidate>();
            if (doc.RootElement.TryGetProperty("visual_matches", out var list) && list.ValueKind == JsonValueKind.Array)
            {
                var position = 0;
                foreach (var item in list.EnumerateArray())
                {
                    position++;
                    if (item.ValueKind != JsonValueKind.Object)
                        continue;

                    var imageUrl = GetString(item, "image");
                    if (string.IsNullOrWhiteSpace(imageUrl))
                        continue;

                    matches.Add(new SearchCandidate
                    {
                        ImageUrl = imageUrl,
                        SourceUrl = GetString(item, "link"),
                        Title = GetString(item, "title"),
                        ThumbnailUrl = GetString(item, "thumbnail"),
                        ReportedWidth = GetInt(item, "image_width"),
                        ReportedHeight = GetInt(item, "image_height"),
                        SimilarityScore = null,
                        ProviderPosition = position,
                        ProviderName = Name
                    });
                }
            }

            _logger.LogInformation("Google Lens returned {Count} visual matches.", matches.Count);
            return matches;
        }
    }

    private string BuildSearchUrl(string imageId)
    {
        var query = new List<string>
        {
            "engine=google_lens",
            "type=visual_matches",
            $"image_id={Uri.EscapeDataString(imageId)}",
            $"api_key={Uri.EscapeDataString(_options.SerpApi.ApiKey!)}"
        };

        if (!string.IsNullOrWhiteSpace(_options.SerpApi.Country))
            query.Add($"gl={Uri.EscapeDataString(_options.SerpApi.Country)}");
        if (!string.IsNullOrWhiteSpace(_options.SerpApi.Language))
            query.Add($"hl={Uri.EscapeDataString(_options.SerpApi.Language)}");

        return _options.SerpApi.SearchEndpoint + "?" + string.Join("&", query);
    }

    private static SearchProviderException ClassifyHttpError(HttpStatusCode statusCode, string body, string stage)
    {
        var detail = Truncate(body);
        return statusCode switch
        {
            HttpStatusCode.Unauthorized => new SearchProviderException(
                ProviderErrorKind.Unauthorized,
                "The search provider rejected the configured API key.",
                detail),
            HttpStatusCode.TooManyRequests => new SearchProviderException(
                ProviderErrorKind.RateLimited,
                "The search provider rate limit was reached. Try again later or upgrade the provider plan.",
                detail),
            _ => new SearchProviderException(
                ProviderErrorKind.Unavailable,
                $"The search provider returned HTTP {(int)statusCode} during {stage}.",
                detail)
        };
    }

    private static bool TryReadProviderError(JsonDocument doc, out string detail, out ProviderErrorKind kind)
    {
        detail = string.Empty;
        kind = ProviderErrorKind.Unavailable;

        if (!doc.RootElement.TryGetProperty("error", out var errorElement))
            return false;

        detail = errorElement.ValueKind == JsonValueKind.String ? errorElement.GetString() ?? string.Empty : errorElement.ToString();
        var lower = detail.ToLowerInvariant();

        kind = lower.Contains("api key") || lower.Contains("unauthorized") || lower.Contains("invalid key")
            ? ProviderErrorKind.Unauthorized
            : lower.Contains("limit") || lower.Contains("quota") || lower.Contains("exceeded") || lower.Contains("too many")
                ? ProviderErrorKind.RateLimited
                : lower.Contains("image") && (lower.Contains("format") || lower.Contains("size") || lower.Contains("file"))
                    ? ProviderErrorKind.RejectedImage
                    : ProviderErrorKind.Unavailable;

        return true;
    }

    private static async Task<string> ReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        var body = await reader.ReadToEndAsync(cancellationToken);
        return body;
    }

    private static JsonDocument ParseJsonOrThrow(string body, string stage)
    {
        try
        {
            return JsonDocument.Parse(body);
        }
        catch (JsonException ex)
        {
            throw new SearchProviderException(
                ProviderErrorKind.InvalidResponse,
                $"The search provider returned non-JSON data during {stage}.",
                Truncate(body),
                ex);
        }
    }

    private static string? GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int? GetInt(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var i)
            ? i
            : null;

    private static string Truncate(string value) =>
        value.Length <= 300 ? value : value[..300] + "…";
}
