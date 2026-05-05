<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import {
  approveJoinRequest,
  createRepresentation,
  getMyRepresentations,
  getRepresentationJoinRequests,
  getRepresentationMembers,
  getRepresentations,
  joinRepresentation,
  kickMember,
  leaveRepresentation,
  localizedRoleLabel,
  rejectJoinRequest,
  updateMemberRole,
  updateRepresentation,
  type Representation,
  type RepresentationJoinRequest,
  type RepresentationMember,
} from '../services/representations'
import { authState, getCurrentProfile } from '../services/auth'

type SortDirection = 'asc' | 'desc'

const allRepresentations = ref<Representation[]>([])
const myRepresentations = ref<Representation[]>([])
const members = ref<RepresentationMember[]>([])
const selectedRepresentationId = ref<number | null>(null)
const isLoading = ref(false)
const isLoadingMembers = ref(false)
const isSaving = ref(false)
const isJoiningId = ref<number | null>(null)
const isLeaveRepresentationModalOpen = ref(false)
const isLeavingRepresentation = ref(false)
const pageError = ref('')
const formError = ref('')
const memberSearch = ref('')
const representationSearch = ref('')
const memberSortDirection = ref<SortDirection>('desc')

const isEditing = ref(false)
const isSavingEdit = ref(false)
const editError = ref('')
const editForm = reactive({
  name: '',
  description: '',
  isPublic: true,
})

const createForm = reactive({
  name: '',
  description: '',
  isPublic: true,
})

const joinRequests = ref<RepresentationJoinRequest[]>([])
const isLoadingRequests = ref(false)
const requestActionId = ref<number | null>(null)
const memberActionId = ref<number | null>(null)
const pendingNotice = ref('')
const showMemberActions = ref(true)

const selectedRepresentation = computed(() =>
  myRepresentations.value.find((representation) => representation.id === selectedRepresentationId.value)
    ?? myRepresentations.value[0]
    ?? null,
)

const availableRepresentations = computed(() =>
  allRepresentations.value
    .filter((representation) => !representation.isMember)
    .sort((first, second) => first.name.localeCompare(second.name, 'lv')),
)

const filteredAvailableRepresentations = computed(() => {
  const query = normalizeSearch(representationSearch.value)
  if (!query) return availableRepresentations.value

  return availableRepresentations.value.filter((representation) =>
    normalizeSearch(representation.name).includes(query) ||
    normalizeSearch(representation.description).includes(query),
  )
})

const myRepresentationRank = computed(() => {
  const mine = selectedRepresentation.value
  if (!mine) return null
  if (isRankingExcludedRepresentation(mine)) return null

  const ranked = allRepresentations.value
    .filter((representation) => !isRankingExcludedRepresentation(representation))
    .sort((first, second) => {
      if (second.averageRating !== first.averageRating) {
        return second.averageRating - first.averageRating
      }
      return second.memberCount - first.memberCount
    })

  const index = ranked.findIndex((representation) => representation.id === mine.id)
  if (index < 0) return null
  return { rank: index + 1, total: ranked.length }
})

function normalizeSearch(value?: string | null) {
  return value?.trim().toLocaleLowerCase('lv-LV') ?? ''
}

function isRankingExcludedRepresentation(representation: Pick<Representation, 'name'>) {
  return normalizeSearch(representation.name) === 'learntocode'
}

const topMembers = computed(() => {
  const query = normalizeSearch(memberSearch.value)

  return rankedMembers.value
    .filter((member) => {
      if (!query) return true
      return (
        normalizeSearch(member.username).includes(query) ||
        normalizeSearch(member.fullName).includes(query) ||
        normalizeSearch(member.role).includes(query)
      )
    })
    .sort((first, second) => {
      const ratingDifference =
        memberSortDirection.value === 'asc'
          ? first.rating - second.rating
          : second.rating - first.rating
      return ratingDifference
    })
})

const rankedMembers = computed(() =>
  [...members.value].sort((first, second) => second.rating - first.rating),
)

const memberRankByUserId = computed(
  () => new Map(rankedMembers.value.map((member, index) => [member.userId, index + 1])),
)

function setMemberSortDirection(direction: SortDirection) {
  memberSortDirection.value = direction
}

onMounted(() => {
  void loadRepresentations()
})

async function loadRepresentations() {
  isLoading.value = true
  pageError.value = ''

  try {
    const [all, mine] = await Promise.all([getRepresentations(), getMyRepresentations()])
    allRepresentations.value = all
    myRepresentations.value = mine

    if (!selectedRepresentationId.value && mine.length) {
      selectedRepresentationId.value = mine[0].id
    }

    await loadMembers()
    await loadJoinRequests()
  } catch (error) {
    pageError.value = error instanceof Error ? error.message : 'Neizdevās ielādēt pārstāvniecības.'
  } finally {
    isLoading.value = false
  }
}

async function loadMembers() {
  const selected = selectedRepresentation.value
  members.value = []

  if (!selected) {
    return
  }

  isLoadingMembers.value = true
  try {
    members.value = await getRepresentationMembers(selected.id)
  } catch {
    members.value = []
  } finally {
    isLoadingMembers.value = false
  }
}

async function submitCreate() {
  const name = createForm.name.trim()
  const description = createForm.description.trim()
  formError.value = ''

  if (name.length < 3) {
    formError.value = 'Nosaukumam jābūt vismaz 3 rakstzīmes garam.'
    return
  }

  isSaving.value = true
  try {
    const created = await createRepresentation({
      name,
      description: description || null,
      isPublic: createForm.isPublic,
    })
    createForm.name = ''
    createForm.description = ''
    createForm.isPublic = true
    selectedRepresentationId.value = created.id
    await loadRepresentations()
    await refreshProfileState()
  } catch (error) {
    formError.value = error instanceof Error ? error.message : 'Neizdevās izveidot pārstāvniecību.'
  } finally {
    isSaving.value = false
  }
}

async function join(id: number) {
  isJoiningId.value = id
  pageError.value = ''
  pendingNotice.value = ''

  try {
    const result = await joinRepresentation(id)
    if (result.kind === 'pending') {
      pendingNotice.value = result.message
      await loadRepresentations()
    } else {
      selectedRepresentationId.value = result.representation.id
      await loadRepresentations()
      await refreshProfileState()
    }
  } catch (error) {
    pageError.value = error instanceof Error ? error.message : 'Neizdevās pievienoties pārstāvniecībai.'
  } finally {
    isJoiningId.value = null
  }
}

function openEdit() {
  const selected = selectedRepresentation.value
  if (!selected) return
  editForm.name = selected.name
  editForm.description = selected.description ?? ''
  editForm.isPublic = selected.isPublic
  editError.value = ''
  isEditing.value = true
}

function cancelEdit() {
  isEditing.value = false
  editError.value = ''
}

async function submitEdit() {
  const selected = selectedRepresentation.value
  if (!selected) return

  const name = editForm.name.trim()
  const description = editForm.description.trim()
  editError.value = ''

  if (name.length < 3) {
    editError.value = 'Nosaukumam jābūt vismaz 3 rakstzīmes garam.'
    return
  }

  isSavingEdit.value = true
  try {
    await updateRepresentation(selected.id, {
      name,
      description: description || null,
      isPublic: editForm.isPublic,
    })
    isEditing.value = false
    await loadRepresentations()
    await refreshProfileState()
  } catch (error) {
    editError.value = error instanceof Error ? error.message : 'Neizdevās saglabāt izmaiņas.'
  } finally {
    isSavingEdit.value = false
  }
}

async function loadJoinRequests() {
  const selected = selectedRepresentation.value
  if (!selected || (!selected.isOwner && !selected.isModerator)) {
    joinRequests.value = []
    return
  }

  isLoadingRequests.value = true
  try {
    joinRequests.value = await getRepresentationJoinRequests(selected.id)
  } catch {
    joinRequests.value = []
  } finally {
    isLoadingRequests.value = false
  }
}

async function approveRequest(requestId: number) {
  const selected = selectedRepresentation.value
  if (!selected) return
  requestActionId.value = requestId
  pageError.value = ''
  try {
    await approveJoinRequest(selected.id, requestId)
    await loadJoinRequests()
    await loadRepresentations()
    await loadMembers()
  } catch (error) {
    pageError.value = error instanceof Error ? error.message : 'Neizdevās apstiprināt pieprasījumu.'
  } finally {
    requestActionId.value = null
  }
}

async function rejectRequest(requestId: number) {
  const selected = selectedRepresentation.value
  if (!selected) return
  requestActionId.value = requestId
  pageError.value = ''
  try {
    await rejectJoinRequest(selected.id, requestId)
    await loadJoinRequests()
    await loadRepresentations()
  } catch (error) {
    pageError.value = error instanceof Error ? error.message : 'Neizdevās noraidīt pieprasījumu.'
  } finally {
    requestActionId.value = null
  }
}

async function kickFromRepresentation(member: RepresentationMember) {
  const selected = selectedRepresentation.value
  if (!selected) return
  if (!confirm(`Izmest ${getMemberName(member)} no pārstāvniecības?`)) return

  memberActionId.value = member.userId
  pageError.value = ''
  try {
    await kickMember(selected.id, member.userId)
    await loadMembers()
    await loadRepresentations()
  } catch (error) {
    pageError.value = error instanceof Error ? error.message : 'Neizdevās izmest dalībnieku.'
  } finally {
    memberActionId.value = null
  }
}

async function promoteToModerator(member: RepresentationMember) {
  await changeMemberRole(member, 'Moderators')
}

async function demoteToMember(member: RepresentationMember) {
  await changeMemberRole(member, 'Member')
}

async function changeMemberRole(member: RepresentationMember, role: string) {
  const selected = selectedRepresentation.value
  if (!selected) return

  memberActionId.value = member.userId
  pageError.value = ''
  try {
    await updateMemberRole(selected.id, member.userId, role)
    await loadMembers()
    await loadRepresentations()
  } catch (error) {
    pageError.value = error instanceof Error ? error.message : 'Neizdevās mainīt lomu.'
  } finally {
    memberActionId.value = null
  }
}

function isRoleOwner(role: string) {
  return role.toLowerCase() === 'owner'
}

function isRoleModerator(role: string) {
  const r = role.toLowerCase()
  return r === 'moderators' || r === 'moderator'
}

function canKick(member: RepresentationMember) {
  const selected = selectedRepresentation.value
  if (!selected) return false
  if (isRoleOwner(member.role)) return false
  if (selected.isOwner) return true
  if (selected.isModerator) {
    // Moderators var izmest tikai parastos dalībniekus
    return !isRoleModerator(member.role)
  }
  return false
}

function openLeaveRepresentationModal() {
  if (!selectedRepresentation.value) return
  pageError.value = ''
  isLeaveRepresentationModalOpen.value = true
}

function closeLeaveRepresentationModal() {
  if (isLeavingRepresentation.value) return
  isLeaveRepresentationModalOpen.value = false
}

async function confirmLeaveRepresentation() {
  const selected = selectedRepresentation.value
  if (!selected) return

  isLeavingRepresentation.value = true
  pageError.value = ''
  try {
    await leaveRepresentation(selected.id)
    selectedRepresentationId.value = null
    isLeaveRepresentationModalOpen.value = false
    await loadRepresentations()
    await refreshProfileState()
  } catch (error) {
    pageError.value = error instanceof Error ? error.message : 'Neizdevās izstāties no pārstāvniecības.'
  } finally {
    isLeavingRepresentation.value = false
  }
}

async function refreshProfileState() {
  try {
    authState.user = await getCurrentProfile()
  } catch {
    // The page data still reflects membership changes even if session refresh fails.
  }
}

function getMemberName(member: RepresentationMember) {
  return member.username?.trim() || member.fullName?.trim() || `Lietotājs #${member.userId}`
}

function formatDate(value: string) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return new Intl.DateTimeFormat('lv-LV', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(date)
}

function memberRankClass(index: number) {
  if (index === 0) return 'representation-member__rank--gold'
  if (index === 1) return 'representation-member__rank--silver'
  if (index === 2) return 'representation-member__rank--bronze'
  return ''
}

function memberRank(member: RepresentationMember) {
  return memberRankByUserId.value.get(member.userId) ?? 0
}

function rankChipClass(index: number) {
  if (index === 0) return 'representations-stat-chip--gold'
  if (index === 1) return 'representations-stat-chip--silver'
  if (index === 2) return 'representations-stat-chip--bronze'
  return ''
}
</script>

<template>
  <section class="representations-view">
    <header class="content-panel card border-primary-subtle">
      <div class="card-body p-3 p-lg-4">
        <div class="theory-header exercises-header">
          <span class="page-title-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" width="34" height="34" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
              <circle cx="9" cy="7" r="4" />
              <path d="M23 21v-2a4 4 0 0 0-3-3.87" />
              <path d="M16 3.13a4 4 0 0 1 0 7.75" />
            </svg>
          </span>
          <div class="theory-heading-copy">
            <h1 class="section-heading mb-1">Pārstāvniecības</h1>
            <p class="representations-lead mb-0">
              Izveido komandu, pievienojies esošai pārstāvniecībai un seko kopējam progresam vienā vietā.
            </p>
          </div>
          <div class="exercises-header__badges representations-summary">
            <div
              v-if="myRepresentationRank"
              class="representations-stat-chip"
              :class="rankChipClass(myRepresentationRank.rank - 1)"
            >
              <span>Vieta reitingā</span>
              <strong>{{ myRepresentationRank.rank }}<small>/{{ myRepresentationRank.total }}</small></strong>
            </div>
            <div v-else class="representations-stat-chip">
              <span>Pieejamas</span>
              <strong>{{ availableRepresentations.length }}</strong>
            </div>
          </div>
        </div>
      </div>
    </header>

    <div v-if="isLoading" class="content-panel card border-primary-subtle">
      <div class="card-body p-3 p-lg-4">Ielādē pārstāvniecības...</div>
    </div>

    <div v-else-if="pageError" class="alert alert-danger">
      {{ pageError }}
    </div>

    <div v-else class="representations-layout">
      <section
        v-if="!myRepresentations.length"
        class="content-panel card border-primary-subtle"
      >
        <div class="card-body p-3 p-lg-4">
          <div class="representations-section-heading mb-3">
            <span class="representations-section-icon" aria-hidden="true">
              <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <line x1="12" y1="5" x2="12" y2="19" />
                <line x1="5" y1="12" x2="19" y2="12" />
              </svg>
            </span>
            <h2 class="section-heading section-heading--caps mb-0">Izveidot pārstāvniecību</h2>
          </div>

          <form class="representation-form" @submit.prevent="submitCreate">
            <label class="representation-field">
              <span class="representation-field__label">Nosaukums</span>
              <input
                v-model="createForm.name"
                class="form-control auth-input"
                type="text"
                maxlength="160"
              />
            </label>

            <label class="representation-field">
              <span class="representation-field__label">Apraksts</span>
              <textarea
                v-model="createForm.description"
                class="form-control auth-input"
                maxlength="800"
                rows="4"
              ></textarea>
            </label>

            <div class="representation-field">
              <span class="representation-field__label">Pieejamība</span>
              <div class="ex-filter-group">
                <button
                  type="button"
                  class="ex-filter-btn"
                  :class="{ active: createForm.isPublic }"
                  @click="createForm.isPublic = true"
                >
                  Publiska
                </button>
                <button
                  type="button"
                  class="ex-filter-btn"
                  :class="{ active: !createForm.isPublic }"
                  @click="createForm.isPublic = false"
                >
                  Privāta
                </button>
              </div>
              <small class="representation-field__hint">
                {{ createForm.isPublic ? 'Jebkurš var iestāties uzreiz.' : 'Jaunajiem dalībniekiem jānosūta pieprasījums.' }}
              </small>
            </div>

            <div v-if="formError" class="invalid-feedback d-block">{{ formError }}</div>

            <button class="btn btn-primary" type="submit" :disabled="isSaving">
              {{ isSaving ? 'Veido...' : 'Izveidot' }}
            </button>
          </form>
        </div>
      </section>

      <section
        v-if="!myRepresentations.length"
        class="content-panel card border-primary-subtle"
      >
        <div class="card-body p-3 p-lg-4">
          <div class="representations-section-heading mb-3">
            <span class="representations-section-icon" aria-hidden="true">
              <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M16 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
                <circle cx="8.5" cy="7" r="4" />
                <line x1="20" y1="8" x2="20" y2="14" />
                <line x1="23" y1="11" x2="17" y2="11" />
              </svg>
            </span>
            <h2 class="section-heading section-heading--caps mb-0">Pievienoties</h2>
          </div>

          <label v-if="availableRepresentations.length" class="representation-field representation-search-field">
            <span class="representation-field__label">Meklēt pārstāvniecību</span>
            <input
              v-model="representationSearch"
              class="form-control auth-input"
              type="search"
            />
          </label>

          <div v-if="!availableRepresentations.length" class="representations-empty">
            Nav citu pārstāvniecību, kurām pievienoties.
          </div>

          <div v-else-if="!filteredAvailableRepresentations.length" class="representations-empty">
            Nav pārstāvniecību, kas atbilst meklēšanai.
          </div>

          <div v-if="pendingNotice" class="alert alert-info">
            {{ pendingNotice }}
          </div>

          <div v-else-if="filteredAvailableRepresentations.length" class="representation-list representation-list--available">
            <router-link
              v-for="representation in filteredAvailableRepresentations"
              :key="representation.id"
              class="representation-list-item"
              :to="{ name: 'representation-detail', params: { name: representation.name } }"
            >
              <div>
                <strong>
                  {{ representation.name }}
                  <span v-if="!representation.isPublic" class="representation-visibility representation-visibility--private" title="Privāta">🔒</span>
                </strong>
                <span>{{ representation.memberCount }} dalībnieki</span>
              </div>
              <div class="representations-panel-actions">
                <button
                  class="btn btn-primary btn-sm"
                  type="button"
                  :disabled="isJoiningId === representation.id || representation.hasPendingJoinRequest"
                  @click.prevent.stop="join(representation.id)"
                >
                <template v-if="representation.hasPendingJoinRequest">Pieprasījums iesniegts</template>
                <template v-else-if="isJoiningId === representation.id">Pievienojas...</template>
                <template v-else-if="representation.isPublic">Pievienoties</template>
                <template v-else>Sūtīt pieprasījumu</template>
                </button>
              </div>
            </router-link>
          </div>
        </div>
      </section>

      <section
        v-if="myRepresentations.length"
        class="content-panel card border-primary-subtle representations-stats-panel"
      >
        <div class="card-body p-3 p-lg-4">
          <div class="representations-panel-header">
            <div class="representations-section-heading">
              <span class="representations-section-icon" aria-hidden="true">
                <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <path d="M3 21v-2a4 4 0 0 1 4-4h10a4 4 0 0 1 4 4v2" />
                  <circle cx="12" cy="7" r="4" />
                </svg>
              </span>
              <h2 class="section-heading section-heading--caps mb-0">Mana pārstāvniecība</h2>
            </div>
            <div class="representations-panel-actions">
              <button
                v-if="selectedRepresentation?.isOwner"
                class="btn btn-outline-light btn-sm"
                type="button"
                @click="openEdit"
              >
                Rediģēt
              </button>
              <button
                v-if="selectedRepresentation"
                class="btn btn-outline-light btn-sm"
                type="button"
                @click="openLeaveRepresentationModal"
              >
                Izstāties
              </button>
            </div>
          </div>

          <div v-if="selectedRepresentation" class="representation-detail">
              <div class="representation-detail__heading">
                <div>
                  <strong>{{ selectedRepresentation.name }}</strong>
                  <span>{{ selectedRepresentation.description || 'Bez apraksta' }}</span>
                </div>
                <small>Izveidota {{ formatDate(selectedRepresentation.createdAtUtc) }}</small>
              </div>

              <div class="representation-metrics">
                <div>
                  <span>Dalībnieki</span>
                  <strong>{{ selectedRepresentation.memberCount }}</strong>
                </div>
                <div>
                  <span>Vidējais reitings</span>
                  <strong>{{ selectedRepresentation.averageRating }}</strong>
                </div>
                <div>
                  <span>Atrisināti kopā</span>
                  <strong>{{ selectedRepresentation.exerciseSolved }}</strong>
                </div>
                <div>
                  <span>Atrisināti pēdējās 7 dienās</span>
                  <strong>{{ selectedRepresentation.exerciseSolvedLast7Days ?? 0 }}</strong>
                </div>
              </div>

              <div
                v-if="(selectedRepresentation.isOwner || selectedRepresentation.isModerator) && joinRequests.length"
                class="representations-requests-panel"
              >
                <div class="representations-panel-header representations-panel-header--sub">
                  <h3 class="section-heading section-heading--caps section-heading--sm mb-0">
                    Pievienošanās pieprasījumi
                    <span class="representations-badge-count">{{ joinRequests.length }}</span>
                  </h3>
                  <span v-if="isLoadingRequests" class="sidebar-link-description">Ielādē...</span>
                </div>

                <div class="representation-list">
                  <article
                    v-for="request in joinRequests"
                    :key="request.id"
                    class="representation-list-item"
                  >
                    <div>
                      <strong>{{ request.username || request.fullName || `Lietotājs #${request.userId}` }}</strong>
                      <span>Reitings: {{ request.userRating }}</span>
                      <small v-if="request.message" class="representations-request-message">"{{ request.message }}"</small>
                    </div>
                    <div class="representations-panel-actions">
                      <button
                        type="button"
                        class="btn btn-primary btn-sm"
                        :disabled="requestActionId === request.id"
                        @click="approveRequest(request.id)"
                      >
                        {{ requestActionId === request.id ? '...' : 'Apstiprināt' }}
                      </button>
                      <button
                        type="button"
                        class="btn btn-outline-light btn-sm"
                        :disabled="requestActionId === request.id"
                        @click="rejectRequest(request.id)"
                      >
                        Noraidīt
                      </button>
                    </div>
                  </article>
                </div>
              </div>

              <div class="rating-controls__grid rating-controls__grid--compact rating-controls__grid--members">
                <label class="rating-control">
                  <span>Meklēt dalībnieku</span>
                  <input
                    v-model="memberSearch"
                    class="form-control auth-input"
                    type="search"
                    placeholder="Vārds, lietotājvārds vai loma"
                  />
                </label>
                <div class="rating-sort" role="group" aria-label="Kārtot dalībniekus pēc reitinga">
                  <span>Kārtot</span>
                  <div class="ex-filter-group">
                    <button
                      class="ex-filter-btn"
                      :class="{ active: memberSortDirection === 'desc' }"
                      type="button"
                      @click="setMemberSortDirection('desc')"
                    >
                      Dilstoši
                    </button>
                    <button
                      class="ex-filter-btn"
                      :class="{ active: memberSortDirection === 'asc' }"
                      type="button"
                      @click="setMemberSortDirection('asc')"
                    >
                      Augoši
                    </button>
                  </div>
                </div>
                <div
                  v-if="selectedRepresentation.isOwner || selectedRepresentation.isModerator || isLoadingMembers"
                  class="representation-management-toggle"
                >
                  <span v-if="isLoadingMembers" class="sidebar-link-description">Ielādē...</span>
                  <button
                    v-if="selectedRepresentation.isOwner || selectedRepresentation.isModerator"
                    type="button"
                    class="btn btn-outline-light btn-sm"
                    @click="showMemberActions = !showMemberActions"
                  >
                    {{ showMemberActions ? 'Slēpt pārvaldību' : 'Pārvaldīt' }}
                  </button>
                </div>
              </div>

              <div v-if="!topMembers.length && !isLoadingMembers" class="representations-empty">
                {{ memberSearch ? 'Nav dalībnieku, kas atbilst meklēšanai.' : 'Dalībnieku dati vēl nav pieejami.' }}
              </div>

              <div v-else class="representation-member-list">
                <div
                  v-for="member in topMembers"
                  :key="member.userId"
                  class="representation-member representation-member--with-actions"
                >
                  <component
                    :is="member.username ? 'router-link' : 'span'"
                    class="representation-member__link"
                    :to="member.username ? { name: 'public-profile', params: { username: member.username } } : undefined"
                  >
                    <span class="representation-member__rank" :class="memberRankClass(memberRank(member) - 1)">
                      {{ memberRank(member) }}
                    </span>
                    <div class="representation-member__name">
                      <span>{{ getMemberName(member) }}</span>
                      <small>{{ localizedRoleLabel(member.role) }}</small>
                    </div>
                  </component>

                  <div
                    v-if="showMemberActions && selectedRepresentation && (selectedRepresentation.isOwner || selectedRepresentation.isModerator) && !isRoleOwner(member.role)"
                    class="representation-member__actions"
                  >
                    <button
                      v-if="selectedRepresentation.isOwner && !isRoleModerator(member.role)"
                      type="button"
                      class="btn btn-outline-light btn-sm"
                      :disabled="memberActionId === member.userId"
                      :title="`Padarīt par moderatoru`"
                      @click="promoteToModerator(member)"
                    >
                      ↑ Mod
                    </button>
                    <button
                      v-if="selectedRepresentation.isOwner && isRoleModerator(member.role)"
                      type="button"
                      class="btn btn-outline-light btn-sm"
                      :disabled="memberActionId === member.userId"
                      :title="`Pazemināt uz dalībnieku`"
                      @click="demoteToMember(member)"
                    >
                      ↓ Dal
                    </button>
                    <button
                      v-if="canKick(member)"
                      type="button"
                      class="btn btn-outline-danger btn-sm"
                      :disabled="memberActionId === member.userId"
                      title="Izmest"
                      @click="kickFromRepresentation(member)"
                    >
                      ✕
                    </button>
                  </div>

                  <strong class="representation-member__rating">{{ member.rating }}</strong>
                </div>
              </div>
            </div>
        </div>
      </section>
    </div>

    <div v-if="isLeaveRepresentationModalOpen" class="app-modal-backdrop" @click.self="closeLeaveRepresentationModal">
      <div class="app-modal app-modal--sm card border-primary-subtle delete-modal" role="dialog" aria-modal="true" aria-labelledby="leaveRepresentationTitle">
        <div class="card-body p-3 p-lg-4">
          <div class="delete-modal__icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" width="26" height="26" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M3 6h18" />
              <path d="M8 6V4h8v2" />
              <path d="M19 6l-1 14H6L5 6" />
              <path d="M10 11v5" />
              <path d="M14 11v5" />
            </svg>
          </div>
          <h2 id="leaveRepresentationTitle" class="section-heading delete-modal__title mb-3">Izstāties no pārstāvniecības?</h2>
          <p class="delete-modal__text mb-4">
            Tu zaudēsi piesaisti “{{ selectedRepresentation?.name }}” pārstāvniecībai.
          </p>
          <div class="delete-modal__actions">
            <button class="btn btn-outline-light" type="button" :disabled="isLeavingRepresentation" @click="closeLeaveRepresentationModal">Atcelt</button>
            <button class="btn btn-primary delete-modal__confirm" type="button" :disabled="isLeavingRepresentation" @click="confirmLeaveRepresentation">
              <span v-if="isLeavingRepresentation" class="logout-modal__spinner" aria-hidden="true"></span>
              {{ isLeavingRepresentation ? 'Izstājas...' : 'Izstāties' }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <Teleport to="body">
      <div v-if="isEditing" class="app-modal-backdrop" @click.self="cancelEdit">
        <div class="app-modal app-modal--sm card border-primary-subtle" role="dialog" aria-modal="true" aria-labelledby="editRepTitle">
          <div class="card-body p-3 p-lg-4">
            <h2 id="editRepTitle" class="section-heading section-heading--caps mb-3">Rediģēt pārstāvniecību</h2>

            <form class="representation-form" @submit.prevent="submitEdit">
              <label class="representation-field">
                <span class="representation-field__label">Nosaukums</span>
                <input
                  v-model="editForm.name"
                  class="form-control auth-input"
                  type="text"
                  maxlength="160"
                />
              </label>

              <label class="representation-field">
                <span class="representation-field__label">Apraksts</span>
                <textarea
                  v-model="editForm.description"
                  class="form-control auth-input"
                  maxlength="800"
                  rows="4"
                ></textarea>
              </label>

              <div class="representation-field">
                <span class="representation-field__label">Pieejamība</span>
                <div class="ex-filter-group">
                  <button
                    type="button"
                    class="ex-filter-btn"
                    :class="{ active: editForm.isPublic }"
                    @click="editForm.isPublic = true"
                  >
                    Publiska
                  </button>
                  <button
                    type="button"
                    class="ex-filter-btn"
                    :class="{ active: !editForm.isPublic }"
                    @click="editForm.isPublic = false"
                  >
                    Privāta
                  </button>
                </div>
              </div>

              <div v-if="editError" class="invalid-feedback d-block">{{ editError }}</div>

              <div class="d-flex justify-content-end gap-2 mt-2">
                <button type="button" class="btn btn-outline-light" @click="cancelEdit">Atcelt</button>
                <button type="submit" class="btn btn-primary" :disabled="isSavingEdit">
                  {{ isSavingEdit ? 'Saglabā...' : 'Saglabāt' }}
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>
    </Teleport>
  </section>
</template>
