import { describe, expect, it } from 'vitest'
import { describeFailure } from './errors'

describe('describeFailure', () => {
  it('maps known codes to Brazilian-Portuguese messages', () => {
    const failure = describeFailure({ code: 'provider_quota_exceeded' })
    expect(failure.code).toBe('provider_quota_exceeded')
    expect(failure.message).toContain('limite de utilização')
  })

  it('maps an upload validation code', () => {
    const failure = describeFailure({ code: 'file_too_large' })
    expect(failure.message).toContain('20 MB')
  })

  it('falls back to a generic message for unknown codes and keeps the reference', () => {
    const failure = describeFailure({ code: 'something_new', reference: 'trace-123' })
    expect(failure.code).toBe('something_new')
    expect(failure.message).toContain('Algo deu errado')
    expect(failure.reference).toBe('trace-123')
  })

  it('never throws and returns a generic failure for non-error values', () => {
    for (const value of [undefined, null, 'boom', 42, {}]) {
      const failure = describeFailure(value)
      expect(failure.code).toBe('unknown_error')
      expect(failure.message).toContain('Algo deu errado')
    }
  })
})
