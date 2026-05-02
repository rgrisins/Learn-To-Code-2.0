<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { authState } from '../services/auth'
import type { AuthUser } from '../services/auth'
import { deleteAdminUser, getAdminUsers, updateAdminUser } from '../services/admin'
import { approveRoleRequest, getAdminRoleRequests, rejectRoleRequest } from '../services/roleRequests'
import type { RoleRequest } from '../services/roleRequests'
import {
  approveQuizRequest,
  approveTopicRequest,
  approveContentRequest,
  getAdminQuizRequests,
  getAdminTopicRequests,
  getAdminContentRequests,
  rejectQuizRequest,
  rejectTopicRequest,
  rejectContentRequest,
} from '../services/theoryRequests'
import type { TheoryTopicRequest, TheoryContentRequest, TheoryQuizRequest } from '../services/theoryRequests'
import {
  approvePendingExercise,
  getPendingExercises,
  rejectPendingExercise,
  type PendingExercise,
  type PendingExerciseStatusFilter,
} from '../services/exercises'
import MarkdownIt from 'markdown-it'

const markdownRenderer = new MarkdownIt({ html: false, linkify: true, breaks: false })

const roles = ['Audzeknis', 'Pedagogs', 'Administrators']

type AdminSection = 'users' | 'editRequests' | 'exerciseRequests' | 'roleRequests'
type SortDirection = 'asc' | 'desc'
type UserSortKey = 'user' | 'role' | 'representation' | 'rating' | 'createdAt'
type EditRequestSortKey = 'user' | 'type' | 'target' | 'status' | 'createdAt'
type RoleRequestSortKey = 'user' | 'requestedRole' | 'reason' | 'status' | 'createdAt'

type SortState<TKey extends string> = {
  key: TKey
  direction: SortDirection
}

type TheoryEditRequest = {
  id: number
  editKind: 'topic' | 'content' | 'quiz'
  original: TheoryTopicRequest | TheoryContentRequest | TheoryQuizRequest
  userId: number
  username?: string
  fullName: string
  email: string
  requestType: 'Add' | 'Edit'
  languageCode: string
  topicSlug: string
  pageIndex?: number
  proposedTitle?: string
  proposedDescription?: string
  proposedDifficulty?: string
  proposedEstimatedMinutes?: number
  proposedMarkdown?: string
  questions?: TheoryQuizRequest['questions']
  status: 'Pending' | 'Approved' | 'Rejected'
  createdAtUtc: string
}

const activeSection = ref<AdminSection>('users')
const users = ref<AuthUser[]>([])
const roleRequests = ref<RoleRequest[]>([])
const topicRequests = ref<TheoryTopicRequest[]>([])
const contentRequests = ref<TheoryContentRequest[]>([])
const quizRequests = ref<TheoryQuizRequest[]>([])
const topicRequestStatusFilter = ref('Pending')
const contentRequestStatusFilter = ref('Pending')
const quizRequestStatusFilter = ref('Pending')
const theoryEditStatusFilter = ref('Pending')
const isLoadingTopicRequests = ref(false)
const isLoadingContentRequests = ref(false)
const isLoadingQuizRequests = ref(false)
const isReviewingRequest = ref(false)
const pendingExercises = ref<PendingExercise[]>([])
const pendingExerciseStatusFilter = ref<PendingExerciseStatusFilter>('Pending')
const isLoadingPendingExercises = ref(false)
const isReviewingPendingExercise = ref(false)
const selectedPendingExercise = ref<PendingExercise | null>(null)
const pendingExerciseCount = computed(
  () => pendingExercises.value.filter((exercise) => exercise.status === 'Pending').length,
)
const selectedTopicRequest = ref<TheoryTopicRequest | null>(null)
const selectedContentRequest = ref<TheoryContentRequest | null>(null)
const selectedQuizRequest = ref<TheoryQuizRequest | null>(null)
const selectedEditRequest = ref<TheoryEditRequest | null>(null)
const searchTerm = ref('')
const roleFilter = ref('')
const roleRequestStatusFilter = ref('Pending')
const userSort = reactive<SortState<UserSortKey>>({ key: 'createdAt', direction: 'desc' })
const editRequestSort = reactive<SortState<EditRequestSortKey>>({ key: 'createdAt', direction: 'desc' })
const roleRequestSort = reactive<SortState<RoleRequestSortKey>>({ key: 'createdAt', direction: 'desc' })
const adminError = ref('')
const isLoadingUsers = ref(false)
const isLoadingRoleRequests = ref(false)
const isSavingUser = ref(false)
const isDeletingUser = ref(false)
const isReviewingRoleRequest = ref(false)
const pendingTopicRequestCount = computed(() => topicRequests.value.filter((r) => r.status === 'Pending').length)
const pendingContentRequestCount = computed(() => contentRequests.value.filter((r) => r.status === 'Pending').length)
const pendingQuizRequestCount = computed(() => quizRequests.value.filter((r) => r.status === 'Pending').length)
const pendingEditRequestCount = computed(() =>
  pendingTopicRequestCount.value + pendingContentRequestCount.value + pendingQuizRequestCount.value,
)
const theoryEditRequests = computed<TheoryEditRequest[]>(() => {
  const topicItems: TheoryEditRequest[] = topicRequests.value.map((request) => ({
    ...request,
    topicSlug: request.topicSlug ?? '',
    editKind: 'topic',
    original: request,
  }))
  const contentItems: TheoryEditRequest[] = contentRequests.value.map((request) => ({
    ...request,
    editKind: 'content',
    original: request,
  }))
  const quizItems: TheoryEditRequest[] = quizRequests.value.map((request) => ({
    ...request,
    proposedTitle: request.proposedTitle,
    proposedDescription: request.proposedDescription,
    editKind: 'quiz',
    original: request,
  }))

  return [...topicItems, ...contentItems, ...quizItems]
})
const isLoadingTheoryEditRequests = computed(() =>
  isLoadingTopicRequests.value || isLoadingContentRequests.value || isLoadingQuizRequests.value,
)
const isReviewingEditRequest = computed(() => isReviewingRequest.value)

const isRefreshing = computed(() => {
  if (activeSection.value === 'roleRequests') return isLoadingRoleRequests.value
  if (activeSection.value === 'editRequests') return isLoadingTheoryEditRequests.value
  if (activeSection.value === 'exerciseRequests') return isLoadingPendingExercises.value
  return isLoadingUsers.value
})
const isEditModalOpen = ref(false)
const isDeleteModalOpen = ref(false)
const selectedUser = ref<AuthUser | null>(null)
const birthDatePickerOpen = ref(false)
const calendarMonth = ref(new Date())
const availableYears = computed(() => {
  const currentYear = new Date().getFullYear()
  return Array.from({ length: 101 }, (_, index) => currentYear - index)
})

const editForm = reactive({
  id: 0,
  username: '',
  firstName: '',
  lastName: '',
  birthDate: '',
  representation: '',
  role: 'Audzeknis',
  rating: 1000,
})

const editErrors = reactive({
  username: '',
  firstName: '',
  lastName: '',
  birthDate: '',
  rating: '',
})

const filteredUsers = computed(() => {
  const query = searchTerm.value.trim().toLowerCase()

  return users.value.filter((user) => {
    const matchesRole = !roleFilter.value || user.role === roleFilter.value
    const matchesSearch =
      !query ||
      [getUsername(user), user.fullName, user.email, user.representation]
        .filter(Boolean)
        .some((value) => String(value).toLowerCase().includes(query))

    return matchesRole && matchesSearch
  })
})

const sortedFilteredUsers = computed(() =>
  sortItems(filteredUsers.value, userSort, (user, key) => getUserSortValue(user, key)),
)

const sortedTheoryEditRequests = computed(() =>
  sortItems(theoryEditRequests.value, editRequestSort, (request, key) => getEditRequestSortValue(request, key)),
)

const sortedRoleRequests = computed(() =>
  sortItems(roleRequests.value, roleRequestSort, (request, key) => getRoleRequestSortValue(request, key)),
)

const adminCount = computed(() => users.value.filter((user) => user.role === 'Administrators').length)
const teacherCount = computed(() => users.value.filter((user) => user.role === 'Pedagogs').length)
const studentCount = computed(() => users.value.filter((user) => user.role === 'Audzeknis').length)
const pendingRoleRequestCount = computed(() =>
  roleRequests.value.filter((request) => request.status === 'Pending').length,
)

const calendarTitle = computed(() =>
  new Intl.DateTimeFormat('lv-LV', { month: 'long', year: 'numeric' }).format(calendarMonth.value),
)

const calendarWeeks = computed(() => {
  const firstDay = new Date(calendarMonth.value.getFullYear(), calendarMonth.value.getMonth(), 1)
  const startOffset = (firstDay.getDay() + 6) % 7
  const daysInMonth = new Date(calendarMonth.value.getFullYear(), calendarMonth.value.getMonth() + 1, 0).getDate()
  const cells: Array<{ label: string; iso: string | null; isEmpty: boolean }> = []

  for (let index = 0; index < startOffset; index += 1) {
    cells.push({ label: '', iso: null, isEmpty: true })
  }

  for (let day = 1; day <= daysInMonth; day += 1) {
    const date = new Date(calendarMonth.value.getFullYear(), calendarMonth.value.getMonth(), day)
    cells.push({
      label: String(day),
      iso: formatCalendarDate(date),
      isEmpty: false,
    })
  }

  return cells
})

onMounted(() => {
  void loadUsers()
  void loadRoleRequests()
  void loadTheoryEditRequests()
  void loadPendingExercises()
})

async function refreshActiveSection() {
  if (activeSection.value === 'roleRequests') { await loadRoleRequests(); return }
  if (activeSection.value === 'editRequests') { await loadTheoryEditRequests(); return }
  if (activeSection.value === 'exerciseRequests') { await loadPendingExercises(); return }
  await loadUsers()
}

async function loadPendingExercises() {
  adminError.value = ''
  isLoadingPendingExercises.value = true
  try {
    pendingExercises.value = await getPendingExercises(pendingExerciseStatusFilter.value)
  } catch (error) {
    adminError.value = error instanceof Error ? error.message : 'Neizdevas ieladet uzdevumu pieprasijumus.'
  } finally {
    isLoadingPendingExercises.value = false
  }
}

async function approvePendingExerciseRequest(exercise: PendingExercise) {
  adminError.value = ''
  isReviewingPendingExercise.value = true
  try {
    await approvePendingExercise(exercise.id)
    if (selectedPendingExercise.value?.id === exercise.id) {
      selectedPendingExercise.value = null
    }
    await loadPendingExercises()
  } catch (error) {
    adminError.value = error instanceof Error ? error.message : 'Neizdevas apstiprinat uzdevumu.'
  } finally {
    isReviewingPendingExercise.value = false
  }
}

async function rejectPendingExerciseRequest(exercise: PendingExercise) {
  adminError.value = ''
  isReviewingPendingExercise.value = true
  try {
    await rejectPendingExercise(exercise.id)
    if (selectedPendingExercise.value?.id === exercise.id) {
      selectedPendingExercise.value = null
    }
    await loadPendingExercises()
  } catch (error) {
    adminError.value = error instanceof Error ? error.message : 'Neizdevas noraidit uzdevumu.'
  } finally {
    isReviewingPendingExercise.value = false
  }
}

function formatPendingExerciseDate(value: string) {
  return formatDateTime(value)
}

async function loadUsers() {
  adminError.value = ''
  isLoadingUsers.value = true

  try {
    users.value = await getAdminUsers()
  } catch (error) {
    adminError.value = error instanceof Error ? error.message : 'Neizdevas ieladet lietotajus.'
  } finally {
    isLoadingUsers.value = false
  }
}

async function loadTopicRequests() {
  adminError.value = ''
  isLoadingTopicRequests.value = true
  try {
    topicRequests.value = await getAdminTopicRequests(topicRequestStatusFilter.value || undefined)
  } catch (error) {
    adminError.value = error instanceof Error ? error.message : 'Neizdevas ieladet temu pieprasijumus.'
  } finally {
    isLoadingTopicRequests.value = false
  }
}

async function loadContentRequests() {
  adminError.value = ''
  isLoadingContentRequests.value = true
  try {
    contentRequests.value = await getAdminContentRequests(contentRequestStatusFilter.value || undefined)
  } catch (error) {
    adminError.value = error instanceof Error ? error.message : 'Neizdevas ieladet satura pieprasijumus.'
  } finally {
    isLoadingContentRequests.value = false
  }
}

async function loadQuizRequests() {
  adminError.value = ''
  isLoadingQuizRequests.value = true
  try {
    quizRequests.value = await getAdminQuizRequests(quizRequestStatusFilter.value || undefined)
  } catch (error) {
    adminError.value = error instanceof Error ? error.message : 'Neizdevas ieladet testu pieprasijumus.'
  } finally {
    isLoadingQuizRequests.value = false
  }
}

async function loadTheoryEditRequests() {
  topicRequestStatusFilter.value = theoryEditStatusFilter.value
  contentRequestStatusFilter.value = theoryEditStatusFilter.value
  quizRequestStatusFilter.value = theoryEditStatusFilter.value
  await Promise.all([loadTopicRequests(), loadContentRequests(), loadQuizRequests()])
}

async function loadRoleRequests() {
  adminError.value = ''
  isLoadingRoleRequests.value = true

  try {
    roleRequests.value = await getAdminRoleRequests(roleRequestStatusFilter.value || undefined)
  } catch (error) {
    adminError.value = error instanceof Error ? error.message : 'Neizdevas ieladet lomu pieprasijumus.'
  } finally {
    isLoadingRoleRequests.value = false
  }
}

function openEditModal(user: AuthUser) {
  selectedUser.value = user
  adminError.value = ''
  clearEditErrors()

  editForm.id = user.id
  editForm.username = getUsername(user)
  editForm.firstName = user.firstName || ''
  editForm.lastName = user.lastName || ''
  editForm.birthDate = formatBirthDateInput(user.birthDate || null)
  editForm.representation = user.representation || ''
  editForm.role = user.role
  editForm.rating = user.rating

  const birthDateResult = parseBirthDate(editForm.birthDate)
  if (birthDateResult.value) {
    const [year, month, day] = birthDateResult.value.split('-').map(Number)
    calendarMonth.value = new Date(year, month - 1, day)
  } else {
    calendarMonth.value = new Date()
  }

  birthDatePickerOpen.value = false
  isEditModalOpen.value = true
}

function closeEditModal() {
  isEditModalOpen.value = false
  selectedUser.value = null
  birthDatePickerOpen.value = false
}

function openDeleteModal(user: AuthUser) {
  selectedUser.value = user
  adminError.value = ''
  isDeleteModalOpen.value = true
}

function closeDeleteModal() {
  isDeleteModalOpen.value = false
  selectedUser.value = null
}

function clearEditErrors() {
  editErrors.username = ''
  editErrors.firstName = ''
  editErrors.lastName = ''
  editErrors.birthDate = ''
  editErrors.rating = ''
}

function validateEditForm() {
  clearEditErrors()

  if (!editForm.username.trim()) {
    editErrors.username = 'Lietotajvards ir obligats.'
  } else if (!/^[a-zA-Z0-9._-]{3,30}$/.test(editForm.username.trim())) {
    editErrors.username = 'Lietotajvardam jabut 3-30 rakstzimes garam.'
  }

  if (!editForm.firstName.trim()) {
    editErrors.firstName = 'Vards ir obligats.'
  }

  if (!editForm.lastName.trim()) {
    editErrors.lastName = 'Uzvards ir obligats.'
  }

  const birthDateResult = parseBirthDate(editForm.birthDate)
  if (editForm.birthDate.trim() && birthDateResult.error) {
    editErrors.birthDate = birthDateResult.error
  }

  if (!Number.isFinite(editForm.rating) || editForm.rating < 0) {
    editErrors.rating = 'Reitingam jabut pozitivam skaitlim.'
  }

  return !editErrors.username && !editErrors.firstName && !editErrors.lastName && !editErrors.birthDate && !editErrors.rating
}

async function saveUser() {
  if (!selectedUser.value || !validateEditForm()) {
    return
  }

  adminError.value = ''
  isSavingUser.value = true

  try {
    const birthDateResult = parseBirthDate(editForm.birthDate)
    const updatedUser = await updateAdminUser(selectedUser.value.id, {
      username: editForm.username.trim(),
      firstName: editForm.firstName.trim(),
      lastName: editForm.lastName.trim(),
      birthDate: birthDateResult.value,
      representation: editForm.representation.trim() || null,
      role: editForm.role,
      rating: editForm.rating,
    })

    users.value = users.value.map((user) => (user.id === updatedUser.id ? updatedUser : user))

    if (authState.user?.id === updatedUser.id) {
      authState.user = updatedUser
    }

    closeEditModal()
  } catch (error) {
    adminError.value = error instanceof Error ? error.message : 'Neizdevas saglabat lietotaju.'
  } finally {
    isSavingUser.value = false
  }
}

async function confirmDeleteUser() {
  if (!selectedUser.value) {
    return
  }

  adminError.value = ''
  isDeletingUser.value = true

  try {
    await deleteAdminUser(selectedUser.value.id)
    users.value = users.value.filter((user) => user.id !== selectedUser.value?.id)
    closeDeleteModal()
  } catch (error) {
    adminError.value = error instanceof Error ? error.message : 'Neizdevas dzest lietotaju.'
  } finally {
    isDeletingUser.value = false
  }
}

async function approveTopicReq(request: TheoryTopicRequest) {
  adminError.value = ''
  isReviewingRequest.value = true
  try {
    const updated = await approveTopicRequest(request.id)
    topicRequests.value = topicRequestStatusFilter.value && updated.status !== topicRequestStatusFilter.value
      ? topicRequests.value.filter((r) => r.id !== updated.id)
      : topicRequests.value.map((r) => (r.id === updated.id ? updated : r))
    if (selectedTopicRequest.value?.id === updated.id) selectedTopicRequest.value = updated
  } catch (error) {
    adminError.value = error instanceof Error ? error.message : 'Neizdevas apstiprinat pieprasijumu.'
  } finally {
    isReviewingRequest.value = false
  }
}

async function rejectTopicReq(request: TheoryTopicRequest) {
  adminError.value = ''
  isReviewingRequest.value = true
  try {
    const updated = await rejectTopicRequest(request.id)
    topicRequests.value = topicRequestStatusFilter.value && updated.status !== topicRequestStatusFilter.value
      ? topicRequests.value.filter((r) => r.id !== updated.id)
      : topicRequests.value.map((r) => (r.id === updated.id ? updated : r))
    if (selectedTopicRequest.value?.id === updated.id) selectedTopicRequest.value = updated
  } catch (error) {
    adminError.value = error instanceof Error ? error.message : 'Neizdevas noraidit pieprasijumu.'
  } finally {
    isReviewingRequest.value = false
  }
}

async function approveContentReq(request: TheoryContentRequest) {
  adminError.value = ''
  isReviewingRequest.value = true
  try {
    const updated = await approveContentRequest(request.id)
    contentRequests.value = contentRequestStatusFilter.value && updated.status !== contentRequestStatusFilter.value
      ? contentRequests.value.filter((r) => r.id !== updated.id)
      : contentRequests.value.map((r) => (r.id === updated.id ? updated : r))
    if (selectedContentRequest.value?.id === updated.id) selectedContentRequest.value = updated
  } catch (error) {
    adminError.value = error instanceof Error ? error.message : 'Neizdevas apstiprinat pieprasijumu.'
  } finally {
    isReviewingRequest.value = false
  }
}

async function rejectContentReq(request: TheoryContentRequest) {
  adminError.value = ''
  isReviewingRequest.value = true
  try {
    const updated = await rejectContentRequest(request.id)
    contentRequests.value = contentRequestStatusFilter.value && updated.status !== contentRequestStatusFilter.value
      ? contentRequests.value.filter((r) => r.id !== updated.id)
      : contentRequests.value.map((r) => (r.id === updated.id ? updated : r))
    if (selectedContentRequest.value?.id === updated.id) selectedContentRequest.value = updated
  } catch (error) {
    adminError.value = error instanceof Error ? error.message : 'Neizdevas noraidit pieprasijumu.'
  } finally {
    isReviewingRequest.value = false
  }
}

async function approveQuizReq(request: TheoryQuizRequest) {
  adminError.value = ''
  isReviewingRequest.value = true
  try {
    const updated = await approveQuizRequest(request.id)
    quizRequests.value = quizRequestStatusFilter.value && updated.status !== quizRequestStatusFilter.value
      ? quizRequests.value.filter((r) => r.id !== updated.id)
      : quizRequests.value.map((r) => (r.id === updated.id ? updated : r))
    if (selectedQuizRequest.value?.id === updated.id) selectedQuizRequest.value = updated
  } catch (error) {
    adminError.value = error instanceof Error ? error.message : 'Neizdevas apstiprinat testa pieprasijumu.'
  } finally {
    isReviewingRequest.value = false
  }
}

async function rejectQuizReq(request: TheoryQuizRequest) {
  adminError.value = ''
  isReviewingRequest.value = true
  try {
    const updated = await rejectQuizRequest(request.id)
    quizRequests.value = quizRequestStatusFilter.value && updated.status !== quizRequestStatusFilter.value
      ? quizRequests.value.filter((r) => r.id !== updated.id)
      : quizRequests.value.map((r) => (r.id === updated.id ? updated : r))
    if (selectedQuizRequest.value?.id === updated.id) selectedQuizRequest.value = updated
  } catch (error) {
    adminError.value = error instanceof Error ? error.message : 'Neizdevas noraidit testa pieprasijumu.'
  } finally {
    isReviewingRequest.value = false
  }
}

async function approveEditRequest(request: TheoryEditRequest) {
  if (request.editKind === 'topic') {
    await approveTopicReq(request.original as TheoryTopicRequest)
  } else if (request.editKind === 'quiz') {
    await approveQuizReq(request.original as TheoryQuizRequest)
  } else {
    await approveContentReq(request.original as TheoryContentRequest)
  }

  selectedEditRequest.value =
    theoryEditRequests.value.find((item) => item.editKind === request.editKind && item.id === request.id) ?? null
}

async function rejectEditRequest(request: TheoryEditRequest) {
  if (request.editKind === 'topic') {
    await rejectTopicReq(request.original as TheoryTopicRequest)
  } else if (request.editKind === 'quiz') {
    await rejectQuizReq(request.original as TheoryQuizRequest)
  } else {
    await rejectContentReq(request.original as TheoryContentRequest)
  }

  selectedEditRequest.value =
    theoryEditRequests.value.find((item) => item.editKind === request.editKind && item.id === request.id) ?? null
}

async function approveRequest(request: RoleRequest) {
  adminError.value = ''
  isReviewingRoleRequest.value = true

  try {
    const updatedRequest = await approveRoleRequest(request.id)
    roleRequests.value =
      roleRequestStatusFilter.value && updatedRequest.status !== roleRequestStatusFilter.value
        ? roleRequests.value.filter((item) => item.id !== updatedRequest.id)
        : roleRequests.value.map((item) => (item.id === updatedRequest.id ? updatedRequest : item))
    await loadUsers()
  } catch (error) {
    adminError.value = error instanceof Error ? error.message : 'Neizdevas apstiprinat pieprasijumu.'
  } finally {
    isReviewingRoleRequest.value = false
  }
}

async function rejectRequest(request: RoleRequest) {
  adminError.value = ''
  isReviewingRoleRequest.value = true

  try {
    const updatedRequest = await rejectRoleRequest(request.id)
    roleRequests.value =
      roleRequestStatusFilter.value && updatedRequest.status !== roleRequestStatusFilter.value
        ? roleRequests.value.filter((item) => item.id !== updatedRequest.id)
        : roleRequests.value.map((item) => (item.id === updatedRequest.id ? updatedRequest : item))
  } catch (error) {
    adminError.value = error instanceof Error ? error.message : 'Neizdevas noraidit pieprasijumu.'
  } finally {
    isReviewingRoleRequest.value = false
  }
}

function formatCalendarDate(date: Date) {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

function formatBirthDateInput(isoDate?: string | null) {
  if (!isoDate) {
    return ''
  }

  const [year, month, day] = isoDate.split('-')
  if (!year || !month || !day) {
    return ''
  }

  return `${day}/${month}/${year}`
}

function parseBirthDate(input: string) {
  const trimmed = input.trim()

  if (!trimmed) {
    return { value: null as string | null, error: '' }
  }

  const isoMatch = /^(\d{4})-(\d{2})-(\d{2})$/.exec(trimmed)
  if (isoMatch) {
    const year = Number(isoMatch[1])
    const month = Number(isoMatch[2])
    const day = Number(isoMatch[3])
    const parsed = new Date(year, month - 1, day)

    if (parsed.getFullYear() !== year || parsed.getMonth() !== month - 1 || parsed.getDate() !== day) {
      return { value: null, error: 'Datums nav derigs.' }
    }

    return { value: formatCalendarDate(parsed), error: '' }
  }

  const match = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(trimmed)
  if (!match) {
    return { value: null, error: 'Dzimsanas datumam jabut formata dd/mm/yyyy.' }
  }

  const day = Number(match[1])
  const month = Number(match[2])
  const year = Number(match[3])
  const parsed = new Date(year, month - 1, day)

  if (parsed.getFullYear() !== year || parsed.getMonth() !== month - 1 || parsed.getDate() !== day) {
    return { value: null, error: 'Dzimsanas datumam jabut derigam datumam.' }
  }

  return { value: formatCalendarDate(parsed), error: '' }
}

function selectCalendarDate(isoDate: string) {
  editForm.birthDate = formatBirthDateInput(isoDate)
  birthDatePickerOpen.value = false
  editErrors.birthDate = ''
}

function moveCalendarMonth(offset: number) {
  calendarMonth.value = new Date(calendarMonth.value.getFullYear(), calendarMonth.value.getMonth() + offset, 1)
}

function selectCalendarYear(event: Event) {
  const target = event.target as HTMLSelectElement
  calendarMonth.value = new Date(Number(target.value), calendarMonth.value.getMonth(), 1)
}

function formatEditRequestType(request: TheoryEditRequest) {
  if (request.editKind === 'quiz') {
    return request.requestType === 'Edit' ? 'Rediģēt testu' : 'Pievienot testu'
  }

  if (request.editKind === 'content') {
    return request.requestType === 'Edit' ? 'Rediģēt teoriju' : 'Pievienot teoriju'
  }

  return request.requestType === 'Add' ? 'Jauna tēma' : 'Rediģēt tēmu'
}

function formatEditRequestStatus(status: string) {
  switch (status) {
    case 'Pending': return 'Gaida'
    case 'Approved': return 'Apstiprināts'
    case 'Rejected': return 'Noraidīts'
    default: return status
  }
}

function getEditRequestStatusClass(status: string) {
  return `admin-request-status--${status.toLowerCase()}`
}

function renderProposalMarkdown(source: string) {
  return markdownRenderer.render(source)
}

function formatDateTime(value: string) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return value
  }

  return new Intl.DateTimeFormat('lv-LV', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(date)
}

function formatRoleRequestStatus(status: string) {
  switch (status) {
    case 'Pending':
      return 'Gaida'
    case 'Approved':
      return 'Apstiprināts'
    case 'Rejected':
      return 'Noraidīts'
    default:
      return status
  }
}

function getRoleRequestStatusClass(status: string) {
  return `admin-request-status--${status.toLowerCase()}`
}

function toggleSort<TKey extends string>(state: SortState<TKey>, key: TKey) {
  if (state.key === key) {
    state.direction = state.direction === 'asc' ? 'desc' : 'asc'
    return
  }

  state.key = key
  state.direction = 'asc'
}

function sortItems<T, TKey extends string>(
  items: T[],
  state: SortState<TKey>,
  getValue: (item: T, key: TKey) => string | number | null | undefined,
) {
  const direction = state.direction === 'asc' ? 1 : -1

  return [...items].sort((left, right) => compareSortValues(getValue(left, state.key), getValue(right, state.key)) * direction)
}

function compareSortValues(left: string | number | null | undefined, right: string | number | null | undefined) {
  if (typeof left === 'number' || typeof right === 'number') {
    return (Number(left ?? Number.NEGATIVE_INFINITY) - Number(right ?? Number.NEGATIVE_INFINITY))
  }

  return String(left ?? '').localeCompare(String(right ?? ''), 'lv-LV', {
    numeric: true,
    sensitivity: 'base',
  })
}

function getUserSortValue(user: AuthUser, key: UserSortKey) {
  switch (key) {
    case 'user':
      return `${getDisplayName(user)} ${user.email} ${getUsername(user)}`
    case 'role':
      return user.role
    case 'representation':
      return user.representation ?? ''
    case 'rating':
      return user.rating
    case 'createdAt':
      return Date.parse(user.createdAtUtc)
    default:
      return ''
  }
}

function getEditRequestSortValue(request: TheoryEditRequest, key: EditRequestSortKey) {
  switch (key) {
    case 'user':
      return `${request.fullName || request.email} ${request.email} ${request.username ?? ''}`
    case 'type':
      return formatEditRequestType(request)
    case 'target':
      return `${request.languageCode} ${request.topicSlug} ${request.pageIndex ?? ''} ${request.proposedTitle ?? ''}`
    case 'status':
      return getStatusRank(request.status)
    case 'createdAt':
      return Date.parse(request.createdAtUtc)
    default:
      return ''
  }
}

function getRoleRequestSortValue(request: RoleRequest, key: RoleRequestSortKey) {
  switch (key) {
    case 'user':
      return `${request.fullName || request.email} ${request.email} ${request.username ?? ''}`
    case 'requestedRole':
      return `${request.currentRole} ${request.requestedRole}`
    case 'reason':
      return request.reason
    case 'status':
      return getStatusRank(request.status)
    case 'createdAt':
      return Date.parse(request.createdAtUtc)
    default:
      return ''
  }
}

function getStatusRank(status: string) {
  switch (status) {
    case 'Pending':
      return 0
    case 'Approved':
      return 1
    case 'Rejected':
      return 2
    default:
      return 99
  }
}

function sortIndicator<TKey extends string>(state: SortState<TKey>, key: TKey) {
  if (state.key !== key) return '↕'
  return state.direction === 'asc' ? '↑' : '↓'
}

function sortAriaLabel<TKey extends string>(state: SortState<TKey>, key: TKey, label: string) {
  if (state.key !== key) return `Kārtot pēc kolonnas ${label}`
  return `Kārtot pēc kolonnas ${label} ${state.direction === 'asc' ? 'dilstoši' : 'augoši'}`
}

function getDisplayName(user: AuthUser) {
  return user.fullName || [user.firstName, user.lastName].filter(Boolean).join(' ') || user.username || user.email
}

function getUsername(user: AuthUser) {
  return user.username?.trim() || ''
}

function isCurrentUser(user: AuthUser) {
  return authState.user?.id === user.id
}
</script>

<template>
  <section class="content-panel card border-primary-subtle admin-panel">
    <div class="card-body p-3 p-lg-4">
      <div class="d-flex flex-wrap align-items-start justify-content-between gap-3 mb-4">
        <div>
          <h1 class="section-heading mb-2">Administrēšanas panelis</h1>
        </div>
        <button class="btn btn-outline-light" type="button" :disabled="isRefreshing" @click="refreshActiveSection">
          {{ isRefreshing ? 'Ielādē...' : 'Atsvaidzināt' }}
        </button>
      </div>

      <div class="ex-filter-group admin-section-tabs mb-4" role="tablist" aria-label="Admin sadalas">
        <button
          type="button"
          class="ex-filter-btn"
          :class="{ active: activeSection === 'users' }"
          @click="activeSection = 'users'"
        >
          Lietotaji
        </button>
        <button
          type="button"
          class="ex-filter-btn"
          :class="{ active: activeSection === 'editRequests' }"
          @click="activeSection = 'editRequests'"
        >
          Rediģēšanas pieprasījumi
          <span v-if="pendingEditRequestCount" class="admin-tab__count">{{ pendingEditRequestCount }}</span>
        </button>
        <button
          type="button"
          class="ex-filter-btn"
          :class="{ active: activeSection === 'exerciseRequests' }"
          @click="activeSection = 'exerciseRequests'"
        >
          Uzdevumu pieprasījumi
          <span v-if="pendingExerciseCount" class="admin-tab__count">{{ pendingExerciseCount }}</span>
        </button>
        <button
          type="button"
          class="ex-filter-btn"
          :class="{ active: activeSection === 'roleRequests' }"
          @click="activeSection = 'roleRequests'"
        >
          Lomu pieprasijumi
          <span v-if="pendingRoleRequestCount" class="admin-tab__count">{{ pendingRoleRequestCount }}</span>
        </button>
      </div>

      <div v-if="adminError" class="alert alert-danger">
        {{ adminError }}
      </div>

      <div v-if="activeSection === 'users'">
        <div class="admin-summary mb-4">
          <div class="admin-summary__item">
            <span>Lietotaji</span>
            <strong>{{ users.length }}</strong>
          </div>
          <div class="admin-summary__item">
            <span>Audzekni</span>
            <strong>{{ studentCount }}</strong>
          </div>
          <div class="admin-summary__item">
            <span>Pedagogi</span>
            <strong>{{ teacherCount }}</strong>
          </div>
          <div class="admin-summary__item">
            <span>Administratori</span>
            <strong>{{ adminCount }}</strong>
          </div>
        </div>

        <div class="admin-toolbar mb-3">
          <input
            v-model="searchTerm"
            class="form-control auth-input"
            type="search"
            placeholder="Meklēt pēc vārda, e-pasta vai pārstāvniecības"
          />
          <select v-model="roleFilter" class="form-select auth-input">
            <option value="">Visas lomas</option>
            <option v-for="role in roles" :key="role" :value="role">{{ role }}</option>
          </select>
        </div>

        <div class="admin-table-wrap">
          <table class="table admin-table align-middle mb-0">
            <thead>
              <tr>
                <th scope="col">
                  <button class="admin-sort-button" type="button" :aria-label="sortAriaLabel(userSort, 'user', 'Lietotājs')" @click="toggleSort(userSort, 'user')">
                    Lietotājs <span>{{ sortIndicator(userSort, 'user') }}</span>
                  </button>
                </th>
                <th scope="col">
                  <button class="admin-sort-button" type="button" :aria-label="sortAriaLabel(userSort, 'role', 'Loma')" @click="toggleSort(userSort, 'role')">
                    Loma <span>{{ sortIndicator(userSort, 'role') }}</span>
                  </button>
                </th>
                <th scope="col">
                  <button class="admin-sort-button" type="button" :aria-label="sortAriaLabel(userSort, 'representation', 'Pārstāvniecība')" @click="toggleSort(userSort, 'representation')">
                    Pārstāvniecība <span>{{ sortIndicator(userSort, 'representation') }}</span>
                  </button>
                </th>
                <th scope="col">
                  <button class="admin-sort-button" type="button" :aria-label="sortAriaLabel(userSort, 'rating', 'Reitings')" @click="toggleSort(userSort, 'rating')">
                    Reitings <span>{{ sortIndicator(userSort, 'rating') }}</span>
                  </button>
                </th>
                <th scope="col">
                  <button class="admin-sort-button" type="button" :aria-label="sortAriaLabel(userSort, 'createdAt', 'Izveidots')" @click="toggleSort(userSort, 'createdAt')">
                    Izveidots <span>{{ sortIndicator(userSort, 'createdAt') }}</span>
                  </button>
                </th>
                <th scope="col" class="text-end">Darbības</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="isLoadingUsers">
                <td colspan="6" class="text-center py-4">Ielādē lietotājus...</td>
              </tr>
              <tr v-else-if="sortedFilteredUsers.length === 0">
                <td colspan="6" class="text-center py-4">Nav atrastu lietotāju.</td>
              </tr>
              <template v-else>
                <tr v-for="user in sortedFilteredUsers" :key="user.id">
                  <td>
                    <div class="admin-user-cell">
                      <strong>{{ getDisplayName(user) }}</strong>
                      <span>{{ user.email }}</span>
                      <small v-if="getUsername(user)">@{{ getUsername(user) }}</small>
                      <small v-else class="admin-user-cell__missing">Lietotājvārds nav ielādēts</small>
                    </div>
                  </td>
                  <td>
                    <span class="admin-role-pill" :class="`admin-role-pill--${user.role.toLowerCase()}`">
                      {{ user.role }}
                    </span>
                  </td>
                  <td>{{ user.representation || 'Nav norādīta' }}</td>
                  <td>{{ user.rating }}</td>
                  <td>{{ formatDateTime(user.createdAtUtc) }}</td>
                  <td>
                    <div class="d-flex justify-content-end gap-2">
                      <button class="btn btn-outline-light btn-sm" type="button" @click="openEditModal(user)">
                        Rediģēt
                      </button>
                      <button
                        class="btn btn-outline-danger btn-sm"
                        type="button"
                        :disabled="isCurrentUser(user)"
                        @click="openDeleteModal(user)"
                      >
                        Dzēst
                      </button>
                    </div>
                  </td>
                </tr>
              </template>
            </tbody>
          </table>
        </div>
      </div>

      <div v-if="activeSection === 'editRequests'">
        <div class="admin-toolbar admin-toolbar--requests mb-3">
          <select v-model="theoryEditStatusFilter" class="form-select auth-input" @change="loadTheoryEditRequests">
            <option value="Pending">Gaida</option>
            <option value="Approved">Apstiprināti</option>
            <option value="Rejected">Noraidīti</option>
            <option value="">Visi pieprasījumi</option>
          </select>
          <button class="btn btn-outline-light" type="button" :disabled="isLoadingTheoryEditRequests" @click="loadTheoryEditRequests">
            {{ isLoadingTheoryEditRequests ? 'Ielādē...' : 'Atsvaidzināt' }}
          </button>
        </div>

        <div class="admin-table-wrap">
          <table class="table admin-table admin-table--requests align-middle mb-0">
            <thead>
              <tr>
                <th scope="col">
                  <button class="admin-sort-button" type="button" :aria-label="sortAriaLabel(editRequestSort, 'user', 'Lietotājs')" @click="toggleSort(editRequestSort, 'user')">
                    Lietotājs <span>{{ sortIndicator(editRequestSort, 'user') }}</span>
                  </button>
                </th>
                <th scope="col">
                  <button class="admin-sort-button" type="button" :aria-label="sortAriaLabel(editRequestSort, 'type', 'Tips')" @click="toggleSort(editRequestSort, 'type')">
                    Tips <span>{{ sortIndicator(editRequestSort, 'type') }}</span>
                  </button>
                </th>
                <th scope="col">
                  <button class="admin-sort-button" type="button" :aria-label="sortAriaLabel(editRequestSort, 'target', 'Valoda / Tēma')" @click="toggleSort(editRequestSort, 'target')">
                    Valoda / Tēma <span>{{ sortIndicator(editRequestSort, 'target') }}</span>
                  </button>
                </th>
                <th scope="col">
                  <button class="admin-sort-button" type="button" :aria-label="sortAriaLabel(editRequestSort, 'status', 'Statuss')" @click="toggleSort(editRequestSort, 'status')">
                    Statuss <span>{{ sortIndicator(editRequestSort, 'status') }}</span>
                  </button>
                </th>
                <th scope="col">
                  <button class="admin-sort-button" type="button" :aria-label="sortAriaLabel(editRequestSort, 'createdAt', 'Iesniegts')" @click="toggleSort(editRequestSort, 'createdAt')">
                    Iesniegts <span>{{ sortIndicator(editRequestSort, 'createdAt') }}</span>
                  </button>
                </th>
                <th scope="col" class="text-end">Darbības</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="isLoadingTheoryEditRequests">
                <td colspan="6" class="text-center py-4">Ielādē pieprasījumus...</td>
              </tr>
              <tr v-else-if="sortedTheoryEditRequests.length === 0">
                <td colspan="6" class="text-center py-4">Nav teorijas pieprasījumu.</td>
              </tr>
              <template v-else>
                <tr v-for="request in sortedTheoryEditRequests" :key="`${request.editKind}-${request.id}`">
                  <td>
                    <div class="admin-user-cell">
                      <strong>{{ request.fullName || request.email }}</strong>
                      <span>{{ request.email }}</span>
                      <small v-if="request.username">@{{ request.username }}</small>
                    </div>
                  </td>
                  <td>
                    <span
                      class="admin-role-pill"
                      :class="request.editKind === 'content' ? 'admin-role-pill--administrators' : request.requestType === 'Add' ? 'admin-role-pill--pedagogs' : 'admin-role-pill--audzeknis'"
                    >
                      {{ formatEditRequestType(request) }}
                    </span>
                  </td>
                  <td>
                    <div class="admin-user-cell">
                      <strong>{{ request.languageCode }}</strong>
                      <span>{{ request.topicSlug }}</span>
                      <small v-if="request.editKind === 'content' && request.pageIndex">Lapa {{ request.pageIndex }}</small>
                      <small v-if="request.editKind === 'quiz'">{{ request.questions?.length ?? 0 }} jautājumi</small>
                      <small v-if="request.proposedTitle">{{ request.proposedTitle }}</small>
                    </div>
                  </td>
                  <td>
                    <span class="admin-request-status" :class="getEditRequestStatusClass(request.status)">
                      {{ formatEditRequestStatus(request.status) }}
                    </span>
                  </td>
                  <td>{{ formatDateTime(request.createdAtUtc) }}</td>
                  <td>
                    <div class="d-flex justify-content-end gap-2">
                      <button
                        class="btn btn-outline-light btn-sm"
                        type="button"
                        @click="selectedEditRequest = request"
                      >
                        Skatīt
                      </button>
                      <template v-if="request.status === 'Pending'">
                        <button
                          class="btn btn-outline-light btn-sm"
                          type="button"
                          :disabled="isReviewingEditRequest"
                          @click="approveEditRequest(request)"
                        >
                          Apstiprināt
                        </button>
                        <button
                          class="btn btn-outline-danger btn-sm"
                          type="button"
                          :disabled="isReviewingEditRequest"
                          @click="rejectEditRequest(request)"
                        >
                          Noraidīt
                        </button>
                      </template>
                    </div>
                  </td>
                </tr>
              </template>
            </tbody>
          </table>
        </div>
      </div>

      <div v-if="activeSection === 'exerciseRequests'">
        <div class="admin-toolbar admin-toolbar--requests mb-3">
          <select
            v-model="pendingExerciseStatusFilter"
            class="form-select auth-input"
            @change="loadPendingExercises"
          >
            <option value="Pending">Gaida</option>
            <option value="Approved">Apstiprināti</option>
            <option value="Rejected">Noraidīti</option>
            <option value="">Visi pieprasījumi</option>
          </select>
        </div>

        <div class="admin-table-wrap">
          <table class="table admin-table admin-table--requests align-middle mb-0">
            <thead>
              <tr>
                <th scope="col">Autors</th>
                <th scope="col">Uzdevums</th>
                <th scope="col">Valoda</th>
                <th scope="col">Grūtība</th>
                <th scope="col">Testi</th>
                <th scope="col">Statuss</th>
                <th scope="col">Iesniegts</th>
                <th scope="col" class="text-end">Darbības</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="isLoadingPendingExercises">
                <td colspan="8" class="text-center py-4">Ielādē uzdevumus...</td>
              </tr>
              <tr v-else-if="pendingExercises.length === 0">
                <td colspan="8" class="text-center py-4">Nav uzdevumu šim filtram.</td>
              </tr>
              <template v-else>
                <tr v-for="exercise in pendingExercises" :key="exercise.id">
                  <td>
                    <div class="admin-user-cell">
                      <strong>{{ exercise.authorName || 'Nezināms autors' }}</strong>
                      <span>{{ exercise.authorEmail }}</span>
                    </div>
                  </td>
                  <td>
                    <div class="admin-user-cell">
                      <strong>{{ exercise.title }}</strong>
                      <small>{{ exercise.description }}</small>
                    </div>
                  </td>
                  <td>
                    <span class="ex-language-tag">
                      {{ exercise.languageCode }} {{ exercise.languageVersion }}
                    </span>
                  </td>
                  <td>
                    <span class="ex-diff-badge" :class="`diff-${exercise.difficulty.toLowerCase()}`">
                      {{ exercise.difficulty }}
                    </span>
                  </td>
                  <td>{{ exercise.testCases.length }}</td>
                  <td>
                    <span
                      class="admin-request-status"
                      :class="`admin-request-status--${exercise.status.toLowerCase()}`"
                    >
                      {{ exercise.status === 'Approved' ? 'Apstiprināts'
                         : exercise.status === 'Rejected' ? 'Noraidīts'
                         : 'Gaida' }}
                    </span>
                  </td>
                  <td>{{ formatPendingExerciseDate(exercise.createdAtUtc) }}</td>
                  <td>
                    <div class="d-flex justify-content-end gap-2">
                      <button
                        class="btn btn-outline-light btn-sm"
                        type="button"
                        @click="selectedPendingExercise = exercise"
                      >
                        Skatīt
                      </button>
                      <template v-if="exercise.status === 'Pending'">
                        <button
                          class="btn btn-outline-light btn-sm"
                          type="button"
                          :disabled="isReviewingPendingExercise"
                          @click="approvePendingExerciseRequest(exercise)"
                        >
                          Apstiprināt
                        </button>
                        <button
                          class="btn btn-outline-danger btn-sm"
                          type="button"
                          :disabled="isReviewingPendingExercise"
                          @click="rejectPendingExerciseRequest(exercise)"
                        >
                          Noraidīt
                        </button>
                      </template>
                    </div>
                  </td>
                </tr>
              </template>
            </tbody>
          </table>
        </div>
      </div>

      <div v-if="activeSection === 'roleRequests'">
        <div class="admin-toolbar admin-toolbar--requests mb-3">
          <select v-model="roleRequestStatusFilter" class="form-select auth-input" @change="loadRoleRequests">
            <option value="Pending">Gaida</option>
            <option value="Approved">Apstiprināti</option>
            <option value="Rejected">Noraidīti</option>
            <option value="">Visi pieprasījumi</option>
          </select>
          <button class="btn btn-outline-light" type="button" :disabled="isLoadingRoleRequests" @click="loadRoleRequests">
            {{ isLoadingRoleRequests ? 'Ielādē...' : 'Atsvaidzināt' }}
          </button>
        </div>

        <div class="admin-table-wrap">
          <table class="table admin-table admin-table--requests align-middle mb-0">
            <thead>
              <tr>
                <th scope="col">
                  <button class="admin-sort-button" type="button" :aria-label="sortAriaLabel(roleRequestSort, 'user', 'Lietotājs')" @click="toggleSort(roleRequestSort, 'user')">
                    Lietotājs <span>{{ sortIndicator(roleRequestSort, 'user') }}</span>
                  </button>
                </th>
                <th scope="col">
                  <button class="admin-sort-button" type="button" :aria-label="sortAriaLabel(roleRequestSort, 'requestedRole', 'Pieprasītā loma')" @click="toggleSort(roleRequestSort, 'requestedRole')">
                    Pieprasītā loma <span>{{ sortIndicator(roleRequestSort, 'requestedRole') }}</span>
                  </button>
                </th>
                <th scope="col">
                  <button class="admin-sort-button" type="button" :aria-label="sortAriaLabel(roleRequestSort, 'reason', 'Iemesls')" @click="toggleSort(roleRequestSort, 'reason')">
                    Iemesls <span>{{ sortIndicator(roleRequestSort, 'reason') }}</span>
                  </button>
                </th>
                <th scope="col">
                  <button class="admin-sort-button" type="button" :aria-label="sortAriaLabel(roleRequestSort, 'status', 'Statuss')" @click="toggleSort(roleRequestSort, 'status')">
                    Statuss <span>{{ sortIndicator(roleRequestSort, 'status') }}</span>
                  </button>
                </th>
                <th scope="col">
                  <button class="admin-sort-button" type="button" :aria-label="sortAriaLabel(roleRequestSort, 'createdAt', 'Izveidots')" @click="toggleSort(roleRequestSort, 'createdAt')">
                    Izveidots <span>{{ sortIndicator(roleRequestSort, 'createdAt') }}</span>
                  </button>
                </th>
                <th scope="col" class="text-end">Darbības</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="isLoadingRoleRequests">
                <td colspan="6" class="text-center py-4">Ielādē lomu pieprasījumus...</td>
              </tr>
              <tr v-else-if="sortedRoleRequests.length === 0">
                <td colspan="6" class="text-center py-4">Nav lomu pieprasījumu.</td>
              </tr>
              <template v-else>
                <tr v-for="request in sortedRoleRequests" :key="request.id">
                  <td>
                    <div class="admin-user-cell">
                      <strong>{{ request.fullName || request.email }}</strong>
                      <span>{{ request.email }}</span>
                      <small v-if="request.username">@{{ request.username }}</small>
                    </div>
                  </td>
                  <td>
                    <div class="admin-request-role">
                      <span>{{ request.currentRole }}</span>
                      <strong>{{ request.requestedRole }}</strong>
                    </div>
                  </td>
                  <td class="admin-request-reason">{{ request.reason }}</td>
                  <td>
                    <span class="admin-request-status" :class="getRoleRequestStatusClass(request.status)">
                      {{ formatRoleRequestStatus(request.status) }}
                    </span>
                  </td>
                  <td>{{ formatDateTime(request.createdAtUtc) }}</td>
                  <td>
                    <div v-if="request.status === 'Pending'" class="d-flex justify-content-end gap-2">
                      <button
                        class="btn btn-outline-light btn-sm"
                        type="button"
                        :disabled="isReviewingRoleRequest"
                        @click="approveRequest(request)"
                      >
                        Apstiprināt
                      </button>
                      <button
                        class="btn btn-outline-danger btn-sm"
                        type="button"
                        :disabled="isReviewingRoleRequest"
                        @click="rejectRequest(request)"
                      >
                        Noraidīt
                      </button>
                    </div>
                    <div v-else class="text-end text-secondary">Izskatīts</div>
                  </td>
                </tr>
              </template>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <div v-if="isEditModalOpen" class="app-modal-backdrop" @click.self="closeEditModal">
      <div class="app-modal card border-primary-subtle">
        <div class="card-body p-3 p-lg-4">
          <h2 class="section-heading mb-3">Rediģēt lietotāju</h2>

          <form class="row g-3" @submit.prevent="saveUser">
            <div class="col-12 col-lg-6">
              <label class="form-label" for="adminEditUsername">Lietotājvārds</label>
              <input
                id="adminEditUsername"
                v-model="editForm.username"
                class="form-control form-control-lg auth-input"
                :class="{ 'is-invalid': !!editErrors.username }"
                type="text"
              />
              <div v-if="editErrors.username" class="invalid-feedback d-block">{{ editErrors.username }}</div>
            </div>
            <div class="col-12 col-lg-6">
              <label class="form-label" for="adminEditEmail">E-pasts</label>
              <input
                id="adminEditEmail"
                class="form-control form-control-lg auth-input"
                type="email"
                :value="selectedUser?.email"
                readonly
              />
            </div>
            <div class="col-12 col-lg-6">
              <label class="form-label" for="adminEditFirstName">Vārds</label>
              <input
                id="adminEditFirstName"
                v-model="editForm.firstName"
                class="form-control form-control-lg auth-input"
                :class="{ 'is-invalid': !!editErrors.firstName }"
                type="text"
              />
              <div v-if="editErrors.firstName" class="invalid-feedback d-block">{{ editErrors.firstName }}</div>
            </div>
            <div class="col-12 col-lg-6">
              <label class="form-label" for="adminEditLastName">Uzvārds</label>
              <input
                id="adminEditLastName"
                v-model="editForm.lastName"
                class="form-control form-control-lg auth-input"
                :class="{ 'is-invalid': !!editErrors.lastName }"
                type="text"
              />
              <div v-if="editErrors.lastName" class="invalid-feedback d-block">{{ editErrors.lastName }}</div>
            </div>
            <div class="col-12 col-lg-4 position-relative">
              <label class="form-label" for="adminEditBirthDate">Dzimšanas datums</label>
              <div class="input-group birthdate-field">
                <input
                  id="adminEditBirthDate"
                  v-model="editForm.birthDate"
                  class="form-control form-control-lg auth-input"
                  :class="{ 'is-invalid': !!editErrors.birthDate }"
                  inputmode="numeric"
                  placeholder="dd/mm/yyyy"
                  type="text"
                  @click="birthDatePickerOpen = true"
                  @focus="birthDatePickerOpen = true"
                />
                <button class="btn btn-outline-light" type="button" @click="birthDatePickerOpen = !birthDatePickerOpen">
                  Kalendārs
                </button>
              </div>
              <div v-if="editErrors.birthDate" class="invalid-feedback d-block">{{ editErrors.birthDate }}</div>

              <div v-if="birthDatePickerOpen" class="birthdate-calendar card border-primary-subtle mt-2">
                <div class="card-body p-3">
                  <div class="d-flex align-items-center justify-content-between gap-2 mb-3">
                    <button class="btn btn-outline-light btn-sm" type="button" @click="moveCalendarMonth(-1)">&lt;</button>
                    <div class="d-flex align-items-center gap-2 flex-grow-1 justify-content-center">
                      <strong class="text-white text-capitalize">{{ calendarTitle }}</strong>
                      <select class="form-select form-select-sm birthdate-year-select" :value="calendarMonth.getFullYear()" @change="selectCalendarYear">
                        <option v-for="year in availableYears" :key="year" :value="year">{{ year }}</option>
                      </select>
                    </div>
                    <button class="btn btn-outline-light btn-sm" type="button" @click="moveCalendarMonth(1)">&gt;</button>
                  </div>

                  <div class="calendar-grid calendar-grid__head mb-2">
                    <span>P</span>
                    <span>O</span>
                    <span>T</span>
                    <span>C</span>
                    <span>P</span>
                    <span>S</span>
                    <span>S</span>
                  </div>

                  <div class="calendar-grid">
                    <span
                      v-for="(cell, index) in calendarWeeks"
                      :key="`${cell.iso || 'empty'}-${index}`"
                      :class="['calendar-cell', { 'is-empty': cell.isEmpty }]"
                    >
                      <button
                        v-if="!cell.isEmpty && cell.iso"
                        type="button"
                        class="calendar-day"
                        @click="selectCalendarDate(cell.iso)"
                      >
                        {{ cell.label }}
                      </button>
                    </span>
                  </div>
                </div>
              </div>
            </div>
            <div class="col-12 col-lg-4">
              <label class="form-label" for="adminEditRole">Loma</label>
              <select id="adminEditRole" v-model="editForm.role" class="form-select form-select-lg auth-input">
                <option v-for="role in roles" :key="role" :value="role">{{ role }}</option>
              </select>
            </div>
            <div class="col-12 col-lg-4">
              <label class="form-label" for="adminEditRating">Reitings</label>
              <input
                id="adminEditRating"
                v-model.number="editForm.rating"
                class="form-control form-control-lg auth-input"
                :class="{ 'is-invalid': !!editErrors.rating }"
                min="0"
                type="number"
              />
              <div v-if="editErrors.rating" class="invalid-feedback d-block">{{ editErrors.rating }}</div>
            </div>
            <div class="col-12">
              <label class="form-label" for="adminEditEducation">Pārstāvniecība</label>
              <input
                id="adminEditEducation"
                v-model="editForm.representation"
                class="form-control form-control-lg auth-input"
                type="text"
              />
            </div>

            <div class="col-12 d-flex flex-wrap justify-content-end gap-2">
              <button class="btn btn-outline-light" type="button" @click="closeEditModal">Atcelt</button>
              <button class="btn btn-primary" type="submit" :disabled="isSavingUser">
                {{ isSavingUser ? 'Saglabaju...' : 'Saglabat' }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>

    <div v-if="selectedEditRequest" class="app-modal-backdrop" @click.self="selectedEditRequest = null">
      <div class="app-modal app-modal--lg card border-primary-subtle">
        <div class="card-body p-3 p-lg-4">
          <div class="mb-3">
            <h2 class="section-heading mb-1">
              {{ selectedEditRequest.languageCode }} / {{ selectedEditRequest.topicSlug }}
            </h2>
            <span class="admin-request-status mt-2 d-inline-block" :class="getEditRequestStatusClass(selectedEditRequest.status)">
              {{ formatEditRequestStatus(selectedEditRequest.status) }}
            </span>
          </div>

          <div v-if="selectedEditRequest.editKind === 'topic' && selectedEditRequest.requestType === 'Add'" class="row g-2 mb-3">
            <div class="col-12 col-md-6">
              <small class="text-secondary">Virsraksts</small>
              <p class="mb-0 fw-semibold">{{ selectedEditRequest.proposedTitle }}</p>
            </div>
            <div class="col-12 col-md-6">
              <small class="text-secondary">Apraksts</small>
              <p class="mb-0">{{ selectedEditRequest.proposedDescription }}</p>
            </div>
            <div class="col-6 col-md-3">
              <small class="text-secondary">Grutibas pakape</small>
              <p class="mb-0">{{ selectedEditRequest.proposedDifficulty }}</p>
            </div>
            <div class="col-6 col-md-3">
              <small class="text-secondary">Minutu aptuvenais laiks</small>
              <p class="mb-0">{{ selectedEditRequest.proposedEstimatedMinutes }} min</p>
            </div>
          </div>

          <div v-if="selectedEditRequest.editKind === 'content' && selectedEditRequest.pageIndex" class="alert alert-secondary mb-3">
            Labojums paredzēts teorijas {{ selectedEditRequest.pageIndex }}. lapai.
          </div>

          <div v-if="selectedEditRequest.editKind === 'quiz'" class="admin-quiz-preview mb-3">
            <div class="mb-3">
              <small class="text-secondary">Testa nosaukums</small>
              <p class="mb-1 fw-semibold">{{ selectedEditRequest.proposedTitle }}</p>
              <p class="mb-0">{{ selectedEditRequest.proposedDescription }}</p>
            </div>
            <ol class="admin-quiz-preview__questions">
              <li v-for="(question, questionIndex) in selectedEditRequest.questions ?? []" :key="questionIndex">
                <strong>{{ question.prompt }}</strong>
                <ul>
                  <li
                    v-for="(option, optionIndex) in question.options"
                    :key="optionIndex"
                    :class="{ 'admin-quiz-preview__correct': optionIndex === question.correctOptionIndex }"
                  >
                    {{ option }}
                  </li>
                </ul>
                <small v-if="question.explanation">{{ question.explanation }}</small>
              </li>
            </ol>
          </div>

          <div
            v-else
            class="theory-markdown admin-proposal-preview mb-3"
            v-html="renderProposalMarkdown(selectedEditRequest.proposedMarkdown || '')"
          ></div>

          <div v-if="selectedEditRequest.status === 'Pending'" class="d-flex justify-content-end gap-2">
            <button
              class="btn btn-outline-danger"
              type="button"
              :disabled="isReviewingEditRequest"
              @click="rejectEditRequest(selectedEditRequest)"
            >
              {{ isReviewingEditRequest ? 'Gaida...' : 'Noraidīt' }}
            </button>
            <button
              class="btn btn-primary"
              type="button"
              :disabled="isReviewingEditRequest"
              @click="approveEditRequest(selectedEditRequest)"
            >
              {{ isReviewingEditRequest ? 'Gaida...' : 'Apstiprināt' }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <div v-if="isDeleteModalOpen" class="app-modal-backdrop" @click.self="closeDeleteModal">
      <div class="app-modal app-modal--sm card border-primary-subtle">
        <div class="card-body p-3 p-lg-4">
          <h2 class="section-heading mb-3">{{ selectedUser ? getDisplayName(selectedUser) : '' }}</h2>
          <p class="logout-text logout-text--navbar mb-4">
            Lietotāja konts un aktīvās sesijas tiks dzēstas.
          </p>

          <div class="d-flex justify-content-end gap-2">
            <button class="btn btn-outline-light" type="button" @click="closeDeleteModal">Atcelt</button>
            <button class="btn btn-primary" type="button" :disabled="isDeletingUser" @click="confirmDeleteUser">
              {{ isDeletingUser ? 'Dzēšu...' : 'Dzēst' }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>
