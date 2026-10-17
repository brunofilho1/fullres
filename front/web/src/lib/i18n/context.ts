import { createContext, useContext } from 'react'
import type { Locale } from './locales'
import type { MessageKey } from './messages/en'
import type { SearchWarning } from '../types'

export type TranslateParams = Record<string, string | number>
export type TFunction = (key: MessageKey, params?: TranslateParams) => string
export type PluralFunction = (baseKey: string, count: number, params?: TranslateParams) => string

export interface I18nValue {
  locale: Locale
  setLocale: (locale: Locale) => void
  t: TFunction
  tc: PluralFunction
  formatNumber: (value: number) => string
  resolutionLabel: (width: number | null, height: number | null) => string
  pixelGain: (originalPixels: number, foundPixels: number) => string
  matchTitle: (title: string | null | undefined) => string
  warningMessage: (warning: SearchWarning) => string | null
  errorMessage: (code: string) => string
}

export const I18nContext = createContext<I18nValue | null>(null)

export function useI18n(): I18nValue {
  const value = useContext(I18nContext)
  if (value === null) throw new Error('useI18n must be used within I18nProvider')
  return value
}
