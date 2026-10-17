import { ArrowLeft, ExternalLink, Layers, Server, ShieldCheck, SlidersHorizontal } from 'lucide-react'
import { routeHref } from '../lib/router'

const REPO_PROVIDERS_URL =
  'https://github.com/brunofilho1/fullres/tree/main/back/FullRes.Api/Providers'

const PIPELINE = [
  {
    icon: Layers,
    title: 'The provider returns candidates',
    desc: 'It receives the prepared image and hands back a list of candidate matches, each with whatever the engine knows: image URL, source page, resolution, format and similarity.',
  },
  {
    icon: ShieldCheck,
    title: 'FullRes verifies them',
    desc: 'Each candidate is probed against the real file. Dimensions that can be read directly from the source are marked as verified; the rest stay reported, exactly as the index returned them.',
  },
  {
    icon: SlidersHorizontal,
    title: 'FullRes de-duplicates, filters and ranks',
    desc: 'Duplicate URLs are merged, invalid or oversized results are dropped, and the rest are sorted by resolution, similarity and file size. This logic is shared by every provider.',
  },
]

export function ProvidersPage() {
  return (
    <section className="doc container container-narrow">
      <a className="doc-back" href={routeHref('home')}>
        <ArrowLeft size={14} />
        Back to search
      </a>

      <header className="doc-header">
        <span className="eyebrow">Search providers</span>
        <h1 className="doc-title">The engines behind your search</h1>
        <p className="doc-lead">
          FullRes does not crawl the web itself. It queries reverse image search providers, then
          verifies and ranks what they return. Today one provider is wired up, and the pipeline is
          built so more can be added.
        </p>
      </header>

      <div className="doc-section">
        <h2>Active providers</h2>
        <p>
          Searches run through public reverse-image search indexes. Only the image you submit is
          sent to the provider.
        </p>
        <div className="provider">
          <div className="provider-head">
            <span className="provider-icon" aria-hidden="true">
              <Server size={18} />
            </span>
            <div>
              <div className="provider-name">
                SerpApi · Google Lens <span className="pill pill-accent">Active</span>
              </div>
              <p className="provider-desc">
                Reverse image search powered by Google Lens, accessed through the SerpApi service.
                Each FullRes search spends one query against the API plan's monthly quota.
              </p>
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
        <h2>How the pipeline works</h2>
        <p>
          A provider has a single job: return candidates. Everything after that is handled by
          FullRes, so a new provider never has to reimplement the hard parts.
        </p>
        <ol className="doc-steps">
          {PIPELINE.map((step) => {
            const Icon = step.icon
            return (
              <li className="doc-step" key={step.title}>
                <span className="doc-step-badge" aria-hidden="true">
                  <Icon size={15} />
                </span>
                <div>
                  <h3>{step.title}</h3>
                  <p>{step.desc}</p>
                </div>
              </li>
            )
          })}
        </ol>
      </div>

      <div className="doc-section">
        <h2>Add your own provider</h2>
        <p>
          New search providers are welcome. Implement{' '}
          <span className="mono">IReverseImageSearchProvider</span> in{' '}
          <span className="mono">back/FullRes.Api/Providers</span> and return a list of candidate
          images — verification, de-duplication and ranking are handled by the pipeline.
        </p>
        <div className="doc-note">
          <strong>Providers never change what is shown.</strong> Unknown dimensions, formats and
          sizes stay unknown, and results are never invented to fill a gap. If a provider cannot
          answer, that is reported honestly instead of being hidden.
        </div>
        <div className="doc-actions">
          <a className="btn" href={REPO_PROVIDERS_URL} target="_blank" rel="noreferrer">
            <ExternalLink size={14} />
            <span>View provider interface on GitHub</span>
          </a>
        </div>
      </div>
    </section>
  )
}
