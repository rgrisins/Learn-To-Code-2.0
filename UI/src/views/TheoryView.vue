<script setup lang="ts">
import MarkdownIt from 'markdown-it'
import { computed, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { hasAnyRole, isAuthenticated } from '../services/auth'
import {
  deleteTheoryPage,
  deleteTheoryTopic,
  getReadPageIndices,
  getTheoryLanguage,
  getTheoryLanguages,
  getTheoryPage,
  markTheoryPageRead,
  type TheoryLanguage,
  type TheoryLanguageDetail,
  type TheoryPage,
  type TheoryProgressUpdate,
  type TheoryTopic,
} from '../services/theory'
import { deleteTheoryQuiz, getTheoryQuizForManagement } from '../services/theoryQuizzes'
import {
  submitContentRequest,
  submitQuizRequest,
  submitTopicRequest,
  type TheoryQuizQuestionPayload,
} from '../services/theoryRequests'
import { clearDraft, loadDraft, saveDraft } from '../services/drafts'
import algoritmiLogo from '../assets/algoritmi.png'

const md = new MarkdownIt({ html: false, linkify: true, breaks: false })

const route = useRoute()
const router = useRouter()

const theoryLanguages = ref<TheoryLanguage[]>([])
const selectedLanguage = ref<TheoryLanguageDetail | null>(null)
const selectedTopic = ref<TheoryTopic | null>(null)
const currentPage = ref<TheoryPage | null>(null)
const theoryError = ref('')
const isLoadingLanguages = ref(false)
const isLoadingLanguage = ref(false)
const isLoadingPage = ref(false)
const isCompletingPage = ref(false)

const renderedMarkdown = computed(() => md.render(currentPage.value?.markdown || ''))
const readPageIndices = ref(new Set<number>())
const activeLanguageId = computed(() => (typeof route.query.language === 'string' ? route.query.language : ''))
const activeTopicId = computed(() => (typeof route.query.topic === 'string' ? route.query.topic : ''))
const activePageNumber = computed(() => readPageNumber(route.query.page, 1))
const showReader = computed(() => Boolean(selectedTopic.value && currentPage.value))
const showPageLoading = computed(() => Boolean(activeLanguageId.value && activeTopicId.value && isLoadingPage.value))
const nextTopic = computed(() => {
  if (!selectedLanguage.value || !selectedTopic.value) return null

  const currentIndex = selectedLanguage.value.topics.findIndex((topic) => topic.id === selectedTopic.value?.id)
  return currentIndex >= 0 ? selectedLanguage.value.topics[currentIndex + 1] ?? null : null
})
const canShowReadProgress = computed(() => isAuthenticated.value)
const isLastPage = computed(() =>
  Boolean(currentPage.value && currentPage.value.pageIndex >= currentPage.value.pageCount),
)
const readerForwardDisabled = computed(() => {
  if (!currentPage.value || isLoadingPage.value || isCompletingPage.value) return true
  if (!isAuthenticated.value) return isLastPage.value
  return currentPage.value.isRead && isLastPage.value
})
const readerForwardLabel = computed(() => {
  if (isCompletingPage.value) return 'Saglabā...'
  if (!currentPage.value) return 'Nākamā lapa'
  if (!isAuthenticated.value) return isLastPage.value ? 'Pēdējā lapa' : 'Nākamā lapa'
  return currentPage.value.isRead ? 'Nākamā lapa' : 'Izlasīju'
})
const showNextTopicPrompt = computed(() =>
  Boolean(
    currentPage.value &&
      nextTopic.value &&
      (!isAuthenticated.value || currentPage.value.isRead) &&
      currentPage.value.pageIndex >= currentPage.value.pageCount,
  ),
)

const currentTopicHasQuiz = computed(() => Boolean(selectedTopic.value?.hasQuiz))

const showQuizPrompt = computed(() =>
  Boolean(
    currentTopicHasQuiz.value &&
      currentPage.value &&
      currentPage.value.pageIndex >= currentPage.value.pageCount &&
      (!isAuthenticated.value || currentPage.value.isRead),
  ),
)

// ─── Proposal modal ───────────────────────────────────────────────────────────

type RequestCategory = 'addTopic' | 'editTopic' | 'addContent' | 'editContent'

const isModalOpen = ref(false)
const isSubmitting = ref(false)
const proposalError = ref('')
const proposalSuccess = ref('')
const requestCategory = ref<RequestCategory>('addTopic')
const markdownPreviewMode = ref(false)
const previewMarkdown = computed(() => md.render(form.markdown || ''))
const canSubmitTheoryRequests = computed(() => hasAnyRole(['Pedagogs', 'Administrators']))
const canManageTheory = computed(() => hasAnyRole(['Administrators']))
const theoryEditingEnabled = ref(false)
const isAddMenuOpen = ref(false)
const isPickQuizTopicOpen = ref(false)

// ─── Languages list filters ──────────────────────────────────────────────────
const languageSearch = ref('')
const hideCompletedLanguages = ref(false)
const databaseSearch = ref('')
const hideCompletedDatabases = ref(false)

const ALGORITHM_TITLES = new Set(['algoritmi'])
const DATABASE_TITLES = new Set(['mysql', 'mongodb'])
const DATABASE_IMAGE_URLS: Record<string, string> = {
  mysql: '/theory/mysql.png',
  mongodb: '/theory/mongodb.png',
}

function isAlgorithmCategory(language: { title: string }) {
  return ALGORITHM_TITLES.has(normalizeCategoryTitle(language.title))
}

function isDatabaseCategory(language: { title: string }) {
  return DATABASE_TITLES.has(normalizeCategoryTitle(language.title))
}

function getLanguageImageUrl(language: { title: string; imageUrl: string }): string {
  const databaseImageUrl = DATABASE_IMAGE_URLS[normalizeCategoryTitle(language.title)]
  if (databaseImageUrl) return databaseImageUrl
  return isAlgorithmCategory(language) ? algoritmiLogo : language.imageUrl
}

function normalizeCategoryTitle(title: string) {
  return title.trim().toLowerCase()
}

function applyLanguageFilters(list: TheoryLanguage[]): TheoryLanguage[] {
  let result = list
  if (canShowReadProgress.value && hideCompletedLanguages.value) {
    result = result.filter((language) => language.progressPercent < 100)
  }
  const query = languageSearch.value.trim().toLowerCase()
  if (query) {
    result = result.filter((language) =>
      `${language.title} ${language.description}`.toLowerCase().includes(query),
    )
  }
  return result
}

function applyDatabaseFilters(list: TheoryLanguage[]): TheoryLanguage[] {
  let result = list
  if (canShowReadProgress.value && hideCompletedDatabases.value) {
    result = result.filter((language) => language.progressPercent < 100)
  }
  const query = databaseSearch.value.trim().toLowerCase()
  if (query) {
    result = result.filter((language) =>
      `${language.title} ${language.description}`.toLowerCase().includes(query),
    )
  }
  return result
}

const programmingLanguages = computed(() =>
  applyLanguageFilters(
    theoryLanguages.value.filter((lang) => !isAlgorithmCategory(lang) && !isDatabaseCategory(lang)),
  ),
)

const algorithmCategories = computed(() =>
  applyLanguageFilters(theoryLanguages.value.filter((lang) => isAlgorithmCategory(lang))).map(
    (lang) => ({ ...lang, imageUrl: algoritmiLogo }),
  ),
)

const databaseCategories = computed(() =>
  applyDatabaseFilters(theoryLanguages.value.filter((lang) => isDatabaseCategory(lang))).map(
    (lang) => ({ ...lang, imageUrl: getLanguageImageUrl(lang) }),
  ),
)

const allDatabaseCategories = computed(() =>
  theoryLanguages.value.filter((lang) => isDatabaseCategory(lang)),
)

const filteredLanguages = computed(() => [
  ...programmingLanguages.value,
  ...algorithmCategories.value,
  ...databaseCategories.value,
])

const overallProgressPercent = computed(() => {
  // Aprēķina TIKAI no programmēšanas valodām — algoritmu kategorijas neietekmē
  // programmēšanas valodu kopējo apguves procentu.
  const languages = theoryLanguages.value.filter(
    (lang) => !isAlgorithmCategory(lang) && !isDatabaseCategory(lang),
  )
  if (!languages.length) return 0
  const totalTopics = languages.reduce((sum, language) => sum + (language.topicCount ?? 0), 0)
  if (totalTopics > 0) {
    const weighted = languages.reduce(
      (sum, language) => sum + (language.progressPercent ?? 0) * (language.topicCount ?? 0),
      0,
    )
    return Math.round(weighted / totalTopics)
  }
  const sum = languages.reduce((s, language) => s + (language.progressPercent ?? 0), 0)
  return Math.round(sum / languages.length)
})

const databaseProgressPercent = computed(() => {
  const databases = allDatabaseCategories.value
  if (!databases.length) return 0
  const totalTopics = databases.reduce((sum, language) => sum + (language.topicCount ?? 0), 0)
  if (totalTopics > 0) {
    const weighted = databases.reduce(
      (sum, language) => sum + (language.progressPercent ?? 0) * (language.topicCount ?? 0),
      0,
    )
    return Math.round(weighted / totalTopics)
  }
  const sum = databases.reduce((total, language) => total + (language.progressPercent ?? 0), 0)
  return Math.round(sum / databases.length)
})

function goToHome() {
  void router.push({ name: 'home' })
}
const modalTitle = computed(() => {
  if (requestCategory.value === 'addContent') return 'Pievienot teoriju'
  if (requestCategory.value === 'editContent') return 'Rediģēt teoriju'
  if (requestCategory.value === 'editTopic') return 'Rediģēt tēmu'
  return 'Pievienot tēmu'
})

type TheoryDeleteTarget = 'topic' | 'page' | 'quiz'

const isDeleteTheoryModalOpen = ref(false)
const isDeletingTheory = ref(false)
const deleteTheoryTarget = ref<TheoryDeleteTarget>('page')
const deleteTheoryTitle = computed(() => {
  if (deleteTheoryTarget.value === 'topic') {
    return selectedTopic.value ? `Dzēst tēmu “${selectedTopic.value.title}”?` : 'Dzēst tēmu?'
  }

  if (deleteTheoryTarget.value === 'quiz') {
    return selectedTopic.value ? `Dzēst "${selectedTopic.value.title}"?` : 'Dzēst?'
  }

  return currentPage.value
    ? `Dzēst ${currentPage.value.pageIndex}. teorijas lapu?`
    : 'Dzēst teorijas lapu?'
})
const deleteTheoryDescription = computed(() => {
  if (deleteTheoryTarget.value === 'quiz') {
    return 'Tests, tā jautājumi un iesniegtās atbildes tiks dzēstas.'
  }

  if (deleteTheoryTarget.value === 'topic') {
    return 'Tēma, visas tās teorijas lapas un lasīšanas progress tiks dzēsts.'
  }

  return 'Lapa tiks dzēsta, atlikušās lapas tiks pārnumurētas, un šīs tēmas lasīšanas progress tiks atiestatīts.'
})

const form = reactive({
  title: '',
  description: '',
  difficulty: 'Iesācējs',
  estimatedMinutes: 30,
  markdown: '',
})

const difficulties = ['Iesācējs', 'Vidējs', 'Pieredzējis']

type QuizRequestMode = 'Add' | 'Edit'

interface QuizQuestionForm {
  prompt: string
  explanation: string
  options: [string, string, string, string]
  correctOptionIndex: number
}

const isQuizModalOpen = ref(false)
const isSubmittingQuizRequest = ref(false)
const isLoadingQuizForEdit = ref(false)
const quizRequestMode = ref<QuizRequestMode>('Add')
const maxQuizQuestions = 100
const quizRequestError = ref('')
const quizRequestSuccess = ref('')
const quizForm = reactive({
  title: '',
  description: '',
  questions: [] as QuizQuestionForm[],
})

interface TheoryDraft {
  title: string
  description: string
  difficulty: string
  estimatedMinutes: number
  markdown: string
}

const draftRestored = ref(false)
const draftSavedAt = ref<Date | null>(null)
const draftSavedLabel = computed(() => {
  if (!draftSavedAt.value) return ''
  return draftSavedAt.value.toLocaleTimeString('lv-LV', { hour: '2-digit', minute: '2-digit' })
})

function theoryDraftKey(): string | null {
  if (!selectedLanguage.value) return null
  const language = selectedLanguage.value.id
  if (requestCategory.value === 'addTopic') {
    return `theory:${language}:addTopic`
  }
  if (requestCategory.value === 'editTopic' && selectedTopic.value) {
    return `theory:${language}:editTopic:${selectedTopic.value.id}`
  }
  if (requestCategory.value === 'addContent' && selectedTopic.value) {
    return `theory:${language}:addContent:${selectedTopic.value.id}`
  }
  if (requestCategory.value === 'editContent' && selectedTopic.value && currentPage.value) {
    return `theory:${language}:editContent:${selectedTopic.value.id}:${currentPage.value.pageIndex}`
  }
  return null
}

function persistTheoryDraft() {
  const key = theoryDraftKey()
  if (!key || !isModalOpen.value) return

  const draft = currentTheoryDraft()
  if (isTheoryDraftPristine(draft)) {
    clearDraft(key)
    draftSavedAt.value = null
    draftRestored.value = false
    return
  }

  saveDraft<TheoryDraft>(key, draft)
  draftSavedAt.value = new Date()
}

function currentTheoryDraft(): TheoryDraft {
  return {
    title: form.title,
    description: form.description,
    difficulty: form.difficulty,
    estimatedMinutes: form.estimatedMinutes,
    markdown: form.markdown,
  }
}

function isTheoryDraftPristine(draft: TheoryDraft) {
  const baseline = editingBaseline.value ?? buildEmptyTheoryBaseline()

  return normalizeDraftText(draft.title) === normalizeDraftText(baseline.title) &&
    normalizeDraftText(draft.description) === normalizeDraftText(baseline.description) &&
    normalizeDraftText(draft.difficulty) === normalizeDraftText(baseline.difficulty) &&
    Number(draft.estimatedMinutes) === Number(baseline.estimatedMinutes) &&
    normalizeDraftText(draft.markdown) === normalizeDraftText(baseline.markdown)
}

function normalizeDraftText(value: string | null | undefined) {
  return (value ?? '').trim()
}

function buildEmptyTheoryBaseline(): TheoryModalBaseline {
  return {
    title: '',
    description: '',
    difficulty: 'Iesācējs',
    estimatedMinutes: 30,
    markdown: '',
  }
}

function applyTheoryDraft(): boolean {
  const key = theoryDraftKey()
  if (!key) return false

  const draft = loadDraft<TheoryDraft>(key)
  if (!draft) return false
  if (isTheoryDraftPristine(draft)) {
    clearDraft(key)
    return false
  }

  if (draft.title) form.title = draft.title
  if (draft.description) form.description = draft.description
  if (draft.difficulty) form.difficulty = draft.difficulty
  if (Number.isFinite(draft.estimatedMinutes)) form.estimatedMinutes = draft.estimatedMinutes
  if (draft.markdown) form.markdown = draft.markdown
  return true
}

function clearTheoryDraft() {
  const key = theoryDraftKey()
  if (!key) return
  clearDraft(key)
  draftRestored.value = false
  draftSavedAt.value = null
}

function discardTheoryDraft() {
  clearTheoryDraft()
  // For edit modes, restore to the values that existed before the user started editing.
  // For add modes, reset to a pristine form.
  applyModalBaseline(editingBaseline.value ?? buildEmptyTheoryBaseline())
}

function createBlankQuizQuestion(): QuizQuestionForm {
  return {
    prompt: '',
    explanation: '',
    options: ['', '', '', ''],
    correctOptionIndex: 0,
  }
}

function resetQuizForm(topic: TheoryTopic) {
  quizForm.title = `Tests: ${topic.title}`
  quizForm.description = 'Pārbaudi zināšanas par šo tēmu.'
  quizForm.questions.splice(0, quizForm.questions.length, createBlankQuizQuestion())
}

async function openQuizModal(topic: TheoryTopic, mode: QuizRequestMode) {
  if (!selectedLanguage.value || !canSubmitTheoryRequests.value) return

  selectedTopic.value = topic
  quizRequestMode.value = mode
  quizRequestError.value = ''
  quizRequestSuccess.value = ''
  resetQuizForm(topic)
  isQuizModalOpen.value = true

  if (mode !== 'Edit') return

  isLoadingQuizForEdit.value = true
  try {
    const quiz = await getTheoryQuizForManagement(selectedLanguage.value.id, topic.id)
    quizForm.title = quiz.title
    quizForm.description = quiz.description
    quizForm.questions.splice(
      0,
      quizForm.questions.length,
      ...quiz.questions.map((question) => ({
        prompt: question.prompt,
        explanation: question.explanation ?? '',
        options: [
          question.options[0] ?? '',
          question.options[1] ?? '',
          question.options[2] ?? '',
          question.options[3] ?? '',
        ] as [string, string, string, string],
        correctOptionIndex: question.correctOptionIndex,
      })),
    )
  } catch (error) {
    quizRequestError.value = error instanceof Error ? error.message : 'Neizdevās ielādēt esošo testu.'
  } finally {
    isLoadingQuizForEdit.value = false
  }
}

function closeQuizModal() {
  if (isSubmittingQuizRequest.value) return
  isQuizModalOpen.value = false
  quizRequestError.value = ''
  quizRequestSuccess.value = ''
}

function toggleAddMenu() {
  isAddMenuOpen.value = !isAddMenuOpen.value
}

function closeAddMenu() {
  isAddMenuOpen.value = false
}

function startAddTopicFromMenu() {
  closeAddMenu()
  openModal('addTopic')
}

function startAddQuizFromMenu() {
  closeAddMenu()
  isPickQuizTopicOpen.value = true
}

function closePickQuizTopic() {
  isPickQuizTopicOpen.value = false
}

const topicsWithoutQuiz = computed(() => {
  if (!selectedLanguage.value) return []
  return selectedLanguage.value.topics.filter((topic) => !topic.hasQuiz)
})

function pickTopicForQuiz(topic: TheoryTopic) {
  closePickQuizTopic()
  openQuizModal(topic, 'Add')
}

function addQuizQuestion() {
  if (quizForm.questions.length >= maxQuizQuestions) return
  quizForm.questions.push(createBlankQuizQuestion())
}

function removeQuizQuestion(index: number) {
  if (quizForm.questions.length <= 1) return
  quizForm.questions.splice(index, 1)
}

function normalizeQuizForm(): TheoryQuizQuestionPayload[] | null {
  if (!quizForm.title.trim()) {
    quizRequestError.value = 'Testa nosaukums nevar būt tukšs.'
    return null
  }

  if (quizForm.questions.length === 0) {
    quizRequestError.value = 'Testam jāpievieno vismaz viens jautājums.'
    return null
  }

  if (quizForm.questions.length > maxQuizQuestions) {
    quizRequestError.value = `Testā var būt ne vairāk kā ${maxQuizQuestions} jautājumi.`
    return null
  }

  const questions: TheoryQuizQuestionPayload[] = []

  for (const [questionIndex, question] of quizForm.questions.entries()) {
    const prompt = question.prompt.trim()
    if (!prompt) {
      quizRequestError.value = `Jautājumam Nr. ${questionIndex + 1} jānorāda teksts.`
      return null
    }

    const options = question.options.map((option) => option.trim())
    const emptyOptionIndex = options.findIndex((option) => !option)
    if (emptyOptionIndex >= 0) {
      quizRequestError.value = `Jautājuma Nr. ${questionIndex + 1} atbilde Nr. ${emptyOptionIndex + 1} nevar būt tukša.`
      return null
    }

    questions.push({
      prompt,
      explanation: question.explanation.trim() || null,
      options,
      correctOptionIndex: question.correctOptionIndex,
    })
  }

  return questions
}

async function submitQuizProposal() {
  if (!selectedLanguage.value || !selectedTopic.value) return

  const questions = normalizeQuizForm()
  if (!questions) return

  quizRequestError.value = ''
  quizRequestSuccess.value = ''
  isSubmittingQuizRequest.value = true

  try {
    await submitQuizRequest({
      requestType: quizRequestMode.value,
      languageCode: selectedLanguage.value.id,
      topicSlug: selectedTopic.value.id,
      proposedTitle: quizForm.title.trim(),
      proposedDescription: quizForm.description.trim(),
      questions,
    })
    quizRequestSuccess.value = 'Testa pieprasījums iesniegts! Administrators to izskatīs drīzumā.'
  } catch (error) {
    quizRequestError.value = error instanceof Error ? error.message : 'Neizdevās iesniegt testa pieprasījumu.'
  } finally {
    isSubmittingQuizRequest.value = false
  }
}

function openQuizDeleteModal(topic: TheoryTopic) {
  selectedTopic.value = topic
  openDeleteTheoryModal('quiz')
}

watch(form, () => {
  if (isModalOpen.value) {
    persistTheoryDraft()
  }
})

// ─── Topic filters ────────────────────────────────────────────────────────────

const topicSearch = ref('')
const topicDiffFilter = ref('all')
const hideCompleted = ref(false)
const topicPage = ref(1)
const topicsPerPage = 4

const filteredTopics = computed(() => {
  if (!selectedLanguage.value) return []
  let list = [...selectedLanguage.value.topics]

  if (canShowReadProgress.value && hideCompleted.value) list = list.filter((t) => t.progressPercent < 100)

  const query = topicSearch.value.trim().toLowerCase()
  if (query) list = list.filter((t) => `${t.title} ${t.description}`.toLowerCase().includes(query))

  if (topicDiffFilter.value !== 'all') {
    list = list.filter((t) => formatDifficulty(t.difficulty).toLowerCase() === topicDiffFilter.value.toLowerCase())
  }

  return list
})

const topicPageCount = computed(() => Math.max(1, Math.ceil(filteredTopics.value.length / topicsPerPage)))

const pagedTopics = computed(() => {
  const start = (topicPage.value - 1) * topicsPerPage
  return filteredTopics.value.slice(start, start + topicsPerPage)
})

const topicPageSummary = computed(() => {
  if (!filteredTopics.value.length) return '0 no 0'
  const start = (topicPage.value - 1) * topicsPerPage + 1
  const end = Math.min(topicPage.value * topicsPerPage, filteredTopics.value.length)
  return `${start}–${end} no ${filteredTopics.value.length}`
})

watch([topicSearch, topicDiffFilter, hideCompleted], () => { topicPage.value = 1 })
watch(topicPageCount, (count) => { if (topicPage.value > count) topicPage.value = count })

watch(activeLanguageId, () => {
  topicSearch.value = ''
  topicDiffFilter.value = 'all'
  hideCompleted.value = false
  topicPage.value = 1
})

let readPagesRequestId = 0

watch([activeLanguageId, activeTopicId, isAuthenticated], async ([languageId, topicId, authenticated]) => {
  const requestId = ++readPagesRequestId
  readPageIndices.value = new Set()
  if (!languageId || !topicId || !authenticated) return

  try {
    const indices = await getReadPageIndices(languageId, topicId)
    if (requestId !== readPagesRequestId) return
    readPageIndices.value = new Set(indices)
  } catch {
    // local tracking still works
  }
}, { immediate: true })

interface TheoryModalBaseline {
  title: string
  description: string
  difficulty: string
  estimatedMinutes: number
  markdown: string
}

const editingBaseline = ref<TheoryModalBaseline | null>(null)

function buildModalBaseline(category: RequestCategory): TheoryModalBaseline {
  if (category === 'editContent') {
    return {
      title: '',
      description: '',
      difficulty: 'Iesācējs',
      estimatedMinutes: 30,
      markdown: currentPage.value?.markdown ?? '',
    }
  }

  if (category === 'editTopic' && selectedTopic.value) {
    return {
      title: selectedTopic.value.title,
      description: selectedTopic.value.description,
      difficulty: selectedTopic.value.difficulty,
      estimatedMinutes: selectedTopic.value.estimatedMinutes,
      markdown: '',
    }
  }

  // addTopic / addContent — pristine baseline.
  return buildEmptyTheoryBaseline()
}

function applyModalBaseline(baseline: TheoryModalBaseline) {
  form.title = baseline.title
  form.description = baseline.description
  form.difficulty = baseline.difficulty
  form.estimatedMinutes = baseline.estimatedMinutes
  form.markdown = baseline.markdown
}

function openModal(category: RequestCategory) {
  if (!canSubmitTheoryRequests.value || !selectedLanguage.value) return
  if (category === 'addContent' && !selectedTopic.value) return
  if (category === 'editContent' && !currentPage.value) return
  if (category === 'editTopic' && !selectedTopic.value) return

  proposalError.value = ''
  proposalSuccess.value = ''
  requestCategory.value = category

  const baseline = buildModalBaseline(category)
  editingBaseline.value = category === 'editTopic' || category === 'editContent' ? baseline : null
  applyModalBaseline(baseline)

  markdownPreviewMode.value = false
  isModalOpen.value = true
  draftRestored.value = applyTheoryDraft()
  if (!draftRestored.value) {
    draftSavedAt.value = null
  } else {
    draftSavedAt.value = new Date()
  }
}

function closeModal() {
  isModalOpen.value = false
  markdownPreviewMode.value = false
  draftRestored.value = false
  draftSavedAt.value = null
  editingBaseline.value = null
}

function openTopicEditModal(topic: TheoryTopic) {
  selectedTopic.value = topic
  openModal('editTopic')
}

function openTopicDeleteModal(topic: TheoryTopic) {
  selectedTopic.value = topic
  openDeleteTheoryModal('topic')
}

function openDeleteTheoryModal(target: TheoryDeleteTarget) {
  if (!canManageTheory.value || !selectedLanguage.value || !selectedTopic.value) return
  if (target === 'page' && !currentPage.value) return
  if (target === 'quiz' && !selectedTopic.value.hasQuiz) return

  theoryError.value = ''
  deleteTheoryTarget.value = target
  isDeleteTheoryModalOpen.value = true
}

function closeDeleteTheoryModal() {
  if (isDeletingTheory.value) return
  isDeleteTheoryModalOpen.value = false
}

async function confirmDeleteTheory() {
  if (!selectedLanguage.value || !selectedTopic.value) return

  const languageId = selectedLanguage.value.id
  const topicId = selectedTopic.value.id
  const deletedPageIndex = currentPage.value?.pageIndex ?? 1

  theoryError.value = ''
  isDeletingTheory.value = true

  try {
    if (deleteTheoryTarget.value === 'topic') {
      await deleteTheoryTopic(languageId, topicId)
      const language = await getTheoryLanguage(languageId)
      selectedLanguage.value = language
      selectedTopic.value = null
      currentPage.value = null
      readPageIndices.value = new Set()
      theoryLanguages.value = theoryLanguages.value.map((languageItem) =>
        languageItem.id === language.id
          ? { ...languageItem, topicCount: language.topics.length, progressPercent: language.progressPercent }
          : languageItem,
      )
      closeDeleteTheoryAfterSuccess()
      await router.replace({ name: 'theory', query: { language: languageId } })
      return
    }

    if (deleteTheoryTarget.value === 'quiz') {
      await deleteTheoryQuiz(languageId, topicId)
      const language = await getTheoryLanguage(languageId)
      selectedLanguage.value = language
      selectedTopic.value = language.topics.find((item) => item.id === topicId) ?? selectedTopic.value
      closeDeleteTheoryAfterSuccess()
      return
    }

    await deleteTheoryPage(languageId, topicId, deletedPageIndex)
    const language = await getTheoryLanguage(languageId)
    const topic = language.topics.find((item) => item.id === topicId) ?? null
    if (!topic) {
      selectedLanguage.value = language
      selectedTopic.value = null
      currentPage.value = null
      closeDeleteTheoryAfterSuccess()
      await router.replace({ name: 'theory', query: { language: languageId } })
      return
    }

    const nextPageIndex = Math.min(deletedPageIndex, topic.pageCount)
    const page = await getTheoryPage(languageId, topicId, nextPageIndex)
    selectedLanguage.value = language
    selectedTopic.value = topic
    currentPage.value = page
    readPageIndices.value = new Set()
    closeDeleteTheoryAfterSuccess()
    await router.replace({ name: 'theory', query: { language: languageId, topic: topicId, page: nextPageIndex } })
  } catch (error) {
    theoryError.value = error instanceof Error ? error.message : 'Neizdevās dzēst teorijas saturu.'
  } finally {
    isDeletingTheory.value = false
  }
}

function closeDeleteTheoryAfterSuccess() {
  isDeleteTheoryModalOpen.value = false
}

async function submitProposal() {
  if (!selectedLanguage.value) return

  proposalError.value = ''
  proposalSuccess.value = ''

  const languageCode = selectedLanguage.value.id

  if (requestCategory.value === 'addContent' || requestCategory.value === 'editContent') {
    if (!form.markdown.trim()) {
      proposalError.value = 'Saturs nevar būt tukšs.'
      return
    }
    if (!selectedTopic.value || (requestCategory.value === 'editContent' && !currentPage.value)) return

    isSubmitting.value = true
    try {
      await submitContentRequest({
        requestType: requestCategory.value === 'editContent' ? 'Edit' : 'Add',
        languageCode,
        topicSlug: selectedTopic.value.id,
        pageIndex: requestCategory.value === 'editContent' ? currentPage.value?.pageIndex : undefined,
        proposedMarkdown: form.markdown.trim(),
      })
      proposalSuccess.value = requestCategory.value === 'editContent'
        ? 'Teorijas labojuma pieprasījums iesniegts! Administrators to izskatīs drīzumā.'
        : 'Teorijas pievienošanas pieprasījums iesniegts! Administrators to izskatīs drīzumā.'
      clearTheoryDraft()
    } catch (err) {
      proposalError.value = err instanceof Error ? err.message : 'Neizdevās iesniegt pieprasījumu.'
    } finally {
      isSubmitting.value = false
    }
    return
  }

  if (requestCategory.value === 'editTopic') {
    if (!selectedTopic.value) return
    if (!form.title.trim()) { proposalError.value = 'Virsraksts nevar būt tukšs.'; return }
    if (!form.description.trim()) { proposalError.value = 'Apraksts nevar būt tukšs.'; return }

    isSubmitting.value = true
    try {
      await submitTopicRequest({
        requestType: 'Edit',
        languageCode,
        topicSlug: selectedTopic.value.id,
        proposedTitle: form.title.trim(),
        proposedDescription: form.description.trim(),
        proposedDifficulty: form.difficulty,
        proposedEstimatedMinutes: form.estimatedMinutes,
      })
      proposalSuccess.value = 'Tēmas pieprasījums iesniegts! Administrators to izskatīs drīzumā.'
      clearTheoryDraft()
    } catch (err) {
      proposalError.value = err instanceof Error ? err.message : 'Neizdevās iesniegt pieprasījumu.'
    } finally {
      isSubmitting.value = false
    }
    return
  }

  // addTopic
  if (!form.title.trim()) { proposalError.value = 'Virsraksts nevar būt tukšs.'; return }
  if (!form.description.trim()) { proposalError.value = 'Apraksts nevar būt tukšs.'; return }
  if (!form.markdown.trim()) { proposalError.value = 'Sākotnējais saturs nevar būt tukšs.'; return }

  isSubmitting.value = true
  try {
    await submitTopicRequest({
      requestType: 'Add',
      languageCode,
      proposedTitle: form.title.trim(),
      proposedDescription: form.description.trim(),
      proposedDifficulty: form.difficulty,
      proposedEstimatedMinutes: form.estimatedMinutes,
      proposedMarkdown: form.markdown.trim(),
    })
    proposalSuccess.value = 'Jaunās tēmas pieprasījums iesniegts! Administrators to izskatīs drīzumā.'
    clearTheoryDraft()
  } catch (err) {
    proposalError.value = err instanceof Error ? err.message : 'Neizdevās iesniegt pieprasījumu.'
  } finally {
    isSubmitting.value = false
  }
}

// ─── Navigation ───────────────────────────────────────────────────────────────

let syncRequestId = 0

watch([activeLanguageId, activeTopicId, activePageNumber], () => {
  void syncTheorySelection()
}, { immediate: true })

function readPageNumber(value: string | null | Array<string | null> | undefined, fallback: number) {
  const rawValue = Array.isArray(value) ? value[0] : value
  const parsedValue = Number.parseInt(rawValue ?? '', 10)
  return Number.isFinite(parsedValue) && parsedValue > 0 ? parsedValue : fallback
}

async function syncTheorySelection() {
  const requestId = ++syncRequestId
  const languageId = activeLanguageId.value
  const topicId = activeTopicId.value
  const pageNumber = activePageNumber.value

  theoryError.value = ''

  if (!languageId) {
    selectedLanguage.value = null
    selectedTopic.value = null
    currentPage.value = null
    isLoadingLanguage.value = false
    isLoadingPage.value = false

    if (theoryLanguages.value.length === 0) {
      isLoadingLanguages.value = true
      try {
        const languages = await getTheoryLanguages()
        if (requestId !== syncRequestId) return
        theoryLanguages.value = languages
      } catch (error) {
        if (requestId !== syncRequestId) return
        theoryError.value = error instanceof Error ? error.message : 'Neizdevās ielādēt valodas.'
      } finally {
        if (requestId === syncRequestId) isLoadingLanguages.value = false
      }
    }
    return
  }

  isLoadingLanguages.value = false

  if (!selectedLanguage.value || selectedLanguage.value.id !== languageId) {
    selectedLanguage.value = null
    selectedTopic.value = null
    currentPage.value = null
    isLoadingLanguage.value = true

    try {
      const language = await getTheoryLanguage(languageId)
      if (requestId !== syncRequestId) return
      selectedLanguage.value = language
    } catch (error) {
      if (requestId !== syncRequestId) return
      theoryError.value = error instanceof Error ? error.message : 'Neizdevās ielādēt valodu.'
      selectedLanguage.value = null
      selectedTopic.value = null
      currentPage.value = null
      return
    } finally {
      if (requestId === syncRequestId) isLoadingLanguage.value = false
    }
  }

  if (requestId !== syncRequestId || !selectedLanguage.value) return

  if (!topicId) {
    selectedTopic.value = null
    currentPage.value = null
    return
  }

  const topic = selectedLanguage.value.topics.find((item) => item.id === topicId) ?? null
  if (!topic) {
    selectedTopic.value = null
    currentPage.value = null
    theoryError.value = 'Izvēlētā tēma nav pieejama.'
    return
  }

  selectedTopic.value = topic

  const currentPageMatchesSelection = Boolean(
    currentPage.value &&
      currentPage.value.topicId === topic.id &&
      currentPage.value.pageIndex === pageNumber,
  )
  if (currentPageMatchesSelection) return

  isLoadingPage.value = true
  try {
    const page = await getTheoryPage(selectedLanguage.value.id, topic.id, pageNumber)
    if (requestId !== syncRequestId) return
    currentPage.value = page
    if (canShowReadProgress.value && page.isRead) readPageIndices.value.add(page.pageIndex)
  } catch (error) {
    if (requestId !== syncRequestId) return
    theoryError.value = error instanceof Error ? error.message : 'Neizdevās ielādēt teorijas lapu.'
    currentPage.value = null
  } finally {
    if (requestId === syncRequestId) isLoadingPage.value = false
  }
}

function selectLanguage(languageId: string) {
  void router.push({ name: 'theory', query: { language: languageId } })
}

function selectTopic(topicId: string) {
  if (!selectedLanguage.value) return
  void router.push({ name: 'theory', query: { language: selectedLanguage.value.id, topic: topicId, page: 1 } })
}

function backToLanguages() {
  void router.replace({ name: 'theory' })
}

function backToTopics() {
  if (!activeLanguageId.value) return
  void router.replace({ name: 'theory', query: { language: activeLanguageId.value } })
}

function goToPage(pageNumber: number) {
  if (!selectedLanguage.value || !selectedTopic.value) return
  void router.replace({
    name: 'theory',
    query: { language: selectedLanguage.value.id, topic: selectedTopic.value.id, page: pageNumber },
  })
}

function goToQuiz(topicId: string) {
  if (!selectedLanguage.value) return
  const target = router.resolve({
    name: 'theory-quiz',
    params: { languageId: selectedLanguage.value.id, topicId },
  })

  if (!isAuthenticated.value) {
    void router.push({ name: 'login', query: { redirect: target.fullPath } })
    return
  }

  void router.push(target)
}

function startNextTopic() {
  if (!selectedLanguage.value || !nextTopic.value) return
  void router.replace({
    name: 'theory',
    query: { language: selectedLanguage.value.id, topic: nextTopic.value.id, page: 1 },
  })
}

async function recordTheoryPageRead(page: TheoryPage) {
  if (!isAuthenticated.value) {
    return false
  }

  try {
    const progress = await markTheoryPageRead(page.languageId, page.topicId, page.pageIndex)
    applyTheoryProgress(progress)
    return true
  } catch {
    theoryError.value = 'Neizdevās saglabāt teorijas progresu.'
    return false
  }
}

async function handleReaderForward() {
  if (!currentPage.value) return

  const page = currentPage.value
  if (!isAuthenticated.value) {
    if (page.pageIndex < page.pageCount) {
      goToPage(page.pageIndex + 1)
    }
    return
  }

  if (page.isRead) {
    if (page.pageIndex < page.pageCount) {
      goToPage(page.pageIndex + 1)
    }
    return
  }

  isCompletingPage.value = true
  try {
    const wasMarkedRead = await recordTheoryPageRead(page)
    if (wasMarkedRead && page.pageIndex < page.pageCount) {
      goToPage(page.pageIndex + 1)
    }
  } finally {
    isCompletingPage.value = false
  }
}

function applyTheoryProgress(progress: TheoryProgressUpdate) {
  theoryLanguages.value = theoryLanguages.value.map((language) =>
    language.id === progress.languageId
      ? { ...language, progressPercent: progress.languageProgressPercent }
      : language,
  )

  if (selectedLanguage.value?.id === progress.languageId) {
    selectedLanguage.value = {
      ...selectedLanguage.value,
      progressPercent: progress.languageProgressPercent,
      topics: selectedLanguage.value.topics.map((topic) =>
        topic.id === progress.topicId
          ? { ...topic, progressPercent: progress.topicProgressPercent }
          : topic,
      ),
    }

    selectedTopic.value = selectedLanguage.value.topics.find((topic) => topic.id === progress.topicId) ?? selectedTopic.value
  }

  if (currentPage.value?.languageId === progress.languageId && currentPage.value.topicId === progress.topicId) {
    currentPage.value = {
      ...currentPage.value,
      isRead: currentPage.value.pageIndex === progress.pageIndex || currentPage.value.isRead,
      topicProgressPercent: progress.topicProgressPercent,
    }
    readPageIndices.value.add(progress.pageIndex)
  }
}

function formatDifficulty(difficulty: string): string {
  if (difficulty.toLowerCase() === 'pamati') return 'Iesācējs'
  return difficulty
}

function difficultyClass(difficulty: string) {
  const normalized = difficulty.toLowerCase() === 'pamati' ? 'iesācējs' : difficulty.toLowerCase()
  return `theory-difficulty--${normalized}`
}

function topicQuizPercent(topic: TheoryTopic): number {
  if (!topic.quizQuestionCount) return 0
  return Math.round((topic.quizAnsweredCount / topic.quizQuestionCount) * 100)
}

function formatSectionCount(count: number): string {
  return count === 1 ? '1 sadaļa' : `${count} sadaļas`
}

</script>

<template>
  <div class="theory-breadcrumb mb-3">
    <button v-if="!activeLanguageId" class="app-back-link" type="button" @click="goToHome">
      <span aria-hidden="true">←</span>
      Mājas lapa
    </button>
    <template v-else>
      <button class="app-back-link" type="button" @click="backToLanguages">
        <span aria-hidden="true">←</span>
        Teorija
      </button>
      <button
        v-if="activeTopicId && selectedLanguage"
        class="app-back-link"
        type="button"
        @click="backToTopics"
      >
        <span aria-hidden="true">←</span>
        {{ selectedLanguage.title }}
      </button>
    </template>
  </div>

  <section
    v-if="!activeLanguageId && !isLoadingLanguages && algorithmCategories.length"
    class="content-panel card border-primary-subtle theory-panel mb-3"
  >
    <div class="card-body p-3 p-lg-4 theory-panel__body">
      <div class="theory-list theory-list--languages">
        <button
          v-for="language in algorithmCategories"
          :key="language.id"
          type="button"
          class="theory-list-item"
          @click="selectLanguage(language.id)"
        >
          <img
            class="theory-list-item__image"
            :src="language.imageUrl"
            :alt="language.title"
          />
          <div class="theory-list-item__body">
            <strong>{{ language.title }}</strong>
            <span>{{ language.description }}</span>
          </div>
          <div class="theory-list-item__meta">
            <div class="theory-language-meta-count">
              <span class="theory-card__kicker">{{ language.topicCount }} tēmas</span>
            </div>
            <div v-if="canShowReadProgress" class="theory-list-item__progress">
              <span class="theory-card__progress">{{ language.progressPercent }}% apgūts</span>
              <span class="theory-progress-track" aria-hidden="true">
                <span class="theory-progress-bar" :style="{ width: `${language.progressPercent}%` }"></span>
              </span>
            </div>
          </div>
        </button>
      </div>
    </div>
  </section>

  <section class="content-panel card border-primary-subtle theory-panel" :class="{ 'theory-editing-enabled': theoryEditingEnabled }">
    <div class="card-body p-3 p-lg-4 theory-panel__body">
      <div class="theory-header">
        <img
          v-if="selectedLanguage"
          class="theory-language-badge"
          :src="getLanguageImageUrl(selectedLanguage)"
          :alt="selectedLanguage.title"
        />
        <span v-else class="page-title-icon page-title-icon--theory" aria-hidden="true">
          <svg viewBox="0 0 24 24" width="34" height="34" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z" />
            <path d="M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z" />
          </svg>
        </span>
        <div class="theory-heading-copy">
          <h1 class="section-heading mb-0">
            <template v-if="selectedTopic && activeTopicId">{{ currentPage?.topicTitle ?? selectedTopic.title }}</template>
            <template v-else-if="selectedLanguage">{{ selectedLanguage.title }}</template>
            <template v-else>Programmēšanas valodas</template>
          </h1>
          <p v-if="selectedTopic && activeTopicId" class="theory-lead mb-0 mt-1">{{ selectedTopic.description }}</p>
          <p v-else-if="selectedLanguage" class="theory-lead mb-0 mt-1">{{ selectedLanguage.description }}</p>
        </div>
        <div
          v-if="!activeLanguageId"
          class="theory-header__actions"
        >
          <span class="theory-section-count">{{ formatSectionCount(programmingLanguages.length) }}</span>
        </div>
        <div
          v-if="showReader && canSubmitTheoryRequests && currentPage"
          class="theory-header__actions"
        >
          <button class="btn btn-theory-edit btn-sm" type="button" @click="openModal('editContent')">Rediģēt teoriju</button>
          <button class="btn btn-theory-add btn-sm" type="button" @click="openModal('addContent')">Pievienot teoriju</button>
          <button
            v-if="canManageTheory"
            class="btn btn-theory-delete btn-sm"
            type="button"
            @click="openDeleteTheoryModal('page')"
          >
            Dzēst lapu
          </button>
        </div>
        <div v-if="canShowReadProgress && selectedTopic && activeTopicId" class="theory-language-progress theory-header__progress">
          <div class="theory-language-progress__label">
            <strong>{{ selectedTopic.progressPercent }}% apgūts</strong>
          </div>
          <span class="theory-language-progress__track" aria-hidden="true">
            <span
              class="theory-language-progress__bar"
              :style="{ width: `${selectedTopic.progressPercent}%` }"
            ></span>
          </span>
        </div>
        <div
          v-else-if="canShowReadProgress && selectedLanguage"
          class="theory-language-progress theory-header__progress"
          role="progressbar"
          aria-label="Valodas apguves progress"
          aria-valuemin="0"
          aria-valuemax="100"
          :aria-valuenow="selectedLanguage.progressPercent"
        >
          <div class="theory-language-progress__label">
            <strong>{{ selectedLanguage.progressPercent }}% apgūts</strong>
          </div>
          <span class="theory-language-progress__track" aria-hidden="true">
            <span
              class="theory-language-progress__bar"
              :style="{ width: `${selectedLanguage.progressPercent}%` }"
            ></span>
          </span>
        </div>
        <div
          v-else-if="canShowReadProgress && !activeLanguageId && theoryLanguages.length"
          class="theory-language-progress theory-header__progress"
          role="progressbar"
          aria-label="Kopējais apguves progress"
          aria-valuemin="0"
          aria-valuemax="100"
          :aria-valuenow="overallProgressPercent"
        >
          <div class="theory-language-progress__label">
            <strong>{{ overallProgressPercent }}% apgūts</strong>
          </div>
          <span class="theory-language-progress__track" aria-hidden="true">
            <span
              class="theory-language-progress__bar"
              :style="{ width: `${overallProgressPercent}%` }"
            ></span>
          </span>
        </div>
      </div>

      <div v-if="theoryError" class="alert alert-danger mb-0">{{ theoryError }}</div>

      <div v-if="!activeLanguageId && isLoadingLanguages" class="theory-empty">Ielādē valodas...</div>

      <div v-else-if="!activeLanguageId" class="theory-topics-wrap">
        <div class="theory-topics-controls">
          <input
            v-model="languageSearch"
            type="search"
            class="theory-search auth-input"
            placeholder="Meklēt..."
          />
          <div v-if="canShowReadProgress" class="theory-filter-group">
            <button
              type="button"
              class="ex-filter-btn"
              :class="{ active: hideCompletedLanguages }"
              @click="hideCompletedLanguages = !hideCompletedLanguages"
            >Paslēpt apgūtos</button>
          </div>
        </div>

        <div v-if="programmingLanguages.length" class="theory-list theory-list--languages">
          <button
            v-for="language in programmingLanguages"
            :key="language.id"
            type="button"
            class="theory-list-item"
            @click="selectLanguage(language.id)"
          >
            <img
              class="theory-list-item__image"
              :src="language.imageUrl"
              :alt="language.title"
            />
            <div class="theory-list-item__body">
              <strong>{{ language.title }}</strong>
              <span>{{ language.description }}</span>
            </div>
            <div class="theory-list-item__meta">
              <div class="theory-language-meta-count">
                <span class="theory-card__kicker">{{ language.topicCount }} tēmas</span>
              </div>
              <div v-if="canShowReadProgress" class="theory-list-item__progress">
                <span class="theory-card__progress">{{ language.progressPercent }}% apgūts</span>
                <span class="theory-progress-track" aria-hidden="true">
                  <span class="theory-progress-bar" :style="{ width: `${language.progressPercent}%` }"></span>
                </span>
              </div>
            </div>
          </button>
        </div>

        <div v-if="!filteredLanguages.length" class="theory-empty">
          Nav valodu, kas atbilst filtriem.
        </div>
      </div>

      <div v-else-if="isLoadingLanguage" class="theory-empty">Ielādē valodu...</div>

      <div v-else-if="selectedLanguage && !activeTopicId" class="theory-topics-wrap">
        <div class="theory-topics-controls">
          <input
            v-model="topicSearch"
            type="search"
            class="theory-search auth-input"
            placeholder="Meklēt tēmu..."
          />
          <div class="theory-filter-group">
            <button
              v-for="d in ['all', 'Iesācējs', 'Vidējs', 'Pieredzējis']"
              :key="d"
              type="button"
              class="ex-filter-btn"
              :class="{ active: topicDiffFilter === d }"
              @click="topicDiffFilter = d"
            >{{ d === 'all' ? 'Visas' : d }}</button>
          </div>
          <div v-if="canShowReadProgress" class="theory-filter-group">
            <button
              type="button"
              class="ex-filter-btn"
              :class="{ active: hideCompleted }"
              @click="hideCompleted = !hideCompleted"
            >Paslēpt apgūtos</button>
          </div>
          <div v-if="canSubmitTheoryRequests" class="theory-topic-actions">
            <button
              class="btn btn-theory-mode btn-sm"
              :class="{ 'is-active': theoryEditingEnabled }"
              type="button"
              @click="theoryEditingEnabled = !theoryEditingEnabled"
            >
              {{ theoryEditingEnabled ? 'Paslēpt rediģēšanu' : 'Iespējot rediģēšanu' }}
            </button>
            <div class="theory-add-menu" @keydown.esc="closeAddMenu">
              <button
                class="btn btn-theory-add btn-sm"
                type="button"
                aria-haspopup="menu"
                :aria-expanded="isAddMenuOpen"
                @click="toggleAddMenu"
              >
                Pievienot
                <span class="theory-add-menu__caret" aria-hidden="true">▾</span>
              </button>
              <div
                v-if="isAddMenuOpen"
                class="theory-add-menu__backdrop"
                @click="closeAddMenu"
              ></div>
              <ul v-if="isAddMenuOpen" class="theory-add-menu__list" role="menu">
                <li role="none">
                  <button
                    class="theory-add-menu__item"
                    type="button"
                    role="menuitem"
                    @click="startAddTopicFromMenu"
                  >Pievienot tēmu</button>
                </li>
                <li role="none">
                  <button
                    class="theory-add-menu__item"
                    type="button"
                    role="menuitem"
                    @click="startAddQuizFromMenu"
                  >Pievienot testu</button>
                </li>
              </ul>
            </div>
          </div>
        </div>

        <div v-if="!filteredTopics.length" class="theory-empty">Nav tēmu, kas atbilst filtriem.</div>

        <div v-else class="theory-list theory-list--topics">
          <template v-for="topic in pagedTopics" :key="topic.id">
            <article
              class="theory-list-item"
              :class="{ 'theory-list-item--done': canShowReadProgress && topic.progressPercent === 100 }"
              role="button"
              tabindex="0"
              @click="selectTopic(topic.id)"
              @keydown.enter.self.prevent="selectTopic(topic.id)"
              @keydown.space.self.prevent="selectTopic(topic.id)"
            >
              <div class="theory-list-item__badges">
                <span class="theory-difficulty" :class="difficultyClass(topic.difficulty)">{{ formatDifficulty(topic.difficulty) }}</span>
                <span class="theory-volume">{{ topic.estimatedMinutes }} min</span>
              </div>
              <div class="theory-list-item__body">
                <strong>{{ topic.title }}</strong>
                <span>{{ topic.description }}</span>
              </div>
              <div class="theory-list-item__meta">
                <div class="theory-list-item__actions">
                  <button
                    v-if="canSubmitTheoryRequests"
                    class="btn btn-theory-edit btn-sm"
                    type="button"
                    @click.stop="openTopicEditModal(topic)"
                  >
                    Rediģēt
                  </button>
                  <button
                    v-if="canSubmitTheoryRequests && !topic.hasQuiz"
                    class="btn btn-theory-add btn-sm"
                    type="button"
                    @click.stop="openQuizModal(topic, 'Add')"
                  >
                    Pievienot testu
                  </button>
                  <button
                    v-if="canManageTheory"
                    class="btn btn-theory-delete btn-sm"
                    type="button"
                    @click.stop="openTopicDeleteModal(topic)"
                  >
                    Dzēst
                  </button>
                </div>
                <div class="theory-language-meta-count">
                  <span class="theory-card__kicker">{{ topic.pageCount }} lapas</span>
                </div>
                <div v-if="canShowReadProgress" class="theory-list-item__progress">
                  <span class="theory-card__progress">{{ topic.progressPercent }}% apgūts</span>
                  <span class="theory-progress-track" aria-hidden="true">
                    <span class="theory-progress-bar" :style="{ width: `${topic.progressPercent}%` }"></span>
                  </span>
                </div>
              </div>
            </article>

            <article
              v-if="topic.hasQuiz"
              :key="`${topic.id}-quiz`"
              class="theory-list-item theory-list-item--quiz"
              :class="{
                'theory-list-item--quiz-complete':
                  canShowReadProgress &&
                  topic.quizQuestionCount > 0 &&
                  topic.quizAnsweredCount >= topic.quizQuestionCount
              }"
              role="button"
              tabindex="0"
              @click="goToQuiz(topic.id)"
              @keydown.enter.self.prevent="goToQuiz(topic.id)"
              @keydown.space.self.prevent="goToQuiz(topic.id)"
            >
              <div class="theory-list-item__badges">
                <span class="theory-list-item__quiz-tag">Tests</span>
              </div>
              <div class="theory-list-item__body">
                <strong>Tests: {{ topic.title }}</strong>
              </div>
              <div class="theory-list-item__meta">
                <div class="theory-list-item__actions">
                  <button
                    v-if="canSubmitTheoryRequests"
                    class="btn btn-theory-edit btn-sm"
                    type="button"
                    @click.stop="openQuizModal(topic, 'Edit')"
                  >
                    Rediģēt
                  </button>
                  <button
                    v-if="canManageTheory"
                    class="btn btn-theory-delete btn-sm"
                    type="button"
                    @click.stop="openQuizDeleteModal(topic)"
                  >
                    Dzēst
                  </button>
                </div>
                <div class="theory-language-meta-count">
                  <span class="theory-card__kicker">{{ topic.quizQuestionCount }} jautājumi</span>
                </div>
                <div v-if="canShowReadProgress" class="theory-list-item__progress">
                  <span class="theory-card__progress">
                    {{ topicQuizPercent(topic) }}% izpildīts
                  </span>
                  <span class="theory-progress-track" aria-hidden="true">
                    <span
                      class="theory-progress-bar"
                      :style="{ width: `${topicQuizPercent(topic)}%` }"
                    ></span>
                  </span>
                </div>
              </div>
            </article>
          </template>
        </div>

        <div v-if="filteredTopics.length > topicsPerPage" class="pagination-row mt-2">
          <span>{{ topicPageSummary }}</span>
          <div class="pagination-controls">
            <button type="button" class="pagination-btn" :disabled="topicPage === 1" @click="topicPage--">Iepriekšējā</button>
            <strong>{{ topicPage }} / {{ topicPageCount }}</strong>
            <button type="button" class="pagination-btn" :disabled="topicPage === topicPageCount" @click="topicPage++">Nākamā</button>
          </div>
        </div>
      </div>

      <div v-else-if="showPageLoading" class="theory-empty">Ielādē lapu...</div>

      <article v-else-if="showReader" class="theory-reader">
        <header class="theory-reader__header">
          <div class="theory-reader__header-top">
            <h2 class="section-heading mb-0">
              Lapa {{ currentPage?.pageIndex }} no {{ currentPage?.pageCount }}
            </h2>
            <div v-if="currentPage && currentPage.pageCount > 1" class="theory-page-nav theory-page-nav--sm">
              <button
                v-for="n in currentPage.pageCount"
                :key="n"
                type="button"
                class="theory-page-btn"
                :class="{
                  'theory-page-btn--active': n === currentPage.pageIndex,
                  'theory-page-btn--read': canShowReadProgress && readPageIndices.has(n) && n !== currentPage.pageIndex
                }"
                :disabled="isLoadingPage || isCompletingPage"
                @click="n !== currentPage.pageIndex && goToPage(n)"
              >{{ n }}</button>
            </div>
            <div class="theory-reader__header-right">
              <span class="theory-difficulty" :class="difficultyClass(currentPage?.difficulty || '')">
                {{ formatDifficulty(currentPage?.difficulty || '') }}
              </span>
              <span class="theory-volume">{{ currentPage?.estimatedMinutes }} min</span>
            </div>
          </div>
        </header>

        <div class="theory-markdown" v-html="renderedMarkdown"></div>

        <div class="theory-reader__footer">
          <button
            class="btn btn-outline-light"
            type="button"
            :disabled="isLoadingPage || isCompletingPage"
            @click="currentPage && currentPage.pageIndex > 1 ? goToPage(currentPage.pageIndex - 1) : backToTopics()"
          >
            {{ currentPage && currentPage.pageIndex > 1 ? 'Iepriekšējā' : 'Tēmas' }}
          </button>
          <button
            v-if="showQuizPrompt && selectedTopic"
            class="btn btn-outline-primary"
            type="button"
            @click="goToQuiz(selectedTopic.id)"
          >
            Iziet testu par šo tēmu
          </button>
          <template v-if="showNextTopicPrompt && nextTopic">
            <button class="btn btn-primary" type="button" @click="startNextTopic">
              {{ nextTopic.title }} →
            </button>
          </template>
          <template v-else>
            <button
              class="btn btn-primary"
              type="button"
              :disabled="readerForwardDisabled"
              @click="handleReaderForward"
            >
              {{ readerForwardLabel }}
            </button>
          </template>
        </div>
      </article>

      <div v-else-if="selectedTopic" class="theory-empty">Lapa pašlaik nav pieejama.</div>
    </div>
  </section>

  <section
    v-if="!activeLanguageId && !isLoadingLanguages && allDatabaseCategories.length"
    class="content-panel card border-primary-subtle theory-panel mb-3"
  >
    <div class="card-body p-3 p-lg-4 theory-panel__body">
      <div class="theory-header theory-header--compact">
        <span class="page-title-icon page-title-icon--theory" aria-hidden="true">
          <svg viewBox="0 0 24 24" width="34" height="34" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <ellipse cx="12" cy="5" rx="8" ry="3" />
            <path d="M4 5v6c0 1.66 3.58 3 8 3s8-1.34 8-3V5" />
            <path d="M4 11v6c0 1.66 3.58 3 8 3s8-1.34 8-3v-6" />
          </svg>
        </span>
        <div class="theory-heading-copy">
          <h1 class="section-heading mb-0">Datu bāzes</h1>
        </div>
        <div class="theory-header__actions">
          <span class="theory-section-count">{{ formatSectionCount(databaseCategories.length) }}</span>
        </div>
        <div
          v-if="canShowReadProgress"
          class="theory-language-progress theory-header__progress"
          role="progressbar"
          aria-label="Datu bāzu apguves progress"
          aria-valuemin="0"
          aria-valuemax="100"
          :aria-valuenow="databaseProgressPercent"
        >
          <div class="theory-language-progress__label">
            <strong>{{ databaseProgressPercent }}% apgūts</strong>
          </div>
          <span class="theory-language-progress__track" aria-hidden="true">
            <span
              class="theory-language-progress__bar"
              :style="{ width: `${databaseProgressPercent}%` }"
            ></span>
          </span>
        </div>
      </div>

      <div class="theory-topics-controls">
        <input
          v-model="databaseSearch"
          type="search"
          class="theory-search auth-input"
          placeholder="Meklēt datu bāzi..."
        />
        <div v-if="canShowReadProgress" class="theory-filter-group">
          <button
            type="button"
            class="ex-filter-btn"
            :class="{ active: hideCompletedDatabases }"
            @click="hideCompletedDatabases = !hideCompletedDatabases"
          >Paslēpt apgūtās</button>
        </div>
      </div>

      <div v-if="databaseCategories.length" class="theory-list theory-list--languages">
        <button
          v-for="language in databaseCategories"
          :key="language.id"
          type="button"
          class="theory-list-item"
          @click="selectLanguage(language.id)"
        >
          <img
            class="theory-list-item__image"
            :src="language.imageUrl"
            :alt="language.title"
          />
          <div class="theory-list-item__body">
            <strong>{{ language.title }}</strong>
            <span>{{ language.description }}</span>
          </div>
          <div class="theory-list-item__meta">
            <div class="theory-language-meta-count">
              <span class="theory-card__kicker">{{ language.topicCount }} tēmas</span>
            </div>
            <div v-if="canShowReadProgress" class="theory-list-item__progress">
              <span class="theory-card__progress">{{ language.progressPercent }}% apgūts</span>
              <span class="theory-progress-track" aria-hidden="true">
                <span class="theory-progress-bar" :style="{ width: `${language.progressPercent}%` }"></span>
              </span>
            </div>
          </div>
        </button>
      </div>
      <div v-else class="theory-empty">
        Nav datu bāzu, kas atbilst filtriem.
      </div>
    </div>
  </section>

  <div v-if="isPickQuizTopicOpen" class="app-modal-backdrop" @click.self="closePickQuizTopic">
    <div class="app-modal app-modal--sm card border-primary-subtle">
      <div class="card-body p-3 p-lg-4">
        <h2 class="section-heading mb-3">Izvēlies tēmu testam</h2>
        <p class="logout-text logout-text--navbar mb-3">
          Izvēlies tēmu, kurai pievienot testu. Sarakstā redzamas tikai tēmas, kurām tests vēl nav izveidots.
        </p>

        <div v-if="!topicsWithoutQuiz.length" class="theory-empty">
          Visām tēmām jau ir testi. Lai rediģētu esošu testu, nospied tēmas rindā uz "Rediģēt testu".
        </div>

        <ul v-else class="theory-pick-topic-list">
          <li v-for="topic in topicsWithoutQuiz" :key="topic.id">
            <button
              class="theory-pick-topic-item"
              type="button"
              @click="pickTopicForQuiz(topic)"
            >
              <strong>{{ topic.title }}</strong>
              <span>{{ topic.description }}</span>
            </button>
          </li>
        </ul>

        <div class="d-flex justify-content-end gap-2 mt-3">
          <button class="btn btn-outline-light" type="button" @click="closePickQuizTopic">Atcelt</button>
        </div>
      </div>
    </div>
  </div>

  <div v-if="isDeleteTheoryModalOpen" class="app-modal-backdrop" @click.self="closeDeleteTheoryModal">
    <div class="app-modal app-modal--sm card border-primary-subtle delete-modal">
      <div class="card-body p-3 p-lg-4">
        <div class="delete-modal__icon" aria-hidden="true">
          <svg viewBox="0 0 24 24" width="28" height="28" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M3 6h18" />
            <path d="M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" />
            <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
            <path d="M10 11v6" />
            <path d="M14 11v6" />
          </svg>
        </div>
        <h2 class="section-heading delete-modal__title mb-3">{{ deleteTheoryTitle }}</h2>
        <p class="delete-modal__text mb-4">{{ deleteTheoryDescription }}</p>

        <div v-if="theoryError" class="alert alert-danger mb-3">{{ theoryError }}</div>

        <div class="delete-modal__actions">
          <button class="btn btn-outline-light" type="button" :disabled="isDeletingTheory" @click="closeDeleteTheoryModal">
            Atcelt
          </button>
          <button class="btn btn-primary delete-modal__confirm" type="button" :disabled="isDeletingTheory" @click="confirmDeleteTheory">
            <span v-if="isDeletingTheory" class="logout-modal__spinner" aria-hidden="true"></span>
            {{ isDeletingTheory ? 'Dzēšu...' : 'Dzēst' }}
          </button>
        </div>
      </div>
    </div>
  </div>

  <div v-if="isQuizModalOpen" class="app-modal-backdrop" @click.self="closeQuizModal">
    <div class="app-modal app-modal--lg card border-primary-subtle">
      <div class="card-body p-3 p-lg-4">
        <h2 class="section-heading mb-3">
          {{ quizRequestMode === 'Add' ? 'Pievienot testu' : 'Rediģēt testu' }}
        </h2>

        <div class="quiz-builder__count mb-3">
          {{ quizForm.questions.length }} / {{ maxQuizQuestions }} jautājumi
        </div>

        <div v-if="quizRequestSuccess" class="alert alert-success mb-3">{{ quizRequestSuccess }}</div>
        <div v-if="quizRequestError" class="alert alert-danger mb-3">{{ quizRequestError }}</div>

        <form class="quiz-builder" @submit.prevent="submitQuizProposal">
          <div class="row g-3 mb-3">
            <div class="col-12 col-lg-5">
              <label class="form-label" for="quizTitle">Testa nosaukums</label>
              <input
                id="quizTitle"
                v-model="quizForm.title"
                class="form-control auth-input"
                type="text"
                maxlength="160"
              />
            </div>
            <div class="col-12 col-lg-7">
              <label class="form-label" for="quizDescription">Apraksts</label>
              <input
                id="quizDescription"
                v-model="quizForm.description"
                class="form-control auth-input"
                type="text"
                maxlength="900"
              />
            </div>
          </div>

          <div v-if="isLoadingQuizForEdit" class="theory-empty">Ielādē esošo testu...</div>

          <div v-else class="quiz-builder__questions">
            <article
              v-for="(question, questionIndex) in quizForm.questions"
              :key="questionIndex"
              class="quiz-builder__question"
            >
              <div class="quiz-builder__question-head">
                <strong>Jautājums {{ questionIndex + 1 }}</strong>
                <button
                  class="btn btn-outline-danger btn-sm"
                  type="button"
                  :disabled="quizForm.questions.length <= 1"
                  @click="removeQuizQuestion(questionIndex)"
                >
                  Noņemt
                </button>
              </div>

              <label class="form-label" :for="`quizQuestion-${questionIndex}`">Jautājums</label>
              <textarea
                :id="`quizQuestion-${questionIndex}`"
                v-model="question.prompt"
                class="form-control auth-input mb-3"
                rows="2"
                maxlength="1000"
              ></textarea>

              <div class="quiz-builder__options">
                <label
                  v-for="(_, optionIndex) in question.options"
                  :key="optionIndex"
                  class="quiz-builder__option"
                >
                  <input
                    v-model.number="question.correctOptionIndex"
                    type="radio"
                    :name="`quiz-correct-${questionIndex}`"
                    :value="optionIndex"
                  />
                  <input
                    v-model="question.options[optionIndex]"
                    class="form-control auth-input"
                    type="text"
                    :placeholder="`Atbilde ${optionIndex + 1}`"
                    maxlength="500"
                  />
                </label>
              </div>

              <label class="form-label mt-3" :for="`quizExplanation-${questionIndex}`">Paskaidrojums</label>
              <textarea
                :id="`quizExplanation-${questionIndex}`"
                v-model="question.explanation"
                class="form-control auth-input"
                rows="2"
                maxlength="2000"
              ></textarea>
            </article>
          </div>

          <div class="d-flex flex-wrap justify-content-between gap-2 mt-3">
            <button
              class="btn btn-outline-light"
              type="button"
              :disabled="quizForm.questions.length >= maxQuizQuestions"
              @click="addQuizQuestion"
            >
              Pievienot jautājumu
            </button>
            <div class="d-flex flex-wrap gap-2">
              <button class="btn btn-outline-light" type="button" @click="closeQuizModal">Aizvērt</button>
              <button class="btn btn-primary" type="submit" :disabled="isSubmittingQuizRequest || isLoadingQuizForEdit">
                {{ isSubmittingQuizRequest ? 'Sūta...' : 'Iesniegt apstiprināšanai' }}
              </button>
            </div>
          </div>
        </form>
      </div>
    </div>
  </div>

  <!-- Request modal -->
  <div v-if="isModalOpen" class="app-modal-backdrop" @click.self="closeModal">
    <div class="app-modal app-modal--lg app-modal--theory-editor card border-primary-subtle">
      <div class="card-body p-3 p-lg-4">
        <h2 class="section-heading mb-3">{{ modalTitle }}</h2>

        <div v-if="proposalSuccess" class="alert alert-success mb-3">{{ proposalSuccess }}</div>

        <form v-else @submit.prevent="submitProposal">
          <div v-if="draftRestored" class="alert alert-info theory-proposal-draft mb-3">
            <span>
              Atjaunots iepriekšējais melnraksts.
              <span v-if="draftSavedAt"> Saglabāts {{ draftSavedLabel }}.</span>
            </span>
            <button type="button" class="btn btn-outline-light btn-sm" @click="discardTheoryDraft">
              Sākt no jauna
            </button>
          </div>
          <!-- Add / Edit topic fields -->
          <div v-if="requestCategory === 'addTopic' || requestCategory === 'editTopic'" class="row g-3 mb-3">
            <div class="col-12">
              <label class="form-label" for="formTitle">Virsraksts</label>
              <input
                id="formTitle"
                v-model="form.title"
                class="form-control auth-input"
                type="text"
                maxlength="160"
              />
            </div>
            <div class="col-12">
              <label class="form-label" for="formDescription">Apraksts</label>
              <input
                id="formDescription"
                v-model="form.description"
                class="form-control auth-input"
                type="text"
                maxlength="900"
              />
            </div>
            <div class="col-12 col-md-6">
              <label class="form-label" for="formDifficulty">Grūtības pakāpe</label>
              <select id="formDifficulty" v-model="form.difficulty" class="form-select auth-input">
                <option v-for="d in difficulties" :key="d" :value="d">{{ d }}</option>
              </select>
            </div>
            <div class="col-12 col-md-6">
              <label class="form-label" for="formMinutes">Aptuvenie minūtes</label>
              <input
                id="formMinutes"
                v-model.number="form.estimatedMinutes"
                class="form-control auth-input"
                type="number"
                min="1"
                max="600"
              />
            </div>
          </div>

          <!-- Markdown for addTopic and addContent -->
          <div v-if="requestCategory === 'addTopic' || requestCategory === 'addContent' || requestCategory === 'editContent'" class="mb-3">
            <div class="theory-editor-bar mb-2">
              <label class="form-label mb-0">
                {{ requestCategory === 'addTopic'
                  ? 'Sākotnējais saturs (Markdown)'
                  : requestCategory === 'editContent'
                    ? 'Labotais saturs (Markdown)'
                    : 'Ieteiktais saturs (Markdown)' }}
              </label>
              <div class="theory-editor-tabs">
                <button type="button" class="theory-editor-tab" :class="{ active: !markdownPreviewMode }" @click="markdownPreviewMode = false">Rediģēt</button>
                <button type="button" class="theory-editor-tab" :class="{ active: markdownPreviewMode }" @click="markdownPreviewMode = true">Priekšskatījums</button>
              </div>
            </div>
            <textarea
              v-if="!markdownPreviewMode"
              id="formMarkdown"
              v-model="form.markdown"
              class="form-control auth-input theory-proposal-editor"
              rows="16"
              placeholder="# Virsraksts&#10;&#10;Saturs šeit..."
            ></textarea>
            <div v-else class="theory-proposal-preview theory-markdown" v-html="previewMarkdown"></div>
          </div>

          <div v-if="proposalError" class="alert alert-danger mb-3">{{ proposalError }}</div>

          <div class="d-flex justify-content-end gap-2">
            <button class="btn btn-outline-light" type="button" @click="closeModal">Atcelt</button>
            <button class="btn btn-primary" type="submit" :disabled="isSubmitting">
              {{ isSubmitting ? 'Sūtu...' : 'Iesniegt' }}
            </button>
          </div>
        </form>

      </div>
    </div>
  </div>
</template>
