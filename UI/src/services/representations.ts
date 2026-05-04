import { apiRequest } from './api'

export interface Representation {
  id: number
  name: string
  description?: string | null
  isPublic: boolean
  memberCount: number
  totalRating: number
  averageRating: number
  theoryProgressPercent: number
  exerciseSolved: number
  exerciseSolvedLast7Days: number
  exerciseSubmissionCount: number
  isMember: boolean
  isOwner: boolean
  isModerator: boolean
  hasPendingJoinRequest: boolean
  pendingJoinRequestCount: number
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

export interface RepresentationJoinRequest {
  id: number
  userId: number
  username?: string | null
  fullName: string
  userRating: number
  message?: string | null
  createdAtUtc: string
}

export interface CreateRepresentationRequest {
  name: string
  description?: string | null
  isPublic: boolean
}

export interface JoinRequestPayload {
  message?: string | null
}

export type JoinResult =
  | { kind: 'membership'; representation: Representation }
  | { kind: 'pending'; message: string }

export async function getRepresentations() {
  const response = await apiRequest('/api/representations')
  if (!response.ok) {
    throw new Error(await readRepresentationError(response))
  }

  return response.json() as Promise<Representation[]>
}

export async function getRepresentationByName(name: string) {
  const response = await apiRequest(`/api/representations/by-name/${encodeURIComponent(name)}`)
  if (!response.ok) {
    throw new Error(await readRepresentationError(response))
  }

  return response.json() as Promise<Representation>
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

export async function getRepresentationJoinRequests(id: number) {
  const response = await apiRequest(`/api/representations/${encodeURIComponent(id)}/requests`)
  if (!response.ok) {
    throw new Error(await readRepresentationError(response))
  }

  return response.json() as Promise<RepresentationJoinRequest[]>
}

export async function approveJoinRequest(repId: number, requestId: number) {
  const response = await apiRequest(
    `/api/representations/${encodeURIComponent(repId)}/requests/${encodeURIComponent(requestId)}/approve`,
    { method: 'POST' },
  )
  if (!response.ok) {
    throw new Error(await readRepresentationError(response))
  }
}

export async function rejectJoinRequest(repId: number, requestId: number) {
  const response = await apiRequest(
    `/api/representations/${encodeURIComponent(repId)}/requests/${encodeURIComponent(requestId)}/reject`,
    { method: 'POST' },
  )
  if (!response.ok) {
    throw new Error(await readRepresentationError(response))
  }
}

export async function kickMember(repId: number, memberUserId: number) {
  const response = await apiRequest(
    `/api/representations/${encodeURIComponent(repId)}/members/${encodeURIComponent(memberUserId)}`,
    { method: 'DELETE' },
  )
  if (!response.ok) {
    throw new Error(await readRepresentationError(response))
  }
}

export async function updateMemberRole(repId: number, memberUserId: number, role: string) {
  const response = await apiRequest(
    `/api/representations/${encodeURIComponent(repId)}/members/${encodeURIComponent(memberUserId)}/role`,
    {
      method: 'PUT',
      body: JSON.stringify({ role }),
    },
  )
  if (!response.ok) {
    throw new Error(await readRepresentationError(response))
  }
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

export async function updateRepresentation(id: number, request: CreateRepresentationRequest) {
  const response = await apiRequest(`/api/representations/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
  if (!response.ok) {
    throw new Error(await readRepresentationError(response))
  }

  return response.json() as Promise<Representation>
}

export async function deleteRepresentation(id: number) {
  const response = await apiRequest(`/api/representations/${encodeURIComponent(id)}`, {
    method: 'DELETE',
  })
  if (!response.ok) {
    throw new Error(await readRepresentationError(response))
  }
}

export async function joinRepresentation(id: number, payload?: JoinRequestPayload): Promise<JoinResult> {
  const response = await apiRequest(`/api/representations/${encodeURIComponent(id)}/join`, {
    method: 'POST',
    body: JSON.stringify(payload ?? {}),
  })
  if (!response.ok) {
    throw new Error(await readRepresentationError(response))
  }

  const body = await response.json().catch(() => ({}))
  if (response.status === 202 || body?.pendingRequest) {
    return { kind: 'pending', message: body?.message ?? 'Pieprasījums nosūtīts.' }
  }
  return { kind: 'membership', representation: body?.representation ?? body }
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
    return 'Tev nav piekļuves šai darbībai.'
  }

  try {
    const payload = await response.json()
    return payload?.message ?? 'Neizdevās ielādēt pārstāvniecības.'
  } catch {
    return 'Neizdevās ielādēt pārstāvniecības.'
  }
}

// ---- Latvian role label helper -------------------------------------------

export function localizedRoleLabel(role: string | null | undefined) {
  const value = (role ?? '').trim().toLowerCase()
  if (value === 'owner') return 'Īpašnieks'
  if (value === 'moderators' || value === 'moderator') return 'Moderators'
  if (value === 'member') return 'Dalībnieks'
  return role ?? ''
}
