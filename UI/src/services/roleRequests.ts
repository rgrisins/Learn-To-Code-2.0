import { apiRequest } from './api'

export interface RoleRequest {
  id: number
  userId: number
  username?: string | null
  fullName: string
  email: string
  currentRole: string
  requestedRole: string
  reason: string
  status: string
  createdAtUtc: string
}

export async function getMyRoleRequests() {
  const response = await apiRequest('/api/profile/role-requests')
  if (!response.ok) {
    throw new Error(await readRoleRequestError(response))
  }
  return response.json() as Promise<RoleRequest[]>
}

export async function createRoleRequest(requestedRole: string, reason: string) {
  const response = await apiRequest('/api/profile/role-requests', {
    method: 'POST',
    body: JSON.stringify({ requestedRole, reason }),
  })
  if (!response.ok) {
    throw new Error(await readRoleRequestError(response))
  }
  return response.json() as Promise<RoleRequest>
}

export async function getAdminRoleRequests(status?: string) {
  const query = status ? `?status=${encodeURIComponent(status)}` : ''
  const response = await apiRequest(`/api/admin/role-requests${query}`)
  if (!response.ok) {
    throw new Error(await readRoleRequestError(response))
  }
  return response.json() as Promise<RoleRequest[]>
}

export async function approveRoleRequest(id: number) {
  const response = await apiRequest(`/api/admin/role-requests/${id}/approve`, {
    method: 'POST',
  })
  if (!response.ok) {
    throw new Error(await readRoleRequestError(response))
  }
  return response.json() as Promise<RoleRequest>
}

export async function rejectRoleRequest(id: number) {
  const response = await apiRequest(`/api/admin/role-requests/${id}/reject`, {
    method: 'POST',
  })
  if (!response.ok) {
    throw new Error(await readRoleRequestError(response))
  }
  return response.json() as Promise<RoleRequest>
}

async function readRoleRequestError(response: Response) {
  if (response.status === 403) {
    return 'Tev nav tiesibu veikt so darbibu.'
  }
  if (response.status === 401) {
    return 'Sesija beigusies. Ielogojies velreiz.'
  }
  try {
    const payload = await response.json()
    return payload?.message ?? 'Nezinama kluda.'
  } catch {
    return 'Nezinama kluda.'
  }
}
