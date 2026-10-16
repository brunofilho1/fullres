import { describeFailure } from './errors'
import { searchImages, toMatch } from './api'
import type { ImageMeta, Match, SearchFailure } from './types'

export interface SearchPayload {
  matches: Match[]
  warnings: string[]
  providerName: string | null
}

export type SearchState =
  | { status: 'idle' }
  | { status: 'searching'; image: ImageMeta }
  | ({ status: 'results'; image: ImageMeta } & SearchPayload)
  | ({ status: 'error'; image: ImageMeta; failure: SearchFailure } & SearchPayload)

const EMPTY_PAYLOAD: SearchPayload = { matches: [], warnings: [], providerName: null }

function payloadOf(state: SearchState): SearchPayload {
  if (state.status === 'results') {
    return { matches: state.matches, warnings: state.warnings, providerName: state.providerName }
  }
  if (state.status === 'error') {
    return { matches: state.matches, warnings: state.warnings, providerName: state.providerName }
  }
  return EMPTY_PAYLOAD
}

/**
 * Framework-free search orchestration used by the UI through `useSyncExternalStore`.
 *
 * Guarantees required by the product:
 * - a single search runs at a time: duplicate submits while a request is in flight are ignored;
 * - a completion is ignored when a newer search has started (no stale response overwrites);
 * - aborting (restart / new image) stops the request and never surfaces an error;
 * - previous results are preserved when a retry fails, so they are not lost.
 */
export class SearchController {
  private state: SearchState = { status: 'idle' }
  private controller: AbortController | null = null
  private sequence = 0
  private listeners = new Set<() => void>()

  subscribe = (listener: () => void): (() => void) => {
    this.listeners.add(listener)
    return () => this.listeners.delete(listener)
  }

  getSnapshot = (): SearchState => this.state

  isSearching = (): boolean => this.controller !== null

  async run(image: ImageMeta): Promise<void> {
    if (this.controller) return // duplicate submit while a search is already in flight

    const controller = new AbortController()
    this.controller = controller
    const sequence = ++this.sequence
    const previous = payloadOf(this.state)
    this.setState({ status: 'searching', image })

    try {
      const response = await searchImages(image.file, { sortBy: 'resolution' }, controller.signal)
      if (sequence !== this.sequence) return // superseded by a newer search
      this.setState({
        status: 'results',
        image,
        matches: response.results.map(toMatch),
        warnings: response.provider.warnings,
        providerName: response.provider.name,
      })
    } catch (error) {
      if (sequence !== this.sequence) return
      if (controller.signal.aborted) return // aborted or superseded: not an error for the user
      this.setState({ status: 'error', image, failure: describeFailure(error), ...previous })
    } finally {
      if (sequence === this.sequence) this.controller = null
    }
  }

  abort(): void {
    this.controller?.abort()
    this.controller = null
    this.sequence++
  }

  reset(): void {
    this.abort()
    this.setState({ status: 'idle' })
  }

  private setState(state: SearchState): void {
    this.state = state
    for (const listener of this.listeners) listener()
  }
}
