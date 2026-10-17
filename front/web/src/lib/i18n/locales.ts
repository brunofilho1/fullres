export const LOCALES = ['en', 'pt-BR', 'es'] as const

export type Locale = (typeof LOCALES)[number]

export interface LocaleMeta {
  code: Locale
  /** Native name shown in the language switcher. */
  label: string
  /** Value for the <html lang> attribute. */
  htmlLang: string
  /** BCP 47 tag used by the Intl API. */
  intl: string
}

export const LOCALE_META: Record<Locale, LocaleMeta> = {
  en: { code: 'en', label: 'English', htmlLang: 'en', intl: 'en-US' },
  'pt-BR': { code: 'pt-BR', label: 'Português (Brasil)', htmlLang: 'pt-BR', intl: 'pt-BR' },
  es: { code: 'es', label: 'Español', htmlLang: 'es', intl: 'es-ES' },
}

export const LOCALE_OPTIONS: LocaleMeta[] = LOCALES.map((code) => LOCALE_META[code])

export function isLocale(value: string): value is Locale {
  return (LOCALES as readonly string[]).includes(value)
}

/**
 * Picks the best supported locale from a list of browser language tags
 * (navigator.languages), following the order the browser reports them in.
 */
export function detectLocale(languages: readonly string[]): Locale {
  for (const language of languages) {
    const normalized = language.toLowerCase()
    if (normalized.startsWith('pt')) return 'pt-BR'
    if (normalized.startsWith('es')) return 'es'
    if (normalized.startsWith('en')) return 'en'
  }
  return 'en'
}
