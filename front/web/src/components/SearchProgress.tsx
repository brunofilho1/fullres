import { useEffect, useState } from 'react'
import { Check, LoaderCircle } from 'lucide-react'
import type { ImageMeta } from '../lib/types'

const STEPS = [
  { label: 'Reading your image', desc: 'Dimensions, format and file size were read in your browser.' },
  { label: 'Searching image indexes', desc: 'Uploading your image and ranking the matches returned by the provider.' },
]

interface SearchProgressProps {
  image: ImageMeta
}

export function SearchProgress({ image }: SearchProgressProps) {
  const [elapsed, setElapsed] = useState(0)

  useEffect(() => {
    const timer = window.setInterval(() => setElapsed((value) => value + 1), 1000)
    return () => window.clearInterval(timer)
  }, [])

  return (
    <div className="progress-page">
      <div className="card progress-card">
        <div className="progress-head">
          <img src={image.src} alt="" />
          <div>
            <div className="progress-title">Searching for higher resolutions</div>
            <div className="muted" style={{ fontSize: 13 }}>
              {image.width} × {image.height} px · {image.format}
            </div>
          </div>
        </div>
        <ol className="steps">
          {STEPS.map((step, index) => {
            const done = index === 0
            const active = index === STEPS.length - 1
            return (
              <li
                key={step.label}
                className={`step${done ? ' step-done' : ''}${active ? ' step-active' : ''}`}
              >
                <span className="step-dot" aria-hidden="true">
                  {done ? (
                    <Check size={12} strokeWidth={3} />
                  ) : (
                    <LoaderCircle size={12} className="spin" />
                  )}
                </span>
                <span>
                  <span className="step-label">{step.label}</span>
                  <span className="step-desc" style={{ display: 'block' }}>
                    {step.desc}
                  </span>
                </span>
              </li>
            )
          })}
        </ol>
        <div className="progress-foot">
          <span>Waiting for the search API response…</span>
          <span className="mono">{elapsed}s</span>
        </div>
      </div>
    </div>
  )
}
