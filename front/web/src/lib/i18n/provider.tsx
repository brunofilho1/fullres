import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { LOCALE_META, detectLocale, isLocale, type Locale } from './locales'
import { en, type MessageKey } from './messages/en'
import { ptBR } from './messages/pt-BR'
import { es } from './messages/es'
import { errorMessageKey } from './errorKeys'
import { I18nContext, type I18nValue, type PluralFunction, type TFunction } from './context'
import { pixelGainInfo, resolutionTier } from '../format'

const STORAGE_KEY = 'fullres.locale'

const MESSAGES: Record<Locale, Record<MessageKey, string>> = {
  en,
  'pt-BR': ptBR,
  es,
}

function interpolate(template: string, params?: Record<string, string | number>): string {
  if (!params) return template
  return template.replace(/\{(\w+)\}/g, (match, name: string) =>
    name in params ? String(params[name]) : match,
  )
}

function readStoredLocale(): Locale | null {
  try {
    const value = window.localStorage.getItem(STORAGE_KEY)
    return value !== null && isLocale(value) ? value : null
  } catch {
    return null
  }
}

function initialLocale(): Locale {
  const stored = readStoredLocale()
  if (stored) return stored
  const languages = navigator.languages?.length ? navigator.languages : [navigator.language]
  return detectLocale(languages)
}

export function I18nProvider({ children }: { children: ReactNode }) {
  const [locale, setLocaleState] = useState<Locale>(initialLocale)

  const setLocale = useCallback((next: Locale) => {
    setLocaleState(next)
    try {
      window.localStorage.setItem(STORAGE_KEY, next)
    } catch {
      // Storage can be unavailable (private mode); the selection still applies for this visit.
    }
  }, [])

  const value = useMemo<I18nValue>(() => {
    const messages = MESSAGES[locale]
    const table = messages as Record<string, string>
    const intlLocale = LOCALE_META[locale].intl
    const plurals = new Intl.PluralRules(intlLocale)
    const numbers = new Intl.NumberFormat(intlLocale)

    const t: TFunction = (key, params) => interpolate(messages[key], params)

    const tc: PluralFunction = (baseKey, count, params) => {
      const template =
        table[`${baseKey}_${plurals.select(count)}`] ?? table[`${baseKey}_other`] ?? baseKey
      return interpolate(template, { count, ...params })
    }

    return {
      locale,
      setLocale,
      t,
      tc,
      formatNumber: (number) => numbers.format(number),
      resolutionLabel: (width, height) => resolutionTier(width, height) ?? t('common.unknown'),
      pixelGain: (originalPixels, foundPixels) => {
        const info = pixelGainInfo(originalPixels, foundPixels)
        if (info.kind === 'more') return t('format.morePixels', { ratio: info.ratio.toFixed(1) })
        if (info.kind === 'fewer') return t('format.fewerPixels', { ratio: info.ratio.toFixed(1) })
        return t('format.samePixels')
      },
      matchTitle: (title) => title?.trim() || t('common.untitled'),
      warningMessage: (warning) => {
        switch (warning.code) {
          case 'duplicates_removed':
            return tc('warnings.duplicates_removed', warning.count ?? 0)
          case 'hidden_by_resolution_filter':
            return tc('warnings.hidden_by_resolution_filter', warning.count ?? 0)
          case 'hidden_by_format_filter':
            return tc('warnings.hidden_by_format_filter', warning.count ?? 0)
          case 'similarity_filter_ignored':
            return t('warnings.similarity_filter_ignored')
          case 'similarity_sort_fallback':
            return t('warnings.similarity_sort_fallback')
          case 'unverified_matches':
            return tc('warnings.unverified_matches', warning.count ?? 0)
          default:
            return null
        }
      },
      errorMessage: (code) => t(errorMessageKey(code)),
    }
  }, [locale, setLocale])

  useEffect(() => {
    document.documentElement.lang = LOCALE_META[locale].htmlLang
    document.title = value.t('meta.title')
    document
      .querySelector('meta[name="description"]')
      ?.setAttribute('content', value.t('meta.description'))
  }, [locale, value])

  return <I18nContext.Provider value={value}>{children}</I18nContext.Provider>
}
