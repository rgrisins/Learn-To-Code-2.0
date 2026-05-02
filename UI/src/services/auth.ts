import { computed, reactive } from 'vue'
import { apiRequest } from './api'

export interface AuthUser {
  id: number
  username?: string | null
  firstName?: string | null
  lastName?: string | null
  birthDate?: string | null
  fullName: string
  email: string
  role: string
  representation?: string | null
  bio?: string | null
  rating: number
  createdAtUtc: string
}

export interface AuthResponse {
  user: AuthUser
}

export interface LoginRequest {
  username: string
  password: string
}

export interface RegisterRequest {
  username: string
  firstName: string
  lastName: string
  birthDate?: string | null
  email: string
  password: string
  representation?: string | null
  role: string
  roleRequestReason?: string | null
}

export interface UpdateProfileRequest {
  username: string
  firstName: string
  lastName: string
  birthDate?: string | null
  representation?: string | null
  bio?: string | null
  currentPassword?: string | null
  newPassword?: string | null
}

export interface ProfileTheoryLanguageProgress {
  languageId: string
  title: string
  topicCount: number
  progressPercent: number
}

export interface ProfileStats {
  theoryLanguages: ProfileTheoryLanguageProgress[]
  exerciseTotal: number
  exerciseAttempted: number
  exerciseSolved: number
  exerciseSubmissionCount: number
  exercisePassedSubmissions: number
  exerciseFailedSubmissions: number
  exerciseCompletionPercent: number
  exerciseSuccessPercent: number
  mostUsedExerciseLanguage: string | null
  mostUsedExerciseLanguageSubmissionCount: number
}

export interface PublicUserProfile {
  id: number
  username?: string | null
  fullName: string
  role: string
  representation?: string | null
  bio?: string | null
  rating: number
  createdAtUtc: string
  stats: ProfileStats
}

export const authState = reactive({
  user: null as AuthUser | null,
})

export const isAuthenticated = computed(() => {
  return authState.user !== null
})

export function hasAnyRole(roles: string[]) {
  if (!isAuthenticated.value || !authState.user) {
    return false
  }

  return roles.includes(authState.user.role)
}

export function restoreAuthState() {
  return getCurrentSession()
    .then((user) => {
      authState.user = user
    })
    .catch(() => {
      clearAuthState()
    })
}

function saveAuthResponse(payload: AuthResponse) {
  authState.user = payload.user
}

export function clearAuthState() {
  authState.user = null
}

async function parseError(response: Response) {
  try {
    const payload = await response.json()

    if (payload?.errors && typeof payload.errors === 'object') {
      const details = Object.values(payload.errors as Record<string, string[]>)
        .flat()
        .filter((message): message is string => Boolean(message))
        .join(', ')

      if (details) {
        return details
      }
    }

    return payload?.message ?? 'Nezināma kļūda.'
  } catch {
    return 'Nezināma kļūda.'
  }
}

export async function login(request: LoginRequest) {
  const response = await apiRequest('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    throw new Error(await parseError(response))
  }

  const payload = (await response.json()) as AuthResponse
  saveAuthResponse(payload)
  return payload
}

export async function register(request: RegisterRequest) {
  const response = await apiRequest('/api/auth/register', {
    method: 'POST',
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    throw new Error(await parseError(response))
  }

  const payload = (await response.json()) as AuthResponse
  saveAuthResponse(payload)
  return payload
}

export async function logout() {
  try {
    await apiRequest('/api/auth/logout', {
      method: 'POST',
    })
  } catch {
    // Ignore server-side session expiry and still clear the local auth state.
  }

  clearAuthState()
}

export async function getCurrentProfile() {
  const response = await apiRequest('/api/profile/me')
  if (!response.ok) {
    throw new Error(await parseError(response))
  }

  return response.json() as Promise<AuthUser>
}

export async function getProfileStats() {
  const response = await apiRequest('/api/profile/stats')
  if (!response.ok) {
    throw new Error(await parseError(response))
  }

  return response.json() as Promise<ProfileStats>
}

export async function getPublicUserProfile(userId: number) {
  const response = await apiRequest(`/api/profile/users/${encodeURIComponent(userId)}`, {
    skipAuth: true,
    skipAuthRefresh: true,
  })
  if (!response.ok) {
    throw new Error(await parseError(response))
  }

  return response.json() as Promise<PublicUserProfile>
}

export async function getCurrentSession() {
  const response = await apiRequest('/api/auth/session')
  if (!response.ok) {
    throw new Error(await parseError(response))
  }

  const payload = (await response.json()) as AuthResponse
  return payload.user
}

export async function updateProfile(request: UpdateProfileRequest) {
  const response = await apiRequest('/api/profile/me', {
    method: 'PUT',
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    throw new Error(await parseError(response))
  }

  const user = (await response.json()) as AuthUser
  authState.user = user
  return user
}
