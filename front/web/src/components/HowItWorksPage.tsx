import {
  ArrowLeft,
  Eye,
  Layers,
  ScanSearch,
  ShieldCheck,
  SlidersHorizontal,
  Upload,
} from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { routeHref } from '../lib/router'
import { useI18n } from '../lib/i18n'
import type { MessageKey } from '../lib/i18n/messages/en'

const STEPS: { icon: LucideIcon; title: MessageKey; desc: MessageKey }[] = [
  { icon: Upload, title: 'how.step1Title', desc: 'how.step1Desc' },
  { icon: Eye, title: 'how.step2Title', desc: 'how.step2Desc' },
  { icon: ScanSearch, title: 'how.step3Title', desc: 'how.step3Desc' },
  { icon: Layers, title: 'how.step4Title', desc: 'how.step4Desc' },
  { icon: ShieldCheck, title: 'how.step5Title', desc: 'how.step5Desc' },
  { icon: SlidersHorizontal, title: 'how.step6Title', desc: 'how.step6Desc' },
]

export function HowItWorksPage() {
  const { t } = useI18n()

  return (
    <section className="doc container container-narrow">
      <a className="doc-back" href={routeHref('home')}>
        <ArrowLeft size={14} />
        {t('how.back')}
      </a>

      <header className="doc-header">
        <span className="eyebrow">{t('how.eyebrow')}</span>
        <h1 className="doc-title">{t('how.title')}</h1>
        <p className="doc-lead">{t('how.lead')}</p>
      </header>

      <ol className="doc-steps">
        {STEPS.map((step, index) => {
          const Icon = step.icon
          return (
            <li className="doc-step" key={step.title}>
              <span className="doc-step-badge mono" aria-hidden="true">
                {String(index + 1).padStart(2, '0')}
              </span>
              <div>
                <h3>
                  <Icon size={15} aria-hidden="true" />
                  {t(step.title)}
                </h3>
                <p>{t(step.desc)}</p>
              </div>
            </li>
          )
        })}
      </ol>

      <div className="doc-note">
        <strong>{t('how.noteStrong')}</strong> {t('how.noteBody')}
      </div>
    </section>
  )
}
