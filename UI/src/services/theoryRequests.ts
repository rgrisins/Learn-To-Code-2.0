import { apiRequest } from './api'

export interface TheoryTopicRequest {
  id: number
  userId: number
  username?: string
  fullName: string
  email: string
  requestType: 'Add' | 'Edit'
  languageCode: string
  topicSlug?: string
  proposedTitle?: string
  proposedDescription?: string
  proposedDifficulty?: string
  proposedEstimatedMinutes?: number
  proposedMarkdown?: string
  status: 'Pending' | 'Approved' | 'Rejected'
  createdAtUtc: string
}

export interface TheoryContentRequest {
  id: number
  userId: number
  username?: string
  fullName: string
  email: string
  languageCode: string
  topicSlug: string
  requestType: 'Add' | 'Edit'
  pageIndex?: number
  proposedMarkdown: string
  status: 'Pending' | 'Approved' | 'Rejected'
  createdAtUtc: string
}

export interface TheoryQuizQuestionPayload {
  prompt: string
  explanation?: string | null
  options: string[]
  correctOptionIndex: number
}

export interface TheoryQuizRequest {
  id: number
  userId: number
  username?: string
  fullName: string
  email: string
  requestType: 'Add' | 'Edit'
  languageCode: string
  topicSlug: string
  proposedTitle: string
  proposedDescription: string
  questions: TheoryQuizQuestionPayload[]
  status: 'Pending' | 'Approved' | 'Rejected'
  createdAtUtc: string
}

export interface CreateTopicRequestPayload {
  requestType: 'Add' | 'Edit'
  languageCode: string
  topicSlug?: string
  proposedTitle?: string
  proposedDescription?: string
  proposedDifficulty?: string
  proposedEstimatedMinutes?: number
  proposedMarkdown?: string
}

export interface CreateContentRequestPayload {
  requestType: 'Add' | 'Edit'
  languageCode: string
  topicSlug: string
  pageIndex?: number
  proposedMarkdown: string
}

export interface CreateQuizRequestPayload {
  requestType: 'Add' | 'Edit'
  languageCode: string
  topicSlug: string
  proposedTitle: string
  proposedDescription: string
  questions: TheoryQuizQuestionPayload[]
}

export async function submitTopicRequest(payload: CreateTopicRequestPayload) {
  const response = await apiRequest('/api/theory/topic-requests', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
  if (!response.ok) throw new Error(await readError(response))
  return response.json() as Promise<TheoryTopicRequest>
}

export async function submitContentRequest(payload: CreateContentRequestPayload) {
  const response = await apiRequest('/api/theory/content-requests', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
  if (!response.ok) throw new Error(await readError(response))
  return response.json() as Promise<TheoryContentRequest>
}

export async function submitQuizRequest(payload: CreateQuizRequestPayload) {
  const response = await apiRequest('/api/theory/quiz-requests', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
  if (!response.ok) throw new Error(await readError(response))
  return response.json() as Promise<TheoryQuizRequest>
}

export async function getAdminTopicRequests(status?: string) {
  const url = status
    ? `/api/admin/theory-requests/topics?status=${encodeURIComponent(status)}`
    : '/api/admin/theory-requests/topics'
  const response = await apiRequest(url)
  if (!response.ok) throw new Error(await readError(response))
  return response.json() as Promise<TheoryTopicRequest[]>
}

export async function getAdminContentRequests(status?: string) {
  const url = status
    ? `/api/admin/theory-requests/content?status=${encodeURIComponent(status)}`
    : '/api/admin/theory-requests/content'
  const response = await apiRequest(url)
  if (!response.ok) throw new Error(await readError(response))
  return response.json() as Promise<TheoryContentRequest[]>
}

export async function getAdminQuizRequests(status?: string) {
  const url = status
    ? `/api/admin/theory-requests/quizzes?status=${encodeURIComponent(status)}`
    : '/api/admin/theory-requests/quizzes'
  const response = await apiRequest(url)
  if (!response.ok) throw new Error(await readError(response))
  return response.json() as Promise<TheoryQuizRequest[]>
}

export async function approveTopicRequest(id: number) {
  const response = await apiRequest(`/api/admin/theory-requests/topics/${id}/approve`, { method: 'POST' })
  if (!response.ok) throw new Error(await readError(response))
  return response.json() as Promise<TheoryTopicRequest>
}

export async function rejectTopicRequest(id: number) {
  const response = await apiRequest(`/api/admin/theory-requests/topics/${id}/reject`, { method: 'POST' })
  if (!response.ok) throw new Error(await readError(response))
  return response.json() as Promise<TheoryTopicRequest>
}

export async function approveContentRequest(id: number) {
  const response = await apiRequest(`/api/admin/theory-requests/content/${id}/approve`, { method: 'POST' })
  if (!response.ok) throw new Error(await readError(response))
  return response.json() as Promise<TheoryContentRequest>
}

export async function rejectContentRequest(id: number) {
  const response = await apiRequest(`/api/admin/theory-requests/content/${id}/reject`, { method: 'POST' })
  if (!response.ok) throw new Error(await readError(response))
  return response.json() as Promise<TheoryContentRequest>
}

export async function approveQuizRequest(id: number) {
  const response = await apiRequest(`/api/admin/theory-requests/quizzes/${id}/approve`, { method: 'POST' })
  if (!response.ok) throw new Error(await readError(response))
  return response.json() as Promise<TheoryQuizRequest>
}

export async function rejectQuizRequest(id: number) {
  const response = await apiRequest(`/api/admin/theory-requests/quizzes/${id}/reject`, { method: 'POST' })
  if (!response.ok) throw new Error(await readError(response))
  return response.json() as Promise<TheoryQuizRequest>
}

async function readError(response: Response) {
  if (response.status === 401) return 'Sesija beigusies. Ielogojies velreiz.'
  if (response.status === 403) return 'Nav pietiekamu tiesibu.'
  try {
    const payload = await response.json()
    return payload?.message ?? 'Nezinama kluda.'
  } catch {
    return 'Nezinama kluda.'
  }
}
