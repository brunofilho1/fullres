import { useState, useSyncExternalStore } from 'react'
import { Header } from './components/Header'
import { UploadScreen } from './components/UploadScreen'
import { SearchProgress } from './components/SearchProgress'
import { ResultsScreen } from './components/ResultsScreen'
import { SearchController } from './lib/searchController'

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
  const [controller] = useState(() => new SearchController())
  const state = useSyncExternalStore(
    controller.subscribe,
    controller.getSnapshot,
    controller.getSnapshot,
  )

  return (
    <div className="app">
      <Header onHome={() => controller.reset()} />

      <main style={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
        {state.status === 'idle' && <UploadScreen onSubmit={(meta) => void controller.run(meta)} />}

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
      </main>

      <Footer />
    </div>
  )
}

export default App
