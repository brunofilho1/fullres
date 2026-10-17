export interface ImageMeta {
  src: string
  file: File
  name: string
  width: number
  height: number
  format: string
  bytes: number
}

export interface Match {
  id: string
  /** Raw title from the search index; null when the index provided none. */
  title: string | null
  url: string
  pageUrl: string | null
  thumb: string
  width: number | null
  height: number | null
  format: string | null
  bytes: number | null
  domain: string | null
  similarity: number | null
  verified: boolean
}

export type SortKey = 'resolution' | 'similarity' | 'size'

export interface ApiQueryInfo {
  width: number
  height: number
  totalPixels: number
  format: string
  sizeBytes: number
  originalSizeBytes: number
  reencodedForProvider: boolean
}

export interface ApiResult {
  imageUrl: string
  sourceUrl: string | null
  title: string | null
  domain: string | null
  width: number | null
  height: number | null
  totalPixels: number | null
  format: string | null
  fileSizeBytes: number | null
  dimensionsVerified: boolean
  similarityScore: number | null
  thumbnailUrl: string | null
  provider: string
}

/**
 * Structured warning emitted by the provider pipeline. `code` is a stable identifier that the
 * UI localises; `count` is only present when the warning is about a number of results.
 */
export interface SearchWarning {
  code: string
  count?: number | null
}

export interface ApiProviderInfo {
  name: string
  configured: boolean
  warnings: SearchWarning[]
}

export interface SearchResponse {
  query: ApiQueryInfo
  results: ApiResult[]
  provider: ApiProviderInfo
}

export interface ApiProblem {
  title?: string
  detail?: string
  status?: number
  code?: string
  traceId?: string
}

/**
 * Localisation-free description of a failed search. The human-readable text is derived from
 * `code` at render time so it follows the selected language.
 */
export interface SearchFailure {
  code: string
  reference?: string
}
