import { en, type MessageKey } from './messages/en'

const ERROR_CODES = new Set(
  Object.keys(en)
    .filter((key) => key.startsWith('errors.'))
    .map((key) => key.slice('errors.'.length)),
)

/** Maps any backend error code onto a message key, falling back for codes the UI does not know. */
export function errorMessageKey(code: string): MessageKey {
  return ERROR_CODES.has(code) ? (`errors.${code}` as MessageKey) : 'errors.unknown_error'
}
