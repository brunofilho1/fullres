import { Languages } from 'lucide-react'
import { useI18n } from '../lib/i18n'
import { LOCALE_OPTIONS, type Locale } from '../lib/i18n/locales'

export function LanguageSwitcher() {
  const { locale, setLocale, t } = useI18n()

  return (
    <span className="lang-switch">
      <Languages size={14} aria-hidden="true" />
      <select
        className="lang-select"
        value={locale}
        onChange={(event) => setLocale(event.target.value as Locale)}
        aria-label={t('header.languageLabel')}
      >
        {LOCALE_OPTIONS.map((option) => (
          <option key={option.code} value={option.code}>
            {option.label}
          </option>
        ))}
      </select>
    </span>
  )
}
