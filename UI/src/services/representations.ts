import { apiRequest } from './api'

export interface Representation {
  id: number
  name: string
  description?: string | null
  memberCount: number
  totalRating: number
  averageRating: number
  theoryProgressPercent: number
  exerciseSolved: number
  exerciseSubmissionCount: number
  isMember: boolean
  isOwner: boolean
  createdAtUtc: string
}

export interface RepresentationMember {
  userId: number
  username?: string | null
  fullName: string
  role: string
  rating: number
  joinedAtUtc: string
}

export interface CreateRepresentationRequest {
  name: string
  description?: string | null
}

export async function getRepresentations() {
  const response = await apiRequest('/api/representations')
  if (!response.ok) {
    throw new Error(await readRepresentationError(response))
  }

  return response.json() as Promise<Representation[]>
}

export async function getMyRepresentations() {
  const response = await apiRequest('/api/representations/mine')
  if (!response.ok) {
    throw new Error(await readRepresentationError(response))
  }

  return response.json() as Promise<Representation[]>
}

export async function getRepresentationMembers(id: number) {
  const response = await apiRequest(`/api/representations/${encodeURIComponent(id)}/members`)
  if (!response.ok) {
    throw new Error(await readRepresentationError(response))
  }

  return response.json() as Promise<RepresentationMember[]>
}

export async function createRepresentation(request: CreateRepresentationRequest) {
  const response = await apiRequest('/api/representations', {
    method: 'POST',
    body: JSON.stringify(request),
  })
  if (!response.ok) {
    throw new Error(await readRepresentationError(response))
  }

  return response.json() as Promise<Representation>
}

export async function joinRepresentation(id: number) {
  const response = await apiRequest(`/api/representations/${encodeURIComponent(id)}/join`, {
    method: 'POST',
  })
  if (!response.ok) {
    throw new Error(await readRepresentationError(response))
  }

  return response.json() as Promise<Representation>
}

export async function leaveRepresentation(id: number) {
  const response = await apiRequest(`/api/representations/${encodeURIComponent(id)}/leave`, {
    method: 'DELETE',
  })
  if (!response.ok) {
    throw new Error(await readRepresentationError(response))
  }
}

async function readRepresentationError(response: Response) {
  if (response.status === 401) {
    return 'Sesija beigusies. Ielogojies vēlreiz.'
  }

  if (response.status === 403) {
    return 'Tev nav piekļuves šai pārstāvniecībai.'
  }

  try {
    const payload = await response.json()
    return payload?.message ?? 'Neizdevās ielādēt pārstāvniecības.'
  } catch {
    return 'Neizdevās ielādēt pārstāvniecības.'
  }
}
