import { apiRequest } from './api'

export interface RatingUser {
  id: number
  username?: string | null
  fullName: string
  role: string
  educationInstitution?: string | null
  rating: number
  createdAtUtc: string
}

export async function getRatingUsers() {
  const response = await apiRequest('/api/ratings/users', {
    skipAuth: true,
    skipAuthRefresh: true,
  })

  if (!response.ok) {
    throw new Error(await readRatingError(response))
  }

  return response.json() as Promise<RatingUser[]>
}

async function readRatingError(response: Response) {
  try {
    const payload = await response.json()
    return payload?.message ?? 'Neizdevās ielādēt reitingu.'
  } catch {
    return 'Neizdevās ielādēt reitingu.'
  }
}
