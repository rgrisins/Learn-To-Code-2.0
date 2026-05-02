import { apiRequest } from './api'

export interface RatingUser {
  id: number
  username?: string | null
  fullName: string
  role: string
  representation?: string | null
  rating: number
  createdAtUtc: string
}

export interface RatingRepresentation {
  id: number
  name: string
  description?: string | null
  memberCount: number
  totalRating: number
  averageRating: number
  theoryProgressPercent: number
  exerciseSolved: number
  exerciseSubmissionCount: number
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

export async function getRatingRepresentations() {
  const response = await apiRequest('/api/ratings/representations', {
    skipAuth: true,
    skipAuthRefresh: true,
  })

  if (!response.ok) {
    throw new Error(await readRatingError(response))
  }

  return response.json() as Promise<RatingRepresentation[]>
}

async function readRatingError(response: Response) {
  try {
    const payload = await response.json()
    return payload?.message ?? 'Neizdevās ielādēt reitingu.'
  } catch {
    return 'Neizdevās ielādēt reitingu.'
  }
}
