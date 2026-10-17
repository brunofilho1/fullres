import { useEffect, useState } from 'react'
import { Download, ExternalLink, X, ZoomIn, ZoomOut } from 'lucide-react'
import type { ImageMeta, Match } from '../lib/types'
import { aspectRatio, formatBytes, pixels } from '../lib/format'
import { useI18n } from '../lib/i18n'
import { Thumb } from './Thumb'

interface CompareModalProps {
  original: ImageMeta
  match: Match
  onClose: () => void
}

export function CompareModal({ original, match, onClose }: CompareModalProps) {
  const { t, formatNumber, resolutionLabel, pixelGain, matchTitle } = useI18n()
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
  const hasDimensions = match.width !== null && match.height !== null
  const sourceUrl = match.pageUrl ?? match.url
  const title = matchTitle(match.title)

  return (
    <div className="modal-backdrop" onClick={onClose} role="presentation">
      <div
        className="modal"
        role="dialog"
        aria-modal="true"
        aria-label={t('compare.dialogAria')}
        onClick={(e) => e.stopPropagation()}
      >
        <div className="modal-head">
          <div>
            <div className="modal-title">{t('compare.title')}</div>
            <div className="muted" style={{ fontSize: 12.5 }}>
              {match.domain ?? t('compare.unknownSource')}
            </div>
          </div>
          <div className="modal-tools">
            <button
              type="button"
              className={`icon-btn${zoom ? ' is-on' : ''}`}
              onClick={() => setZoom((z) => !z)}
              aria-pressed={zoom}
              aria-label={zoom ? t('compare.zoomOut') : t('compare.zoomIn')}
            >
              {zoom ? <ZoomOut size={15} /> : <ZoomIn size={15} />}
            </button>
            <button
              type="button"
              className="icon-btn"
              onClick={onClose}
              aria-label={t('compare.close')}
            >
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
            {t('compare.original')}
          </button>
          <button
            type="button"
            className={`btn${side === 'found' ? ' btn-primary' : ''}`}
            onClick={() => setSide('found')}
          >
            {t('compare.found')}
          </button>
        </div>

        <div className="compare-grid">
          <div className="compare-pane" data-hidden={side !== 'original'}>
            <span className="eyebrow">
              <span>{t('compare.yourOriginal')}</span>
              <span className="mono">
                {original.width} × {original.height}
              </span>
            </span>
            <div className={`compare-frame${zoom ? ' is-zoom' : ''}`}>
              <Thumb src={original.src} alt={t('results.originalAlt')} />
            </div>
            <div className="compare-meta">
              <span>{original.format}</span>
              <span>{formatBytes(original.bytes)}</span>
              <span>{aspectRatio(original.width, original.height)}</span>
              <span>{formatNumber(originalPx)} px</span>
            </div>
          </div>

          <div className="compare-pane" data-hidden={side !== 'found'}>
            <span className="eyebrow">
              <span>{t('compare.foundVersion')}</span>
              <span className="mono">
                {hasDimensions ? `${match.width} × ${match.height}` : t('compare.unknownDimensions')}
              </span>
            </span>
            <div className={`compare-frame${zoom ? ' is-zoom' : ''}`}>
              <Thumb src={match.thumb} alt={title} />
            </div>
            <div className="compare-meta">
              <span>{match.format ?? '—'}</span>
              <span>{formatBytes(match.bytes)}</span>
              <span>{aspectRatio(match.width, match.height)}</span>
              <span>{resolutionLabel(match.width, match.height)}</span>
              <span>{hasDimensions ? `${formatNumber(matchPx)} px` : '— px'}</span>
            </div>
          </div>
        </div>

        <div className="modal-foot">
          <div className="delta">
            {!hasDimensions ? (
              <span className="muted">{t('compare.noDimensions')}</span>
            ) : (
              <>
                {matchPx > originalPx ? t('compare.resolutionGain') : t('compare.resolutionChange')}
                <strong>{pixelGain(originalPx, matchPx)}</strong>
                {match.similarity !== null && (
                  <span className="muted">
                    {t('compare.visualMatchSuffix', { similarity: match.similarity })}
                  </span>
                )}
              </>
            )}
          </div>
          <div className="modal-tools">
            <a className="btn btn-ghost" href={match.url} download>
              <Download size={15} />
              {t('compare.download')}
            </a>
            <a className="btn btn-primary" href={sourceUrl} target="_blank" rel="noreferrer">
              <ExternalLink size={15} />
              {t('compare.openSource')}
            </a>
          </div>
        </div>
      </div>
    </div>
  )
}
