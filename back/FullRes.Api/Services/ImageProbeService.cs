using FullRes.Api.Options;
using FullRes.Api.Security;

namespace FullRes.Api.Services;

public sealed class ImageProbeService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly FullResOptions _options;
    private readonly SemaphoreSlim _gate;
    private readonly ILogger<ImageProbeService> _logger;

    public ImageProbeService(IHttpClientFactory httpClientFactory, FullResOptions options, ILogger<ImageProbeService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _gate = new SemaphoreSlim(Math.Max(1, options.Probe.MaxConcurrent));
        _logger = logger;
    }

    public async Task<ProbeSummary> EnrichAsync(IReadOnlyList<Models.SearchCandidate> candidates, CancellationToken cancellationToken)
    {
        var probe = _options.Probe;
        if (!probe.Enabled || candidates.Count == 0)
            return new ProbeSummary(0, 0);

        var targets = candidates.Take(probe.MaxResults).ToArray();
        await Task.WhenAll(targets.Select(candidate => ProbeAsync(candidate, cancellationToken)));
        return new ProbeSummary(targets.Length, targets.Count(c => c.DimensionsVerified));
    }

    private async Task ProbeAsync(Models.SearchCandidate candidate, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await ProbeCoreAsync(candidate, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Probe failed for {Url}", candidate.ImageUrl);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task ProbeCoreAsync(Models.SearchCandidate candidate, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(candidate.ImageUrl, UriKind.Absolute, out var uri))
            return;
        if (!SsrfGuard.IsPortAllowed(uri.IsDefaultPort ? (uri.Scheme == "https" ? 443 : 80) : uri.Port))
            return;

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linked.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.Probe.TimeoutSeconds)));

        var client = _httpClientFactory.CreateClient("probe");
        var current = uri;
        for (var hop = 0; hop <= _options.Probe.MaxRedirects; hop++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, current);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linked.Token);

            if (IsRedirect(response.StatusCode) && response.Headers.Location is { } location)
            {
                current = location.IsAbsoluteUri ? location : new Uri(current, location);
                continue;
            }

            if (!response.IsSuccessStatusCode)
                return;

            await ReadMetadataAsync(response, candidate, linked.Token);
            return;
        }
    }

    private static async Task ReadMetadataAsync(HttpResponseMessage response, Models.SearchCandidate candidate, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var buffer = new byte[262_144];
        var read = 0;
        while (read < buffer.Length)
        {
            var chunk = await stream.ReadAsync(buffer.AsMemory(read, buffer.Length - read), cancellationToken);
            if (chunk == 0)
                break;
            read += chunk;
        }

        if (read == 0)
            return;

        if (!TryInspectHead(buffer, read, out var kind, out var width, out var height))
            return;

        candidate.DimensionsVerified = true;
        candidate.VerifiedWidth = width;
        candidate.VerifiedHeight = height;
        candidate.VerifiedFormat = ImageFormatInspector.ToFormatName(kind);
        candidate.FileSizeBytes = response.Content.Headers.ContentLength;
    }

    private static bool TryInspectHead(byte[] buffer, int count, out ImageFormatKind kind, out int width, out int height)
    {
        var span = buffer.AsSpan(0, count);
        kind = ImageFormatInspector.Detect(span);
        if (kind == ImageFormatKind.Unknown)
        {
            width = height = 0;
            return false;
        }

        return ImageFormatInspector.TryReadDimensions(span, out width, out height);
    }

    private static bool IsRedirect(System.Net.HttpStatusCode status) => status is
        System.Net.HttpStatusCode.MovedPermanently or
        System.Net.HttpStatusCode.Found or
        System.Net.HttpStatusCode.SeeOther or
        System.Net.HttpStatusCode.TemporaryRedirect or
        System.Net.HttpStatusCode.PermanentRedirect;
}

public sealed record ProbeSummary(int Attempted, int Verified);
