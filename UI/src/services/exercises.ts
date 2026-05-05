import { apiRequest } from './api'

export interface ExerciseListItem {
  id: number
  title: string
  description: string
  difficulty: string
  languageCode: string
  languageVersion: string
  testCaseCount: number
  submissionCount: number
  attemptedUserCount: number
  solvedAttemptPercent: number
  isSolved: boolean
}

export interface ExerciseDetail {
  id: number
  title: string
  description: string
  difficulty: string
  languageCode: string
  languageVersion: string
  visibleTestCases: ExerciseTestCase[]
  isSolved: boolean
  hasPendingDescriptionEditRequest: boolean
}

export interface ExerciseTestCase {
  id: number
  input: string
  expectedOutput: string
  orderIndex: number
}

export interface SubmissionResult {
  id: number
  status: 'Passed' | 'Failed' | 'Error' | 'TimedOut' | string
  testsPassed: number
  testsTotal: number
  errorMessage: string | null
  testResults: TestCaseResult[]
  ratingDelta: number
}

export interface SubmissionProgressEvent {
  type: 'started' | 'progress' | 'completed' | string
  submissionId: number
  current: number
  total: number
  testsPassed: number
  status: string
  errorMessage: string | null
  testResult: TestCaseResult | null
  result: SubmissionResult | null
}

export interface ExerciseSubmission {
  id: number
  exerciseId: number
  exerciseTitle: string
  difficulty: string
  languageCode: string
  status: 'Passed' | 'Failed' | 'Error' | 'TimedOut' | 'Running' | string
  testsPassed: number
  testsTotal: number
  errorMessage: string | null
  testResults: TestCaseResult[]
  submittedAtUtc: string
  executedAtUtc: string | null
}

export interface CreateExerciseTestCase {
  input: string
  expectedOutput: string
  isHidden: boolean
  orderIndex: number
}

export interface CreateExerciseRequest {
  title: string
  description: string
  difficulty: string
  languageCode: string
  solutionLanguageCode: string
  solutionCode: string
  testCases: CreateExerciseTestCase[]
}

export interface UpdateExerciseDescriptionRequest {
  description: string
}

export interface TestCaseResult {
  orderIndex: number
  isHidden: boolean
  passed: boolean
  actualOutput: string | null
  expectedOutput: string | null
  errorMessage: string | null
}

export async function getExercises(): Promise<ExerciseListItem[]> {
  const response = await apiRequest('/api/exercises')
  if (!response.ok) throw new Error(await readError(response))
  return response.json()
}

export async function getExercise(id: number): Promise<ExerciseDetail> {
  const response = await apiRequest(`/api/exercises/${id}`)
  if (!response.ok) throw new Error(await readError(response))
  return response.json()
}

export async function createExercise(request: CreateExerciseRequest): Promise<void> {
  const response = await apiRequest('/api/exercises', {
    method: 'POST',
    body: JSON.stringify(request),
  })
  if (!response.ok) throw new Error(await readError(response))
}

export async function requestExerciseDescriptionEdit(
  id: number,
  request: UpdateExerciseDescriptionRequest,
): Promise<void> {
  const response = await apiRequest(`/api/exercises/${id}/description-request`, {
    method: 'POST',
    body: JSON.stringify(request),
  })
  if (!response.ok) throw new Error(await readError(response))
}

export interface PendingExerciseTestCase {
  orderIndex: number
  isHidden: boolean
  input: string
  expectedOutput: string
}

export interface PendingExercise {
  id: number
  requestType: 'Create' | 'EditDescription' | string
  exerciseId: number | null
  title: string
  description: string
  difficulty: string
  languageCode: string
  languageVersion: string
  authorId: number | null
  authorName: string
  authorEmail: string
  createdAtUtc: string
  status: 'Pending' | 'Approved' | 'Rejected' | string
  testCases: PendingExerciseTestCase[]
}

export type PendingExerciseStatusFilter = 'Pending' | 'Approved' | 'Rejected' | ''

export async function getPendingExercises(
  status: PendingExerciseStatusFilter = 'Pending',
): Promise<PendingExercise[]> {
  const search = new URLSearchParams()
  if (status) {
    search.set('status', status)
  } else {
    search.set('status', 'all')
  }
  const response = await apiRequest(`/api/exercises/admin/pending?${search.toString()}`)
  if (!response.ok) throw new Error(await readError(response))
  return response.json()
}

export async function approvePendingExercise(id: number): Promise<ExerciseListItem> {
  const response = await apiRequest(`/api/exercises/admin/${id}/approve`, { method: 'POST' })
  if (!response.ok) throw new Error(await readError(response))
  return response.json()
}

export async function rejectPendingExercise(id: number): Promise<void> {
  const response = await apiRequest(`/api/exercises/admin/${id}/reject`, { method: 'POST' })
  if (!response.ok) throw new Error(await readError(response))
}

export async function deleteExercise(id: number): Promise<void> {
  const response = await apiRequest(`/api/exercises/admin/exercises/${id}`, { method: 'DELETE' })
  if (!response.ok) throw new Error(await readError(response))
}

export async function getSubmissions(): Promise<ExerciseSubmission[]> {
  const response = await apiRequest('/api/exercises/submissions')
  if (!response.ok) throw new Error(await readError(response))
  return response.json()
}

export async function submitCode(
  id: number,
  code: string,
  languageCode: string,
): Promise<SubmissionResult> {
  const response = await apiRequest(`/api/exercises/${id}/submit`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ code, languageCode }),
  })
  if (!response.ok) throw new Error(await readError(response))
  return response.json()
}

export async function submitCodeWithProgress(
  id: number,
  code: string,
  languageCode: string,
  onProgress: (event: SubmissionProgressEvent) => void,
): Promise<SubmissionResult> {
  const response = await apiRequest(`/api/exercises/${id}/submit-stream`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ code, languageCode }),
  })

  if (!response.ok) throw new Error(await readError(response))
  if (!response.body) throw new Error('Neizdevas nolasit parbaudes progresu.')

  const reader = response.body.getReader()
  const decoder = new TextDecoder()
  let buffer = ''
  let finalResult: SubmissionResult | null = null

  function handleLine(line: string) {
    const trimmed = line.trim()
    if (!trimmed) return

    const event = JSON.parse(trimmed) as SubmissionProgressEvent
    onProgress(event)

    if (event.result) {
      finalResult = event.result
    }
  }

  while (true) {
    const { value, done } = await reader.read()
    if (done) break

    buffer += decoder.decode(value, { stream: true })
    const lines = buffer.split(/\r?\n/)
    buffer = lines.pop() ?? ''
    lines.forEach(handleLine)
  }

  buffer += decoder.decode()
  handleLine(buffer)

  if (!finalResult) {
    throw new Error('Parbaude beidzas bez rezultata.')
  }

  return finalResult
}

async function readError(response: Response): Promise<string> {
  if (response.status === 401) return 'Ludzu ielogojies, lai iesniegtu kodu.'
  try {
    const payload = await response.json()
    return payload?.message ?? 'Kluda.'
  } catch {
    return 'Kluda.'
  }
}
