import { apiRequest } from './api'

export interface TheoryLanguage {
  id: string
  title: string
  description: string
  imageUrl: string
  topicCount: number
  progressPercent: number
}

export interface TheoryTopic {
  id: string
  languageId: string
  title: string
  difficulty: string
  description: string
  estimatedMinutes: number
  pageCount: number
  progressPercent: number
  hasQuiz: boolean
  quizQuestionCount: number
  quizAnsweredCount: number
  quizCorrectAnswerCount: number
}

export interface TheoryLanguageDetail {
  id: string
  title: string
  description: string
  imageUrl: string
  progressPercent: number
  topics: TheoryTopic[]
}

export interface TheoryPage {
  languageId: string
  topicId: string
  topicTitle: string
  difficulty: string
  description: string
  estimatedMinutes: number
  pageIndex: number
  pageCount: number
  isRead: boolean
  topicProgressPercent: number
  markdown: string
}

export interface TheoryProgressUpdate {
  languageId: string
  topicId: string
  pageIndex: number
  isRead: boolean
  topicProgressPercent: number
  languageProgressPercent: number
}

export async function getTheoryLanguages(): Promise<TheoryLanguage[]> {
  const response = await apiRequest('/api/theory/languages')
  if (!response.ok) throw new Error(await readError(response))
  return response.json()
}

export async function getTheoryLanguage(languageId: string): Promise<TheoryLanguageDetail> {
  const response = await apiRequest(`/api/theory/languages/${encodeURIComponent(languageId)}`)
  if (!response.ok) throw new Error(await readError(response))
  return response.json()
}

export async function getTheoryPage(
  languageId: string,
  topicId: string,
  page: number,
): Promise<TheoryPage> {
  const response = await apiRequest(
    `/api/theory/languages/${encodeURIComponent(languageId)}/topics/${encodeURIComponent(topicId)}/pages/${page}`,
  )
  if (!response.ok) throw new Error(await readError(response))
  return response.json()
}

export async function getReadPageIndices(
  languageId: string,
  topicId: string,
): Promise<number[]> {
  const response = await apiRequest(
    `/api/theory/languages/${encodeURIComponent(languageId)}/topics/${encodeURIComponent(topicId)}/read-pages`,
  )
  if (!response.ok) throw new Error(await readError(response))
  return response.json()
}

export async function markTheoryPageRead(
  languageId: string,
  topicId: string,
  page: number,
): Promise<TheoryProgressUpdate> {
  const response = await apiRequest(
    `/api/theory/languages/${encodeURIComponent(languageId)}/topics/${encodeURIComponent(topicId)}/pages/${page}/read`,
    { method: 'POST' },
  )
  if (!response.ok) throw new Error(await readError(response))
  return response.json()
}

export async function deleteTheoryTopic(languageId: string, topicId: string): Promise<void> {
  const response = await apiRequest(
    `/api/theory/languages/${encodeURIComponent(languageId)}/topics/${encodeURIComponent(topicId)}`,
    { method: 'DELETE' },
  )
  if (!response.ok) throw new Error(await readError(response))
}

export async function deleteTheoryPage(
  languageId: string,
  topicId: string,
  page: number,
): Promise<void> {
  const response = await apiRequest(
    `/api/theory/languages/${encodeURIComponent(languageId)}/topics/${encodeURIComponent(topicId)}/pages/${page}`,
    { method: 'DELETE' },
  )
  if (!response.ok) throw new Error(await readError(response))
}

async function readError(response: Response): Promise<string> {
  if (response.status === 401) return 'Ludzu ielogojies, lai turpinatu.'
  if (response.status === 403) return 'Nav pietiekamu tiesibu.'
  try {
    const payload = await response.json()
    return payload?.message ?? 'Neizdevas iegut teoriju.'
  } catch {
    return 'Neizdevas iegut teoriju.'
  }
}
