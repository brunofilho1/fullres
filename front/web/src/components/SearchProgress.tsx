import { useEffect, useState } from 'react'
import { Check, LoaderCircle } from 'lucide-react'
import type { ImageMeta } from '../lib/types'

const STEPS = [
  { label: 'Reading your image', desc: 'Extracting dimensions, format and a visual fingerprint.' },
  { label: 'Querying search sources', desc: 'Asking reverse image engines in parallel.' },
  { label: 'Fetching candidate pages', desc: 'Loading the pages that host matching files.' },
  { label: 'Measuring each candidate', desc: 'Reading real dimensions and file sizes.' },
  { label: 'Ranking matches', desc: 'Removing duplicates and sorting by resolution.' },
]

interface SearchProgressProps {
  image: ImageMeta
  onDone: () => void
}

export function SearchProgress({ image, onDone }: SearchProgressProps) {
  const [current, setCurrent] = useState(0)

  useEffect(() => {
    if (current >= STEPS.length) {
      const done = window.setTimeout(onDone, 420)
      return () => window.clearTimeout(done)
    }
    const delay = 620 + Math.round(Math.random() * 260)
    const timer = window.setTimeout(() => setCurrent((c) => c + 1), delay)
    return () => window.clearTimeout(timer)
  }, [current, onDone])

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
            const done = index < current
            const active = index === current
            return (
              <li
                key={step.label}
                className={`step${done ? ' step-done' : ''}${active ? ' step-active' : ''}`}
              >
                <span className="step-dot" aria-hidden="true">
                  {done ? (
                    <Check size={12} strokeWidth={3} />
                  ) : active ? (
                    <LoaderCircle size={12} className="spin" />
                  ) : (
                    <span style={{ fontSize: 10 }}>{index + 1}</span>
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
          <span>No artificial delay — each step is real work.</span>
          <span className="mono">
            {Math.min(current, STEPS.length)}/{STEPS.length}
          </span>
        </div>
      </div>
    </div>
  )
}
