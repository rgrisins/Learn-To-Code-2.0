import { apiRequest } from './api'
import type { AuthUser } from './auth'

export interface AdminUpdateUserRequest {
  username: string
  firstName: string
  lastName: string
  birthDate?: string | null
  educationInstitution?: string | null
  role: string
  rating: number
}

export async function getAdminUsers() {
  const response = await apiRequest('/api/admin/users')
  if (!response.ok) {
    throw new Error(await readAdminError(response))
  }

  return response.json() as Promise<AuthUser[]>
}

export async function updateAdminUser(userId: number, request: AdminUpdateUserRequest) {
  const response = await apiRequest(`/api/admin/users/${userId}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    throw new Error(await readAdminError(response))
  }

  return response.json() as Promise<AuthUser>
}

export async function deleteAdminUser(userId: number) {
  const response = await apiRequest(`/api/admin/users/${userId}`, {
    method: 'DELETE',
  })

  if (!response.ok) {
    throw new Error(await readAdminError(response))
  }
}

async function readAdminError(response: Response) {
  if (response.status === 403) {
    return 'Tev nav administratora tiesibu.'
  }

  if (response.status === 401) {
    return 'Sesija beigusies. Ielogojies velreiz.'
  }

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

    return payload?.message ?? 'Nezinama kluda.'
  } catch {
    return 'Nezinama kluda.'
  }
}
