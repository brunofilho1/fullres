using FullRes.Api.Dtos;
using FullRes.Api.Models;

namespace FullRes.Api.Services;

public sealed record SearchFilters(
    string SortBy = "resolution",
    int? MinWidth = null,
    int? MinHeight = null,
    string? Format = null,
    int? MinSimilarity = null);

public sealed record ProcessedResults(
    IReadOnlyList<ImageSearchResult> Results,
    IReadOnlyList<SearchWarning> Warnings);

public sealed class SearchResultProcessor
{
    public (List<SearchCandidate> Items, int Removed) Deduplicate(IReadOnlyList<SearchCandidate> candidates) =>
        DeduplicateCore(candidates);

    public ProcessedResults Process(
        IReadOnlyList<SearchCandidate> candidates,
        SearchFilters filters,
        int preRemovedDuplicates = 0)
    {
        var warnings = new List<SearchWarning>();

        var (deduped, duplicateCount) = DeduplicateCore(candidates);
        duplicateCount += preRemovedDuplicates;
        if (duplicateCount > 0)
            warnings.Add(new SearchWarning { Code = "duplicates_removed", Count = duplicateCount });

        var filtered = new List<SearchCandidate>();
        var hiddenByDimensions = 0;
        var hiddenByFormat = 0;

        foreach (var candidate in deduped)
        {
            if (filters.MinWidth is > 0 || filters.MinHeight is > 0)
            {
                var width = candidate.EffectiveWidth;
                var height = candidate.EffectiveHeight;
                var belowWidth = filters.MinWidth is > 0 && (width is null || width < filters.MinWidth);
                var belowHeight = filters.MinHeight is > 0 && (height is null || height < filters.MinHeight);
                if (belowWidth || belowHeight)
                {
                    hiddenByDimensions++;
                    continue;
                }
            }

            if (!string.IsNullOrWhiteSpace(filters.Format))
            {
                var format = candidate.VerifiedFormat;
                if (format is null || !format.Equals(filters.Format, StringComparison.OrdinalIgnoreCase))
                {
                    hiddenByFormat++;
                    continue;
                }
            }

            filtered.Add(candidate);
        }

        if (hiddenByDimensions > 0)
            warnings.Add(new SearchWarning { Code = "hidden_by_resolution_filter", Count = hiddenByDimensions });
        if (hiddenByFormat > 0)
            warnings.Add(new SearchWarning { Code = "hidden_by_format_filter", Count = hiddenByFormat });

        var similarityAvailable = filtered.Any(c => c.SimilarityScore is not null);
        var minSimilarity = filters.MinSimilarity;
        if (minSimilarity is > 0)
        {
            if (similarityAvailable)
            {
                filtered = filtered
                    .Where(c => c.SimilarityScore is not null && c.SimilarityScore >= minSimilarity)
                    .ToList();
            }
            else
            {
                minSimilarity = null;
                warnings.Add(new SearchWarning { Code = "similarity_filter_ignored" });
            }
        }

        var sortBy = filters.SortBy;
        if (sortBy == "similarity" && !similarityAvailable)
        {
            sortBy = "resolution";
            warnings.Add(new SearchWarning { Code = "similarity_sort_fallback" });
        }

        var ordered = ApplySort(filtered, sortBy);
        var results = ordered.Select(ToDto).ToList();
        return new ProcessedResults(results, warnings);
    }

    private static (List<SearchCandidate> Items, int Removed) DeduplicateCore(IReadOnlyList<SearchCandidate> candidates)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var output = new List<SearchCandidate>(candidates.Count);
        var removed = 0;

        foreach (var candidate in candidates)
        {
            var key = NormalizeUrl(candidate.ImageUrl);
            if (seen.Add(key))
                output.Add(candidate);
            else
                removed++;
        }

        return (output, removed);
    }

    private static string NormalizeUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url.Trim();

        var builder = new UriBuilder(uri) { Fragment = string.Empty };
        if ((builder.Scheme == "http" && builder.Port == 80) || (builder.Scheme == "https" && builder.Port == 443))
            builder.Port = -1;

        var path = builder.Path.TrimEnd('/');
        return $"{builder.Scheme}://{builder.Host.ToLowerInvariant()}{(builder.Uri.IsDefaultPort ? "" : ":" + builder.Port)}{path}{builder.Query}"
            .ToLowerInvariant();
    }

    private static List<SearchCandidate> ApplySort(List<SearchCandidate> candidates, string sortBy) =>
        sortBy switch
        {
            "size" => candidates
                .OrderBy(c => c.FileSizeBytes is null ? 1 : 0)
                .ThenBy(c => c.FileSizeBytes ?? long.MaxValue)
                .ThenBy(c => c.ProviderPosition)
                .ToList(),
            "similarity" => candidates
                .OrderBy(c => c.SimilarityScore is null ? 1 : 0)
                .ThenByDescending(c => c.SimilarityScore ?? 0)
                .ThenBy(c => c.ProviderPosition)
                .ToList(),
            "width" => candidates
                .OrderBy(c => c.EffectiveWidth is null ? 1 : 0)
                .ThenByDescending(c => c.EffectiveWidth ?? 0)
                .ThenBy(c => c.ProviderPosition)
                .ToList(),
            _ => candidates
                .OrderBy(c => c.TotalPixels is null ? 1 : 0)
                .ThenByDescending(c => c.TotalPixels ?? 0)
                .ThenBy(c => c.ProviderPosition)
                .ToList()
        };

    private static ImageSearchResult ToDto(SearchCandidate candidate) => new()
    {
        ImageUrl = candidate.ImageUrl,
        SourceUrl = candidate.SourceUrl,
        Title = candidate.Title,
        Domain = ExtractDomain(candidate.SourceUrl),
        Width = candidate.EffectiveWidth,
        Height = candidate.EffectiveHeight,
        TotalPixels = candidate.TotalPixels,
        Format = candidate.VerifiedFormat,
        FileSizeBytes = candidate.FileSizeBytes,
        DimensionsVerified = candidate.DimensionsVerified,
        SimilarityScore = candidate.SimilarityScore,
        ThumbnailUrl = candidate.ThumbnailUrl,
        Provider = candidate.ProviderName
    };

    private static string? ExtractDomain(string? sourceUrl)
    {
        if (string.IsNullOrWhiteSpace(sourceUrl))
            return null;
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri))
            return null;
        var host = uri.Host.ToLowerInvariant();
        return host.StartsWith("www.", StringComparison.Ordinal) ? host[4..] : host;
    }
}
