import { useCallback, useRef, useState } from 'react'
import { Header } from './components/Header'
import { UploadScreen } from './components/UploadScreen'
import { SearchProgress } from './components/SearchProgress'
import { ResultsScreen } from './components/ResultsScreen'
import { ApiError, searchImages, toMatch } from './lib/api'
import type { ImageMeta, Match } from './lib/types'

type View = 'upload' | 'searching' | 'results'

function Footer() {
  return (
    <footer className="footer">
      <div className="container footer-inner">
        <span>
          FullRes — find the original, get the best resolution. Open source image search.
        </span>
        <div className="footer-right">
          <nav className="footer-links">
            <a href="#how">How it works</a>
            <a href="#providers">Search providers</a>
            <a href="https://github.com/brunofilho1/fullres" target="_blank" rel="noreferrer">
              Source code
            </a>
          </nav>
          <span className="footer-credit">
            Made by{' '}
            <a href="https://github.com/brunofilho1" target="_blank" rel="noreferrer">
              Bruno Filho
            </a>
          </span>
        </div>
      </div>
    </footer>
  )
}

function App() {
  const [view, setView] = useState<View>('upload')
  const [image, setImage] = useState<ImageMeta | null>(null)
  const [matches, setMatches] = useState<Match[]>([])
  const [warnings, setWarnings] = useState<string[]>([])
  const [providerName, setProviderName] = useState<string | null>(null)
  const [apiError, setApiError] = useState<string | null>(null)
  const abortRef = useRef<AbortController | null>(null)

  const runSearch = useCallback(async (meta: ImageMeta) => {
    abortRef.current?.abort()
    const controller = new AbortController()
    abortRef.current = controller

    setImage(meta)
    setMatches([])
    setWarnings([])
    setProviderName(null)
    setApiError(null)
    setView('searching')

    try {
      const response = await searchImages(meta.file, { sortBy: 'resolution' }, controller.signal)
      if (controller.signal.aborted) return
      setMatches(response.results.map(toMatch))
      setWarnings(response.provider.warnings)
      setProviderName(response.provider.name)
      setView('results')
    } catch (error) {
      if (controller.signal.aborted) return
      setApiError(
        error instanceof ApiError
          ? error.message
          : 'Something went wrong while searching. Please try again.',
      )
      setView('results')
    }
  }, [])

  const restart = useCallback(() => {
    abortRef.current?.abort()
    abortRef.current = null
    setView('upload')
    setImage(null)
  }, [])

  return (
    <div className="app">
      <Header onHome={restart} />

      <main style={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
        {view === 'upload' && <UploadScreen onSubmit={(meta) => void runSearch(meta)} />}

        {view === 'searching' && image && <SearchProgress image={image} />}

        {view === 'results' && image && (
          <ResultsScreen
            image={image}
            matches={matches}
            warnings={warnings}
            providerName={providerName}
            apiError={apiError}
            onRetry={() => void runSearch(image)}
            onRestart={restart}
          />
        )}
      </main>

      <Footer />
    </div>
  )
}

export default App
