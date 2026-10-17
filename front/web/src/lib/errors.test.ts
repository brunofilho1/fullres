import { describe, expect, it } from 'vitest'
import { describeFailure } from './errors'
import { errorMessageKey } from './i18n/errorKeys'
import { en } from './i18n/messages/en'
import { ptBR } from './i18n/messages/pt-BR'
import { es } from './i18n/messages/es'

describe('describeFailure', () => {
  it('keeps known codes and the support reference', () => {
    const failure = describeFailure({ code: 'provider_quota_exceeded', reference: 'trace-1' })
    expect(failure).toEqual({ code: 'provider_quota_exceeded', reference: 'trace-1' })
  })

  it('never throws and returns a generic code for non-error values', () => {
    for (const value of [undefined, null, 'boom', 42, {}]) {
      const failure = describeFailure(value)
      expect(failure.code).toBe('unknown_error')
      expect(failure.reference).toBeUndefined()
    }
  })
})

describe('errorMessageKey', () => {
  it('maps known codes to their message key', () => {
    expect(errorMessageKey('provider_quota_exceeded')).toBe('errors.provider_quota_exceeded')
    expect(errorMessageKey('file_too_large')).toBe('errors.file_too_large')
  })

  it('falls back to the generic key for unknown codes', () => {
    expect(errorMessageKey('something_new')).toBe('errors.unknown_error')
  })
})

describe('message dictionaries', () => {
  it('translate every English key in Portuguese and Spanish', () => {
    const keys = Object.keys(en) as (keyof typeof en)[]
    for (const key of keys) {
      expect(ptBR[key], `pt-BR missing ${key}`).toBeTruthy()
      expect(es[key], `es missing ${key}`).toBeTruthy()
    }
  })
})
