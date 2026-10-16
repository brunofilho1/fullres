namespace FullRes.Api.Options;

public sealed class FullResOptions
{
    public const string SectionName = "FullRes";

    public long MaxUploadBytes { get; set; } = 20 * 1024 * 1024;
    public long MaxImagePixels { get; set; } = 30_000_000;
    public long ProviderMaxImageBytes { get; set; } = 500 * 1024;
    public int ProviderTimeoutSeconds { get; set; } = 20;
    public int MaxConcurrentSearches { get; set; } = 4;
    public ProbeOptions Probe { get; set; } = new();
    public CorsOptions Cors { get; set; } = new();
    public RateLimitOptions RateLimit { get; set; } = new();
    public ResilienceOptions Resilience { get; set; } = new();
    public ForwardedHeadersSettings ForwardedHeaders { get; set; } = new();
    public SerpApiOptions SerpApi { get; set; } = new();

    public bool HasProviderKey => !string.IsNullOrWhiteSpace(SerpApi.ApiKey);

    public IReadOnlyList<string> Validate()
    {
        var issues = new List<string>();
        if (MaxUploadBytes < 1024)
            issues.Add("FullRes:MaxUploadBytes must be at least 1024.");
        if (MaxImagePixels < 1)
            issues.Add("FullRes:MaxImagePixels must be positive.");
        if (ProviderMaxImageBytes < 64 * 1024)
            issues.Add("FullRes:ProviderMaxImageBytes must be at least 65536.");
        if (ProviderTimeoutSeconds < 1)
            issues.Add("FullRes:ProviderTimeoutSeconds must be positive.");
        if (MaxConcurrentSearches < 1)
            issues.Add("FullRes:MaxConcurrentSearches must be at least 1.");
        if (Probe.MaxConcurrent < 1)
            issues.Add("FullRes:Probe:MaxConcurrent must be at least 1.");
        if (RateLimit.PermitLimit < 1)
            issues.Add("FullRes:RateLimit:PermitLimit must be at least 1.");
        if (RateLimit.WindowSeconds < 1)
            issues.Add("FullRes:RateLimit:WindowSeconds must be at least 1.");
        if (Cors.AllowedOrigins.Length == 0)
            issues.Add("FullRes:Cors:AllowedOrigins must contain at least one origin.");
        if (Resilience.CircuitBreakerEnabled && Resilience.CircuitBreakerFailureThreshold < 2)
            issues.Add("FullRes:Resilience:CircuitBreakerFailureThreshold must be at least 2.");
        if (Resilience.CircuitBreakerEnabled && Resilience.CircuitBreakerOpenSeconds < 1)
            issues.Add("FullRes:Resilience:CircuitBreakerOpenSeconds must be positive.");
        if (Resilience.QuotaCooldownSeconds < 1)
            issues.Add("FullRes:Resilience:QuotaCooldownSeconds must be positive.");
        if (!HasProviderKey)
            issues.Add("FullRes:SerpApi:ApiKey is empty; POST /api/images/search will return 503 provider_not_configured.");
        return issues;
    }
}

public sealed class ProbeOptions
{
    public bool Enabled { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 6;
    public int MaxConcurrent { get; set; } = 4;
    public int MaxResults { get; set; } = 30;
    public int MaxRedirects { get; set; } = 3;
    public int HeaderByteLimit { get; set; } = 131_072;
}

public sealed class CorsOptions
{
    public string[] AllowedOrigins { get; set; } = [ "http://localhost:5173" ];
}

public sealed class RateLimitOptions
{
    public int PermitLimit { get; set; } = 20;
    public int WindowSeconds { get; set; } = 60;
}

public sealed class ResilienceOptions
{
    /// <summary>Maximum additional attempts (after the first) for provider HTTP 5xx responses.</summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>Base delay for exponential backoff between retries, in milliseconds.</summary>
    public int RetryBaseDelayMs { get; set; } = 300;

    public bool CircuitBreakerEnabled { get; set; } = true;
    public int CircuitBreakerFailureThreshold { get; set; } = 5;
    public int CircuitBreakerOpenSeconds { get; set; } = 30;

    /// <summary>Default suspension window after the provider reports its search quota exhausted.</summary>
    public int QuotaCooldownSeconds { get; set; } = 600;
}

public sealed class ForwardedHeadersSettings
{
    /// <summary>
    /// Only enable behind a reverse proxy/CDN you control. When enabled, the proxy IP must be
    /// listed in <see cref="KnownProxies"/> (or leave both empty to trust loopback only, the
    /// ASP.NET Core default), otherwise clients could spoof their IP and bypass the rate limit.
    /// </summary>
    public bool Enabled { get; set; }
    public string[] KnownProxies { get; set; } = [];
}

public sealed class SerpApiOptions
{
    public string? ApiKey { get; set; }
    public string SearchEndpoint { get; set; } = "https://serpapi.com/search";
    public string UploadEndpoint { get; set; } = "https://serpapi.com/image";
    public string? Country { get; set; }
    public string? Language { get; set; }
}
