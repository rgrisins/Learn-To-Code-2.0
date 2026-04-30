export const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL as string | undefined)?.replace(/\/$/, '') ?? ''

export interface ApiRequestInit extends RequestInit {
  skipAuth?: boolean
  skipAuthRefresh?: boolean
}

interface AuthSessionPayload {
  accessToken?: string | null
  accessTokenExpiresAtUtc?: string | null
  tokenType?: string | null
  user?: unknown
}

let accessToken: string | null = null
let refreshPromise: Promise<string | null> | null = null
let onSessionRefreshed: ((payload: AuthSessionPayload) => void) | null = null
let onSessionExpired: (() => void) | null = null

export function setAccessToken(token?: string | null) {
  accessToken = token || null
}

export function clearAccessToken() {
  accessToken = null
}

export function setAuthSessionHandlers(handlers: {
  onRefreshed?: (payload: AuthSessionPayload) => void
  onExpired?: () => void
}) {
  onSessionRefreshed = handlers.onRefreshed ?? null
  onSessionExpired = handlers.onExpired ?? null
}

export async function apiRequest(path: string, init: ApiRequestInit = {}) {
  const response = await sendApiRequest(path, init)

  if (response.status !== 401 || init.skipAuthRefresh) {
    return response
  }

  const refreshedToken = await refreshAccessToken()
  if (!refreshedToken) {
    return response
  }

  return sendApiRequest(path, init)
}

async function sendApiRequest(path: string, init: ApiRequestInit = {}) {
  const requestInit: ApiRequestInit = { ...init }
  delete requestInit.skipAuth
  delete requestInit.skipAuthRefresh

  const headers = new Headers(requestInit.headers)

  if (!headers.has('Content-Type') && requestInit.body) {
    headers.set('Content-Type', 'application/json')
  }

  if (!init.skipAuth && accessToken && !headers.has('Authorization')) {
    headers.set('Authorization', `Bearer ${accessToken}`)
  }

  const requestUrl = apiBaseUrl ? `${apiBaseUrl}${path}` : path

  return fetch(requestUrl, {
    ...requestInit,
    headers,
    credentials: 'include',
  })
}

async function refreshAccessToken() {
  if (!refreshPromise) {
    refreshPromise = requestSessionToken().finally(() => {
      refreshPromise = null
    })
  }

  return refreshPromise
}

async function requestSessionToken() {
  const response = await sendApiRequest('/api/auth/session', {
    skipAuth: true,
    skipAuthRefresh: true,
  })

  if (!response.ok) {
    clearAccessToken()
    onSessionExpired?.()
    return null
  }

  const payload = (await response.json()) as AuthSessionPayload
  setAccessToken(payload.accessToken)
  onSessionRefreshed?.(payload)

  return accessToken
}
