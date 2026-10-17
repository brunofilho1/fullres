import { useSyncExternalStore } from 'react'

export type Route = 'home' | 'how-it-works' | 'providers'

const DOC_ROUTES = ['how-it-works', 'providers'] as const

function parse(hash: string): Route {
  const path = hash.replace(/^#\/?/, '').replace(/\/+$/, '')
  return (DOC_ROUTES as readonly string[]).includes(path) ? (path as Route) : 'home'
}

let current: Route = parse(window.location.hash)
const listeners = new Set<() => void>()

function handleHashChange() {
  const next = parse(window.location.hash)
  if (next === current) return
  current = next
  for (const listener of listeners) listener()
}

window.addEventListener('hashchange', handleHashChange)

function subscribe(listener: () => void) {
  listeners.add(listener)
  return () => {
    listeners.delete(listener)
  }
}

function getSnapshot(): Route {
  return current
}

export function useRoute(): Route {
  return useSyncExternalStore(subscribe, getSnapshot, getSnapshot)
}

export function navigate(route: Route): void {
  const hash = route === 'home' ? '#/' : `#/${route}`
  if (window.location.hash === hash) return
  window.location.hash = hash
}

export function routeHref(route: Route): string {
  return route === 'home' ? '#/' : `#/${route}`
}
