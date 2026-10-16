namespace FullRes.Api.Options;

public sealed class FullResOptions
{
    public const string SectionName = "FullRes";

    public long MaxUploadBytes { get; set; } = 20 * 1024 * 1024;
    public long MaxImagePixels { get; set; } = 30_000_000;
    public long ProviderMaxImageBytes { get; set; } = 500 * 1024;
    public int ProviderTimeoutSeconds { get; set; } = 20;
    public ProbeOptions Probe { get; set; } = new();
    public CorsOptions Cors { get; set; } = new();
    public RateLimitOptions RateLimit { get; set; } = new();
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
        if (Probe.MaxConcurrent < 1)
            issues.Add("FullRes:Probe:MaxConcurrent must be at least 1.");
        if (RateLimit.PermitLimit < 1)
            issues.Add("FullRes:RateLimit:PermitLimit must be at least 1.");
        if (RateLimit.WindowSeconds < 1)
            issues.Add("FullRes:RateLimit:WindowSeconds must be at least 1.");
        if (Cors.AllowedOrigins.Length == 0)
            issues.Add("FullRes:Cors:AllowedOrigins must contain at least one origin.");
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

public sealed class SerpApiOptions
{
    public string? ApiKey { get; set; }
    public string SearchEndpoint { get; set; } = "https://serpapi.com/search";
    public string UploadEndpoint { get; set; } = "https://serpapi.com/image";
    public string? Country { get; set; }
    public string? Language { get; set; }
}
