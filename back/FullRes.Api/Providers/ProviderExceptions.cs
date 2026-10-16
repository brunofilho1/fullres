namespace FullRes.Api.Providers;

public enum ProviderErrorKind
{
    NotConfigured,
    Unauthorized,
    RateLimited,
    QuotaExceeded,
    RejectedImage,
    Timeout,
    Unavailable,
    InvalidResponse,
    BadRequest
}

public sealed class SearchProviderException : Exception
{
    public ProviderErrorKind Kind { get; }
    public string? ProviderDetail { get; }

    /// <summary>Parsed from the provider's Retry-After header, when present and valid.</summary>
    public int? RetryAfterSeconds { get; }

    /// <summary>Provider-side search identifier (search_metadata.id), useful for diagnosis.</summary>
    public string? SearchId { get; }

    public SearchProviderException(
        ProviderErrorKind kind,
        string message,
        string? providerDetail = null,
        Exception? inner = null,
        int? retryAfterSeconds = null,
        string? searchId = null)
        : base(message, inner)
    {
        Kind = kind;
        ProviderDetail = providerDetail;
        RetryAfterSeconds = retryAfterSeconds;
        SearchId = searchId;
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
