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
  const gain = pixels(match.width, match.height) / Math.max(pixels(original.width, original.height), 1)
  const bigger = gain > 1
  const initial = match.domain.replace(/^www\./, '').charAt(0)

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
          <span className="mono">{match.domain}</span>
          {match.note && <span className="muted">· {match.note}</span>}
        </div>

        <h3 className="result-title">
          <a href={match.url} target="_blank" rel="noreferrer" className="link">
            {match.title}
          </a>
        </h3>

        <div className="similarity">
          <span>Visual match</span>
          <span className="sim-bar" aria-hidden="true">
            <span className="sim-fill" style={{ width: `${match.similarity}%` }} />
          </span>
          <span className="mono">{match.similarity}%</span>
        </div>

        <div className="result-stats">
          <span className="res-main">
            {match.width} × {match.height}
          </span>
          <span className="res-tag">{resolutionLabel(match.width, match.height)}</span>
          <span className="res-tag">{match.format}</span>
          <span className="res-detail">
            {aspectRatio(match.width, match.height)} · {formatBytes(match.bytes)}
          </span>
          <span className={`gain${bigger ? '' : ' gain-down'}`}>{pixelGain(
            pixels(original.width, original.height),
            pixels(match.width, match.height),
          )}</span>
        </div>

        <div className="result-actions">
          <a className="btn" href={match.url} target="_blank" rel="noreferrer">
            <ExternalLink size={15} />
            View original
          </a>
          <a className="btn btn-ghost" href={match.url} download>
            <Download size={15} />
            Download
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
