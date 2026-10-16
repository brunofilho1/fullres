using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FullRes.Api.Models;
using FullRes.Api.Options;
using Microsoft.Extensions.Options;

namespace FullRes.Api.Providers.SerpApi;

public sealed class SerpApiGoogleLensProvider : IReverseImageSearchProvider
{
    private const int MaxRetryDelayMs = 5_000;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly FullResOptions _options;
    private readonly ProviderCircuitBreaker _circuitBreaker;
    private readonly ILogger<SerpApiGoogleLensProvider> _logger;
    private readonly Random _jitter = new();

    public SerpApiGoogleLensProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<FullResOptions> options,
        ProviderCircuitBreaker circuitBreaker,
        ILogger<SerpApiGoogleLensProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _circuitBreaker = circuitBreaker;
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

        _circuitBreaker.EnsureAllowed();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var client = _httpClientFactory.CreateClient("serpapi");
            var imageId = await UploadImageAsync(client, image, cancellationToken);
            var (matches, searchId) = await SearchByImageIdAsync(client, imageId, cancellationToken);

            _circuitBreaker.RecordSuccess();
            _logger.LogInformation(
                "Provider search finished with {MatchCount} matches in {DurationMs} ms (searchId: {SearchId}).",
                matches.Count, stopwatch.ElapsedMilliseconds, searchId ?? "none");
            return matches;
        }
        catch (SearchProviderException ex)
        {
            _circuitBreaker.RecordFailure(ex.Kind, ex.RetryAfterSeconds);
            LogFailure(ex, stopwatch);
            throw;
        }
    }

    private void LogFailure(SearchProviderException ex, System.Diagnostics.Stopwatch stopwatch)
    {
        // Never log the API key, request URLs (they carry the key) or the uploaded image.
        var properties = new Dictionary<string, object?>
        {
            ["ProviderErrorKind"] = ex.Kind.ToString(),
            ["DurationMs"] = stopwatch.ElapsedMilliseconds,
            ["SearchId"] = ex.SearchId ?? "none",
            ["RetryAfterSeconds"] = ex.RetryAfterSeconds,
            ["CircuitState"] = _circuitBreaker.State.ToString(),
            ["ProviderDetail"] = ex.ProviderDetail
        };

        switch (ex.Kind)
        {
            case ProviderErrorKind.NotConfigured:
            case ProviderErrorKind.Unauthorized:
                _logger.LogError(
                    "Search provider configuration error ({@Properties}). Check FullRes:SerpApi:ApiKey.",
                    properties);
                break;
            case ProviderErrorKind.QuotaExceeded:
            case ProviderErrorKind.RateLimited:
            case ProviderErrorKind.Timeout:
            case ProviderErrorKind.Unavailable:
            case ProviderErrorKind.InvalidResponse:
            case ProviderErrorKind.BadRequest:
                _logger.LogWarning("Search provider call failed ({@Properties}).", properties);
                break;
            default:
                _logger.LogInformation("Search provider rejected the search ({@Properties}).", properties);
                break;
        }
    }

    private async Task<string> UploadImageAsync(HttpClient client, PreparedImage image, CancellationToken cancellationToken)
    {
        using var form = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(image.Content);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);
        form.Add(imageContent, "image", image.FileName);
        form.Add(new StringContent(_options.SerpApi.ApiKey!), "api_key");

        var (response, _) = await SendWithRetryAsync(
            () => client.PostAsync(_options.SerpApi.UploadEndpoint, form, cancellationToken),
            "image upload",
            cancellationToken);

        using (response)
        {
            var body = await ReadBodyAsync(response, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw ClassifyHttpError(response.StatusCode, body, "image upload", response.Headers.RetryAfter);

            using var doc = ParseJsonOrThrow(body, "image upload");
            if (TryReadProviderError(doc, out var providerError, out var kind, out var retryAfter))
                throw new SearchProviderException(
                    kind,
                    "The search provider rejected the request.",
                    providerError,
                    retryAfterSeconds: retryAfter);

            if (!doc.RootElement.TryGetProperty("image_id", out var idElement) || idElement.ValueKind != JsonValueKind.String)
                throw new SearchProviderException(
                    ProviderErrorKind.InvalidResponse,
                    "The search provider returned an unexpected upload response.",
                    Truncate(body));

            return idElement.GetString()!;
        }
    }

    private async Task<(IReadOnlyList<SearchCandidate> Matches, string? SearchId)> SearchByImageIdAsync(
        HttpClient client, string imageId, CancellationToken cancellationToken)
    {
        var url = BuildSearchUrl(imageId);

        var (response, _) = await SendWithRetryAsync(
            () => client.GetAsync(url, cancellationToken),
            "search",
            cancellationToken);

        using (response)
        {
            var body = await ReadBodyAsync(response, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw ClassifyHttpError(response.StatusCode, body, "search", response.Headers.RetryAfter);

            using var doc = ParseJsonOrThrow(body, "search");
            var searchId = ReadSearchId(doc);

            var status = ReadMetadataStatus(doc);
            var hasProviderError = TryReadProviderError(doc, out var providerError, out var errorKind, out var retryAfter);

            if (status == "Error")
            {
                // HTTP 200 with search_metadata.status = "Error": the provider failed the search
                // before completion (e.g. proxy timeout on their side). Not a "no results" case.
                throw new SearchProviderException(
                    ProviderErrorKind.Unavailable,
                    "The provider reported the search as failed before completion.",
                    hasProviderError ? providerError : null,
                    searchId: searchId);
            }

            if (hasProviderError)
            {
                if (status == "Success")
                {
                    // A successful search may still carry an informational "error" string meaning
                    // the engine returned no matches. That is an empty result, not a failure.
                    _logger.LogInformation(
                        "Provider reported success with no matches (searchId: {SearchId}).", searchId ?? "none");
                    return (Array.Empty<SearchCandidate>(), searchId);
                }

                throw new SearchProviderException(
                    errorKind,
                    "The search provider rejected the search request.",
                    providerError,
                    retryAfterSeconds: retryAfter,
                    searchId: searchId);
            }

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

            return (matches, searchId);
        }
    }

    /// <summary>
    /// Sends a request, retrying only when the provider answered with an HTTP 5xx status — a
    /// definitive server-side failure, so the search did not complete and no result was consumed.
    /// Network faults and timeouts are NOT retried: after a send with no response it is unclear
    /// whether the provider processed (and billed) the search.
    /// </summary>
    private async Task<(HttpResponseMessage Response, int Attempts)> SendWithRetryAsync(
        Func<Task<HttpResponseMessage>> send, string stage, CancellationToken cancellationToken)
    {
        var maxRetries = Math.Max(0, _options.Resilience.MaxRetries);
        var attempt = 0;
        while (true)
        {
            attempt++;
            HttpResponseMessage response;
            try
            {
                response = await send();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                throw new SearchProviderException(
                    ProviderErrorKind.Timeout,
                    $"The request to the search provider timed out during {stage}.");
            }
            catch (HttpRequestException ex)
            {
                throw new SearchProviderException(
                    ProviderErrorKind.Unavailable,
                    $"Could not reach the search provider during {stage}.",
                    inner: ex);
            }

            var status = (int)response.StatusCode;
            var isServerError = status >= 500;
            var retryAfter = ReadRetryAfterSeconds(response.Headers.RetryAfter);
            var canRetry = isServerError
                && attempt <= maxRetries
                && (retryAfter is null || retryAfter <= MaxRetryDelayMs / 1000);

            if (!canRetry)
                return (response, attempt);

            var delayMs = ComputeBackoffMs(attempt);
            _logger.LogWarning(
                "Provider returned HTTP {StatusCode} during {Stage}; retrying in {DelayMs} ms (attempt {Attempt}/{MaxAttempts}).",
                status, stage, delayMs, attempt, maxRetries + 1);
            response.Dispose();
            await Task.Delay(delayMs, cancellationToken);
        }
    }

    private int ComputeBackoffMs(int attempt)
    {
        var baseDelay = Math.Max(50, _options.Resilience.RetryBaseDelayMs);
        var exponential = baseDelay * (1 << (attempt - 1));
        int jitter;
        lock (_jitter)
            jitter = _jitter.Next(0, baseDelay);
        return Math.Min(MaxRetryDelayMs, exponential + jitter);
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

    private static SearchProviderException ClassifyHttpError(
        HttpStatusCode statusCode, string body, string stage, RetryConditionHeaderValue? retryAfterHeader)
    {
        var detail = Truncate(body);
        var retryAfter = ReadRetryAfterSeconds(retryAfterHeader);
        var status = (int)statusCode;

        if (status == 429)
        {
            // Per SerpApi docs, HTTP 429 means either the hourly throughput limit was exceeded
            // or the account ran out of searches. The message distinguishes the two cases.
            var quotaExhausted = detail.Contains("out of searches", StringComparison.OrdinalIgnoreCase)
                || detail.Contains("quota", StringComparison.OrdinalIgnoreCase);
            return new SearchProviderException(
                quotaExhausted ? ProviderErrorKind.QuotaExceeded : ProviderErrorKind.RateLimited,
                quotaExhausted
                    ? "The search provider account has run out of searches."
                    : "The search provider rate limit was reached.",
                detail,
                retryAfterSeconds: retryAfter);
        }

        return status switch
        {
            400 => new SearchProviderException(
                ProviderErrorKind.BadRequest,
                $"The search provider could not process the {stage} request (HTTP 400).",
                detail),
            401 => new SearchProviderException(
                ProviderErrorKind.Unauthorized,
                "The search provider rejected the configured API key.",
                detail),
            403 => new SearchProviderException(
                ProviderErrorKind.Unauthorized,
                "The search provider account is not allowed to perform this request.",
                detail),
            _ => new SearchProviderException(
                ProviderErrorKind.Unavailable,
                $"The search provider returned HTTP {status} during {stage}.",
                detail,
                retryAfterSeconds: retryAfter)
        };
    }

    private static bool TryReadProviderError(JsonDocument doc, out string detail, out ProviderErrorKind kind, out int? retryAfter)
    {
        detail = string.Empty;
        kind = ProviderErrorKind.Unavailable;
        retryAfter = null;

        if (!doc.RootElement.TryGetProperty("error", out var errorElement))
            return false;

        detail = errorElement.ValueKind == JsonValueKind.String ? errorElement.GetString() ?? string.Empty : errorElement.ToString();
        var lower = detail.ToLowerInvariant();

        kind = lower.Contains("api key") || lower.Contains("unauthorized") || lower.Contains("invalid key")
            ? ProviderErrorKind.Unauthorized
            : lower.Contains("out of searches") || lower.Contains("quota")
                ? ProviderErrorKind.QuotaExceeded
                : lower.Contains("limit") || lower.Contains("exceeded") || lower.Contains("too many") || lower.Contains("rate")
                    ? ProviderErrorKind.RateLimited
                    : lower.Contains("image") && (lower.Contains("format") || lower.Contains("size") || lower.Contains("file"))
                        ? ProviderErrorKind.RejectedImage
                        : ProviderErrorKind.Unavailable;

        return true;
    }

    private static string? ReadSearchId(JsonDocument doc)
    {
        if (!doc.RootElement.TryGetProperty("search_metadata", out var metadata) || metadata.ValueKind != JsonValueKind.Object)
            return null;
        return GetString(metadata, "id");
    }

    private static string? ReadMetadataStatus(JsonDocument doc)
    {
        if (!doc.RootElement.TryGetProperty("search_metadata", out var metadata) || metadata.ValueKind != JsonValueKind.Object)
            return null;
        return GetString(metadata, "status");
    }

    private static int? ReadRetryAfterSeconds(RetryConditionHeaderValue? retryAfter)
    {
        if (retryAfter is null)
            return null;
        if (retryAfter.Delta is { } delta)
            return (int)Math.Ceiling(delta.TotalSeconds);
        if (retryAfter.Date is { } date)
        {
            var seconds = (int)Math.Ceiling((date - DateTimeOffset.UtcNow).TotalSeconds);
            return seconds >= 0 ? seconds : null;
        }
        return null;
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
