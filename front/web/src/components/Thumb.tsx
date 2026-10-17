import { useState } from 'react'
import { ImageOff } from 'lucide-react'
import { useI18n } from '../lib/i18n'

interface ThumbProps {
  src: string
  alt: string
  className?: string
}

export function Thumb({ src, alt, className }: ThumbProps) {
  const { t } = useI18n()
  const [failed, setFailed] = useState(false)

  if (failed) {
    return (
      <span
        className={className}
        style={{
          display: 'grid',
          placeItems: 'center',
          width: '100%',
          height: '100%',
          color: 'var(--muted)',
        }}
        role="img"
        aria-label={t('thumb.previewUnavailable', { alt })}
      >
        <ImageOff size={22} />
      </span>
    )
  }

  return (
    <img
      className={className}
      src={src}
      alt={alt}
      loading="lazy"
      decoding="async"
      onError={() => setFailed(true)}
    />
  )
}
