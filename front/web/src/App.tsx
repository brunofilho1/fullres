import { useState } from 'react'
import { Header } from './components/Header'
import { UploadScreen } from './components/UploadScreen'
import { SearchProgress } from './components/SearchProgress'
import { ResultsScreen } from './components/ResultsScreen'
import { mockMatches } from './lib/mock'
import type { ImageMeta } from './lib/types'

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

  return (
    <div className="app">
      <Header
        onHome={() => {
          setView('upload')
          setImage(null)
        }}
      />

      <main style={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
        {view === 'upload' && (
          <UploadScreen
            onSubmit={(meta) => {
              setImage(meta)
              setView('searching')
            }}
          />
        )}

        {view === 'searching' && image && (
          <SearchProgress image={image} onDone={() => setView('results')} />
        )}

        {view === 'results' && image && (
          <ResultsScreen
            image={image}
            matches={mockMatches}
            onRestart={() => {
              setImage(null)
              setView('upload')
            }}
          />
        )}
      </main>

      <Footer />
    </div>
  )
}

export default App
