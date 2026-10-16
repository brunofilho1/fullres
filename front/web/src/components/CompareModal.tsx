import { useEffect, useState } from 'react'
import { Download, ExternalLink, X, ZoomIn, ZoomOut } from 'lucide-react'
import type { ImageMeta, Match } from '../lib/types'
import { aspectRatio, formatBytes, pixelGain, pixels, resolutionLabel } from '../lib/format'
import { Thumb } from './Thumb'

interface CompareModalProps {
  original: ImageMeta
  match: Match
  onClose: () => void
}

export function CompareModal({ original, match, onClose }: CompareModalProps) {
  const [zoom, setZoom] = useState(false)
  const [side, setSide] = useState<'original' | 'found'>('found')

  useEffect(() => {
    const onKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape') onClose()
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [onClose])

  const originalPx = pixels(original.width, original.height)
  const matchPx = pixels(match.width, match.height)

  return (
    <div
      className="modal-backdrop"
      onClick={onClose}
      role="presentation"
    >
      <div
        className="modal"
        role="dialog"
        aria-modal="true"
        aria-label="Compare original and found image"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="modal-head">
          <div>
            <div className="modal-title">Comparing versions</div>
            <div className="muted" style={{ fontSize: 12.5 }}>
              {match.domain}
            </div>
          </div>
          <div className="modal-tools">
            <button
              type="button"
              className={`icon-btn${zoom ? ' is-on' : ''}`}
              onClick={() => setZoom((z) => !z)}
              aria-pressed={zoom}
              aria-label={zoom ? 'Zoom out' : 'Zoom in'}
            >
              {zoom ? <ZoomOut size={15} /> : <ZoomIn size={15} />}
            </button>
            <button type="button" className="icon-btn" onClick={onClose} aria-label="Close compare">
              <X size={16} />
            </button>
          </div>
        </div>

        <div className="compare-toggle">
          <button
            type="button"
            className={`btn${side === 'original' ? ' btn-primary' : ''}`}
            onClick={() => setSide('original')}
          >
            Original
          </button>
          <button
            type="button"
            className={`btn${side === 'found' ? ' btn-primary' : ''}`}
            onClick={() => setSide('found')}
          >
            Found
          </button>
        </div>

        <div className="compare-grid">
          <div className="compare-pane" data-hidden={side !== 'original'}>
            <span className="eyebrow">
              <span>Your original</span>
              <span className="mono">
                {original.width} × {original.height}
              </span>
            </span>
            <div className={`compare-frame${zoom ? ' is-zoom' : ''}`}>
              <Thumb src={original.src} alt="Your uploaded image" />
            </div>
            <div className="compare-meta">
              <span>{original.format}</span>
              <span>{formatBytes(original.bytes)}</span>
              <span>{aspectRatio(original.width, original.height)}</span>
              <span>{originalPx.toLocaleString('en-US')} px</span>
            </div>
          </div>

          <div className="compare-pane" data-hidden={side !== 'found'}>
            <span className="eyebrow">
              <span>Found version</span>
              <span className="mono">
                {match.width} × {match.height}
              </span>
            </span>
            <div className={`compare-frame${zoom ? ' is-zoom' : ''}`}>
              <Thumb src={match.thumb} alt={match.title} />
            </div>
            <div className="compare-meta">
              <span>{match.format}</span>
              <span>{formatBytes(match.bytes)}</span>
              <span>{aspectRatio(match.width, match.height)}</span>
              <span>{resolutionLabel(match.width, match.height)}</span>
              <span>{matchPx.toLocaleString('en-US')} px</span>
            </div>
          </div>
        </div>

        <div className="modal-foot">
          <div className="delta">
            {matchPx > originalPx ? 'Resolution gain' : 'Resolution change'}
            <strong>{pixelGain(originalPx, matchPx)}</strong>
            <span className="muted">· {match.similarity}% visual match</span>
          </div>
          <div className="modal-tools">
            <a className="btn btn-ghost" href={match.url} download>
              <Download size={15} />
              Download
            </a>
            <a className="btn btn-primary" href={match.url} target="_blank" rel="noreferrer">
              <ExternalLink size={15} />
              Open source page
            </a>
          </div>
        </div>
      </div>
    </div>
  )
}
