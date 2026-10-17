using FullRes.Api.Dtos;
using FullRes.Api.Errors;
using FullRes.Api.Models;
using FullRes.Api.Options;
using FullRes.Api.Providers;
using FullRes.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FullRes.Api.Controllers;

[ApiController]
[Route("api/images")]
public sealed class ImagesSearchController : ControllerBase
{
    private static readonly string[] AllowedSort = [ "resolution", "similarity", "size", "width" ];

    private readonly UploadedImagePreparationService _preparer;
    private readonly IReverseImageSearchProvider _provider;
    private readonly ImageProbeService _probe;
    private readonly SearchResultProcessor _processor;
    private readonly SearchConcurrencyGate _gate;
    private readonly FullResOptions _options;

    public ImagesSearchController(
        UploadedImagePreparationService preparer,
        IReverseImageSearchProvider provider,
        ImageProbeService probe,
        SearchResultProcessor processor,
        SearchConcurrencyGate gate,
        FullResOptions options)
    {
        _preparer = preparer;
        _provider = provider;
        _probe = probe;
        _processor = processor;
        _gate = gate;
        _options = options;
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search(
        [FromForm] IFormFile? image = null,
        [FromForm] string? sortBy = null,
        [FromForm] int? minWidth = null,
        [FromForm] int? minHeight = null,
        [FromForm] string? format = null,
        [FromForm] int? minSimilarity = null,
        CancellationToken cancellationToken = default)
    {
        sortBy = string.IsNullOrWhiteSpace(sortBy) ? "resolution" : sortBy.Trim().ToLowerInvariant();
        if (!AllowedSort.Contains(sortBy))
            return ApiProblem.Fail(HttpContext, StatusCodes.Status400BadRequest, "invalid_request",
                "The request is invalid.", $"sortBy must be one of: {string.Join(", ", AllowedSort)}.");

        if (minWidth is < 0 or > 100_000 || minHeight is < 0 or > 100_000)
            return ApiProblem.Fail(HttpContext, StatusCodes.Status400BadRequest, "invalid_request",
                "The request is invalid.", "minWidth and minHeight must be between 0 and 100000.");

        if (minSimilarity is < 0 or > 100)
            return ApiProblem.Fail(HttpContext, StatusCodes.Status400BadRequest, "invalid_request",
                "The request is invalid.", "minSimilarity must be between 0 and 100.");

        if (minSimilarity == 0)
            minSimilarity = null;
        if (minWidth == 0)
            minWidth = null;
        if (minHeight == 0)
            minHeight = null;

        PreparedImage prepared;
        try
        {
            prepared = await _preparer.PrepareAsync(image!, cancellationToken);
        }
        catch (ImageValidationException ex)
        {
            return MapValidationError(ex);
        }

        IReadOnlyList<SearchCandidate> matches;
        using (var slot = await _gate.AcquireAsync(TimeSpan.FromSeconds(5), cancellationToken))
        {
            if (slot is null)
                return ApiProblem.FailWithRetryAfter(HttpContext, StatusCodes.Status503ServiceUnavailable,
                    "search_busy",
                    "Too many searches are running right now.",
                    "Too many searches are running at the moment. Try again in a few seconds.",
                    retryAfterSeconds: 5);

            try
            {
                matches = await _provider.SearchAsync(prepared, cancellationToken);
            }
            catch (SearchProviderException ex)
            {
                return MapProviderError(ex);
            }
        }

        var (deduped, removedDuplicates) = _processor.Deduplicate(matches);
        var probeSummary = await _probe.EnrichAsync(deduped, cancellationToken);

        var filters = new SearchFilters(sortBy, minWidth, minHeight, format, minSimilarity);
        var processed = _processor.Process(deduped, filters, removedDuplicates);

        var warnings = new List<SearchWarning>(processed.Warnings);
        if (probeSummary.Attempted > 0 && probeSummary.Verified < probeSummary.Attempted)
        {
            var failed = probeSummary.Attempted - probeSummary.Verified;
            warnings.Add(new SearchWarning { Code = "unverified_matches", Count = failed });
        }

        return Ok(new ImageSearchResponse
        {
            Query = new ImageQueryInfo
            {
                Width = prepared.Width,
                Height = prepared.Height,
                TotalPixels = (long)prepared.Width * prepared.Height,
                Format = prepared.OriginalFormat,
                SizeBytes = prepared.Content.LongLength,
                OriginalSizeBytes = prepared.OriginalSizeBytes,
                ReencodedForProvider = prepared.ReencodedForProvider
            },
            Results = processed.Results,
            Provider = new SearchProviderInfo
            {
                Name = _provider.Name,
                Configured = _provider.IsConfigured,
                Warnings = warnings
            }
        });
    }

    private IActionResult MapValidationError(ImageValidationException ex) => ex.Kind switch
    {
        ImageValidationError.Missing =>
            ApiProblem.Fail(HttpContext, StatusCodes.Status400BadRequest, "file_missing",
                "No image file was provided.", "Send the image as multipart/form-data in the 'image' field."),
        ImageValidationError.Empty =>
            ApiProblem.Fail(HttpContext, StatusCodes.Status400BadRequest, "file_empty",
                "The uploaded file is empty."),
        ImageValidationError.TooLarge =>
            ApiProblem.Fail(HttpContext, StatusCodes.Status413PayloadTooLarge, "file_too_large",
                "The uploaded image is too large.", ex.Message),
        ImageValidationError.UnsupportedFormat =>
            ApiProblem.Fail(HttpContext, StatusCodes.Status415UnsupportedMediaType, "format_unsupported",
                "Unsupported image format.", ex.Message),
        ImageValidationError.InvalidContent =>
            ApiProblem.Fail(HttpContext, StatusCodes.Status415UnsupportedMediaType, "file_invalid",
                "The file is not a valid image.", ex.Message),
        ImageValidationError.DimensionsTooLarge =>
            ApiProblem.Fail(HttpContext, StatusCodes.Status422UnprocessableEntity, "image_dimensions_too_large",
                "The image has too many pixels to process.", ex.Message),
        ImageValidationError.CouldNotFitProviderLimit =>
            ApiProblem.Fail(HttpContext, StatusCodes.Status422UnprocessableEntity, "image_cannot_be_prepared",
                "The image could not be prepared for the search provider.", ex.Message),
        _ => ApiProblem.Fail(HttpContext, StatusCodes.Status400BadRequest, "invalid_request",
            "The request is invalid.", ex.Message)
    };

    /// <summary>
    /// Maps provider failures to stable FullRes error codes. Raw provider bodies and internal
    /// configuration guidance stay in the server logs; clients only get safe, generic details.
    /// </summary>
    private IActionResult MapProviderError(SearchProviderException ex) => ex.Kind switch
    {
        ProviderErrorKind.NotConfigured =>
            ApiProblem.Fail(HttpContext, StatusCodes.Status503ServiceUnavailable, "provider_not_configured",
                "Reverse image search is not available.",
                "Reverse image search is temporarily unavailable on this server."),
        ProviderErrorKind.Unauthorized =>
            ApiProblem.Fail(HttpContext, StatusCodes.Status503ServiceUnavailable, "provider_credentials_invalid",
                "The search provider rejected the configured credentials.",
                "The search service is temporarily unavailable. Please try again later."),
        ProviderErrorKind.RateLimited =>
            ApiProblem.FailWithRetryAfter(HttpContext, StatusCodes.Status503ServiceUnavailable, "provider_rate_limited",
                "The search provider rate limit was reached.",
                "The search service is receiving too many requests. Try again in a few seconds.",
                ex.RetryAfterSeconds),
        ProviderErrorKind.QuotaExceeded =>
            ApiProblem.FailWithRetryAfter(HttpContext, StatusCodes.Status503ServiceUnavailable, "provider_quota_exceeded",
                "The search provider quota is exhausted.",
                "Searches are temporarily unavailable because the search service reached its usage limit. Please try again later.",
                ex.RetryAfterSeconds),
        ProviderErrorKind.RejectedImage =>
            ApiProblem.Fail(HttpContext, StatusCodes.Status422UnprocessableEntity, "provider_rejected_image",
                "The search provider rejected this image.", ex.Message),
        ProviderErrorKind.BadRequest =>
            ApiProblem.Fail(HttpContext, StatusCodes.Status502BadGateway, "provider_error",
                "The search provider could not process the request.",
                "The search service could not process this request. Please try again later."),
        ProviderErrorKind.Timeout =>
            ApiProblem.Fail(HttpContext, StatusCodes.Status504GatewayTimeout, "provider_timeout",
                "The search provider did not respond in time.",
                "The search took longer than expected and could not be completed. Please try again."),
        ProviderErrorKind.InvalidResponse =>
            ApiProblem.Fail(HttpContext, StatusCodes.Status502BadGateway, "provider_invalid_response",
                "The search provider returned an unexpected response.",
                "The search service returned an unexpected response. Please try again later."),
        _ => ApiProblem.Fail(HttpContext, StatusCodes.Status503ServiceUnavailable, "provider_unavailable",
                "The search provider is unavailable.",
                "The search service is temporarily unavailable. Please try again later.")
    };
}

/// <summary>
/// Bounds how many provider searches run at the same time, protecting the external quota.
/// In-memory only: with multiple API instances the effective limit is per instance.
/// </summary>
public sealed class SearchConcurrencyGate
{
    private readonly SemaphoreSlim _semaphore;

    public SearchConcurrencyGate(FullResOptions options)
    {
        _semaphore = new SemaphoreSlim(Math.Max(1, options.MaxConcurrentSearches));
    }

    /// <summary>Returns null when no slot could be acquired within the timeout.</summary>
    public async Task<IDisposable?> AcquireAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (!await _semaphore.WaitAsync(timeout, cancellationToken))
            return null;
        return new Releaser(_semaphore);
    }

    private sealed class Releaser : IDisposable
    {
        private SemaphoreSlim? _semaphore;

        public Releaser(SemaphoreSlim semaphore) => _semaphore = semaphore;

        public void Dispose() => Interlocked.Exchange(ref _semaphore, null)?.Release();
    }
}
