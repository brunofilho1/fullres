import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('./api', () => ({
  searchImages: vi.fn(),
  toMatch: (result: { imageUrl: string }, index: number) => ({
    id: String(index),
    title: 'match',
    url: result.imageUrl,
    pageUrl: null,
    thumb: result.imageUrl,
    width: null,
    height: null,
    format: null,
    bytes: null,
    domain: null,
    similarity: null,
    verified: false,
  }),
}))

import { searchImages } from './api'
import { SearchController } from './searchController'
import type { ImageMeta, SearchResponse } from './types'

const searchMock = vi.mocked(searchImages)

const image = {
  src: 'blob:image',
  file: new File([new Uint8Array([1])], 'a.jpg', { type: 'image/jpeg' }),
  name: 'a.jpg',
  width: 10,
  height: 10,
  format: 'JPG',
  bytes: 1,
} satisfies ImageMeta

const okResponse: SearchResponse = {
  query: {
    width: 10,
    height: 10,
    totalPixels: 100,
    format: 'JPG',
    sizeBytes: 1,
    originalSizeBytes: 1,
    reencodedForProvider: false,
  },
  results: [
    {
      imageUrl: 'https://example.com/a.jpg',
      sourceUrl: null,
      title: null,
      domain: null,
      width: 100,
      height: 100,
      totalPixels: 10000,
      format: 'JPG',
      fileSizeBytes: 10,
      dimensionsVerified: true,
      similarityScore: null,
      thumbnailUrl: null,
      provider: 'SerpAPI',
    },
  ],
  provider: { name: 'SerpAPI', configured: true, warnings: [] },
}

function deferred<T>() {
  let resolve!: (value: T) => void
  let reject!: (reason?: unknown) => void
  const promise = new Promise<T>((res, rej) => {
    resolve = res
    reject = rej
  })
  return { promise, resolve, reject }
}

describe('SearchController', () => {
  beforeEach(() => {
    searchMock.mockReset()
  })

  it('starts idle and moves to results on success', async () => {
    searchMock.mockResolvedValue(okResponse)
    const controller = new SearchController()

    expect(controller.getSnapshot().status).toBe('idle')

    await controller.run(image)

    const state = controller.getSnapshot()
    expect(state.status).toBe('results')
    if (state.status === 'results') {
      expect(state.matches).toHaveLength(1)
      expect(state.providerName).toBe('SerpAPI')
    }
  })

  it('ignores a duplicate submit while a search is already in flight', async () => {
    const pending = deferred<SearchResponse>()
    searchMock.mockReturnValueOnce(pending.promise)
    const controller = new SearchController()

    const first = controller.run(image)
    await Promise.resolve()
    await controller.run(image) // duplicate: must be ignored

    expect(searchMock).toHaveBeenCalledTimes(1)

    pending.resolve(okResponse)
    await first
    expect(controller.getSnapshot().status).toBe('results')
  })

  it('discards a stale response when the search was reset', async () => {
    const pending = deferred<SearchResponse>()
    searchMock.mockReturnValueOnce(pending.promise)
    const controller = new SearchController()

    const running = controller.run(image)
    await Promise.resolve()
    controller.reset()

    expect(controller.getSnapshot().status).toBe('idle')

    pending.resolve(okResponse)
    await running

    expect(controller.getSnapshot().status).toBe('idle')
  })

  it('reports a friendly failure and preserves previous results', async () => {
    searchMock.mockResolvedValueOnce(okResponse)
    const controller = new SearchController()
    await controller.run(image)
    expect(controller.getSnapshot().status).toBe('results')

    searchMock.mockRejectedValueOnce({ code: 'provider_quota_exceeded', reference: 'trace-1' })
    await controller.run(image)

    const state = controller.getSnapshot()
    expect(state.status).toBe('error')
    if (state.status === 'error') {
      expect(state.failure.code).toBe('provider_quota_exceeded')
      expect(state.failure.message).toContain('limite de utilização')
      expect(state.failure.reference).toBe('trace-1')
      expect(state.matches).toHaveLength(1)
    }
  })
})
