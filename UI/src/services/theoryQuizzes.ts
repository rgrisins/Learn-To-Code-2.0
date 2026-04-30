import { apiRequest } from './api'

export interface TheoryQuizOption {
  id: number
  orderIndex: number
  text: string
}

export interface TheoryQuizQuestion {
  id: number
  orderIndex: number
  prompt: string
  explanation: string | null
  isAnswered: boolean
  isAnsweredCorrectly: boolean
  selectedOptionId: number | null
  correctOptionId: number | null
  options: TheoryQuizOption[]
}

export interface TheoryQuiz {
  id: number
  topicId: number
  topicSlug: string
  topicTitle: string
  languageCode: string
  title: string
  description: string
  questionCount: number
  correctAnswerCount: number
  questions: TheoryQuizQuestion[]
}

export interface TheoryQuizManagementQuestion {
  id: number
  orderIndex: number
  prompt: string
  explanation: string | null
  correctOptionIndex: number
  options: string[]
}

export interface TheoryQuizManagement {
  id: number
  topicId: number
  topicSlug: string
  topicTitle: string
  languageCode: string
  title: string
  description: string
  questions: TheoryQuizManagementQuestion[]
}

export interface TheoryQuizAnswerResult {
  questionId: number
  selectedOptionId: number
  correctOptionId: number
  isCorrect: boolean
  ratingAwarded: boolean
  ratingDelta: number
  explanation: string | null
}

export async function getTheoryQuiz(
  languageCode: string,
  topicSlug: string,
): Promise<TheoryQuiz> {
  const response = await apiRequest(
    `/api/theory/languages/${encodeURIComponent(languageCode)}/topics/${encodeURIComponent(topicSlug)}/quiz`,
  )
  if (!response.ok) throw new Error(await readError(response))
  return response.json()
}

export async function getTheoryQuizForManagement(
  languageCode: string,
  topicSlug: string,
): Promise<TheoryQuizManagement> {
  const response = await apiRequest(
    `/api/theory/languages/${encodeURIComponent(languageCode)}/topics/${encodeURIComponent(topicSlug)}/quiz/manage`,
  )
  if (!response.ok) throw new Error(await readError(response))
  return response.json()
}

export async function submitTheoryQuizAnswer(
  languageCode: string,
  topicSlug: string,
  questionId: number,
  optionId: number,
): Promise<TheoryQuizAnswerResult> {
  const response = await apiRequest(
    `/api/theory/languages/${encodeURIComponent(languageCode)}/topics/${encodeURIComponent(topicSlug)}/quiz/answer`,
    {
      method: 'POST',
      body: JSON.stringify({ questionId, optionId }),
    },
  )
  if (!response.ok) throw new Error(await readError(response))
  return response.json()
}

export async function deleteTheoryQuiz(languageCode: string, topicSlug: string): Promise<void> {
  const response = await apiRequest(
    `/api/theory/languages/${encodeURIComponent(languageCode)}/topics/${encodeURIComponent(topicSlug)}/quiz`,
    { method: 'DELETE' },
  )
  if (!response.ok) throw new Error(await readError(response))
}

async function readError(response: Response): Promise<string> {
  if (response.status === 401) return 'Lūdzu ielogojies, lai turpinātu testu.'
  try {
    const payload = await response.json()
    return payload?.message ?? 'Neizdevās ielādēt testu.'
  } catch {
    return 'Neizdevās ielādēt testu.'
  }
}
