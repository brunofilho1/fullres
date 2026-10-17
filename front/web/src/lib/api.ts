import type { ApiProblem, ApiResult, Match, SearchResponse, SortKey } from './types'

const API_BASE = (import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5080').replace(/\/+$/, '')

// Upper bound for a single search request from the browser's point of view. The backend has
// its own (shorter) provider timeout; this watchdog only prevents a hung connection from
// leaving the UI spinning forever.
const CLIENT_TIMEOUT_MS = 45_000

export class ApiError extends Error {
  readonly code: string
  readonly status: number
  readonly reference?: string

  constructor(message: string, code: string, status: number, reference?: string) {
    super(message)
    this.name = 'ApiError'
    this.code = code
    this.status = status
    this.reference = reference
  }
}

export interface SearchOptions {
  sortBy?: SortKey
  minWidth?: number
  minHeight?: number
  format?: string
  minSimilarity?: number
}

export async function searchImages(
  file: File,
  options: SearchOptions,
  signal: AbortSignal,
): Promise<SearchResponse> {
  const form = new FormData()
  form.append('image', file, file.name)
  form.append('sortBy', options.sortBy ?? 'resolution')
  if (options.minWidth) form.append('minWidth', String(options.minWidth))
  if (options.minHeight) form.append('minHeight', String(options.minHeight))
  if (options.format && options.format !== 'any') form.append('format', options.format)
  if (options.minSimilarity) form.append('minSimilarity', String(options.minSimilarity))

  // The caller's signal (used to cancel/supersede searches) is linked to an internal
  // controller so a client-side timeout can also abort the request without being mistaken
  // for a user cancellation.
  const requestController = new AbortController()
  let timedOut = false
  const onAbort = () => requestController.abort()
  signal.addEventListener('abort', onAbort, { once: true })
  const timer = setTimeout(() => {
    timedOut = true
    requestController.abort()
  }, CLIENT_TIMEOUT_MS)

  let response: Response
  try {
    response = await fetch(`${API_BASE}/api/images/search`, {
      method: 'POST',
      body: form,
      signal: requestController.signal,
    })
  } catch (error) {
    if (signal.aborted) throw error
    if (timedOut) {
      throw new ApiError('The search request timed out.', 'client_timeout', 0)
    }
    throw new ApiError(
      `Could not reach the FullRes API at ${API_BASE}. Make sure the backend is running.`,
      'network_error',
      0,
    )
  } finally {
    clearTimeout(timer)
    signal.removeEventListener('abort', onAbort)
  }

  if (!response.ok) {
    const problem = await readProblem(response)
    throw new ApiError(
      problem?.detail ?? problem?.title ?? `The search request failed (HTTP ${response.status}).`,
      problem?.code ?? 'unknown_error',
      response.status,
      problem?.traceId,
    )
  }

  try {
    return (await response.json()) as SearchResponse
  } catch {
    throw new ApiError(
      'The search API returned an unexpected response.',
      'provider_invalid_response',
      response.status,
    )
  }
}

async function readProblem(response: Response): Promise<ApiProblem | null> {
  try {
    return (await response.json()) as ApiProblem
  } catch {
    return null
  }
}

export function toMatch(result: ApiResult, index: number): Match {
  return {
    id: `${index}-${result.imageUrl}`,
    title: result.title?.trim() || result.domain || null,
    url: result.imageUrl,
    pageUrl: result.sourceUrl,
    thumb: result.thumbnailUrl ?? result.imageUrl,
    width: result.width,
    height: result.height,
    format: result.format,
    bytes: result.fileSizeBytes,
    domain: result.domain,
    similarity: result.similarityScore,
    verified: result.dimensionsVerified,
  }
}
