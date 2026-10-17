export function formatBytes(bytes: number | null | undefined): string {
  if (!bytes) return '—'
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

export function pixels(width: number | null, height: number | null): number {
  if (width === null || height === null) return 0
  return width * height
}

export type ResolutionTier = '4K UHD' | 'QHD' | 'FHD' | 'HD' | 'SD'

/** Technical tier token for a resolution, or null when the dimensions are unknown. */
export function resolutionTier(width: number | null, height: number | null): ResolutionTier | null {
  if (width === null || height === null) return null
  const h = Math.max(width, height)
  const w = Math.min(width, height)
  if (w >= 3800 && h >= 2100) return '4K UHD'
  if (w >= 2500 && h >= 1400) return 'QHD'
  if (w >= 1900 && h >= 1050) return 'FHD'
  if (w >= 1260 && h >= 700) return 'HD'
  return 'SD'
}

export type PixelGainInfo =
  | { kind: 'more'; ratio: number }
  | { kind: 'fewer'; ratio: number }
  | { kind: 'same'; ratio: number }

/** How many more (or fewer) pixels `found` has than `original`, in a form ready for formatting. */
export function pixelGainInfo(original: number, found: number): PixelGainInfo {
  const ratio = found / original
  if (ratio >= 1.05) return { kind: 'more', ratio }
  if (ratio <= 0.95) return { kind: 'fewer', ratio: 1 / ratio }
  return { kind: 'same', ratio: 1 }
}

export function aspectRatio(width: number | null, height: number | null): string {
  if (width === null || height === null) return '—'
  const gcd = (a: number, b: number): number => (b === 0 ? a : gcd(b, a % b))
  const d = gcd(width, height) || 1
  const w = Math.round(width / d)
  const h = Math.round(height / d)
  if (w > 40 || h > 40) {
    const r = width / height
    if (Math.abs(r - 16 / 9) < 0.03) return '16:9'
    if (Math.abs(r - 4 / 3) < 0.03) return '4:3'
    if (Math.abs(r - 1) < 0.02) return '1:1'
    if (Math.abs(r - 3 / 2) < 0.03) return '3:2'
    if (Math.abs(r - 21 / 9) < 0.05) return '21:9'
    return `${r.toFixed(2)}:1`
  }
  return `${w}:${h}`
}

export function ratioGroup(width: number | null, height: number | null): string {
  if (width === null || height === null) return 'other'
  const r = width / height
  if (Math.abs(r - 16 / 9) < 0.04) return '16:9'
  if (Math.abs(r - 4 / 3) < 0.04) return '4:3'
  if (Math.abs(r - 1) < 0.03) return '1:1'
  if (Math.abs(r - 3 / 2) < 0.04) return '3:2'
  return 'other'
}
