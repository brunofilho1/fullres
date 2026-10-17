import {
  ArrowLeft,
  Eye,
  Layers,
  ScanSearch,
  ShieldCheck,
  SlidersHorizontal,
  Upload,
} from 'lucide-react'
import { routeHref } from '../lib/router'

const STEPS = [
  {
    icon: Upload,
    title: 'Upload your image',
    desc: 'Drag & drop a file, browse your device, paste from the clipboard (Ctrl+V) or paste a remote image URL. JPG, PNG and WEBP up to 20 MB are supported.',
  },
  {
    icon: Eye,
    title: 'Read it locally',
    desc: 'Your browser reads the dimensions, format and file size before anything is sent. Nothing leaves your device until you start the search.',
  },
  {
    icon: ScanSearch,
    title: 'Prepare the request',
    desc: 'On the server the image is re-encoded with SkiaSharp so it fits the provider limits, without changing what the image shows.',
  },
  {
    icon: Layers,
    title: 'Search the indexes',
    desc: 'The prepared image is sent to a reverse image search provider — currently Google Lens through SerpApi. Only the image you submitted is sent, and no account or personal data is required.',
  },
  {
    icon: ShieldCheck,
    title: 'Verify every match',
    desc: 'Each candidate is checked against the real file. When the dimensions can be read directly from the source image it is marked as verified; otherwise it stays reported, exactly as the index returned it.',
  },
  {
    icon: SlidersHorizontal,
    title: 'De-duplicate, filter and rank',
    desc: 'Duplicate URLs are merged, invalid or oversized results are dropped, and the rest is ranked by resolution, visual similarity and file size — with the best match highlighted.',
  },
]

export function HowItWorksPage() {
  return (
    <section className="doc container container-narrow">
      <a className="doc-back" href={routeHref('home')}>
        <ArrowLeft size={14} />
        Back to search
      </a>

      <header className="doc-header">
        <span className="eyebrow">How it works</span>
        <h1 className="doc-title">From a single image to the best version on the web</h1>
        <p className="doc-lead">
          FullRes looks at the image you already have, searches public reverse-image indexes, then
          verifies and ranks every candidate before showing it to you. Here is exactly what happens
          behind the scenes.
        </p>
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
                  {step.title}
                </h3>
                <p>{step.desc}</p>
              </div>
            </li>
          )
        })}
      </ol>

      <div className="doc-note">
        <strong>Everything on screen is real.</strong> Dimensions, formats, sizes and similarity
        scores that could not be measured are shown as <span className="mono">—</span>, never
        invented. The largest image is only the largest match that was found — not necessarily the
        original.
      </div>
    </section>
  )
}
