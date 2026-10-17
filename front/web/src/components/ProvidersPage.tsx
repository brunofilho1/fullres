import { ArrowLeft, ExternalLink, Layers, Server, ShieldCheck, SlidersHorizontal } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { routeHref } from '../lib/router'
import { useI18n } from '../lib/i18n'
import type { MessageKey } from '../lib/i18n/messages/en'

const REPO_PROVIDERS_URL =
  'https://github.com/brunofilho1/fullres/tree/main/back/FullRes.Api/Providers'

const PIPELINE: { icon: LucideIcon; title: MessageKey; desc: MessageKey }[] = [
  { icon: Layers, title: 'providers.pipeline1Title', desc: 'providers.pipeline1Desc' },
  { icon: ShieldCheck, title: 'providers.pipeline2Title', desc: 'providers.pipeline2Desc' },
  { icon: SlidersHorizontal, title: 'providers.pipeline3Title', desc: 'providers.pipeline3Desc' },
]

export function ProvidersPage() {
  const { t } = useI18n()

  return (
    <section className="doc container container-narrow">
      <a className="doc-back" href={routeHref('home')}>
        <ArrowLeft size={14} />
        {t('providers.back')}
      </a>

      <header className="doc-header">
        <span className="eyebrow">{t('providers.eyebrow')}</span>
        <h1 className="doc-title">{t('providers.title')}</h1>
        <p className="doc-lead">{t('providers.lead')}</p>
      </header>

      <div className="doc-section">
        <h2>{t('providers.activeTitle')}</h2>
        <p>{t('providers.activeNote')}</p>
        <div className="provider">
          <div className="provider-head">
            <span className="provider-icon" aria-hidden="true">
              <Server size={18} />
            </span>
            <div>
              <div className="provider-name">
                SerpApi · Google Lens{' '}
                <span className="pill pill-accent">{t('providers.active')}</span>
              </div>
              <p className="provider-desc">{t('providers.serpapiDesc')}</p>
            </div>
          </div>
          <a
            className="btn btn-ghost"
            href="https://serpapi.com/google-lens-api"
            target="_blank"
            rel="noreferrer"
          >
            <span>SerpApi</span>
            <ExternalLink size={14} />
          </a>
        </div>
      </div>

      <div className="doc-section">
        <h2>{t('providers.pipelineTitle')}</h2>
        <p>{t('providers.pipelineLead')}</p>
        <ol className="doc-steps">
          {PIPELINE.map((step) => {
            const Icon = step.icon
            return (
              <li className="doc-step" key={step.title}>
                <span className="doc-step-badge" aria-hidden="true">
                  <Icon size={15} />
                </span>
                <div>
                  <h3>{t(step.title)}</h3>
                  <p>{t(step.desc)}</p>
                </div>
              </li>
            )
          })}
        </ol>
      </div>

      <div className="doc-section">
        <h2>{t('providers.addTitle')}</h2>
        <p>
          {t('providers.addIntro')} <span className="mono">IReverseImageSearchProvider</span>{' '}
          {t('providers.addMiddle')} <span className="mono">back/FullRes.Api/Providers</span>{' '}
          {t('providers.addOutro')}
        </p>
        <div className="doc-note">
          <strong>{t('providers.noteStrong')}</strong> {t('providers.noteBody')}
        </div>
        <div className="doc-actions">
          <a className="btn" href={REPO_PROVIDERS_URL} target="_blank" rel="noreferrer">
            <ExternalLink size={14} />
            <span>{t('providers.viewInterface')}</span>
          </a>
        </div>
      </div>
    </section>
  )
}
