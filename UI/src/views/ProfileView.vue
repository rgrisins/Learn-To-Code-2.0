<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import {
  authState,
  clearAuthState,
  getProfileStats,
  logout,
  updateProfile,
  type ProfileStats,
} from '../services/auth'
import { createRoleRequest, getMyRoleRequests } from '../services/roleRequests'
import type { RoleRequest } from '../services/roleRequests'

const profileStatsVisibilityKey = 'learn-to-code:profile-stats-visible'

const router = useRouter()
const isEditModalOpen = ref(false)
const isLogoutModalOpen = ref(false)
const isSavingProfile = ref(false)
const isLoggingOut = ref(false)
const isRoleRequestModalOpen = ref(false)
const isSendingRoleRequest = ref(false)
const profileError = ref('')
const profileStats = ref<ProfileStats | null>(null)
const profileStatsError = ref('')
const isLoadingProfileStats = ref(false)
const showProfileStats = ref(localStorage.getItem(profileStatsVisibilityKey) !== 'false')
const roleRequestError = ref('')
const roleRequestReason = ref('')
const roleRequests = ref<RoleRequest[]>([])
const birthDatePickerOpen = ref(false)
const calendarMonth = ref(new Date())
const availableYears = computed(() => {
  const currentYear = new Date().getFullYear()
  return Array.from({ length: 101 }, (_, index) => currentYear - index)
})

const user = computed(() => authState.user)
const latestPedagogRequest = computed(() =>
  roleRequests.value.find((request) => request.requestedRole === 'Pedagogs') ?? null,
)
const pendingPedagogRequest = computed(() =>
  roleRequests.value.find((request) => request.requestedRole === 'Pedagogs' && request.status === 'Pending') ?? null,
)
const canRequestPedagog = computed(() => user.value?.role === 'Audzeknis' && !pendingPedagogRequest.value)
const editForm = reactive({
  username: '',
  firstName: '',
  lastName: '',
  birthDate: '',
  educationInstitution: '',
})

const editErrors = reactive({
  username: '',
  firstName: '',
  lastName: '',
  birthDate: '',
  educationInstitution: '',
})

onMounted(() => {
  void loadRoleRequests()
  if (showProfileStats.value) {
    void loadProfileStats()
  }
})

function formatCalendarDate(date: Date) {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

function formatBirthDateDisplay(isoDate?: string | null) {
  if (!isoDate) {
    return 'Nav norādīts'
  }

  const [year, month, day] = isoDate.split('-')
  if (!year || !month || !day) {
    return isoDate
  }

  return `${day}/${month}/${year}`
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
    return { value: null, error: '' }
  }

  const isoMatch = /^(\d{4})-(\d{2})-(\d{2})$/.exec(trimmed)
  if (isoMatch) {
    const year = Number(isoMatch[1])
    const month = Number(isoMatch[2])
    const day = Number(isoMatch[3])
    const parsed = new Date(year, month - 1, day)

    if (
      parsed.getFullYear() !== year ||
      parsed.getMonth() !== month - 1 ||
      parsed.getDate() !== day
    ) {
      return { value: null, error: 'Datums nav derīgs.' }
    }

    return { value: formatCalendarDate(parsed), error: '' }
  }

  const match = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(trimmed)
  if (!match) {
    return { value: null, error: 'Dzimšanas datumam jābūt formātā dd/mm/yyyy.' }
  }

  const day = Number(match[1])
  const month = Number(match[2])
  const year = Number(match[3])
  const parsed = new Date(year, month - 1, day)

  if (
    parsed.getFullYear() !== year ||
    parsed.getMonth() !== month - 1 ||
    parsed.getDate() !== day
  ) {
    return { value: null, error: 'Dzimšanas datumam jābūt derīgam datumam.' }
  }

  return { value: formatCalendarDate(parsed), error: '' }
}

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

function selectCalendarDate(isoDate: string) {
  const [year, month, day] = isoDate.split('-').map(Number)
  const parsed = new Date(year, month - 1, day)
  editForm.birthDate = formatBirthDateInput(formatCalendarDate(parsed))
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

function clearEditErrors() {
  Object.keys(editErrors).forEach((key) => {
    editErrors[key as keyof typeof editErrors] = ''
  })
}

function validateEditForm() {
  clearEditErrors()

  const username = editForm.username.trim()
  const firstName = editForm.firstName.trim()
  const lastName = editForm.lastName.trim()
  const birthDate = editForm.birthDate.trim()
  const educationInstitution = editForm.educationInstitution.trim()
  const birthDateResult = parseBirthDate(birthDate)

  if (!username) {
    editErrors.username = 'Lietotājvārds ir obligāts.'
  } else if (!/^[a-zA-Z0-9._-]{3,30}$/.test(username)) {
    editErrors.username = 'Lietotājvārdam jābūt 3-30 rakstzīmes garam.'
  }

  if (!firstName) {
    editErrors.firstName = 'Vārds ir obligāts.'
  }

  if (!lastName) {
    editErrors.lastName = 'Uzvārds ir obligāts.'
  }

  if (birthDate && birthDateResult.error) {
    editErrors.birthDate = birthDateResult.error
  }

  if (educationInstitution && educationInstitution.length > 120) {
    editErrors.educationInstitution = 'Izglītības iestādes nosaukums ir par garu.'
  }

  return Object.values(editErrors).every((value) => !value)
}

function openEditModal() {
  profileError.value = ''
  clearEditErrors()
  birthDatePickerOpen.value = false

  editForm.username = user.value?.username || ''
  editForm.firstName = user.value?.firstName || ''
  editForm.lastName = user.value?.lastName || ''
  editForm.birthDate = formatBirthDateInput(user.value?.birthDate || null)
  const birthDateResult = parseBirthDate(editForm.birthDate)
  if (birthDateResult.value) {
    const [year, month, day] = birthDateResult.value.split('-').map(Number)
    calendarMonth.value = new Date(year, month - 1, day)
  } else {
    calendarMonth.value = new Date()
  }
  editForm.educationInstitution = user.value?.educationInstitution || ''
  isEditModalOpen.value = true
}

function closeEditModal() {
  isEditModalOpen.value = false
  birthDatePickerOpen.value = false
}

async function loadRoleRequests() {
  try {
    roleRequests.value = await getMyRoleRequests()
  } catch {
    roleRequests.value = []
  }
}

async function loadProfileStats() {
  profileStatsError.value = ''
  isLoadingProfileStats.value = true

  try {
    profileStats.value = await getProfileStats()
  } catch (error) {
    profileStats.value = null
    profileStatsError.value = error instanceof Error ? error.message : 'Neizdevās ielādēt statistiku.'
  } finally {
    isLoadingProfileStats.value = false
  }
}

function toggleProfileStats() {
  showProfileStats.value = !showProfileStats.value
  localStorage.setItem(profileStatsVisibilityKey, showProfileStats.value ? 'true' : 'false')

  if (showProfileStats.value && !profileStats.value) {
    void loadProfileStats()
  }
}

function languageLabel(languageCode?: string | null) {
  if (!languageCode) return 'Nav datu'

  const code = languageCode.toLowerCase()
  if (code === 'python') return 'Python'
  if (code === 'java') return 'Java'
  return languageCode
}

function openRoleRequestModal() {
  roleRequestError.value = ''
  roleRequestReason.value = ''
  isRoleRequestModalOpen.value = true
}

function closeRoleRequestModal() {
  isRoleRequestModalOpen.value = false
}

async function submitRoleRequest() {
  const reason = roleRequestReason.value.trim()
  if (reason.length < 10) {
    roleRequestError.value = 'Uzraksti vismaz 10 rakstzimes.'
    return
  }

  isSendingRoleRequest.value = true
  roleRequestError.value = ''

  try {
    const request = await createRoleRequest('Pedagogs', reason)
    roleRequests.value = [request, ...roleRequests.value]
    closeRoleRequestModal()
  } catch (error) {
    roleRequestError.value = error instanceof Error ? error.message : 'Neizdevas nosutit pieprasijumu.'
  } finally {
    isSendingRoleRequest.value = false
  }
}

function openLogoutModal() {
  isLogoutModalOpen.value = true
}

function closeLogoutModal() {
  isLogoutModalOpen.value = false
}

async function saveProfile() {
  profileError.value = ''

  if (!validateEditForm()) {
    profileError.value = 'Aizpildi iezīmētos laukus.'
    return
  }

  isSavingProfile.value = true

  try {
    const birthDateResult = parseBirthDate(editForm.birthDate)

    await updateProfile({
      username: editForm.username.trim(),
      firstName: editForm.firstName.trim(),
      lastName: editForm.lastName.trim(),
      birthDate: birthDateResult.value,
      educationInstitution: editForm.educationInstitution.trim() || null,
    })

    closeEditModal()
  } catch (error) {
    const message = error instanceof Error ? error.message : 'Neizdevās saglabāt profilu.'

    if (/401|unauthorized|sesija/i.test(message)) {
      clearAuthState()
      await router.replace({ name: 'login', query: { redirect: '/profile' } })
      return
    }

    profileError.value = message
  } finally {
    isSavingProfile.value = false
  }
}

async function handleLogout() {
  isLoggingOut.value = true
  try {
    await logout()
    await router.replace('/')
  } finally {
    isLoggingOut.value = false
    closeLogoutModal()
  }
}

async function confirmLogout() {
  await handleLogout()
}
</script>

<template>
  <section class="content-panel card border-primary-subtle">
    <div class="card-body p-3 p-lg-4">
      <h1 class="section-heading mb-3">Lietotāja profils</h1>

      <div v-if="user" class="row g-3 mb-4">
        <div class="col-12 col-lg-6">
          <div class="profile-stat">
            <span class="profile-stat__label">Lietotājvārds</span>
            <strong>{{ user.username || 'Nav norādīts' }}</strong>
          </div>
        </div>
        <div class="col-12 col-lg-6">
          <div class="profile-stat">
            <span class="profile-stat__label">Vārds</span>
            <strong>{{ user.firstName || 'Nav norādīts' }}</strong>
          </div>
        </div>
        <div class="col-12 col-lg-6">
          <div class="profile-stat">
            <span class="profile-stat__label">Uzvārds</span>
            <strong>{{ user.lastName || 'Nav norādīts' }}</strong>
          </div>
        </div>
        <div class="col-12 col-lg-6">
          <div class="profile-stat">
            <span class="profile-stat__label">Dzimšanas datums</span>
            <strong>{{ formatBirthDateDisplay(user.birthDate) }}</strong>
          </div>
        </div>
        <div class="col-12 col-lg-6">
          <div class="profile-stat">
            <span class="profile-stat__label">Loma</span>
            <strong>{{ user.role }}</strong>
          </div>
        </div>
        <div class="col-12 col-lg-6">
          <div class="profile-stat">
            <span class="profile-stat__label">E-pasts</span>
            <strong>{{ user.email }}</strong>
          </div>
        </div>
        <div class="col-12 col-lg-6">
          <div class="profile-stat">
            <span class="profile-stat__label">Reitings</span>
            <strong>{{ user.rating }}</strong>
          </div>
        </div>
        <div class="col-12">
          <div class="profile-stat">
            <span class="profile-stat__label">Izglītības iestāde</span>
            <strong>{{ user.educationInstitution || 'Nav norādīta' }}</strong>
          </div>
        </div>
      </div>

      <div v-if="user" class="profile-statistics mb-4">
        <div class="profile-statistics__header">
          <h2>Statistika</h2>
          <button class="btn btn-outline-light btn-sm" type="button" @click="toggleProfileStats">
            {{ showProfileStats ? 'Paslēpt statistiku' : 'Rādīt statistiku' }}
          </button>
        </div>

        <div v-if="showProfileStats" class="profile-progress-grid">
          <section class="profile-progress-panel">
            <div class="profile-progress-panel__header">
              <h2>Teorija</h2>
              <span>{{ profileStats?.theoryLanguages.length ?? 0 }} valodas</span>
            </div>

            <div v-if="isLoadingProfileStats" class="profile-progress-empty">Ielādē statistiku...</div>
            <div v-else-if="profileStatsError" class="profile-progress-empty">{{ profileStatsError }}</div>
            <div v-else-if="profileStats?.theoryLanguages.length" class="profile-progress-list">
              <div
                v-for="language in profileStats.theoryLanguages"
                :key="language.languageId"
                class="profile-progress-item"
              >
                <div class="profile-progress-item__top">
                  <div>
                    <strong>{{ language.title }}</strong>
                    <span>{{ language.topicCount }} tēmas</span>
                  </div>
                  <strong>{{ language.progressPercent }}%</strong>
                </div>
                <div class="profile-progress-track" aria-hidden="true">
                  <span class="profile-progress-bar" :style="{ width: `${language.progressPercent}%` }"></span>
                </div>
              </div>
            </div>
            <div v-else class="profile-progress-empty">Vēl nav iesāktu programmēšanas valodu.</div>
          </section>

          <section class="profile-progress-panel">
            <div class="profile-progress-panel__header">
              <h2>Uzdevumi</h2>
              <span>{{ profileStats?.exerciseCompletionPercent ?? 0 }}% izpildīti</span>
            </div>

            <div v-if="isLoadingProfileStats" class="profile-progress-empty">Ielādē statistiku...</div>
            <div v-else-if="profileStatsError" class="profile-progress-empty">{{ profileStatsError }}</div>
            <template v-else>
              <div class="profile-progress-track profile-progress-track--large" aria-hidden="true">
                <span
                  class="profile-progress-bar"
                  :style="{ width: `${profileStats?.exerciseCompletionPercent ?? 0}%` }"
                ></span>
              </div>
              <div class="profile-task-stats">
                <div>
                  <span>Izpildīti</span>
                  <strong>{{ profileStats?.exerciseSolved ?? 0 }} izpildīti</strong>
                </div>
                <div>
                  <span>Mēģināti</span>
                  <strong>{{ profileStats?.exerciseAttempted ?? 0 }}</strong>
                </div>
                <div>
                  <span>Iesniegumi</span>
                  <strong>{{ profileStats?.exerciseSubmissionCount ?? 0 }}</strong>
                </div>
                <div>
                  <span>Pareizo iesniegumu %</span>
                  <strong>{{ profileStats?.exerciseSuccessPercent ?? 0 }}%</strong>
                </div>
                <div>
                  <span>Izmantotākā valoda</span>
                  <strong>{{ languageLabel(profileStats?.mostUsedExerciseLanguage) }}</strong>
                </div>
              </div>
            </template>
          </section>
        </div>
      </div>

      <div v-if="latestPedagogRequest" class="profile-role-request mb-4">
        <span class="profile-stat__label">Pedagoga lomas pieprasījums</span>
        <strong>{{ latestPedagogRequest.status }}</strong>
        <p class="mb-0">{{ latestPedagogRequest.reason }}</p>
      </div>

      <div class="d-flex flex-wrap gap-2">
        <button class="btn btn-outline-light" type="button" @click="openEditModal">
          Rediģēt profilu
        </button>
        <button v-if="canRequestPedagog" class="btn btn-outline-light" type="button" @click="openRoleRequestModal">
          Pieprasit pedagoga lomu
        </button>
        <button class="btn btn-primary" type="button" :disabled="isLoggingOut" @click="openLogoutModal">
          Iziet no konta
        </button>
      </div>
    </div>

    <div v-if="isEditModalOpen" class="app-modal-backdrop" @click.self="closeEditModal">
      <div class="app-modal card border-primary-subtle">
        <div class="card-body p-3 p-lg-4">
          <h2 class="section-heading mb-3">Mainīt datus</h2>

          <div v-if="profileError" class="alert alert-danger">
            {{ profileError }}
          </div>

          <form class="row g-3" @submit.prevent="saveProfile">
            <div class="col-12">
              <label class="form-label" for="editUsername">Lietotājvārds</label>
              <input id="editUsername" v-model="editForm.username" type="text" class="form-control form-control-lg auth-input" :class="{ 'is-invalid': !!editErrors.username }" />
              <div v-if="editErrors.username" class="invalid-feedback d-block">{{ editErrors.username }}</div>
            </div>

            <div class="col-12 col-lg-6">
              <label class="form-label" for="editFirstName">Vārds</label>
              <input id="editFirstName" v-model="editForm.firstName" type="text" class="form-control form-control-lg auth-input" :class="{ 'is-invalid': !!editErrors.firstName }" />
              <div v-if="editErrors.firstName" class="invalid-feedback d-block">{{ editErrors.firstName }}</div>
            </div>

            <div class="col-12 col-lg-6">
              <label class="form-label" for="editLastName">Uzvārds</label>
              <input id="editLastName" v-model="editForm.lastName" type="text" class="form-control form-control-lg auth-input" :class="{ 'is-invalid': !!editErrors.lastName }" />
              <div v-if="editErrors.lastName" class="invalid-feedback d-block">{{ editErrors.lastName }}</div>
            </div>

            <div class="col-12 col-lg-6 position-relative">
              <label class="form-label" for="editBirthDate">Dzimšanas datums</label>
              <div class="input-group birthdate-field">
                <input
                  id="editBirthDate"
                  v-model="editForm.birthDate"
                  type="text"
                  inputmode="numeric"
                  placeholder="dd/mm/yyyy"
                  class="form-control form-control-lg auth-input"
                  :class="{ 'is-invalid': !!editErrors.birthDate }"
                  @focus="birthDatePickerOpen = true"
                  @click="birthDatePickerOpen = true"
                />
                <button class="btn btn-outline-light" type="button" @click="birthDatePickerOpen = !birthDatePickerOpen">
                  Kalendārs
                </button>
              </div>
              <div v-if="editErrors.birthDate" class="invalid-feedback d-block">{{ editErrors.birthDate }}</div>

              <div v-if="birthDatePickerOpen" class="birthdate-calendar card border-primary-subtle mt-2">
                <div class="card-body p-3">
                  <div class="d-flex align-items-center justify-content-between gap-2 mb-3">
                    <button class="btn btn-outline-light btn-sm" type="button" @click="moveCalendarMonth(-1)">‹</button>
                    <div class="d-flex align-items-center gap-2 flex-grow-1 justify-content-center">
                      <strong class="text-white text-capitalize">{{ calendarTitle }}</strong>
                      <select class="form-select form-select-sm birthdate-year-select" :value="calendarMonth.getFullYear()" @change="selectCalendarYear">
                        <option v-for="year in availableYears" :key="year" :value="year">{{ year }}</option>
                      </select>
                    </div>
                    <button class="btn btn-outline-light btn-sm" type="button" @click="moveCalendarMonth(1)">›</button>
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
                    <span v-for="(cell, index) in calendarWeeks" :key="`${cell.iso || 'empty'}-${index}`" :class="['calendar-cell', { 'is-empty': cell.isEmpty }]">
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

            <div class="col-12 col-lg-6">
              <label class="form-label" for="editEducationInstitution">Izglītības iestāde</label>
              <input id="editEducationInstitution" v-model="editForm.educationInstitution" type="text" class="form-control form-control-lg auth-input" :class="{ 'is-invalid': !!editErrors.educationInstitution }" />
              <div v-if="editErrors.educationInstitution" class="invalid-feedback d-block">{{ editErrors.educationInstitution }}</div>
            </div>

            <div v-if="canRequestPedagog" class="col-12">
              <button class="btn btn-outline-light" type="button" @click="openRoleRequestModal">
                Pieprasit pedagoga lomu
              </button>
            </div>

            <div class="col-12 d-flex flex-wrap gap-2 justify-content-end">
              <button class="btn btn-outline-light" type="button" @click="closeEditModal">Atcelt</button>
              <button class="btn btn-primary" type="submit" :disabled="isSavingProfile">
                {{ isSavingProfile ? 'Saglabā...' : 'Saglabāt' }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>

    <div v-if="isRoleRequestModalOpen" class="app-modal-backdrop" @click.self="closeRoleRequestModal">
      <div class="app-modal app-modal--sm card border-primary-subtle">
        <div class="card-body p-3 p-lg-4">
          <h2 class="section-heading mb-3">Pieprasījuma iemesls</h2>
          <p class="logout-text logout-text--navbar mb-3">
            Admins apstiprinās pieprasījumu, ja iemesls būs pamatots.
          </p>

          <label class="form-label" for="profileRoleRequestReason">Iemesls</label>
          <textarea
            id="profileRoleRequestReason"
            v-model="roleRequestReason"
            class="form-control auth-input"
            maxlength="1000"
            rows="5"
            placeholder="Apraksti, kapec tev vajadziga pedagoga loma..."
          ></textarea>
          <div v-if="roleRequestError" class="invalid-feedback d-block">{{ roleRequestError }}</div>

          <div class="d-flex justify-content-end gap-2 mt-4">
            <button class="btn btn-outline-light" type="button" @click="closeRoleRequestModal">Atcelt</button>
            <button class="btn btn-primary" type="button" :disabled="isSendingRoleRequest" @click="submitRoleRequest">
              {{ isSendingRoleRequest ? 'Sutu...' : 'Nosutit' }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <div v-if="isLogoutModalOpen" class="app-modal-backdrop" @click.self="closeLogoutModal">
      <div class="app-modal app-modal--sm card border-primary-subtle">
        <div class="card-body p-3 p-lg-4">
            <h2 class="section-heading logout-modal-title mb-3">Iziet no konta?</h2>
            <p class="logout-modal-text mb-4">
                Vai tiešām vēlies iziet no sava konta?
            </p>

            <div class="d-flex justify-content-end gap-2">
                <button class="btn btn-outline-light" type="button" @click="closeLogoutModal">Atcelt</button>
                <button class="btn btn-primary" type="button" :disabled="isLoggingOut" @click="confirmLogout">
                {{ isLoggingOut ? 'Notiek iziešana...' : 'Jā, iziet' }}
                </button>
            </div>
        </div>
      </div>
    </div>
  </section>
</template>
