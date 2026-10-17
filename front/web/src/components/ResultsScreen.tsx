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
import type {
  ImageMeta,
  Match,
  SearchFailure,
  SearchWarning,
  SortKey,
} from '../lib/types'
import { aspectRatio, formatBytes, pixels, ratioGroup } from '../lib/format'
import { useI18n } from '../lib/i18n'
import type { MessageKey } from '../lib/i18n/messages/en'
import { ResultCard } from './ResultCard'
import { CompareModal } from './CompareModal'
import { Thumb } from './Thumb'

interface ResultsScreenProps {
  image: ImageMeta
  matches: Match[]
  warnings: SearchWarning[]
  providerName: string | null
  failure: SearchFailure | null
  onRetry: () => void
  onRestart: () => void
}

interface Option {
  value: string | number
  message?: MessageKey
  literal?: string
}

const MIN_RES_OPTIONS: Option[] = [
  { value: 0, message: 'filters.anyResolution' },
  { value: 1920, literal: '1920px +' },
  { value: 2560, literal: '2560px +' },
  { value: 3840, literal: '3840px (4K) +' },
]

const RATIO_OPTIONS: Option[] = [
  { value: 'any', message: 'filters.anyRatio' },
  { value: '16:9', literal: '16:9' },
  { value: '3:2', literal: '3:2' },
  { value: '4:3', literal: '4:3' },
  { value: '1:1', literal: '1:1' },
]

const FORMAT_OPTIONS: Option[] = [
  { value: 'any', message: 'filters.anyFormat' },
  { value: 'JPG', literal: 'JPG' },
  { value: 'PNG', literal: 'PNG' },
  { value: 'WEBP', literal: 'WEBP' },
]

const SORT_LABELS: Record<SortKey, MessageKey> = {
  resolution: 'results.sortedByResolution',
  similarity: 'results.sortedBySimilarity',
  size: 'results.sortedBySize',
}

function optionLabel(option: Option, t: ReturnType<typeof useI18n>['t']): string {
  return option.message ? t(option.message) : (option.literal ?? '')
}

export function ResultsScreen({
  image,
  matches,
  warnings,
  providerName,
  failure,
  onRetry,
  onRestart,
}: ResultsScreenProps) {
  const { t, tc, formatNumber, resolutionLabel, warningMessage, errorMessage } = useI18n()
  const [sort, setSort] = useState<SortKey>('resolution')
  const [minRes, setMinRes] = useState(0)
  const [format, setFormat] = useState('any')
  const [ratio, setRatio] = useState('any')
  const [minSimilarity, setMinSimilarity] = useState(0)
  const [comparing, setComparing] = useState<Match | null>(null)

  const hasSimilarity = useMemo(() => matches.some((match) => match.similarity !== null), [matches])

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
      if (minRes > 0 && Math.min(match.width ?? 0, match.height ?? 0) < minRes) return false
      if (filterFormat !== 'any' && match.format?.toLowerCase() !== filterFormat) return false
      if (ratio !== 'any' && ratioGroup(match.width, match.height) !== ratio) return false
      if (match.similarity !== null && match.similarity < minSimilarity) return false
      return true
    })
    return list.sort((a, b) => {
      if (sort === 'similarity') return (b.similarity ?? -1) - (a.similarity ?? -1)
      if (sort === 'size')
        return (a.bytes ?? Number.MAX_SAFE_INTEGER) - (b.bytes ?? Number.MAX_SAFE_INTEGER)
      return pixels(b.width, b.height) - pixels(a.width, a.height)
    })
  }, [matches, format, minRes, ratio, minSimilarity, sort])

  const filtersActive = minRes !== 0 || format !== 'any' || ratio !== 'any' || minSimilarity !== 0

  function resetFilters() {
    setMinRes(0)
    setFormat('any')
    setRatio('any')
    setMinSimilarity(0)
  }

  const hasError = failure !== null
  const state =
    hasError && matches.length === 0 ? 'error' : matches.length === 0 ? 'empty' : 'results'

  return (
    <section className="results container">
      <div className="results-top">
        <div>
          <button type="button" className="btn btn-ghost" onClick={onRestart}>
            <ArrowLeft size={15} />
            {t('results.newSearch')}
          </button>
          <h1 className="results-title">{t('results.title')}</h1>
          <p className="results-sub">
            {state === 'empty'
              ? t('results.subtitleEmpty')
              : state === 'error'
                ? t('results.subtitleError')
                : tc('results.possibleMatches', matches.length, { sort: t(SORT_LABELS[sort]) })}
          </p>
        </div>
        <div className="results-toolbar">
          <label className="filter">
            <ArrowUpDown size={14} />
            <select
              value={sort}
              onChange={(e) => setSort(e.target.value as SortKey)}
              aria-label={t('results.sortAria')}
            >
              <option value="resolution">{t('results.sortResolution')}</option>
              {hasSimilarity && <option value="similarity">{t('results.sortSimilarity')}</option>}
              <option value="size">{t('results.sortSize')}</option>
            </select>
          </label>
        </div>
      </div>

      {state === 'error' ? (
        <div className="state is-error">
          <span className="state-icon" aria-hidden="true">
            <CircleAlert size={22} />
          </span>
          <h3>{t('results.errorTitle')}</h3>
          <p>{errorMessage(failure?.code ?? 'unknown_error')}</p>
          {failure?.reference && (
            <p className="banner-ref">{t('common.reference', { reference: failure.reference })}</p>
          )}
          <div className="state-actions">
            <button type="button" className="btn btn-primary" onClick={onRetry}>
              <RotateCcw size={15} />
              {t('results.tryAgain')}
            </button>
            <button type="button" className="btn btn-ghost" onClick={onRestart}>
              {t('results.useAnotherImage')}
            </button>
          </div>
        </div>
      ) : (
        <div className="results-grid">
          <aside className="card original-panel">
            <span className="eyebrow">{t('results.yourOriginal')}</span>
            <div className="original-img">
              <Thumb src={image.src} alt={t('results.originalAlt')} />
            </div>
            <div>
              <div className="original-name" title={image.name}>
                {image.name}
              </div>
              <dl className="spec-list">
                <div className="spec-row">
                  <dt>{t('results.specDimensions')}</dt>
                  <dd>
                    {image.width} × {image.height}
                  </dd>
                </div>
                <div className="spec-row">
                  <dt>{t('results.specPixelCount')}</dt>
                  <dd>{formatNumber(pixels(image.width, image.height))}</dd>
                </div>
                <div className="spec-row">
                  <dt>{t('results.specQuality')}</dt>
                  <dd>{resolutionLabel(image.width, image.height)}</dd>
                </div>
                <div className="spec-row">
                  <dt>{t('results.specFormat')}</dt>
                  <dd>{image.format}</dd>
                </div>
                <div className="spec-row">
                  <dt>{t('results.specRatio')}</dt>
                  <dd>{aspectRatio(image.width, image.height)}</dd>
                </div>
                <div className="spec-row">
                  <dt>{t('results.specFileSize')}</dt>
                  <dd>{formatBytes(image.bytes)}</dd>
                </div>
              </dl>
            </div>
          </aside>

          <div>
            {hasError && (
              <div className="banner is-error" role="alert">
                <CircleAlert size={16} style={{ flex: 'none', marginTop: 2 }} />
                <div>
                  <strong>{t('results.latestSearchFailed')}</strong>
                  <p>{errorMessage(failure?.code ?? 'unknown_error')}</p>
                  {failure?.reference && (
                    <p className="banner-ref">
                      {t('common.reference', { reference: failure.reference })}
                    </p>
                  )}
                  <div className="state-actions">
                    <button type="button" className="btn btn-primary" onClick={onRetry}>
                      <RotateCcw size={15} />
                      {t('results.tryAgain')}
                    </button>
                  </div>
                </div>
              </div>
            )}

            {state !== 'empty' && warnings.length > 0 && (
              <div className="banner" role="status">
                <TriangleAlert size={16} style={{ flex: 'none', marginTop: 2 }} />
                <div>
                  <strong>{t('results.partialResults')}</strong>
                  {warnings.map((warning) => {
                    const message = warningMessage(warning)
                    return message ? <p key={warning.code}>{message}</p> : null
                  })}
                </div>
              </div>
            )}

            <div className="filters">
              <span className="filter" aria-hidden="true">
                <SlidersHorizontal size={14} />
                {t('results.filters')}
              </span>
              <label className="filter">
                <select
                  value={minRes}
                  onChange={(e) => setMinRes(Number(e.target.value))}
                  aria-label={t('results.minResolutionAria')}
                >
                  {MIN_RES_OPTIONS.map((option) => (
                    <option key={option.value} value={option.value}>
                      {optionLabel(option, t)}
                    </option>
                  ))}
                </select>
              </label>
              <label className="filter">
                <select
                  value={format}
                  onChange={(e) => setFormat(e.target.value)}
                  aria-label={t('results.formatAria')}
                >
                  {FORMAT_OPTIONS.map((option) => (
                    <option key={option.value} value={option.value}>
                      {optionLabel(option, t)}
                    </option>
                  ))}
                </select>
              </label>
              <label className="filter">
                <select
                  value={ratio}
                  onChange={(e) => setRatio(e.target.value)}
                  aria-label={t('results.ratioAria')}
                >
                  {RATIO_OPTIONS.map((option) => (
                    <option key={option.value} value={option.value}>
                      {optionLabel(option, t)}
                    </option>
                  ))}
                </select>
              </label>
              {hasSimilarity && (
                <label className="filter filter-slider">
                  {t('results.visualMatch')}
                  <input
                    type="range"
                    min={0}
                    max={100}
                    step={5}
                    value={minSimilarity}
                    onChange={(e) => setMinSimilarity(Number(e.target.value))}
                    aria-label={t('results.minVisualMatchAria')}
                  />
                  <span className="filter-value">{minSimilarity}%</span>
                </label>
              )}
              <span className="results-count">
                {t('results.countOf', { visible: visible.length, total: matches.length })}
              </span>
            </div>

            {state === 'empty' ? (
              <div className="state card">
                <span className="state-icon" aria-hidden="true">
                  <SearchX size={22} />
                </span>
                <h3>{t('results.emptyTitle')}</h3>
                <p>{t('results.emptyBody')}</p>
                <div className="state-actions">
                  <button type="button" className="btn btn-primary" onClick={onRestart}>
                    <ScanSearch size={15} />
                    {t('results.tryAnotherImage')}
                  </button>
                </div>
              </div>
            ) : visible.length === 0 ? (
              <div className="state card">
                <span className="state-icon" aria-hidden="true">
                  <SlidersHorizontal size={22} />
                </span>
                <h3>{t('results.noFilterMatchTitle')}</h3>
                <p>{tc('results.foundMatches', matches.length)}</p>
                <div className="state-actions">
                  <button type="button" className="btn" onClick={resetFilters}>
                    <RotateCcw size={15} />
                    {t('results.clearFilters')}
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
              <p className="disclaimer">{t('results.largestDisclaimer')}</p>
            )}
            {state !== 'empty' && matches.length > 0 && (
              <p className="disclaimer">
                {providerName ? `${t('results.resultsProvidedBy', { provider: providerName })} ` : ''}
                {t('results.verifiedNote')}
              </p>
            )}
          </div>
        </div>
      )}

      {comparing && (
        <CompareModal original={image} match={comparing} onClose={() => setComparing(null)} />
      )}
    </section>
  )
}
