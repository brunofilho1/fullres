namespace FullRes.Api.Providers;

public enum ProviderErrorKind
{
    NotConfigured,
    Unauthorized,
    RateLimited,
    RejectedImage,
    Timeout,
    Unavailable,
    InvalidResponse
}

public sealed class SearchProviderException : Exception
{
    public ProviderErrorKind Kind { get; }
    public string? ProviderDetail { get; }

    public SearchProviderException(ProviderErrorKind kind, string message, string? providerDetail = null, Exception? inner = null)
        : base(message, inner)
    {
        Kind = kind;
        ProviderDetail = providerDetail;
    }
}

public enum ImageValidationError
{
    Missing,
    Empty,
    TooLarge,
    UnsupportedFormat,
    InvalidContent,
    DimensionsTooLarge,
    CouldNotFitProviderLimit
}

public sealed class ImageValidationException : Exception
{
    public ImageValidationError Kind { get; }
    public long? ActualBytes { get; }

    public ImageValidationException(ImageValidationError kind, string message, long? actualBytes = null)
        : base(message)
    {
        Kind = kind;
        ActualBytes = actualBytes;
    }
}
