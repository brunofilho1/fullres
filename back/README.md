# FullRes API

ASP.NET Core 8 Web API that powers [FullRes](../README.md). It accepts an uploaded image, prepares it for the search provider, queries [SerpApi's Google Lens engine](https://serpapi.com/google-lens-api), verifies and enriches the returned matches, then de-duplicates, filters and ranks them before returning a single JSON response.

## Requirements

- [.NET SDK 8.0](https://dotnet.microsoft.com/download) or newer (the projects target `net8.0`).
- A [SerpApi](https://serpapi.com) API key for real searches. The API boots without one, but searches return `503 provider_not_configured`.

## Setup

```bash
cd back
cp .env.example .env          # fill in FullRes__SerpApi__ApiKey
dotnet run --project FullRes.Api
```

On startup the API loads `back/.env` (found by walking up from the app directory) via [DotNetEnv](https://www.nuget.org/packages/DotNetEnv), then reads standard ASP.NET Core configuration. Shell-exported environment variables take precedence over `.env`; these also work:

- Environment variables: `FullRes__SerpApi__ApiKey=...`
- `appsettings.Development.json`
- `dotnet user-secrets`

By default the API listens on `http://localhost:5080` (see `FullRes.Api/Properties/launchSettings.json`).

### SerpApi notes

- Create a key at <https://serpapi.com/manage-api-key>. The free plan allows **250 searches/month**; each FullRes search spends one search.
- FullRes uses two SerpApi endpoints: `POST /image` to upload the image (returns an `image_id` valid for ~10 minutes) and `GET /search?engine=google_lens&type=visual_matches&image_id=...`.
- Provider uploads are capped at **500 KB**, so larger images are transparently re-encoded (see below).
- Review SerpApi's and Google's terms before deploying this publicly; FullRes does not grant any right to redistribute third-party images and the provider does not expose a similarity score.

## Deploy (Docker)

A multi-stage [`Dockerfile`](Dockerfile) builds the API on the official .NET 8 images. It publishes the API only (not the tests), runs as the non-root `app` user, installs `libfontconfig1` (required by SkiaSharp at runtime) and binds to `${PORT}` so it works on platforms like Render.

```bash
# from the repo root; the build context is the back/ directory
docker build -t fullres-api ./back
docker run --rm -p 8080:8080 -e FullRes__SerpApi__ApiKey=<key> fullres-api
```

On **Render** (Web Service, Docker):

- Root Directory: `back`
- Dockerfile Path: `Dockerfile`
- Port: leave the default — Render injects `PORT` and the container binds to it.
- Add configuration as environment variables (`FullRes__SerpApi__ApiKey`, `FullRes__Cors__AllowedOrigins__0=https://<your-pages-domain>`, …). No `.env` file is needed; `.dockerignore` keeps it out of the image.

Notes for a public deployment:

- Set `Cors:AllowedOrigins` to the deployed frontend origin (e.g. `https://fullres.pages.dev`), otherwise the browser will block the API calls.
- The per-IP rate limiter uses `Connection.RemoteIpAddress`. Behind Render's proxy this is the proxy address unless forwarded headers are configured; since the proxy IPs are not fixed you cannot easily pin `ForwardedHeaders:KnownProxies`, so all clients may share one bucket. Adjust `RateLimit` accordingly or keep the app behind your own proxy if you need accurate per-client limits.

## Configuration

| Key | Default | Description |
| --- | --- | --- |
| `FullRes:MaxUploadBytes` | `20971520` (20 MB) | Maximum accepted upload size. |
| `FullRes:MaxImagePixels` | `30000000` | Maximum decoded pixel count (compression-bomb guard). |
| `FullRes:ProviderMaxImageBytes` | `512000` | Target byte size when re-encoding for the provider. |
| `FullRes:ProviderTimeoutSeconds` | `20` | Timeout for provider upload + search calls. |
| `FullRes:MaxConcurrentSearches` | `4` | Maximum provider searches running at once on this instance. |
| `FullRes:Resilience:MaxRetries` | `2` | Retries for transient provider failures (HTTP 5xx only), with exponential backoff + jitter. |
| `FullRes:Resilience:RetryBaseDelayMs` | `300` | Base delay for the backoff. |
| `FullRes:Resilience:CircuitBreakerEnabled` | `true` | Stop calling the provider after repeated transient failures. |
| `FullRes:Resilience:CircuitBreakerFailureThreshold` | `5` | Consecutive transient failures before the breaker opens. |
| `FullRes:Resilience:CircuitBreakerOpenSeconds` | `30` | How long the breaker stays open before probing again. |
| `FullRes:Resilience:QuotaCooldownSeconds` | `600` | How long to avoid the provider after a quota-exhausted response. |
| `FullRes:SerpApi:ApiKey` | *(empty)* | SerpApi key. Empty disables real searches. |
| `FullRes:SerpApi:SearchEndpoint` | `https://serpapi.com/search` | Search endpoint (overridable for tests). |
| `FullRes:SerpApi:UploadEndpoint` | `https://serpapi.com/image` | Upload endpoint (overridable for tests). |
| `FullRes:SerpApi:Country` / `Language` | *(empty)* | Optional provider locale hints. |
| `FullRes:Probe:Enabled` | `true` | Read dimensions/format of each result from the source image. |
| `FullRes:Probe:TimeoutSeconds` | `6` | Per-result probe timeout. |
| `FullRes:Probe:MaxConcurrent` | `4` | Probe concurrency. |
| `FullRes:Probe:MaxResults` | `30` | Maximum results probed per search. |
| `FullRes:Probe:MaxRedirects` | `3` | Redirects followed while probing (each revalidated). |
| `FullRes:Cors:AllowedOrigins` | `["http://localhost:5173"]` | Origins allowed to call the API. |
| `FullRes:RateLimit:PermitLimit` | `20` | Requests allowed per IP per window. |
| `FullRes:RateLimit:WindowSeconds` | `60` | Rate-limit window length. |
| `FullRes:ForwardedHeaders:Enabled` | `false` | Trust `X-Forwarded-For`/`-Proto`. Enable **only** behind a proxy you control. |
| `FullRes:ForwardedHeaders:KnownProxies` | `[]` | Proxy IPs allowed to set forwarded headers. Empty ⇒ loopback only. |

## Endpoints

### `GET /api/health`

```json
{
  "status": "ok",
  "provider": "Google Lens (SerpApi)",
  "providerConfigured": true,
  "utc": "2026-01-01T00:00:00Z"
}
```

### `POST /api/images/search`

`multipart/form-data`:

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `image` | file | yes | The image to search for (JPG, PNG, WEBP, GIF or BMP). |
| `sortBy` | string | no | `resolution` (default), `similarity`, `size` or `width`. |
| `minWidth` / `minHeight` | int | no | Minimum width/height in pixels. |
| `format` | string | no | Keep only this format (`JPG`, `PNG`, `WEBP`). |
| `minSimilarity` | int | no | Minimum similarity, `0–100` (only meaningful when the provider reports one). |

Response (abridged):

```json
{
  "query": {
    "width": 1200,
    "height": 900,
    "totalPixels": 1080000,
    "format": "JPG",
    "sizeBytes": 417495,
    "originalSizeBytes": 2147986,
    "reencodedForProvider": true
  },
  "results": [
    {
      "imageUrl": "https://cdn.example.com/mountain-4k.jpg",
      "sourceUrl": "https://www.example.com/mountain-4k",
      "title": "Mountain lake at sunrise - 4K wallpaper",
      "domain": "example.com",
      "width": 3840,
      "height": 2160,
      "totalPixels": 8294400,
      "format": "JPG",
      "fileSizeBytes": 1834210,
      "dimensionsVerified": true,
      "similarityScore": null,
      "thumbnailUrl": "https://cdn.example.com/mountain-4k-thumb.jpg",
      "provider": "Google Lens (SerpApi)"
    }
  ],
  "provider": {
    "name": "Google Lens (SerpApi)",
    "configured": true,
    "warnings": ["3 matches could not be verified against the source image (host unreachable or blocked); listed dimensions come from the search index."]
  }
}
```

Unknown values are always `null` — the API never invents dimensions, formats, sizes or scores. `dimensionsVerified` is `true` only when FullRes read the value from the source image itself.

### Error responses

Errors use [RFC 7807 ProblemDetails](https://datatracker.ietf.org/doc/html/rfc7807) with two extra fields:

- `code` — a stable, machine-readable identifier (table below). The frontend maps these to user-facing messages.
- `traceId` — the request's correlation id (the W3C `Activity` id, or the ASP.NET `TraceIdentifier`). Log it to correlate a report with server logs; it is never a stack trace.

When the failure is a transient limit, the response also carries a `Retry-After` header (seconds).

Example (`Content-Type: application/problem+json`):

```json
{
  "type": "about:blank",
  "title": "The search provider quota is exhausted.",
  "status": 503,
  "detail": "Searches are temporarily unavailable because the search service reached its usage limit. Please try again later.",
  "instance": "/api/images/search",
  "code": "provider_quota_exceeded",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

| HTTP | `code` | Meaning |
| --- | --- | --- |
| 400 | `file_missing` | No `image` field in the request. |
| 400 | `file_empty` | The uploaded file is empty. |
| 400 | `invalid_request` | Invalid query parameters or malformed request. |
| 413 | `file_too_large` | Upload exceeds `MaxUploadBytes` (also returned when Kestrel/multipart rejects an oversized body). |
| 415 | `format_unsupported` | Not a supported image format. |
| 415 | `file_invalid` | Content is not a valid image. |
| 422 | `image_dimensions_too_large` | Too many pixels to process. |
| 422 | `image_cannot_be_prepared` | Could not shrink/re-encode below the provider limit. |
| 422 | `provider_rejected_image` | The provider refused the image. |
| 429 | `rate_limited` | Per-IP rate limit exceeded (FullRes). |
| 502 | `provider_invalid_response` | Provider returned an unexpected payload. |
| 502 | `provider_error` | Provider rejected the request as invalid (bad upload). |
| 503 | `provider_not_configured` | No `FullRes:SerpApi:ApiKey` configured. |
| 503 | `provider_credentials_invalid` | Provider rejected the API key. |
| 503 | `provider_rate_limited` | Provider per-hour throughput limit reached. |
| 503 | `provider_quota_exceeded` | Provider account is out of searches/searches limit reached. |
| 503 | `provider_unavailable` | Provider could not be reached. |
| 503 | `search_busy` | Too many concurrent searches on this instance; retry shortly. |
| 504 | `provider_timeout` | Provider timed out. |
| 500 | `unexpected_error` | Unhandled server error. |

> `provider_credentials_invalid`, `provider_unavailable`, `search_busy` and the quota/rate-limit codes all map to **503** because they are transient from the client's point of view — retrying later is reasonable. Server logs keep the real cause and the raw provider detail; clients only receive safe, generic `detail` text.

## Resilience and failure handling

The provider is the most likely thing to fail, so its failures are classified and contained:

- **Classification** — the SerpApi provider distinguishes an out-of-searches account (`provider_quota_exceeded`) from the hourly throughput limit (`provider_rate_limited`), a rejected key (`provider_credentials_invalid`), a bad upload (`provider_error`), a rejected image (`provider_rejected_image`), timeouts (`provider_timeout`) and malformed responses (`provider_invalid_response`, including a `search_metadata.status` of `Error`).
- **Retries** — transient failures (HTTP 5xx, network errors) are retried up to `Resilience:MaxRetries` with exponential backoff and jitter. Non-transient failures (4xx, quota, rejected image) are never retried.
- **Circuit breaker** — after `CircuitBreakerFailureThreshold` consecutive transient failures the provider is skipped for `CircuitBreakerOpenSeconds`; a quota-exhausted response parks the provider for `QuotaCooldownSeconds`. While open, searches return `503` immediately instead of hammering the provider.
- **Concurrency gate** — at most `MaxConcurrentSearches` provider searches run at once; extra requests get `503 search_busy` with `Retry-After` instead of silently consuming quota.
- **Client messages** — the frontend translates error `code`s into Brazilian-Portuguese messages (see the frontend README). Raw provider payloads, API keys and stack traces are never sent to the client; those stay in the server logs, keyed by the `traceId` returned to the user.

The frontend has its own request watchdog (a client-side timeout) and guards against stale/duplicate searches, so a slow or failed request never leaves the UI stuck.

## How a search is processed

1. **Validate & prepare** — the upload is validated by inspecting its header bytes (`ImageFormatInspector`), not by decoding untrusted content. Oversized files are decoded with SkiaSharp, alpha is flattened to white, and the image is re-encoded to JPEG (quality ladder 85 → 45, downscaling to 0.7× as needed) until it fits the provider limit.
2. **Provider query** — the prepared image is uploaded and searched through the configured `IReverseImageSearchProvider` (Google Lens via SerpApi).
3. **De-duplicate** — results pointing at the same image URL are collapsed before any network probing.
4. **Probe** — for each remaining result (up to `Probe:MaxResults`), FullRes reads the source image headers to confirm real dimensions/format/size. Probes go through the SSRF guard and any failure simply leaves the value unverified.
5. **Filter, sort & rank** — filters and sorting are applied; results are returned with honest `warnings` describing anything that could not be verified.

## Security

- **SSRF guard** (`Security/SsrfGuard.cs`) — remote fetches resolve DNS and connect directly to the validated IP (no rebinding gap), allow only `http`/`https` on ports 80/443, block loopback/private/link-local/CGNAT/metadata ranges, and re-validate every redirect.
- **Untrusted uploads are never decoded blindly** — format detection reads headers first and a pixel cap blocks compression bombs.
- **Rate limiting** — a per-IP fixed-window limiter protects the provider quota. It is **in-memory**, so with multiple API instances each instance enforces its own limit; use a shared store (e.g. Redis) if you scale out.
- **Forwarded headers** — disabled by default. When enabling behind a proxy/CDN, list the proxy IPs in `ForwardedHeaders:KnownProxies`; otherwise clients could spoof `X-Forwarded-For` and bypass the per-IP rate limit.
- **CORS** — only the configured origins may call the API from a browser.

## Tests

```bash
dotnet test back/FullRes.Tests/FullRes.Tests.csproj
```

The suite covers header inspection, upload preparation (including the re-encode path), result processing, the SSRF guard, the SerpApi provider (with a stubbed HTTP handler — including quota vs. rate-limit, `search_metadata.status = Error`, `Retry-After` and retry behaviour), the circuit breaker, and the search endpoint end to end via `WebApplicationFactory` (error contract, `Retry-After`, `traceId`, rate limiting and the upload-size guard).

## Project layout

```text
FullRes.Api/
├── Controllers/ImagesSearchController.cs   # POST /api/images/search
├── Dtos/SearchDtos.cs                      # response contract
├── Models/SearchModels.cs                  # PreparedImage, SearchCandidate, filters
├── Options/FullResOptions.cs               # strongly-typed configuration
├── Providers/
│   ├── IReverseImageSearchProvider.cs      # add new providers here
│   ├── ProviderExceptions.cs               # ProviderErrorKind + exceptions
│   ├── ProviderCircuitBreaker.cs           # transient-failure / quota cooldown
│   └── SerpApi/SerpApiGoogleLensProvider.cs
├── Security/SsrfGuard.cs
├── Services/
│   ├── ImageFormatInspector.cs             # header-only format/dimension detection
│   ├── UploadedImagePreparationService.cs  # validate + re-encode (SkiaSharp)
│   ├── ImageProbeService.cs                # verify results against the source
│   └── SearchResultProcessor.cs            # dedupe, filter, sort
├── Errors/ApiProblem.cs                    # ProblemDetails helper
└── Program.cs
```

## Adding another provider

Implement `IReverseImageSearchProvider` (return a list of `SearchCandidate`), register it in `Program.cs`, and the rest of the pipeline — deduplication, probing, filtering, sorting and warnings — keeps working unchanged.

## License

The source code in this repository has no license file yet; add one before relying on it. Note that this only governs FullRes's own code, not the third-party images it helps you find.
