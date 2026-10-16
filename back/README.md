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

## Configuration

| Key | Default | Description |
| --- | --- | --- |
| `FullRes:MaxUploadBytes` | `20971520` (20 MB) | Maximum accepted upload size. |
| `FullRes:MaxImagePixels` | `30000000` | Maximum decoded pixel count (compression-bomb guard). |
| `FullRes:ProviderMaxImageBytes` | `512000` | Target byte size when re-encoding for the provider. |
| `FullRes:ProviderTimeoutSeconds` | `20` | Timeout for provider upload + search calls. |
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

Errors use [RFC 7807 ProblemDetails](https://datatracker.ietf.org/doc/html/rfc7807) with an extra `code` field:

| HTTP | `code` | Meaning |
| --- | --- | --- |
| 400 | `file_missing` | No `image` field in the request. |
| 400 | `file_empty` | The uploaded file is empty. |
| 400 | `invalid_request` | Invalid query parameters. |
| 413 | `file_too_large` | Upload exceeds `MaxUploadBytes`. |
| 415 | `format_unsupported` | Not a supported image format. |
| 415 | `file_invalid` | Content is not a valid image. |
| 422 | `image_dimensions_too_large` | Too many pixels to process. |
| 422 | `image_cannot_be_prepared` | Could not shrink/re-encode below the provider limit. |
| 422 | `provider_rejected_image` | The provider refused the image. |
| 429 | `rate_limited` | Per-IP rate limit exceeded. |
| 502 | `provider_credentials_invalid` | Provider rejected the API key. |
| 502 | `provider_invalid_response` | Provider returned an unexpected payload. |
| 502 | `provider_unavailable` | Provider could not be reached. |
| 503 | `provider_not_configured` | No `FullRes:SerpApi:ApiKey` configured. |
| 503 | `provider_rate_limited` | Provider quota/rate limit reached. |
| 504 | `provider_timeout` | Provider timed out. |
| 500 | `unexpected_error` | Unhandled server error. |

## How a search is processed

1. **Validate & prepare** — the upload is validated by inspecting its header bytes (`ImageFormatInspector`), not by decoding untrusted content. Oversized files are decoded with SkiaSharp, alpha is flattened to white, and the image is re-encoded to JPEG (quality ladder 85 → 45, downscaling to 0.7× as needed) until it fits the provider limit.
2. **Provider query** — the prepared image is uploaded and searched through the configured `IReverseImageSearchProvider` (Google Lens via SerpApi).
3. **De-duplicate** — results pointing at the same image URL are collapsed before any network probing.
4. **Probe** — for each remaining result (up to `Probe:MaxResults`), FullRes reads the source image headers to confirm real dimensions/format/size. Probes go through the SSRF guard and any failure simply leaves the value unverified.
5. **Filter, sort & rank** — filters and sorting are applied; results are returned with honest `warnings` describing anything that could not be verified.

## Security

- **SSRF guard** (`Security/SsrfGuard.cs`) — remote fetches resolve DNS and connect directly to the validated IP (no rebinding gap), allow only `http`/`https` on ports 80/443, block loopback/private/link-local/CGNAT/metadata ranges, and re-validate every redirect.
- **Untrusted uploads are never decoded blindly** — format detection reads headers first and a pixel cap blocks compression bombs.
- **Rate limiting** — a per-IP fixed-window limiter protects the provider quota.
- **CORS** — only the configured origins may call the API from a browser.

## Tests

```bash
dotnet test back/FullRes.Tests/FullRes.Tests.csproj
```

The suite covers header inspection, upload preparation (including the re-encode path), result processing, the SSRF guard, the SerpApi provider (with a stubbed HTTP handler) and the search endpoint end to end via `WebApplicationFactory`.

## Project layout

```text
FullRes.Api/
├── Controllers/ImagesSearchController.cs   # POST /api/images/search
├── Dtos/SearchDtos.cs                      # response contract
├── Models/SearchModels.cs                  # PreparedImage, SearchCandidate, filters
├── Options/FullResOptions.cs               # strongly-typed configuration
├── Providers/
│   ├── IReverseImageSearchProvider.cs      # add new providers here
│   ├── ProviderExceptions.cs
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
