import { useEffect, useState } from 'react'
import { Check, LoaderCircle } from 'lucide-react'
import type { ImageMeta } from '../lib/types'

const STEPS = [
  { label: 'Reading your image', desc: 'Dimensions, format and file size were read in your browser.' },
  { label: 'Searching image indexes', desc: 'Uploading your image and ranking the matches returned by the provider.' },
]

const TIPS = [
  'Maybe the server went down…',
  'Maybe SerpApi went down…',
  "Okay, maybe it's just our terrible code that's broken…",
  "Maybe it's just a really big image.",
  'Our hamster powering the server is probably taking a break.',
  'The internet is a big place. Give it a moment.',
  'Still faster than reverse-searching by hand. Probably.',
  'This is the fun part. Any second now.',
  'Maybe someone unplugged the wrong cable.',
  'Searching every corner of the web. Even the weird ones.',
  'Patience is a virtue. So they say.',
  'Have you tried turning it off and on again? Too late for that.',
  'The results will be worth it. We hope.',
]

const TIP_INTERVAL_MS = 3500

function randomTipIndex(except: number): number {
  let index = except
  while (index === except) {
    index = Math.floor(Math.random() * TIPS.length)
  }
  return index
}

interface SearchProgressProps {
  image: ImageMeta
}

export function SearchProgress({ image }: SearchProgressProps) {
  const [elapsed, setElapsed] = useState(0)
  const [tipIndex, setTipIndex] = useState(() => Math.floor(Math.random() * TIPS.length))

  useEffect(() => {
    const timer = window.setInterval(() => setElapsed((value) => value + 1), 1000)
    return () => window.clearInterval(timer)
  }, [])

  useEffect(() => {
    const timer = window.setInterval(
      () => setTipIndex((current) => randomTipIndex(current)),
      TIP_INTERVAL_MS,
    )
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
          <span key={tipIndex} className="progress-tip">
            {TIPS[tipIndex]}
          </span>
          <span className="mono">{elapsed}s</span>
        </div>
      </div>
    </div>
  )
}
