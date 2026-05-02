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
import { createRoleRequest, getMyRoleRequests, type RoleRequest } from '../services/roleRequests'
import algoritmiLogo from '../assets/algoritmi.png'

function getLanguageImageUrl(language: { languageId: string; title: string }): string {
  if (language.title.trim().toLowerCase() === 'algoritmi') return algoritmiLogo
  return `/theory/${language.languageId.toLowerCase()}.png`
}

function getExerciseLanguageImageUrl(languageCode?: string | null): string {
  const code = (languageCode ?? '').trim().toLowerCase()
  if (!code) return '/theory/python.png'
  return `/theory/${code}.png`
}

const profileStatsVisibilityKey = 'learn-to-code:profile-stats-visible'

const router = useRouter()
const user = computed(() => authState.user)
const displayName = computed(() => user.value?.username?.trim() || user.value?.fullName?.trim() || 'Mans profils')

const isEditModalOpen = ref(false)
const isLogoutModalOpen = ref(false)
const isRoleRequestModalOpen = ref(false)
const isSavingProfile = ref(false)
const isLoggingOut = ref(false)
const isSendingRoleRequest = ref(false)
const isLoadingProfileStats = ref(false)
const showProfileStats = ref(localStorage.getItem(profileStatsVisibilityKey) !== 'false')

const profileError = ref('')
const profileStats = ref<ProfileStats | null>(null)
const profileStatsError = ref('')
const roleRequestError = ref('')
const roleRequestReason = ref('')
const roleRequests = ref<RoleRequest[]>([])

const editForm = reactive({
  username: '',
  firstName: '',
  lastName: '',
  birthDate: '',
  representation: '',
  bio: '',
  currentPassword: '',
  newPassword: '',
  confirmPassword: '',
})

const editErrors = reactive({
  username: '',
  firstName: '',
  lastName: '',
  birthDate: '',
  representation: '',
  bio: '',
  currentPassword: '',
  newPassword: '',
  confirmPassword: '',
})

const latestPedagogRequest = computed(() =>
  roleRequests.value.find((request) => request.requestedRole === 'Pedagogs') ?? null,
)
const pendingPedagogRequest = computed(() =>
  roleRequests.value.find((request) => request.requestedRole === 'Pedagogs' && request.status === 'Pending') ?? null,
)
const canRequestPedagog = computed(() => user.value?.role === 'Audzeknis' && !pendingPedagogRequest.value)

const sortedTheoryLanguages = computed(() => {
  const list = profileStats.value?.theoryLanguages ?? []
  return [...list].sort((a, b) => {
    const aIsAlgo = a.title.trim().toLowerCase() === 'algoritmi'
    const bIsAlgo = b.title.trim().toLowerCase() === 'algoritmi'
    if (aIsAlgo && !bIsAlgo) return -1
    if (!aIsAlgo && bIsAlgo) return 1
    return 0
  })
})

onMounted(() => {
  void loadRoleRequests()
  if (showProfileStats.value) {
    void loadProfileStats()
  }
})

function formatBirthDateDisplay(isoDate?: string | null) {
  if (!isoDate) {
    return 'Nav norādīts'
  }

  const date = new Date(isoDate)
  if (Number.isNaN(date.getTime())) {
    return isoDate
  }

  return new Intl.DateTimeFormat('lv-LV', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(date)
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
  const representation = editForm.representation.trim()

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

  if (representation && representation.length > 120) {
    editErrors.representation = 'Pārstāvniecības nosaukums ir par garu.'
  }

  if (editForm.bio.length > 500) {
    editErrors.bio = 'Apraksts nedrīkst pārsniegt 500 rakstzīmes.'
  }

  // Paroles maiņas validācija (tikai ja ir aizpildīts kāds no laukiem)
  const wantsPasswordChange =
    editForm.currentPassword || editForm.newPassword || editForm.confirmPassword
  if (wantsPasswordChange) {
    if (!editForm.currentPassword) {
      editErrors.currentPassword = 'Norādi pašreizējo paroli.'
    }
    if (!editForm.newPassword) {
      editErrors.newPassword = 'Ievadi jauno paroli.'
    } else if (editForm.newPassword.length < 6) {
      editErrors.newPassword = 'Jaunajai parolei jābūt vismaz 6 rakstzīmes garai.'
    }
    if (editForm.newPassword !== editForm.confirmPassword) {
      editErrors.confirmPassword = 'Paroles nesakrīt.'
    }
  }

  return Object.values(editErrors).every((value) => !value)
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

function openEditModal() {
  const currentUser = user.value
  if (!currentUser) return

  profileError.value = ''
  clearEditErrors()
  editForm.username = currentUser.username || ''
  editForm.firstName = currentUser.firstName || ''
  editForm.lastName = currentUser.lastName || ''
  editForm.birthDate = currentUser.birthDate || ''
  editForm.representation = currentUser.representation || ''
  editForm.bio = currentUser.bio || ''
  editForm.currentPassword = ''
  editForm.newPassword = ''
  editForm.confirmPassword = ''
  isEditModalOpen.value = true
}

function closeEditModal() {
  isEditModalOpen.value = false
}

async function saveProfile() {
  profileError.value = ''

  if (!validateEditForm()) {
    profileError.value = 'Aizpildi iezīmētos laukus.'
    return
  }

  isSavingProfile.value = true

  try {
    const wantsPasswordChange = !!(editForm.currentPassword && editForm.newPassword)
    await updateProfile({
      username: editForm.username.trim(),
      firstName: editForm.firstName.trim(),
      lastName: editForm.lastName.trim(),
      birthDate: editForm.birthDate || null,
      representation: editForm.representation.trim() || null,
      bio: editForm.bio.trim() || null,
      currentPassword: wantsPasswordChange ? editForm.currentPassword : null,
      newPassword: wantsPasswordChange ? editForm.newPassword : null,
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
    roleRequestError.value = 'Uzraksti vismaz 10 rakstzīmes.'
    return
  }

  isSendingRoleRequest.value = true
  roleRequestError.value = ''

  try {
    const request = await createRoleRequest('Pedagogs', reason)
    roleRequests.value = [request, ...roleRequests.value]
    closeRoleRequestModal()
  } catch (error) {
    roleRequestError.value = error instanceof Error ? error.message : 'Neizdevās nosūtīt pieprasījumu.'
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

async function confirmLogout() {
  isLoggingOut.value = true
  try {
    await logout()
    await router.replace('/')
  } finally {
    isLoggingOut.value = false
    closeLogoutModal()
  }
}
</script>

<template>
  <section class="content-panel card border-primary-subtle">
    <div class="card-body p-3 p-lg-4">
      <div v-if="user" class="profile-hero mb-4">
        <div class="profile-hero__identity">
          <span class="profile-avatar">{{ displayName.slice(0, 1).toUpperCase() }}</span>
          <div class="profile-hero__details">
            <p class="section-kicker mb-2">Mans profils</p>
            <h1 class="section-heading mb-1">{{ displayName }}</h1>
            <p class="profile-public-lead mb-0">{{ user.fullName }} · {{ user.role }}</p>
          </div>
        </div>

        <div class="profile-hero__bio">
          <span class="profile-stat__label">Par sevi</span>
          <p v-if="user.bio" class="profile-hero__bio-text">{{ user.bio }}</p>
          <p v-else class="profile-hero__bio-placeholder">Vēl nav aprakstīts</p>
        </div>

        <div class="profile-hero__score">
          <span>Reitings</span>
          <strong>{{ user.rating }}</strong>
        </div>
      </div>

      <div v-if="user" class="profile-overview-grid mb-4">
        <div class="profile-stat">
          <span class="profile-stat__label">Vārds</span>
          <strong>{{ user.firstName || 'Nav norādīts' }}</strong>
        </div>
        <div class="profile-stat">
          <span class="profile-stat__label">Uzvārds</span>
          <strong>{{ user.lastName || 'Nav norādīts' }}</strong>
        </div>
        <div class="profile-stat">
          <span class="profile-stat__label">Dzimšanas datums</span>
          <strong>{{ formatBirthDateDisplay(user.birthDate) }}</strong>
        </div>
        <div class="profile-stat">
          <span class="profile-stat__label">E-pasts</span>
          <strong>{{ user.email }}</strong>
        </div>
        <div class="profile-stat profile-stat--wide">
          <span class="profile-stat__label">Pārstāvniecība</span>
          <strong>{{ user.representation || 'Nav norādīta' }}</strong>
        </div>
        <div class="profile-stat-actions">
          <button class="btn btn-outline-light btn-sm" type="button" @click="toggleProfileStats">
            {{ showProfileStats ? 'Paslēpt statistiku' : 'Rādīt statistiku' }}
          </button>
        </div>
      </div>

      <hr v-if="user && showProfileStats" class="profile-section-divider" />

      <div v-if="user" class="profile-statistics mb-4">
        <div v-if="showProfileStats" class="profile-progress-grid profile-progress-grid--wide-first">
          <section class="profile-progress-panel profile-progress-panel--theory">
            <div class="profile-progress-panel__header">
              <h2>Teorija</h2>
            </div>

            <div v-if="isLoadingProfileStats" class="profile-progress-empty">Ielādē statistiku...</div>
            <div v-else-if="profileStatsError" class="profile-progress-empty">{{ profileStatsError }}</div>
            <div v-else-if="sortedTheoryLanguages.length" class="profile-progress-list">
              <div
                v-for="language in sortedTheoryLanguages"
                :key="language.languageId"
                class="profile-progress-item"
              >
                <div class="profile-progress-item__top">
                  <img
                    class="profile-progress-item__icon"
                    :src="getLanguageImageUrl(language)"
                    :alt="language.title"
                  />
                  <div class="profile-progress-item__title">
                    <strong>{{ language.title }}</strong>
                  </div>
                  <span class="theory-card__progress">{{ language.progressPercent }}% apgūts</span>
                </div>
                <div class="profile-progress-track" aria-hidden="true">
                  <span class="profile-progress-bar" :style="{ width: `${language.progressPercent}%` }"></span>
                </div>
              </div>
            </div>
            <div v-else class="profile-progress-list">
              <div class="profile-progress-item">
                <div class="profile-progress-item__top">
                  <img
                    class="profile-progress-item__icon"
                    src="/theory/python.png"
                    alt="Python"
                  />
                  <div class="profile-progress-item__title">
                    <strong>Python</strong>
                  </div>
                  <span class="theory-card__progress">0% apgūts</span>
                </div>
                <div class="profile-progress-track" aria-hidden="true">
                  <span class="profile-progress-bar" style="width: 0%"></span>
                </div>
              </div>
            </div>
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

              <div class="profile-task-stats profile-task-stats--grid">
                <div class="profile-task-stat">
                  <span class="profile-task-stat__label">Izpildīti</span>
                  <strong class="profile-task-stat__value">{{ profileStats?.exerciseSolved ?? 0 }}</strong>
                </div>
                <div class="profile-task-stat">
                  <span class="profile-task-stat__label">Mēģināti</span>
                  <strong class="profile-task-stat__value">{{ profileStats?.exerciseAttempted ?? 0 }}</strong>
                </div>
                <div class="profile-task-stat">
                  <span class="profile-task-stat__label">Iesniegumi</span>
                  <strong class="profile-task-stat__value">{{ profileStats?.exerciseSubmissionCount ?? 0 }}</strong>
                </div>
                <div class="profile-task-stat profile-task-stat--accent">
                  <span class="profile-task-stat__label">Pareizi</span>
                  <strong class="profile-task-stat__value">{{ profileStats?.exerciseSuccessPercent ?? 0 }}%</strong>
                </div>
              </div>

              <div v-if="profileStats?.mostUsedExerciseLanguage" class="profile-favourite-language">
                <img
                  class="profile-favourite-language__icon"
                  :src="getExerciseLanguageImageUrl(profileStats.mostUsedExerciseLanguage)"
                  :alt="languageLabel(profileStats.mostUsedExerciseLanguage)"
                />
                <div class="profile-favourite-language__text">
                  <span class="profile-favourite-language__label">Visbiežāk lietotā valoda</span>
                  <strong>{{ languageLabel(profileStats.mostUsedExerciseLanguage) }}</strong>
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
          Pieprasīt pedagoga lomu
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

            <div class="col-12 col-lg-6">
              <label class="form-label" for="editBirthDate">Dzimšanas datums</label>
              <input id="editBirthDate" v-model="editForm.birthDate" type="date" class="form-control form-control-lg auth-input" :class="{ 'is-invalid': !!editErrors.birthDate }" />
              <div v-if="editErrors.birthDate" class="invalid-feedback d-block">{{ editErrors.birthDate }}</div>
            </div>

            <div class="col-12 col-lg-6">
              <label class="form-label" for="editRepresentation">Pārstāvniecība <span class="text-secondary">(opcionāli)</span></label>
              <input id="editRepresentation" v-model="editForm.representation" type="text" class="form-control form-control-lg auth-input" :class="{ 'is-invalid': !!editErrors.representation }" />
              <div v-if="editErrors.representation" class="invalid-feedback d-block">{{ editErrors.representation }}</div>
            </div>

            <div class="col-12">
              <label class="form-label" for="editBio">Par sevi <span class="text-secondary">(opcionāli, līdz 500 rakstzīmēm)</span></label>
              <textarea
                id="editBio"
                v-model="editForm.bio"
                class="form-control auth-input"
                :class="{ 'is-invalid': !!editErrors.bio }"
                rows="3"
                maxlength="500"
                placeholder="Pastāsti par sevi..."
              ></textarea>
              <div v-if="editErrors.bio" class="invalid-feedback d-block">{{ editErrors.bio }}</div>
            </div>

            <div class="col-12">
              <hr class="profile-edit-divider" />
              <p class="text-secondary small mb-2">Mainīt paroli (atstāj tukšu, ja nemaini)</p>
            </div>

            <div class="col-12 col-lg-6">
              <label class="form-label" for="editCurrentPassword">Pašreizējā parole</label>
              <input id="editCurrentPassword" v-model="editForm.currentPassword" type="password" class="form-control form-control-lg auth-input" :class="{ 'is-invalid': !!editErrors.currentPassword }" autocomplete="current-password" />
              <div v-if="editErrors.currentPassword" class="invalid-feedback d-block">{{ editErrors.currentPassword }}</div>
            </div>

            <div class="col-12 col-lg-6">
              <label class="form-label" for="editNewPassword">Jaunā parole</label>
              <input id="editNewPassword" v-model="editForm.newPassword" type="password" class="form-control form-control-lg auth-input" :class="{ 'is-invalid': !!editErrors.newPassword }" autocomplete="new-password" />
              <div v-if="editErrors.newPassword" class="invalid-feedback d-block">{{ editErrors.newPassword }}</div>
            </div>

            <div class="col-12 col-lg-6">
              <label class="form-label" for="editConfirmPassword">Apstipriniet jauno paroli</label>
              <input id="editConfirmPassword" v-model="editForm.confirmPassword" type="password" class="form-control form-control-lg auth-input" :class="{ 'is-invalid': !!editErrors.confirmPassword }" autocomplete="new-password" />
              <div v-if="editErrors.confirmPassword" class="invalid-feedback d-block">{{ editErrors.confirmPassword }}</div>
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
            placeholder="Apraksti, kāpēc tev vajadzīga pedagoga loma..."
          ></textarea>
          <div v-if="roleRequestError" class="invalid-feedback d-block">{{ roleRequestError }}</div>

          <div class="d-flex justify-content-end gap-2 mt-4">
            <button class="btn btn-outline-light" type="button" @click="closeRoleRequestModal">Atcelt</button>
            <button class="btn btn-primary" type="button" :disabled="isSendingRoleRequest" @click="submitRoleRequest">
              {{ isSendingRoleRequest ? 'Sūta...' : 'Nosūtīt' }}
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
