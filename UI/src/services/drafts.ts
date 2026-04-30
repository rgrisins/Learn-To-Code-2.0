import { authState } from './auth'

const STORAGE_PREFIX = 'ltc:draft'

function userKey(name: string): string {
  const userId = authState.user?.id ?? 'anon'
  return `${STORAGE_PREFIX}:${userId}:${name}`
}

function safeStorage(): Storage | null {
  try {
    if (typeof window === 'undefined') return null
    return window.localStorage
  } catch {
    return null
  }
}

export function loadDraft<T>(name: string): T | null {
  const storage = safeStorage()
  if (!storage) return null

  const raw = storage.getItem(userKey(name))
  if (!raw) return null

  try {
    return JSON.parse(raw) as T
  } catch {
    storage.removeItem(userKey(name))
    return null
  }
}

export function saveDraft<T>(name: string, value: T): void {
  const storage = safeStorage()
  if (!storage) return

  try {
    storage.setItem(userKey(name), JSON.stringify(value))
  } catch {
    // Out of quota or storage unavailable — ignore silently.
  }
}

export function clearDraft(name: string): void {
  const storage = safeStorage()
  if (!storage) return

  storage.removeItem(userKey(name))
}
