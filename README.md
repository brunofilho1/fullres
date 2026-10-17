# FullRes

**Find the original. Get the best resolution.**

FullRes is an open-source image search tool: upload an image and compare larger versions, original sources and better quality found across the web. The focus is on the image you sent and the results that were found — fast, simple and transparent, with no account required.

> The repository is organized as a monorepo: the web app lives in [`front/web/`](front/web) and the ASP.NET Core API that talks to the reverse image search provider lives in [`back/`](back). To run a real search you need a [SerpApi](https://serpapi.com) API key — see [`back/README.md`](back/README.md).

## Screenshots

![FullRes — upload screen](https://github.com/user-attachments/assets/0592fad0-214d-422d-a10e-1a724e6cd6c1)

![FullRes — results screen](https://github.com/user-attachments/assets/03492d78-64d5-4853-9975-a452359aa965)

## Screens

### 1. Upload

- Drag & drop, browse from the device, paste from the clipboard (`Ctrl+V`) or use a remote image URL.
- Accepts JPG, PNG and WEBP up to 20 MB, with clear error messages for unsupported formats, oversized files and URLs that block hotlinking.
- Preview of the chosen image with dimensions, format and file size, plus the option to change it before searching.

### 2. Search progress

- Two honest steps — read the image locally (dimensions, format, size) and search the provider indexes — plus an elapsed-time counter.
- No artificial progress bar that fills for no reason; each step represents real work.

### 3. Results

- **Your original** panel with dimensions, pixel count, quality label, format, ratio and file size.
- **Filters** — minimum resolution, aspect ratio, format and minimum visual match (applied in the browser, so they stay instant).
- **Sorting** — highest resolution, highest similarity or lightest file.
- **Match cards** — resolution, quality label, format, file size, source domain and how many times more pixels the found version has. Each result is marked as **verified** (dimensions read directly from the source image) or **reported** (dimensions come from the search index only).
- Highlighted best match, without hiding the alternatives: the largest image is not always the original or the best quality.
- **Comparison modal** — side by side on desktop, toggle between original and found on mobile, with zoom, resolution delta and direct links to open or download the source.

### 4. Result states

States are driven by the real API response, so the product stays honest when a search does not go perfectly:

- **No matches found** — with a suggestion to try a less cropped version.
- **Partial results** — the provider warning list is shown above the results (for example, results that could not be verified or duplicates that were removed).
- **Temporary error** — instead of a raw API message, the UI shows a friendly Brazilian-Portuguese explanation matched to the error `code` (quota exhausted, provider busy, timeout, oversized image, etc.), a **Try again** action, and — for unexpected failures — a short **reference code** (`traceId`) to quote to support. A retry never discards results that were already on screen; a failed retry keeps them visible with an error banner on top.

## Design

Dark graphite + mint.

| Token      | Value     |
| ---------- | --------- |
| Background | `#101113` |
| Surface    | `#191A1E` |
| Accent     | `#B8F3D0` |
| Text       | `#F4F4F5` |
| Muted      | `#71717A` |

- **Typography:** Geist, with Inter and system fonts as fallback.
- **Icons:** [Lucide](https://lucide.dev).
- **Borders:** subtle, with 8–12 px radii.
- **Motion:** limited to loading a file, showing results and comparing images.
- **Responsive:** on mobile the results become vertical cards and the comparison switches between original and found.
- **Accessibility:** keyboard support, visible focus states and an appropriate contrast ratio.

No exaggerated gradients, no glass everywhere and no wall of cards on the home page — the difference is the search experience.

## Stack

Frontend (`front/web/`):

- [React 19](https://react.dev) + [TypeScript](https://www.typescriptlang.org)
- [Vite](https://vite.dev)
- [Vitest](https://vitest.dev) for tests
- [Oxlint](https://oxc.rs)
- [pnpm](https://pnpm.io)

Backend (`back/`):

- [.NET 8 (ASP.NET Core)](https://dotnet.microsoft.com) Web API
- [SkiaSharp](https://github.com/mono/SkiaSharp) for image re-encoding
- [xUnit](https://xunit.net) for tests
- Reverse image search via [SerpApi](https://serpapi.com) (Google Lens)

## Running it

### 1. Backend

```bash
cd back
cp .env.example .env          # then set FullRes__SerpApi__ApiKey=<your key>
dotnet run --project FullRes.Api
```

The API starts on `http://localhost:5080` (see `back/FullRes.Api/Properties/launchSettings.json`). It boots without an API key, but searches return `503 provider_not_configured` until one is set. Full setup, endpoints and error codes are documented in [`back/README.md`](back/README.md).

### 2. Frontend

```bash
cd front/web   # all frontend commands run from here
pnpm install
pnpm dev      # start the dev server
pnpm build    # type-check + production build
pnpm test     # run unit tests (Vitest)
pnpm lint     # oxlint
pnpm preview  # preview the production build
```

The web app calls `http://localhost:5080` by default; override it with `VITE_API_BASE_URL` (see `front/web/.env.example`).

### 3. Tests

```bash
dotnet test back/FullRes.Tests/FullRes.Tests.csproj   # backend (xUnit)
cd front/web && pnpm test                             # frontend (Vitest)
```

## Repository layout

```text
FullRes/
├── front/
│   └── web/            # React + TypeScript app (Vite, Vitest, Oxlint, pnpm)
│       ├── src/
│       ├── public/
│       ├── package.json
│       ├── pnpm-lock.yaml
│       └── .env.example
│
├── back/
│   ├── FullRes.Api/            # ASP.NET Core Web API (search pipeline)
│   │   ├── Controllers/
│   │   ├── Services/
│   │   ├── Providers/
│   │   ├── Security/
│   │   └── Program.cs
│   ├── FullRes.Tests/          # xUnit test suite
│   ├── .env.example
│   └── README.md               # API setup, endpoints and error codes
│
├── .gitignore
└── README.md
```

## Frontend structure

```text
front/web/
├── index.html
├── vite.config.ts
├── tsconfig*.json
├── .oxlintrc.json
├── src/
│   ├── App.tsx                    # flow: upload → searching → results
│   ├── index.css                  # design tokens and all styles
│   ├── components/
│   │   ├── Header.tsx             # brand, open-source badge, GitHub link
│   │   ├── UploadScreen.tsx       # hero, dropzone, URL input, preview
│   │   ├── SearchProgress.tsx     # two-step search progress with elapsed time
│   │   ├── ResultsScreen.tsx      # results layout, filters, sorting, states
│   │   ├── ResultCard.tsx         # a single match with verified/reported badge
│   │   ├── CompareModal.tsx       # side-by-side comparison with zoom
│   │   └── Thumb.tsx              # image with graceful fallback
│   └── lib/
│       ├── types.ts               # ImageMeta, Match, SortKey and API types
│       ├── api.ts                 # searchImages() client (timeout watchdog + traceId)
│       ├── errors.ts              # maps error codes to pt-BR messages
│       ├── searchController.ts    # search state machine (stale/duplicate guards)
│       ├── format.ts              # pixels, resolution labels, ratios, file sizes
│       └── *.test.ts              # Vitest unit tests
└── public/
    └── favicon.svg
```

## The important detail

The interface is relatively simple; the real challenge is finding the images. There is no universal mechanism that guarantees the highest resolution available on the internet, so FullRes combines reverse image search indexes within their API limits, then verifies, de-duplicates and ranks the results.

Two rules keep the results trustworthy: **never invent data** (unknown dimensions, formats, sizes and similarity scores are returned as `null` and shown as `—` or omitted) and **say what was checked** (each result is marked as verified or reported, and every failed check becomes a visible warning). The largest image is never claimed to be the original — it is only the largest match that was found.

**New search providers are welcome.** Contributing one means implementing `IReverseImageSearchProvider` in [`back/FullRes.Api/Providers`](back/FullRes.Api/Providers) and returning a list of candidate images; verifying, de-duplicating and ranking are handled by the pipeline.

## Author

Built by [brunofilho1](https://github.com/brunofilho1) — [github.com/brunofilho1/fullres](https://github.com/brunofilho1/fullres).
