import type { ApiProblem, ApiResult, Match, SearchResponse, SortKey } from './types'

const API_BASE = (import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5080').replace(/\/+$/, '')

export class ApiError extends Error {
  readonly code: string
  readonly status: number

  constructor(message: string, code: string, status: number) {
    super(message)
    this.name = 'ApiError'
    this.code = code
    this.status = status
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

  let response: Response
  try {
    response = await fetch(`${API_BASE}/api/images/search`, {
      method: 'POST',
      body: form,
      signal,
    })
  } catch (error) {
    if (signal.aborted) throw error
    throw new ApiError(
      `Could not reach the FullRes API at ${API_BASE}. Make sure the backend is running.`,
      'network_error',
      0,
    )
  }

  if (!response.ok) {
    const problem = await readProblem(response)
    throw new ApiError(
      problem?.detail ?? problem?.title ?? `The search request failed (HTTP ${response.status}).`,
      problem?.code ?? 'unknown_error',
      response.status,
    )
  }

  return (await response.json()) as SearchResponse
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
    title: result.title?.trim() || result.domain || 'Untitled match',
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
