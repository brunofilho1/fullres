import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { searchImages } from './api'

const file = new File([new Uint8Array([1, 2, 3])], 'photo.jpg', { type: 'image/jpeg' })

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'content-type': 'application/json' },
  })
}

describe('searchImages', () => {
  const fetchMock = vi.fn()

  beforeEach(() => {
    fetchMock.mockReset()
    vi.stubGlobal('fetch', fetchMock)
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('posts the image as multipart form data to the search endpoint', async () => {
    fetchMock.mockResolvedValue(
      jsonResponse({
        query: {},
        results: [],
        provider: { name: 'SerpAPI', configured: true, warnings: [] },
      }),
    )

    const response = await searchImages(file, { sortBy: 'resolution' }, new AbortController().signal)

    expect(response.provider.name).toBe('SerpAPI')
    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit]
    expect(url).toContain('/api/images/search')
    expect(init.method).toBe('POST')
    expect(init.body).toBeInstanceOf(FormData)
    expect((init.body as FormData).get('sortBy')).toBe('resolution')
  })

  it('surfaces the problem code and traceId when the API rejects the request', async () => {
    fetchMock.mockResolvedValue(
      jsonResponse(
        { title: 'Unavailable', detail: 'Quota gone', code: 'provider_quota_exceeded', traceId: 'abc' },
        503,
      ),
    )

    await expect(searchImages(file, {}, new AbortController().signal)).rejects.toMatchObject({
      code: 'provider_quota_exceeded',
      status: 503,
      reference: 'abc',
    })
  })

  it('maps a transport failure to network_error', async () => {
    fetchMock.mockRejectedValue(new TypeError('fetch failed'))

    await expect(searchImages(file, {}, new AbortController().signal)).rejects.toMatchObject({
      code: 'network_error',
    })
  })

  it('rethrows when the caller aborts the request', async () => {
    const controller = new AbortController()
    fetchMock.mockImplementation(
      () =>
        new Promise((_resolve, reject) => {
          controller.signal.addEventListener('abort', () =>
            reject(new DOMException('aborted', 'AbortError')),
          )
        }),
    )

    const promise = searchImages(file, {}, controller.signal)
    controller.abort()

    await expect(promise).rejects.toThrow()
  })
})
