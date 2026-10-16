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

const MAX_BYTES = 20 * 1024 * 1024
const ALLOWED = ['image/jpeg', 'image/png', 'image/webp']

function extOf(nameOrUrl: string): string {
  const clean = nameOrUrl.split(/[?#]/)[0]
  const ext = clean.split('.').pop() ?? ''
  return ext.toUpperCase().slice(0, 5) || 'IMG'
}

function loadMeta(src: string, name: string, format: string, bytes: number) {
  return new Promise<ImageMeta>((resolve, reject) => {
    const img = new Image()
    img.onload = () =>
      resolve({ src, name, width: img.naturalWidth, height: img.naturalHeight, format, bytes })
    img.onerror = () => reject(new Error('Could not read that image.'))
    img.src = src
  })
}

interface UploadScreenProps {
  onSubmit: (image: ImageMeta) => void
}

export function UploadScreen({ onSubmit }: UploadScreenProps) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [staged, setStaged] = useState<ImageMeta | null>(null)
  const [url, setUrl] = useState('')
  const [dragging, setDragging] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const acceptFile = useCallback(async (file: File) => {
    setError(null)
    if (!ALLOWED.includes(file.type)) {
      setError('Unsupported format. Use a JPG, PNG or WEBP file.')
      return
    }
    if (file.size > MAX_BYTES) {
      setError('That file is larger than 20 MB. Try a smaller copy.')
      return
    }
    try {
      const meta = await loadMeta(
        URL.createObjectURL(file),
        file.name,
        extOf(file.type.split('/')[1] ?? 'img'),
        file.size,
      )
      setStaged(meta)
    } catch {
      setError('Could not read that image. It may be corrupted.')
    }
  }, [])

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
    try {
      new URL(value)
    } catch {
      setError('That does not look like a valid image URL.')
      return
    }
    try {
      const meta = await loadMeta(value, value.split('/').pop() ?? 'remote-image', extOf(value), 0)
      setStaged(meta)
      setUrl('')
    } catch {
      setError('The image could not be loaded from that URL. The host may block hotlinking.')
    }
  }

  return (
    <>
      <section className="hero container container-narrow">
        <span className="eyebrow">Your image, at its best</span>
        <h1>Find the highest-resolution version.</h1>
        <p className="hero-sub">
          Upload an image. Find larger versions, original sources and better quality across the
          web.
        </p>

        <div className="dropzone-wrap">
          {staged ? (
            <div className="card preview">
              <div className="preview-thumb">
                <img src={staged.src} alt="Selected upload preview" />
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
                    Search for higher resolutions
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
                    Change image
                  </button>
                </div>
              </div>
            </div>
          ) : (
            <div
              className={`dropzone${dragging ? ' is-over' : ''}`}
              role="button"
              tabIndex={0}
              aria-label="Drop an image here or browse files"
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
                aria-label="Choose an image"
              />
              <span className="dropzone-icon" aria-hidden="true">
                {dragging ? <FileImage size={20} /> : <Upload size={20} />}
              </span>
              <span className="dropzone-title">Drop your image here</span>
              <span className="dropzone-sub">or browse files from your device</span>
              <span className="dropzone-hint">JPG · PNG · WEBP · Up to 20 MB</span>
            </div>
          )}
        </div>

        {!staged && (
          <>
            <div className="divider">or</div>
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
                  placeholder="Or paste an image URL..."
                  value={url}
                  onChange={(e) => setUrl(e.target.value)}
                  aria-label="Image URL"
                />
              </span>
              <button type="submit" className="btn">
                Use URL
              </button>
            </form>
            <p className="trust-note">
              <ClipboardPaste size={14} />
              You can also paste an image from your clipboard
            </p>
          </>
        )}

        {error && (
          <div className="form-error" role="alert">
            <CircleAlert size={16} style={{ flex: 'none', marginTop: 1 }} />
            <span>{error}</span>
          </div>
        )}

        <p className="trust-note">No account required · Simple and transparent</p>
      </section>

      <section className="how container">
        <div className="how-head">
          <span className="eyebrow">How it works</span>
        </div>
        <div className="how-grid">
          {[
            { n: '01', t: 'Upload', d: 'Choose any image.' },
            { n: '02', t: 'Search', d: 'Find matching versions.' },
            { n: '03', t: 'Recover', d: 'Get the best version.' },
          ].map((step) => (
            <div className="how-item" key={step.n}>
              <div className="how-num">{step.n}</div>
              <h3>{step.t}</h3>
              <p>{step.d}</p>
            </div>
          ))}
        </div>
        <p className="disclaimer">
          Conceptual mockup of the visual direction — not a functional application. Results shown are
          illustrative.
        </p>
      </section>
    </>
  )
}
