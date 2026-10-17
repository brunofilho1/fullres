import { useEffect, useState } from 'react'
import { Check, LoaderCircle } from 'lucide-react'
import type { ImageMeta } from '../lib/types'
import { useI18n } from '../lib/i18n'
import type { MessageKey } from '../lib/i18n/messages/en'

const STEPS: { label: MessageKey; desc: MessageKey }[] = [
  { label: 'progress.step1Label', desc: 'progress.step1Desc' },
  { label: 'progress.step2Label', desc: 'progress.step2Desc' },
]

const TIP_KEYS: MessageKey[] = [
  'progress.tip1',
  'progress.tip2',
  'progress.tip3',
  'progress.tip4',
  'progress.tip5',
  'progress.tip6',
  'progress.tip7',
  'progress.tip8',
  'progress.tip9',
  'progress.tip10',
  'progress.tip11',
  'progress.tip12',
  'progress.tip13',
]

const TIP_INTERVAL_MS = 3500

function randomTipIndex(except: number): number {
  let index = except
  while (index === except) {
    index = Math.floor(Math.random() * TIP_KEYS.length)
  }
  return index
}

interface SearchProgressProps {
  image: ImageMeta
}

export function SearchProgress({ image }: SearchProgressProps) {
  const { t } = useI18n()
  const [elapsed, setElapsed] = useState(0)
  const [tipIndex, setTipIndex] = useState(() => Math.floor(Math.random() * TIP_KEYS.length))

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
            <div className="progress-title">{t('progress.title')}</div>
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
                  <span className="step-label">{t(step.label)}</span>
                  <span className="step-desc" style={{ display: 'block' }}>
                    {t(step.desc)}
                  </span>
                </span>
              </li>
            )
          })}
        </ol>
        <div className="progress-foot">
          <span key={tipIndex} className="progress-tip">
            {t(TIP_KEYS[tipIndex])}
          </span>
          <span className="mono">{elapsed}s</span>
        </div>
      </div>
    </div>
  )
}
