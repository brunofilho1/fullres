import type { SearchFailure } from './types'

/**
 * Converts anything thrown while searching into a localisation-free failure for the UI.
 *
 * Only the stable `code` and the optional support `reference` are kept here: the
 * human-readable text is resolved from the code at render time, so it follows whichever
 * language is selected (see `errorMessageKey` in `lib/i18n`). Technical provider wording,
 * stack traces and raw payloads are never shown to the user.
 */
export function describeFailure(error: unknown): SearchFailure {
  if (isFailureLike(error)) {
    return {
      code: error.code ?? 'unknown_error',
      reference: error.reference ?? undefined,
    }
  }

  return { code: 'unknown_error' }
}

interface FailureLike {
  code?: string
  reference?: string
}

function isFailureLike(value: unknown): value is FailureLike {
  return typeof value === 'object' && value !== null && typeof (value as { code?: unknown }).code === 'string'
}
