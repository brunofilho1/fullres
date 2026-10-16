namespace FullRes.Api.Models;

public sealed class PreparedImage
{
    public required byte[] Content { get; init; }
    public required string ContentType { get; init; }
    public required string FileName { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required long OriginalSizeBytes { get; init; }
    public required string OriginalFormat { get; init; }
    public bool ReencodedForProvider { get; init; }
}

public sealed class SearchCandidate
{
    public required string ImageUrl { get; init; }
    public string? SourceUrl { get; init; }
    public string? Title { get; init; }
    public string? ThumbnailUrl { get; init; }
    public int? ReportedWidth { get; init; }
    public int? ReportedHeight { get; init; }
    public double? SimilarityScore { get; init; }
    public int ProviderPosition { get; init; }
    public required string ProviderName { get; init; }

    public int? VerifiedWidth { get; set; }
    public int? VerifiedHeight { get; set; }
    public string? VerifiedFormat { get; set; }
    public long? FileSizeBytes { get; set; }
    public bool DimensionsVerified { get; set; }

    public int? EffectiveWidth => VerifiedWidth ?? ReportedWidth;
    public int? EffectiveHeight => VerifiedHeight ?? ReportedHeight;
    public long? TotalPixels
    {
        get
        {
            var w = EffectiveWidth;
            var h = EffectiveHeight;
            return w is null || h is null ? null : (long)w.Value * h.Value;
        }
    }
}
