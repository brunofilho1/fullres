import { Download, ExternalLink, GitCompareArrows } from 'lucide-react'
import type { ImageMeta, Match } from '../lib/types'
import { aspectRatio, formatBytes, pixels } from '../lib/format'
import { useI18n } from '../lib/i18n'
import { Thumb } from './Thumb'

interface ResultCardProps {
  match: Match
  original: ImageMeta
  isBest: boolean
  onCompare: (match: Match) => void
}

export function ResultCard({ match, original, isBest, onCompare }: ResultCardProps) {
  const { t, resolutionLabel, pixelGain, matchTitle } = useI18n()
  const title = matchTitle(match.title)
  const originalPixels = pixels(original.width, original.height)
  const matchPixels = pixels(match.width, match.height)
  const hasDimensions = match.width !== null && match.height !== null
  const gain = matchPixels / Math.max(originalPixels, 1)
  const bigger = gain > 1
  const sourceUrl = match.pageUrl ?? match.url
  const initial = (match.domain ?? '?').replace(/^www\./, '').charAt(0).toUpperCase()
  const dimensions = hasDimensions ? `${match.width} × ${match.height}` : t('card.dimensionsUnknown')

  return (
    <article className={`card result-card${isBest ? ' is-best' : ''}`}>
      <button
        type="button"
        className="result-media"
        onClick={() => onCompare(match)}
        aria-label={t('card.compareAria', { title })}
      >
        <Thumb src={match.thumb} alt={title} />
        <span className={`media-badge${isBest ? ' is-best' : ''}`}>
          {isBest ? t('card.bestResolution') : resolutionLabel(match.width, match.height)}
        </span>
      </button>

      <div className="result-body">
        <div className="result-source">
          <span className="favicon" aria-hidden="true">
            {initial}
          </span>
          <span className="mono">{match.domain ?? t('card.unknownSource')}</span>
        </div>

        <h3 className="result-title">
          <a href={sourceUrl} target="_blank" rel="noreferrer" className="link">
            {title}
          </a>
        </h3>

        {match.similarity !== null && (
          <div className="similarity">
            <span>{t('card.visualMatch')}</span>
            <span className="sim-bar" aria-hidden="true">
              <span className="sim-fill" style={{ width: `${match.similarity}%` }} />
            </span>
            <span className="mono">{match.similarity}%</span>
          </div>
        )}

        <div className="result-stats">
          <span className="res-main">{dimensions}</span>
          {hasDimensions && (
            <span className="res-tag">{resolutionLabel(match.width, match.height)}</span>
          )}
          <span className="res-tag">{match.format ?? '—'}</span>
          <span
            className="res-tag"
            title={match.verified ? t('card.verifiedTitle') : t('card.reportedTitle')}
          >
            {match.verified ? t('card.verified') : t('card.reported')}
          </span>
          <span className="res-detail">
            {aspectRatio(match.width, match.height)} · {formatBytes(match.bytes)}
          </span>
          {hasDimensions && (
            <span className={`gain${bigger ? '' : ' gain-down'}`}>
              {pixelGain(originalPixels, matchPixels)}
            </span>
          )}
        </div>

        <div className="result-actions">
          <a className="btn" href={sourceUrl} target="_blank" rel="noreferrer">
            <ExternalLink size={15} />
            {t('card.viewSource')}
          </a>
          <a className="btn btn-ghost" href={match.url} download>
            <Download size={15} />
            {t('card.downloadImage')}
          </a>
          <button type="button" className="btn btn-ghost" onClick={() => onCompare(match)}>
            <GitCompareArrows size={15} />
            {t('card.compareWithMine')}
          </button>
        </div>
      </div>
    </article>
  )
}
