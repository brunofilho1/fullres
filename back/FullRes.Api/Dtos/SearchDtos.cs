namespace FullRes.Api.Dtos;

public sealed class ImageSearchResponse
{
    public required ImageQueryInfo Query { get; init; }
    public required IReadOnlyList<ImageSearchResult> Results { get; init; }
    public required SearchProviderInfo Provider { get; init; }
}

public sealed class ImageQueryInfo
{
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required long TotalPixels { get; init; }
    public required string Format { get; init; }
    public required long SizeBytes { get; init; }
    public required long OriginalSizeBytes { get; init; }
    public required bool ReencodedForProvider { get; init; }
}

public sealed class ImageSearchResult
{
    public required string ImageUrl { get; init; }
    public string? SourceUrl { get; init; }
    public string? Title { get; init; }
    public string? Domain { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
    public long? TotalPixels { get; init; }
    public string? Format { get; init; }
    public long? FileSizeBytes { get; init; }
    public bool DimensionsVerified { get; init; }
    public double? SimilarityScore { get; init; }
    public string? ThumbnailUrl { get; init; }
    public required string Provider { get; init; }
}

public sealed class SearchProviderInfo
{
    public required string Name { get; init; }
    public required bool Configured { get; init; }
    public required IReadOnlyList<string> Warnings { get; init; }
}
