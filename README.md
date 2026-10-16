# FullRes

**Find the original. Get the best resolution.**

FullRes is an open-source image search tool: upload an image and compare larger versions, original sources and better quality found across the web. The focus is on the image you sent and the results that were found — fast, simple and transparent, with no account required.

> This repository contains the **front-end interface prototype**. The upload flow, the search progress and the results/comparison screens are implemented; the search results are illustrative mock data and no reverse image search provider is wired up yet. Results are intentionally marked as illustrative in the UI.
>
> The repository is organized as a monorepo: the web app lives in [`front/web/`](front/web) and a future ASP.NET Core backend will live in `back/` (currently empty, reserved).

## Screens

### 1. Upload

- Drag & drop, browse from the device, paste from the clipboard (`Ctrl+V`) or use a remote image URL.
- Accepts JPG, PNG and WEBP up to 20 MB, with clear error messages for unsupported formats, oversized files and URLs that block hotlinking.
- Preview of the chosen image with dimensions, format and file size, plus the option to change it before searching.

### 2. Search progress

- Explicit per-step progress — extract fingerprint, query sources, fetch candidate pages, measure each candidate, rank matches.
- No artificial progress bar that fills for no reason; each step represents real work.

### 3. Results

- **Your original** panel with dimensions, pixel count, quality label, format, ratio and file size.
- **Filters** — minimum resolution, aspect ratio, format and minimum visual match.
- **Sorting** — highest resolution, highest similarity or lightest file.
- **Match cards** — resolution, quality label, format, file size, source domain, similarity meter and how many times more pixels the found version has.
- Highlighted best match, without hiding the alternatives: the largest image is not always the original or the best quality.
- **Comparison modal** — side by side on desktop, toggle between original and found on mobile, with zoom, resolution delta and direct links to open or download the source.

### 4. Result states

Three deliberately designed states, so the product feels mature even when a search does not go perfectly:

- **No matches found** — with a suggestion to try a less cropped version.
- **Partial results** — one or more providers failed (rate limit), so the list is incomplete and says so.
- **Temporary error** — the request was interrupted and can be retried.

You can preview all of them from the **Demo state** selector in the results header.

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
- [Oxlint](https://oxc.rs)
- [pnpm](https://pnpm.io)

Backend (`back/`): reserved for an ASP.NET Core service — not implemented yet.

## Running it

```bash
cd front/web   # all frontend commands run from here
pnpm install
pnpm dev      # start the dev server
pnpm build    # type-check + production build
pnpm lint     # oxlint
pnpm preview  # preview the production build
```

## Repository layout

```text
FullRes/
├── front/
│   └── web/            # React + TypeScript app (Vite, Oxlint, pnpm)
│       ├── src/
│       ├── public/
│       ├── package.json
│       └── pnpm-lock.yaml
│
├── back/               # reserved for the ASP.NET Core backend (empty)
│   └── .gitkeep
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
│   │   ├── SearchProgress.tsx     # per-step search progress
│   │   ├── ResultsScreen.tsx      # results layout, filters, sorting, states
│   │   ├── ResultCard.tsx         # a single match with actions
│   │   ├── CompareModal.tsx       # side-by-side comparison with zoom
│   │   └── Thumb.tsx              # image with graceful fallback
│   └── lib/
│       ├── types.ts               # ImageMeta, Match, SearchState, SortKey
│       ├── format.ts              # pixels, resolution labels, ratios, file sizes
│       └── mock.ts                # illustrative matches
└── public/
    └── favicon.svg
```

## The important detail

The interface is relatively simple; the real challenge is finding the images. There is no universal mechanism that guarantees the highest resolution available on the internet, so FullRes is designed to combine reverse image search sources and image search engines within their API limits, then measure each result's resolution, remove duplicates and rank them by similarity.

**New search providers are welcome.** Contributing one means returning a list of candidate images for a query; measuring, deduplicating and ranking are handled by the app.

## Author

Built by [brunofilho1](https://github.com/brunofilho1) — [github.com/brunofilho1/fullres](https://github.com/brunofilho1/fullres).
