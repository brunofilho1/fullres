export interface ImageMeta {
  src: string
  name: string
  width: number
  height: number
  format: string
  bytes: number
}

export interface Match {
  id: string
  title: string
  url: string
  thumb: string
  width: number
  height: number
  format: string
  bytes: number
  domain: string
  similarity: number
  note?: string
}

export type SearchState = 'results' | 'empty' | 'partial' | 'error'

export type SortKey = 'resolution' | 'similarity' | 'size'
