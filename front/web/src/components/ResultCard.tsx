import { Download, ExternalLink, GitCompareArrows } from 'lucide-react'
import type { ImageMeta, Match } from '../lib/types'
import {
  aspectRatio,
  formatBytes,
  pixelGain,
  pixels,
  resolutionLabel,
} from '../lib/format'
import { Thumb } from './Thumb'

interface ResultCardProps {
  match: Match
  original: ImageMeta
  isBest: boolean
  onCompare: (match: Match) => void
}

export function ResultCard({ match, original, isBest, onCompare }: ResultCardProps) {
  const originalPixels = pixels(original.width, original.height)
  const matchPixels = pixels(match.width, match.height)
  const hasDimensions = match.width !== null && match.height !== null
  const gain = matchPixels / Math.max(originalPixels, 1)
  const bigger = gain > 1
  const sourceUrl = match.pageUrl ?? match.url
  const initial = (match.domain ?? '?').replace(/^www\./, '').charAt(0).toUpperCase()
  const dimensions = hasDimensions ? `${match.width} × ${match.height}` : 'Dimensions unknown'

  return (
    <article className={`card result-card${isBest ? ' is-best' : ''}`}>
      <button
        type="button"
        className="result-media"
        onClick={() => onCompare(match)}
        aria-label={`Compare ${match.title}`}
      >
        <Thumb src={match.thumb} alt={match.title} />
        <span className={`media-badge${isBest ? ' is-best' : ''}`}>
          {isBest ? 'BEST RESOLUTION' : resolutionLabel(match.width, match.height)}
        </span>
      </button>

      <div className="result-body">
        <div className="result-source">
          <span className="favicon" aria-hidden="true">
            {initial}
          </span>
          <span className="mono">{match.domain ?? 'unknown source'}</span>
        </div>

        <h3 className="result-title">
          <a href={sourceUrl} target="_blank" rel="noreferrer" className="link">
            {match.title}
          </a>
        </h3>

        {match.similarity !== null && (
          <div className="similarity">
            <span>Visual match</span>
            <span className="sim-bar" aria-hidden="true">
              <span className="sim-fill" style={{ width: `${match.similarity}%` }} />
            </span>
            <span className="mono">{match.similarity}%</span>
          </div>
        )}

        <div className="result-stats">
          <span className="res-main">{dimensions}</span>
          {hasDimensions && <span className="res-tag">{resolutionLabel(match.width, match.height)}</span>}
          <span className="res-tag">{match.format ?? '—'}</span>
          <span className="res-tag" title={match.verified ? 'Read directly from the source image' : 'Reported by the search index, not verified'}>
            {match.verified ? 'verified' : 'reported'}
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
            View source page
          </a>
          <a className="btn btn-ghost" href={match.url} download>
            <Download size={15} />
            Download image
          </a>
          <button type="button" className="btn btn-ghost" onClick={() => onCompare(match)}>
            <GitCompareArrows size={15} />
            Compare with mine
          </button>
        </div>
      </div>
    </article>
  )
}
