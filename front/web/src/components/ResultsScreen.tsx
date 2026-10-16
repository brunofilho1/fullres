import { useMemo, useState } from 'react'
import {
  ArrowLeft,
  ArrowUpDown,
  CircleAlert,
  RotateCcw,
  ScanSearch,
  SearchX,
  SlidersHorizontal,
  TriangleAlert,
} from 'lucide-react'
import type { ImageMeta, Match, SearchState, SortKey } from '../lib/types'
import { aspectRatio, formatBytes, pixels, ratioGroup, resolutionLabel } from '../lib/format'
import { ResultCard } from './ResultCard'
import { CompareModal } from './CompareModal'
import { Thumb } from './Thumb'

interface ResultsScreenProps {
  image: ImageMeta
  matches: Match[]
  onRestart: () => void
}

const MIN_RES_OPTIONS = [
  { value: 0, label: 'Any resolution' },
  { value: 1920, label: '1920px +' },
  { value: 2560, label: '2560px +' },
  { value: 3840, label: '3840px (4K) +' },
]

const RATIO_OPTIONS = [
  { value: 'any', label: 'Any ratio' },
  { value: '16:9', label: '16:9' },
  { value: '3:2', label: '3:2' },
  { value: '4:3', label: '4:3' },
  { value: '1:1', label: '1:1' },
]

const SORT_LABELS: Record<SortKey, string> = {
  resolution: 'Sorted by resolution',
  similarity: 'Sorted by similarity',
  size: 'Sorted by lightest file',
}

export function ResultsScreen({ image, matches, onRestart }: ResultsScreenProps) {
  const [state, setState] = useState<SearchState>('results')
  const [sort, setSort] = useState<SortKey>('resolution')
  const [minRes, setMinRes] = useState(0)
  const [format, setFormat] = useState('any')
  const [ratio, setRatio] = useState('any')
  const [minSimilarity, setMinSimilarity] = useState(0)
  const [comparing, setComparing] = useState<Match | null>(null)

  const bestId = useMemo(() => {
    let best: Match | null = null
    for (const match of matches) {
      if (!best || pixels(match.width, match.height) > pixels(best.width, best.height)) {
        best = match
      }
    }
    return best?.id
  }, [matches])

  const visible = useMemo(() => {
    const filterFormat = format.toLowerCase()
    const list = matches.filter((match) => {
      if (Math.min(match.width, match.height) < minRes) return false
      if (filterFormat !== 'any' && match.format.toLowerCase() !== filterFormat) return false
      if (ratio !== 'any' && ratioGroup(match.width, match.height) !== ratio) return false
      if (match.similarity < minSimilarity) return false
      return true
    })
    return list.sort((a, b) => {
      if (sort === 'similarity') return b.similarity - a.similarity
      if (sort === 'size') return a.bytes - b.bytes
      return pixels(b.width, b.height) - pixels(a.width, a.height)
    })
  }, [matches, format, minRes, ratio, minSimilarity, sort])

  const filtersActive =
    minRes !== 0 || format !== 'any' || ratio !== 'any' || minSimilarity !== 0

  function resetFilters() {
    setMinRes(0)
    setFormat('any')
    setRatio('any')
    setMinSimilarity(0)
  }

  return (
    <section className="results container">
      <div className="results-top">
        <div>
          <button type="button" className="btn btn-ghost" onClick={onRestart}>
            <ArrowLeft size={15} />
            New search
          </button>
          <h1 className="results-title">Search results</h1>
          <p className="results-sub">
            {state === 'empty'
              ? 'No matches found'
              : state === 'error'
                ? 'Search interrupted'
                : `${matches.length} possible matches · ${SORT_LABELS[sort]}`}
          </p>
        </div>
        <div className="results-toolbar">
          <span className="demo">
            Demo state
            <select
              value={state}
              onChange={(e) => setState(e.target.value as SearchState)}
              aria-label="Demo state"
            >
              <option value="results">Results</option>
              <option value="partial">Partial results</option>
              <option value="empty">No matches</option>
              <option value="error">Temporary error</option>
            </select>
          </span>
          <label className="filter">
            <ArrowUpDown size={14} />
            <select
              value={sort}
              onChange={(e) => setSort(e.target.value as SortKey)}
              aria-label="Sort results"
            >
              <option value="resolution">Highest resolution</option>
              <option value="similarity">Highest similarity</option>
              <option value="size">Lightest file</option>
            </select>
          </label>
        </div>
      </div>

      {state === 'error' ? (
        <div className="state is-error">
          <span className="state-icon" aria-hidden="true">
            <CircleAlert size={22} />
          </span>
          <h3>Something went wrong</h3>
          <p>
            One of the search providers returned an unexpected response and the request was
            interrupted. Your image was not uploaded anywhere.
          </p>
          <div className="state-actions">
            <button type="button" className="btn btn-primary" onClick={() => setState('results')}>
              <RotateCcw size={15} />
              Try again
            </button>
            <button type="button" className="btn btn-ghost" onClick={onRestart}>
              Use another image
            </button>
          </div>
        </div>
      ) : (
        <div className="results-grid">
          <aside className="card original-panel">
            <span className="eyebrow">Your original</span>
            <div className="original-img">
              <Thumb src={image.src} alt="Your uploaded image" />
            </div>
            <div>
              <div className="original-name" title={image.name}>
                {image.name}
              </div>
              <dl className="spec-list">
                <div className="spec-row">
                  <dt>Dimensions</dt>
                  <dd>
                    {image.width} × {image.height}
                  </dd>
                </div>
                <div className="spec-row">
                  <dt>Pixel count</dt>
                  <dd>{pixels(image.width, image.height).toLocaleString('en-US')}</dd>
                </div>
                <div className="spec-row">
                  <dt>Quality</dt>
                  <dd>{resolutionLabel(image.width, image.height)}</dd>
                </div>
                <div className="spec-row">
                  <dt>Format</dt>
                  <dd>{image.format}</dd>
                </div>
                <div className="spec-row">
                  <dt>Ratio</dt>
                  <dd>{aspectRatio(image.width, image.height)}</dd>
                </div>
                <div className="spec-row">
                  <dt>File size</dt>
                  <dd>{formatBytes(image.bytes)}</dd>
                </div>
              </dl>
            </div>
          </aside>

          <div>
            {state === 'partial' && (
              <div className="banner" role="status">
                <TriangleAlert size={16} style={{ flex: 'none', marginTop: 2 }} />
                <div>
                  <strong>Partial results</strong>
                  <p>
                    Two sources could not be queried (rate limit). Results below only cover the
                    providers that answered — higher resolutions may still exist elsewhere.
                  </p>
                </div>
              </div>
            )}

            <div className="filters">
              <span className="filter" aria-hidden="true">
                <SlidersHorizontal size={14} />
                Filters
              </span>
              <label className="filter">
                <select
                  value={minRes}
                  onChange={(e) => setMinRes(Number(e.target.value))}
                  aria-label="Minimum resolution"
                >
                  {MIN_RES_OPTIONS.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </label>
              <label className="filter">
                <select
                  value={format}
                  onChange={(e) => setFormat(e.target.value)}
                  aria-label="Format"
                >
                  <option value="any">Any format</option>
                  <option value="JPG">JPG</option>
                  <option value="PNG">PNG</option>
                  <option value="WEBP">WEBP</option>
                </select>
              </label>
              <label className="filter">
                <select value={ratio} onChange={(e) => setRatio(e.target.value)} aria-label="Ratio">
                  {RATIO_OPTIONS.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </label>
              <label className="filter filter-slider">
                Visual match
                <input
                  type="range"
                  min={0}
                  max={100}
                  step={5}
                  value={minSimilarity}
                  onChange={(e) => setMinSimilarity(Number(e.target.value))}
                  aria-label="Minimum visual match"
                />
                <span className="filter-value">{minSimilarity}%</span>
              </label>
              <span className="results-count">
                {visible.length} of {matches.length}
              </span>
            </div>

            {state === 'empty' ? (
              <div className="state card">
                <span className="state-icon" aria-hidden="true">
                  <SearchX size={22} />
                </span>
                <h3>No matches found</h3>
                <p>
                  We could not find another copy of this image. It may be very recent, cropped or
                  never published. Try a less cropped version or search by the image URL.
                </p>
                <div className="state-actions">
                  <button type="button" className="btn btn-primary" onClick={onRestart}>
                    <ScanSearch size={15} />
                    Try another image
                  </button>
                  <button type="button" className="btn btn-ghost" onClick={() => setState('results')}>
                    Back to demo results
                  </button>
                </div>
              </div>
            ) : visible.length === 0 ? (
              <div className="state card">
                <span className="state-icon" aria-hidden="true">
                  <SlidersHorizontal size={22} />
                </span>
                <h3>No matches meet your filters</h3>
                <p>
                  {matches.length} matches were found, but none of them match the current filter
                  combination.
                </p>
                <div className="state-actions">
                  <button type="button" className="btn" onClick={resetFilters}>
                    <RotateCcw size={15} />
                    Clear filters
                  </button>
                </div>
              </div>
            ) : (
              <div className="result-list">
                {visible.map((match) => (
                  <ResultCard
                    key={match.id}
                    match={match}
                    original={image}
                    isBest={match.id === bestId}
                    onCompare={setComparing}
                  />
                ))}
              </div>
            )}

            {filtersActive && state !== 'empty' && visible.length > 0 && (
              <p className="disclaimer">
                The largest image is not always the original or the best quality — check the source
                before downloading.
              </p>
            )}
            <p className="disclaimer">
              Illustrative results — example images and resolutions, not actual search findings.
            </p>
          </div>
        </div>
      )}

      {comparing && (
        <CompareModal original={image} match={comparing} onClose={() => setComparing(null)} />
      )}
    </section>
  )
}
