using FullRes.Api.Models;
using FullRes.Api.Services;

namespace FullRes.Tests.Services;

public class SearchResultProcessorTests
{
    private readonly SearchResultProcessor _processor = new();

    private static SearchCandidate Candidate(
        string imageUrl,
        int? width = null,
        int? height = null,
        string? format = null,
        long? bytes = null,
        double? similarity = null,
        int position = 1,
        string? source = null) => new()
        {
            ImageUrl = imageUrl,
            SourceUrl = source,
            ReportedWidth = width,
            ReportedHeight = height,
            VerifiedFormat = format,
            FileSizeBytes = bytes,
            SimilarityScore = similarity,
            ProviderPosition = position,
            ProviderName = "Test",
            DimensionsVerified = format is not null
        };

    [Fact]
    public void RemovesDuplicateImageUrls()
    {
        var candidates = new List<SearchCandidate>
        {
            Candidate("https://cdn.example.com/a.jpg", 100, 100, position: 1),
            Candidate("https://cdn.example.com/a.jpg?x=1#frag", 100, 100, position: 2),
            Candidate("https://cdn.example.com/a.jpg", 100, 100, position: 3),
            Candidate("https://cdn.example.com/b.jpg", 100, 100, position: 4)
        };

        var result = _processor.Process(candidates, new SearchFilters());

        Assert.Equal(3, result.Results.Count);
        Assert.Contains(result.Warnings, w => w.Code == "duplicates_removed" && w.Count == 1);
    }

    [Fact]
    public void SortsByResolutionByDefaultWithUnknownLast()
    {
        var candidates = new List<SearchCandidate>
        {
            Candidate("https://a/1.jpg", 800, 600, position: 1),
            Candidate("https://a/2.jpg", null, null, position: 2),
            Candidate("https://a/3.jpg", 1920, 1080, position: 3)
        };

        var result = _processor.Process(candidates, new SearchFilters());

        Assert.Equal(new[] { "https://a/3.jpg", "https://a/1.jpg", "https://a/2.jpg" }, result.Results.Select(r => r.ImageUrl));
    }

    [Fact]
    public void SortsByLightestFileFirstWithUnknownLast()
    {
        var candidates = new List<SearchCandidate>
        {
            Candidate("https://a/big.jpg", 100, 100, bytes: 9000, position: 1),
            Candidate("https://a/unknown.jpg", 100, 100, position: 2),
            Candidate("https://a/small.jpg", 100, 100, bytes: 1000, position: 3)
        };

        var result = _processor.Process(candidates, new SearchFilters(SortBy: "size"));

        Assert.Equal(new[] { "https://a/small.jpg", "https://a/big.jpg", "https://a/unknown.jpg" }, result.Results.Select(r => r.ImageUrl));
    }

    [Fact]
    public void AppliesDimensionFilterAndReportsHidden()
    {
        var candidates = new List<SearchCandidate>
        {
            Candidate("https://a/small.jpg", 800, 600, position: 1),
            Candidate("https://a/big.jpg", 2560, 1440, position: 2),
            Candidate("https://a/unknown.jpg", null, null, position: 3)
        };

        var result = _processor.Process(candidates, new SearchFilters(MinWidth: 1920));

        Assert.Single(result.Results);
        Assert.Equal("https://a/big.jpg", result.Results[0].ImageUrl);
        Assert.Contains(result.Warnings, w => w.Code == "hidden_by_resolution_filter" && w.Count == 2);
    }

    [Fact]
    public void AppliesFormatFilter()
    {
        var candidates = new List<SearchCandidate>
        {
            Candidate("https://a/one.png", 100, 100, format: "PNG", position: 1),
            Candidate("https://a/two.jpg", 100, 100, format: "JPG", position: 2),
            Candidate("https://a/three.jpg", 100, 100, position: 3)
        };

        var result = _processor.Process(candidates, new SearchFilters(Format: "jpg"));

        Assert.Single(result.Results);
        Assert.Equal("https://a/two.jpg", result.Results[0].ImageUrl);
        Assert.Contains(result.Warnings, w => w.Code == "hidden_by_format_filter" && w.Count == 2);
    }

    [Fact]
    public void SimilaritySortFallsBackWithWarningWhenScoresUnavailable()
    {
        var candidates = new List<SearchCandidate>
        {
            Candidate("https://a/1.jpg", 800, 600, position: 1),
            Candidate("https://a/2.jpg", 1920, 1080, position: 2)
        };

        var result = _processor.Process(candidates, new SearchFilters(SortBy: "similarity"));

        Assert.Equal("https://a/2.jpg", result.Results[0].ImageUrl);
        Assert.Contains(result.Warnings, w => w.Code == "similarity_sort_fallback");
    }

    [Fact]
    public void AppliesSimilarityFilterWhenScoresAvailable()
    {
        var candidates = new List<SearchCandidate>
        {
            Candidate("https://a/high.jpg", 100, 100, similarity: 95, position: 1),
            Candidate("https://a/low.jpg", 100, 100, similarity: 40, position: 2)
        };

        var result = _processor.Process(candidates, new SearchFilters(SortBy: "similarity", MinSimilarity: 80));

        Assert.Single(result.Results);
        Assert.Equal("https://a/high.jpg", result.Results[0].ImageUrl);
        Assert.Equal(95, result.Results[0].SimilarityScore);
    }

    [Fact]
    public void ExtractsDomainFromSourceUrl()
    {
        var candidates = new List<SearchCandidate>
        {
            Candidate("https://cdn.example.com/a.jpg", 100, 100, position: 1, source: "https://www.Example.com:8443/page")
        };

        var result = _processor.Process(candidates, new SearchFilters());

        Assert.Equal("example.com", result.Results[0].Domain);
    }

    [Fact]
    public void MapsVerifiedDimensionsAndNulls()
    {
        var candidate = Candidate("https://a/1.jpg", 100, 100, position: 1);
        candidate.VerifiedWidth = 4000;
        candidate.VerifiedHeight = 3000;
        candidate.VerifiedFormat = "PNG";
        candidate.DimensionsVerified = true;

        var result = _processor.Process([candidate], new SearchFilters());

        Assert.Equal(4000, result.Results[0].Width);
        Assert.Equal(3000, result.Results[0].Height);
        Assert.Equal(12_000_000, result.Results[0].TotalPixels);
        Assert.True(result.Results[0].DimensionsVerified);
        Assert.Null(result.Results[0].SimilarityScore);
        Assert.Equal("PNG", result.Results[0].Format);
    }

    [Fact]
    public void EmptyInputProducesNoWarnings()
    {
        var result = _processor.Process([], new SearchFilters());
        Assert.Empty(result.Results);
        Assert.Empty(result.Warnings);
    }
}
