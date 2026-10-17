import { useEffect, useState, useSyncExternalStore } from 'react'
import { Header } from './components/Header'
import { UploadScreen } from './components/UploadScreen'
import { SearchProgress } from './components/SearchProgress'
import { ResultsScreen } from './components/ResultsScreen'
import { HowItWorksPage } from './components/HowItWorksPage'
import { ProvidersPage } from './components/ProvidersPage'
import { SearchController } from './lib/searchController'
import { navigate, routeHref, useRoute } from './lib/router'

function Footer() {
  return (
    <footer className="footer">
      <div className="container footer-inner">
        <div className="footer-left">
          <span className="footer-tagline">
            FullRes — find the original, get the best resolution. Open source image search.
          </span>
          <div className="footer-partners">
            <span className="footer-partners-label">Powered by</span>
            <a
              className="footer-logo"
              href="https://www.google.com"
              target="_blank"
              rel="noreferrer"
              aria-label="Google"
            >
              <img src="/google-logo.svg" alt="Google" />
            </a>
            <a
              className="footer-logo footer-logo-tall"
              href="https://serpapi.com"
              target="_blank"
              rel="noreferrer"
              aria-label="SerpApi"
            >
              <img src="/serpapi-logo.svg" alt="SerpApi" />
            </a>
          </div>
        </div>
        <div className="footer-right">
          <nav className="footer-links">
            <a href={routeHref('how-it-works')}>How it works</a>
            <a href={routeHref('providers')}>Search providers</a>
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
  const [controller] = useState(() => new SearchController())
  const route = useRoute()
  const state = useSyncExternalStore(
    controller.subscribe,
    controller.getSnapshot,
    controller.getSnapshot,
  )

  useEffect(() => {
    window.scrollTo({ top: 0 })
  }, [route])

  return (
    <div className="app">
      <Header
        onHome={() => {
          controller.reset()
          navigate('home')
        }}
      />

      <main style={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
        {route === 'how-it-works' && <HowItWorksPage />}
        {route === 'providers' && <ProvidersPage />}

        {route === 'home' && (
          <>
            {state.status === 'idle' && (
              <UploadScreen onSubmit={(meta) => void controller.run(meta)} />
            )}

            {state.status === 'searching' && <SearchProgress image={state.image} />}

            {(state.status === 'results' || state.status === 'error') && (
              <ResultsScreen
                image={state.image}
                matches={state.matches}
                warnings={state.warnings}
                providerName={state.providerName}
                failure={state.status === 'error' ? state.failure : null}
                onRetry={() => void controller.run(state.image)}
                onRestart={() => controller.reset()}
              />
            )}
          </>
        )}
      </main>

      <Footer />
    </div>
  )
}

export default App
