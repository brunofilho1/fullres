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
    private readonly FullResOptions _options;

    public ImagesSearchController(
        UploadedImagePreparationService preparer,
        IReverseImageSearchProvider provider,
        ImageProbeService probe,
        SearchResultProcessor processor,
        FullResOptions options)
    {
        _preparer = preparer;
        _provider = provider;
        _probe = probe;
        _processor = processor;
        _options = options;
    }

    [HttpPost("search")]
    [RequestSizeLimit(26_843_545)]
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
        try
        {
            matches = await _provider.SearchAsync(prepared, cancellationToken);
        }
        catch (SearchProviderException ex)
        {
            return MapProviderError(ex);
        }

        var (deduped, removedDuplicates) = _processor.Deduplicate(matches);
        var probeSummary = await _probe.EnrichAsync(deduped, cancellationToken);

        var filters = new SearchFilters(sortBy, minWidth, minHeight, format, minSimilarity);
        var processed = _processor.Process(deduped, filters, removedDuplicates);

        var warnings = new List<string>(processed.Warnings);
        if (probeSummary.Attempted > 0 && probeSummary.Verified < probeSummary.Attempted)
        {
            var failed = probeSummary.Attempted - probeSummary.Verified;
            warnings.Add(
                $"{failed} match{(failed == 1 ? "" : "es")} could not be verified against the source image (host unreachable or blocked); listed dimensions come from the search index.");
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

    private IActionResult MapProviderError(SearchProviderException ex) => ex.Kind switch
    {
        ProviderErrorKind.NotConfigured =>
            ApiProblem.Fail(HttpContext, StatusCodes.Status503ServiceUnavailable, "provider_not_configured",
                "Reverse image search is not configured.",
                "Set FullRes:SerpApi:ApiKey (see back/.env.example) and restart the API."),
        ProviderErrorKind.Unauthorized =>
            ApiProblem.Fail(HttpContext, StatusCodes.Status502BadGateway, "provider_credentials_invalid",
                "The search provider rejected the configured credentials.", ex.ProviderDetail),
        ProviderErrorKind.RateLimited =>
            ApiProblem.Fail(HttpContext, StatusCodes.Status503ServiceUnavailable, "provider_rate_limited",
                "The search provider rate limit was reached.", ex.ProviderDetail),
        ProviderErrorKind.RejectedImage =>
            ApiProblem.Fail(HttpContext, StatusCodes.Status422UnprocessableEntity, "provider_rejected_image",
                "The search provider rejected this image.", ex.ProviderDetail),
        ProviderErrorKind.Timeout =>
            ApiProblem.Fail(HttpContext, StatusCodes.Status504GatewayTimeout, "provider_timeout",
                "The search provider did not respond in time.", ex.Message),
        ProviderErrorKind.InvalidResponse =>
            ApiProblem.Fail(HttpContext, StatusCodes.Status502BadGateway, "provider_invalid_response",
                "The search provider returned an unexpected response.", ex.ProviderDetail),
        _ => ApiProblem.Fail(HttpContext, StatusCodes.Status502BadGateway, "provider_unavailable",
                "The search provider is unavailable.", ex.Message)
    };
}
