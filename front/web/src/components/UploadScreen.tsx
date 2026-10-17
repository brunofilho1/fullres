import { useCallback, useEffect, useRef, useState } from 'react'
import {
  CircleAlert,
  ClipboardPaste,
  FileImage,
  Link2,
  ScanSearch,
  Upload,
} from 'lucide-react'
import type { ImageMeta } from '../lib/types'
import { formatBytes } from '../lib/format'
import { useI18n } from '../lib/i18n'
import type { MessageKey } from '../lib/i18n/messages/en'

const MAX_BYTES = 20 * 1024 * 1024
const ALLOWED = ['image/jpeg', 'image/png', 'image/webp']

const HOW_STEPS: { n: string; title: MessageKey; desc: MessageKey }[] = [
  { n: '01', title: 'upload.howStep1Title', desc: 'upload.howStep1Desc' },
  { n: '02', title: 'upload.howStep2Title', desc: 'upload.howStep2Desc' },
  { n: '03', title: 'upload.howStep3Title', desc: 'upload.howStep3Desc' },
]

function extOf(nameOrUrl: string): string {
  const clean = nameOrUrl.split(/[?#]/)[0]
  const ext = clean.split('.').pop() ?? ''
  return ext.toUpperCase().slice(0, 5) || 'IMG'
}

function extensionFor(mime: string): string {
  if (mime === 'image/png') return 'png'
  if (mime === 'image/webp') return 'webp'
  return 'jpg'
}

function isAllowedType(type: string, name: string): boolean {
  if (ALLOWED.includes(type)) return true
  return type === '' && /\.(jpe?g|png|webp)$/i.test(name)
}

function loadMeta(file: File): Promise<ImageMeta> {
  return new Promise<ImageMeta>((resolve, reject) => {
    const src = URL.createObjectURL(file)
    const img = new Image()
    img.onload = () =>
      resolve({
        src,
        file,
        name: file.name,
        width: img.naturalWidth,
        height: img.naturalHeight,
        format: extOf(file.name),
        bytes: file.size,
      })
    img.onerror = () => {
      URL.revokeObjectURL(src)
      reject(new Error('Could not read that image.'))
    }
    img.src = src
  })
}

interface UploadScreenProps {
  onSubmit: (image: ImageMeta) => void
}

export function UploadScreen({ onSubmit }: UploadScreenProps) {
  const { t } = useI18n()
  const inputRef = useRef<HTMLInputElement>(null)
  const [staged, setStaged] = useState<ImageMeta | null>(null)
  const [url, setUrl] = useState('')
  const [dragging, setDragging] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const acceptFile = useCallback(
    async (file: File) => {
      setError(null)
      if (!isAllowedType(file.type, file.name)) {
        setError(t('upload.errorFormat'))
        return
      }
      if (file.size > MAX_BYTES) {
        setError(t('upload.errorTooLarge'))
        return
      }
      try {
        const meta = await loadMeta(file)
        setStaged(meta)
      } catch {
        setError(t('upload.errorCorrupt'))
      }
    },
    [t],
  )

  useEffect(() => {
    const onPaste = (event: ClipboardEvent) => {
      const file = Array.from(event.clipboardData?.files ?? [])[0]
      if (file?.type.startsWith('image/')) {
        event.preventDefault()
        void acceptFile(file)
      }
    }
    window.addEventListener('paste', onPaste)
    return () => window.removeEventListener('paste', onPaste)
  }, [acceptFile])

  async function acceptUrl() {
    const value = url.trim()
    if (!value) return
    setError(null)

    let parsed: URL
    try {
      parsed = new URL(value)
    } catch {
      setError(t('upload.errorInvalidUrl'))
      return
    }
    if (parsed.protocol !== 'http:' && parsed.protocol !== 'https:') {
      setError(t('upload.errorProtocol'))
      return
    }

    try {
      const response = await fetch(parsed.toString(), { mode: 'cors' })
      if (!response.ok) {
        setError(t('upload.errorHostStatus', { status: response.status }))
        return
      }
      const blob = await response.blob()
      if (blob.size > MAX_BYTES) {
        setError(t('upload.errorImageTooLarge'))
        return
      }
      const name = decodeURIComponent(parsed.pathname.split('/').pop() || 'remote-image')
      const type = blob.type && ALLOWED.includes(blob.type) ? blob.type : ''
      if (!isAllowedType(type, name)) {
        setError(t('upload.errorNotImage'))
        return
      }
      const file = new File([blob], name, { type: type || `image/${extensionFor(type)}` })
      const meta = await loadMeta(file)
      setStaged(meta)
      setUrl('')
    } catch {
      setError(t('upload.errorDownload'))
    }
  }

  return (
    <>
      <section className="hero container container-narrow">
        <span className="eyebrow">{t('upload.eyebrow')}</span>
        <h1>{t('upload.title')}</h1>
        <p className="hero-sub">{t('upload.subtitle')}</p>

        <div className="dropzone-wrap">
          {staged ? (
            <div className="card preview">
              <div className="preview-thumb">
                <img src={staged.src} alt={t('upload.previewAlt')} />
              </div>
              <div>
                <div className="preview-title">{staged.name}</div>
                <p className="muted mono" style={{ fontSize: 12.5 }}>
                  {staged.width} × {staged.height} px
                </p>
                <div className="preview-meta">
                  <span className="pill mono">
                    {staged.width} × {staged.height}
                  </span>
                  <span className="pill mono">{staged.format}</span>
                  <span className="pill mono">{formatBytes(staged.bytes)}</span>
                </div>
                <div className="preview-actions">
                  <button
                    type="button"
                    className="btn btn-primary btn-lg"
                    onClick={() => onSubmit(staged)}
                  >
                    <ScanSearch size={16} />
                    {t('upload.searchButton')}
                  </button>
                  <button
                    type="button"
                    className="btn btn-lg"
                    onClick={() => {
                      setStaged(null)
                      setError(null)
                      if (inputRef.current) inputRef.current.value = ''
                    }}
                  >
                    {t('upload.changeImage')}
                  </button>
                </div>
              </div>
            </div>
          ) : (
            <div
              className={`dropzone${dragging ? ' is-over' : ''}`}
              role="button"
              tabIndex={0}
              aria-label={t('upload.dropzoneAria')}
              onClick={() => inputRef.current?.click()}
              onKeyDown={(e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                  e.preventDefault()
                  inputRef.current?.click()
                }
              }}
              onDragOver={(e) => {
                e.preventDefault()
                setDragging(true)
              }}
              onDragLeave={() => setDragging(false)}
              onDrop={(e) => {
                e.preventDefault()
                setDragging(false)
                const file = e.dataTransfer.files[0]
                if (file) void acceptFile(file)
              }}
            >
              <input
                ref={inputRef}
                type="file"
                accept="image/jpeg,image/png,image/webp"
                onChange={(e) => {
                  const file = e.target.files?.[0]
                  if (file) void acceptFile(file)
                }}
                tabIndex={-1}
                aria-label={t('upload.chooseImage')}
              />
              <span className="dropzone-icon" aria-hidden="true">
                {dragging ? <FileImage size={20} /> : <Upload size={20} />}
              </span>
              <span className="dropzone-title">{t('upload.dropTitle')}</span>
              <span className="dropzone-sub">{t('upload.dropSub')}</span>
              <span className="dropzone-hint">{t('upload.dropHint')}</span>
            </div>
          )}
        </div>

        {!staged && (
          <>
            <div className="divider">{t('upload.or')}</div>
            <form
              className="url-row"
              onSubmit={(e) => {
                e.preventDefault()
                void acceptUrl()
              }}
            >
              <span className="input-wrap">
                <Link2 size={15} className="input-icon" />
                <input
                  className="input"
                  style={{ paddingLeft: 34 }}
                  type="url"
                  placeholder={t('upload.urlPlaceholder')}
                  value={url}
                  onChange={(e) => setUrl(e.target.value)}
                  aria-label={t('upload.imageUrl')}
                />
              </span>
              <button type="submit" className="btn">
                {t('upload.useUrl')}
              </button>
            </form>
            <p className="trust-note">
              <ClipboardPaste size={14} />
              {t('upload.pasteNote')}
            </p>
          </>
        )}

        {error && (
          <div className="form-error" role="alert">
            <CircleAlert size={16} style={{ flex: 'none', marginTop: 1 }} />
            <span>{error}</span>
          </div>
        )}

        <p className="trust-note">{t('upload.trustNote')}</p>
      </section>

      <section className="how container" id="how">
        <div className="how-head">
          <span className="eyebrow">{t('upload.howEyebrow')}</span>
        </div>
        <div className="how-grid">
          {HOW_STEPS.map((step) => (
            <div className="how-item" key={step.n}>
              <div className="how-num">{step.n}</div>
              <h3>{t(step.title)}</h3>
              <p>{t(step.desc)}</p>
            </div>
          ))}
        </div>
        <p className="disclaimer" id="providers">
          {t('upload.howDisclaimer')}
        </p>
      </section>
    </>
  )
}
